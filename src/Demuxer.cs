using System.CommandLine;
using System.Text.Json;
using GICutscenes.FileTypes;

namespace GICutscenes
{
    internal class VersionList
    {
        public Version[] list { get; set; }
    }
    internal class Version
    {
        public string version { get; set; }
        public string[] videos { get; set; }
        public ulong key { get; set; }
        public bool? encAudio { get; set; }
    }
    internal class Demuxer
    {
        //private bool audioEnc = false;
        private static ulong EncryptionKeyInFilename(string filename)
        {
            string[] intros = { "MDAQ001_OPNew_Part1.usm", "MDAQ001_OPNew_Part2_PlayerBoy.usm", "MDAQ001_OPNew_Part2_PlayerGirl.usm" };
            if (intros.Contains(filename))
            {
                filename = "MDAQ001_OP";
            }
            filename = filename.Split('.')[0];
            ulong sum = 0;

            foreach (char c in filename) sum = c + 3 * sum;

            sum &= 0xFFFFFFFFFFFFFF;
            ulong result = 0x100000000000000;
            if (sum > 0) result = sum;
            return result;
        }

        private static (ulong, bool) EncryptionKeyInBLK(string videoFilename)
        {
            videoFilename = videoFilename.Split('.')[0];
            string jsonString = File.ReadAllText("versions.json");
            VersionList versions = JsonSerializer.Deserialize<VersionList>(jsonString);
            if (versions == null) throw new JsonException("Json content from versions.json is invalid or couldn't be parsed...");
            Version v = Array.Find(versions.list, x => x.videos.Contains(videoFilename));
            if (v == null) throw new KeyNotFoundException("Unable to find blk key for " + videoFilename);
            return (v.key, v.encAudio ?? false);
        }

        public static ulong EncryptionKey(string videoFilename)
        {
            ulong key1 = EncryptionKeyInFilename(videoFilename);
            (ulong, bool) blk = EncryptionKeyInBLK(videoFilename);
            ulong key2 = blk.Item1;
            //audioEnc = blk.Item2;

            ulong finalKey = 0x100000000000000;
            if ((key1 + key2 & 0xFFFFFFFFFFFFFF) != 0) finalKey = key1 + key2 & 0xFFFFFFFFFFFFFF;
            return finalKey;
        }

        public static (byte[], byte[]) KeySplitter(ulong key)
        {
            byte[] keyArray = new byte[8];
            BitConverter.GetBytes(key).CopyTo(keyArray, 0);
            byte[] key1 = new byte[4];
            byte[] key2 = new byte[4];
            Array.Copy(keyArray, 0, key1, 0, 4);
            Array.Copy(keyArray, 4, key2, 0, 4);
            return (key1, key2);
        }

        public static void Demux(string filenameArg, byte[] key1Arg, byte[] key2Arg, string output)
        {
            string filename = Path.GetFileName(filenameArg);
            byte[] key1, key2;
            if (key1Arg.Length == 0 && key2Arg.Length == 0)
            {
                Console.WriteLine("Finding encryption key for {0}...", filename);
                (byte[], byte[]) split = KeySplitter(EncryptionKey(filename));
                key1 = split.Item1;
                key2 = split.Item2;
            } else
            {
                key1 = key1Arg;
                key2 = key2Arg;
            }

            USM file = new(filenameArg, key1, key2);
            file.Demux(true, true, output);
            foreach (string f in Directory.EnumerateFiles(output, Path.GetFileNameWithoutExtension(filename) + "_*.hca"))
            {
                HCA audioFile = new(f, key1, key2);
                audioFile.ConvertToWAV(output);
            }
            Console.WriteLine("Extraction completed !");
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
