using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using GICutscenes.FileTypes;

namespace GICutscenes
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(VersionList), GenerationMode = JsonSourceGenerationMode.Metadata)]
    internal partial class VersionJson : JsonSerializerContext
    { }

    internal class VersionList
    {
        public Version[]? list { get; set; }
    }
    internal class Version
    {
        public string? version { get; set; }
        public string[]? videos { get; set; }
        public Version[]? videoGroups { get; set; }
        public ulong? key { get; set; }
        public bool? encAudio { get; set; }
        // 7.1+ 视频加密组携带：32 个十六进制字符 = AES-128 密钥；
        // 缺失（null）表示旧版 XOR 体系。新体系下 key 字段承载 audioKey（直接切半，不叠加文件名哈希）。
        public string? aesKey { get; set; }
    }
    internal class Demuxer
    {
        //private bool audioEnc = false;
        private static ulong EncryptionKeyInFilename(string filename)
        {
            filename = Path.GetFileNameWithoutExtension(filename);
            string[] intros = { "MDAQ001_OPNew_Part1", "MDAQ001_OPNew_Part2_PlayerBoy", "MDAQ001_OPNew_Part2_PlayerGirl" };
            if (intros.Contains(filename))
            {
                filename = "MDAQ001_OP";
            }
            ulong sum = 0;

            foreach (char c in filename) sum = c + 3 * sum;

            sum &= 0xFFFFFFFFFFFFFF;
            ulong result = 0x100000000000000;
            if (sum > 0) result = sum;
            return result;
        }

        private static (ulong, bool)? EncryptionKeyInBLK(string videoFilename)
        {
            var versionsFilePath = Path.Combine(AppContext.BaseDirectory, "versions.json");
            if (!File.Exists(versionsFilePath)) throw new FileNotFoundException("File versions.json couldn't be found in the folder of the tool.");
            videoFilename = Path.GetFileNameWithoutExtension(videoFilename);
            string jsonString = File.ReadAllText(versionsFilePath);
            VersionList? versions = JsonSerializer.Deserialize<VersionList>(jsonString, VersionJson.Default.VersionList);
            if (versions?.list == null) throw new JsonException("Json content from versions.json is invalid or couldn't be parsed...");
            Version? v = Array.Find(versions.list, x => (x.videos != null && x.videos.Contains(videoFilename)) || (x.videoGroups != null && Array.Exists(x.videoGroups, y => y.videos != null && y.videos.Contains(videoFilename))));
            if (v == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Unable to find the second key in versions.json for " + videoFilename);
                Console.ResetColor();
                return null;
            }
            ulong key = v.key ?? 0;
            if (v.videoGroups != null)
            {
                key = Array.Find(v.videoGroups, y => y.videos != null && y.videos.Contains(videoFilename))?.key ?? throw new KeyNotFoundException("Unable to find the second key in versions.json for " + videoFilename);
            }
            return (key, v.encAudio ?? false);
        }

        public static ulong? EncryptionKey(string videoFilename)
        {
            ulong key1 = EncryptionKeyInFilename(videoFilename);
            (ulong, bool)? blk = EncryptionKeyInBLK(videoFilename);
            if (blk == null) return null;
            ulong key2 = blk.Value.Item1;
            //audioEnc = blk.Value.Item2;

            ulong finalKey = 0x100000000000000;
            if ((key1 + key2 & 0xFFFFFFFFFFFFFF) != 0) finalKey = key1 + key2 & 0xFFFFFFFFFFFFFF;
            return finalKey;
        }

        public static (byte[], byte[])? KeySplitter(ulong? key)
        {
            if (key == null) return null;
            byte[] keyArray = new byte[8];
            BitConverter.GetBytes(key.Value).CopyTo(keyArray, 0);
            byte[] key1 = keyArray[..4];
            byte[] key2 = keyArray[4..];
            return (key1, key2);
        }

        /// <summary>
        /// 加载 versions.json，返回 (版本条目, 命中密钥组)；扁平版条目时命中组即条目自身。
        /// 对应 charlotte 的 find_video。未命中返回 null 并给出与历史一致的警告。
        /// </summary>
        private static (Version Entry, Version Group)? FindEntry(string videoFilename)
        {
            string versionsFilePath = Path.Combine(AppContext.BaseDirectory, "versions.json");
            if (!File.Exists(versionsFilePath))
                throw new FileNotFoundException("File versions.json couldn't be found in the folder of the tool.");

            videoFilename = Path.GetFileNameWithoutExtension(videoFilename);
            string jsonString = File.ReadAllText(versionsFilePath);
            VersionList? versions = JsonSerializer.Deserialize<VersionList>(jsonString, VersionJson.Default.VersionList);
            if (versions?.list == null)
                throw new JsonException("Json content from versions.json is invalid or couldn't be parsed...");

            foreach (Version entry in versions.list)
            {
                if (entry.videos != null && entry.videos.Contains(videoFilename))
                    return (entry, entry);   // 扁平版：版本条目即密钥组

                if (entry.videoGroups != null)
                {
                    Version? group = Array.Find(entry.videoGroups,
                        g => g.videos != null && g.videos.Contains(videoFilename));
                    if (group != null) return (entry, group);
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Unable to find the second key in versions.json for " + videoFilename);
            Console.ResetColor();
            return null;
        }

        /// <summary>
        /// 版本条目是否属于 7.1 新世代。该世代内没有 aesKey 的组（如 40251、_reunion_）
        /// 只携带音频密钥：key=audioKey 直接切半喂给 HCA，视频因无 AES 密钥不解密。
        /// </summary>
        private static bool IsNewGeneration(Version versionEntry)
        {
            // 注意：必须用完全限定名 System.Version，否则解析到本文件的 JSON 模型类 Version
            return System.Version.TryParse(versionEntry.version, out System.Version? v) && v >= new System.Version(7, 1);
        }

        public static bool Demux(string filenameArg, byte[] key1Arg, byte[] key2Arg, string output)
        {
            if (!File.Exists(filenameArg)) throw new FileNotFoundException($"File {filenameArg} doesn't exist...");
            string filename = Path.GetFileName(filenameArg);
            byte[] key1, key2;
            USM file;
            if (key1Arg.Length == 0 && key2Arg.Length == 0)
            {
                // 无手动 key：统一查 versions.json，新旧体系分流均基于命中组的 aesKey
                Console.WriteLine($"Finding encryption key for {filename}...");
                (Version Entry, Version Group)? found = FindEntry(filename);
                if (found == null) return false;
                Version entry = found.Value.Entry;
                Version group = found.Value.Group;

                if (group.aesKey != null)
                {
                    // ===== 7.1 新体系：视频 AES-128-CTR，音频 audioKey（直接切半，不叠加文件名哈希）=====
                    ulong? nonce = USM.ReadVideoNonce(filenameArg);
                    if (nonce == null)
                        throw new InvalidDataException(
                            $"{filename} matched a 7.1 AES group but no @UTF header/nonce was found (JSON/file mismatch).");

                    (byte[], byte[])? audioSplit = KeySplitter(group.key);
                    if (audioSplit == null)
                        throw new JsonException($"7.1 group '{group.version}' is missing its audio key (field 'key').");
                    key1 = audioSplit.Value.Item1;
                    key2 = audioSplit.Value.Item2;

                    file = new USM(filenameArg, key1, key2, Convert.FromHexString(group.aesKey), nonce.Value);
                }
                else if (IsNewGeneration(entry))
                {
                    // ===== 7.1 音频-only 组（40251 / _reunion_）：无 AES 密钥，视频跳过 =====
                    (byte[], byte[])? audioSplit = KeySplitter(group.key);
                    if (audioSplit == null)
                        throw new JsonException($"Audio-only group '{group.version}' is missing its audio key (field 'key').");
                    key1 = audioSplit.Value.Item1;
                    key2 = audioSplit.Value.Item2;

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Group '{group.version}' has no video key: skipping video, extracting audio only.");
                    Console.ResetColor();

                    file = new USM(filenameArg, key1, key2, videoEncrypted: false);
                }
                else
                {
                    // ===== 旧体系（common / 2.0~7.0）：文件名滚动哈希 + 组 videoKey，复刻 EncryptionKey =====
                    ulong filenameKey = EncryptionKeyInFilename(filename);
                    ulong combined = (filenameKey + (group.key ?? 0)) & 0xFFFFFFFFFFFFFF;
                    ulong finalKey = combined != 0 ? combined : 0x100000000000000;

                    (byte[], byte[])? split = KeySplitter(finalKey);
                    if (split == null) return false;
                    key1 = split.Value.Item1;
                    key2 = split.Value.Item2;
                    file = new USM(filenameArg, key1, key2);
                }
            }
            else
            {
                // 手动 -a/-b：维持旧 XOR 体系语义（4B+4B 参数无法承载 16B AES 密钥，故不支持 7.1）
                key1 = key1Arg;
                key2 = key2Arg;
                file = new USM(filenameArg, key1, key2);
            }

            Dictionary<string, List<string>> filePaths = file.Demux(true, true, output);  // TODO: Return file list for easier parsing

            if (!filePaths.TryGetValue("hca", out List<string> hcaPaths)) throw new Exception("No HCA files could be demuxed...");

            Task[] decodingTasks = new Task[hcaPaths.Count];
            for (int i = 0; i < decodingTasks.Length; i++)
            {
                int j = i;
                decodingTasks[i] = Task.Run(() =>
                {
                    Hca audioFile = new(hcaPaths[j], key1, key2);
                    audioFile.ConvertToWAV(output);
                });
            }
            Task.WaitAll(decodingTasks);
            Console.WriteLine("Extraction completed !");
            return true;
        }
    }
}

// Checksum unit testing
//string bytestring = "C8 C3 C1 00 02 00 00 60 E6 ED F4 00 02 00 BB 80 00 00 2C D5 00 80 03 83 E3 EF ED F0 02 AA 01 0F 01 00 80 80 00 00 00 00 E3 E9 F0 E8 00 38 F0 E1 E4 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00";
//string[] stringarray = bytestring.Split(" ");
//byte[] vs = new byte[stringarray.Length];
//for (int i = 0; i < stringarray.Length; i++)
//{
//    vs[i] = Convert.ToByte(stringarray[i], 16);
//}
//Console.WriteLine(Utils.Bswap(HCA.CheckSum(vs, vs.Length)));
// Should be equal to 13856
