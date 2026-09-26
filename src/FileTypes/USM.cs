using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using GICutscenes.Utils;

namespace GICutscenes.FileTypes
{
    internal struct Info
    {
        public uint signature;
        public uint dataSize;
        public byte dataOffset;
        public ushort paddingSize;
        public byte chno;
        public byte dataType;
        public uint frameTime;
        public uint frameRate;
    }
    internal class USM
    {
        private readonly string _filename;
        private readonly string _path;
        private readonly byte[] _key1;
        private readonly byte[] _key2;
        private byte[] _videoMask1;
        private byte[] _videoMask2;
        private byte[] _audioMask;

        // 7.1+ AES-CTR 参数；旧体系保持 null
        private readonly byte[]? _aesKey;
        private readonly ulong _nonce;
        // true（默认）：视频走旧 XOR 掩码；false：该文件无可用视频密钥（音频-only 组），@SFV 跳过
        private readonly bool _videoEncrypted;
        private Aes? _aesAlgorithm;
        private ICryptoTransform? _ctrEncryptor;

        /// <param name="aesKey">AES-128 密钥（16B）。null=旧 XOR 体系，旧调用方完全不受影响。</param>
        /// <param name="nonce">7.1 视频 nonce（从 @UTF 头读取）。</param>
        /// <param name="videoEncrypted">false 时视频块原样跳过，仅提取音频（40251/_reunion_ 组）。</param>
        public USM(string filename, byte[] key1, byte[] key2, byte[]? aesKey = null, ulong nonce = 0, bool videoEncrypted = true)
        {
            _path = filename;
            _filename = Path.GetFileName(filename);
            _key1 = key1;
            _key2 = key2;
            _aesKey = aesKey;
            _nonce = nonce;
            _videoEncrypted = videoEncrypted;
            Console.WriteLine($"key1={Convert.ToHexString(_key1)} key2={Convert.ToHexString(_key2)}");

            if (aesKey != null)
            {
                if (aesKey.Length != 16)
                    throw new ArgumentException("aesKey must be 16 bytes (32 hex characters).", nameof(aesKey));
                // CTR 无填充；密钥流 = ECB 加密计数器块。只创建一次，供所有视频块复用
                _aesAlgorithm = Aes.Create();
                _aesAlgorithm.Mode = CipherMode.ECB;
                _aesAlgorithm.Padding = PaddingMode.None;
                _aesAlgorithm.Key = aesKey;
                _ctrEncryptor = _aesAlgorithm.CreateEncryptor();
            }
            else
            {
                InitMask(key1, key2);   // 仅旧体系需要掩码表
            }
        }
        private void InitMask(byte[] key1, byte[] key2)
        {
            _videoMask1 = new byte[0x20];
            _videoMask1[0x00] = key1[0];
            _videoMask1[0x01] = key1[1];
            _videoMask1[0x02] = key1[2];
            _videoMask1[0x03] = (byte)(key1[3] - 0x34);
            _videoMask1[0x04] = (byte)(key2[0] + 0xF9);
            _videoMask1[0x05] = (byte)(key2[1] ^ 0x13);
            _videoMask1[0x06] = (byte)(key2[2] + 0x61);
            _videoMask1[0x07] = (byte)(_videoMask1[0x00] ^ 0xFF);
            _videoMask1[0x08] = (byte)(_videoMask1[0x02] + _videoMask1[0x01]);
            _videoMask1[0x09] = (byte)(_videoMask1[0x01] - _videoMask1[0x07]);
            _videoMask1[0x0A] = (byte)(_videoMask1[0x02] ^ 0xFF);
            _videoMask1[0x0B] = (byte)(_videoMask1[0x01] ^ 0xFF);
            _videoMask1[0x0C] = (byte)(_videoMask1[0x0B] + _videoMask1[0x09]);
            _videoMask1[0x0D] = (byte)(_videoMask1[0x08] - _videoMask1[0x03]);
            _videoMask1[0x0E] = (byte)(_videoMask1[0x0D] ^ 0xFF);
            _videoMask1[0x0F] = (byte)(_videoMask1[0x0A] - _videoMask1[0x0B]);
            _videoMask1[0x10] = (byte)(_videoMask1[0x08] - _videoMask1[0x0F]);
            _videoMask1[0x11] = (byte)(_videoMask1[0x10] ^ _videoMask1[0x07]);
            _videoMask1[0x12] = (byte)(_videoMask1[0x0F] ^ 0xFF);
            _videoMask1[0x13] = (byte)(_videoMask1[0x03] ^ 0x10);
            _videoMask1[0x14] = (byte)(_videoMask1[0x04] - 0x32);
            _videoMask1[0x15] = (byte)(_videoMask1[0x05] + 0xED);
            _videoMask1[0x16] = (byte)(_videoMask1[0x06] ^ 0xF3);
            _videoMask1[0x17] = (byte)(_videoMask1[0x13] - _videoMask1[0x0F]);
            _videoMask1[0x18] = (byte)(_videoMask1[0x15] + _videoMask1[0x07]);
            _videoMask1[0x19] = (byte)(0x21 - _videoMask1[0x13]);
            _videoMask1[0x1A] = (byte)(_videoMask1[0x14] ^ _videoMask1[0x17]);
            _videoMask1[0x1B] = (byte)(_videoMask1[0x16] + _videoMask1[0x16]);
            _videoMask1[0x1C] = (byte)(_videoMask1[0x17] + 0x44);
            _videoMask1[0x1D] = (byte)(_videoMask1[0x03] + _videoMask1[0x04]);
            _videoMask1[0x1E] = (byte)(_videoMask1[0x05] - _videoMask1[0x16]);
            _videoMask1[0x1F] = (byte)(_videoMask1[0x1D] ^ _videoMask1[0x13]);

            byte[] table2 = Encoding.ASCII.GetBytes("URUC");
            _videoMask2 = new byte[0x20];
            _audioMask = new byte[0x20];
            for (int i = 0; i < 0x20; i++)
            {
                _videoMask2[i] = (byte)(_videoMask1[i] ^ 0xFF);
                _audioMask[i] = (byte)((i & 1) == 1 ? table2[i >> 1 & 3] : _videoMask1[i] ^ 0xFF);
            }
        }

        private void MaskVideo(ref byte[] data, int size)
        {
            const int dataOffset = 0x40;
            size -= dataOffset;
            if (size < 0x200) return;
            byte[] mask = new byte[0x20];
            Array.Copy(_videoMask2, mask, 0x20);
            for (int i = 0x100; i < size; i++) mask[i & 0x1F] = (byte)((data[i + dataOffset] ^= mask[i & 0x1F]) ^ _videoMask2[i & 0x1F]);
            Array.Copy(_videoMask1, mask, 0x20);
            for (int i = 0; i < 0x100; i++) data[i + dataOffset] ^= mask[i & 0x1F] ^= data[0x100 + i + dataOffset];
        }

        /// <summary>
        /// 7.1 视频块解密：AES-128-CTR，只解密 data[0x40:]。
        /// IV(16B) = BE(nonce,8) || BE(frameTime,4) || 00000000。
        /// CTR 的加解密对称，密钥流取 AES-ECB 对计数器块的加密；每个 chunk 计数器从 IV 重新开始
        /// （对应 charlotte 对每个数据块新建 Cipher(IV=nonce+frame_time)）。
        /// </summary>
        private void DecryptStream(ref byte[] data, uint frameTime)
        {
            const int offset = 0x40;
            int payloadLen = data.Length - offset;
            if (payloadLen <= 0 || _ctrEncryptor == null) return;

            Span<byte> counter = stackalloc byte[16];
            BinaryPrimitives.WriteUInt64BigEndian(counter[..8], _nonce);
            BinaryPrimitives.WriteUInt32BigEndian(counter.Slice(8, 4), frameTime);
            // counter[12..16] 保持 0：块内字节计数器

            byte[] counterArr = counter.ToArray();
            byte[] keystream = new byte[16];
            for (int pos = 0; pos < payloadLen; pos += 16)
            {
                _ctrEncryptor.TransformBlock(counterArr, 0, 16, keystream, 0);
                int blockLen = Math.Min(16, payloadLen - pos);
                for (int j = 0; j < blockLen; j++)
                    data[offset + pos + j] ^= keystream[j];

                // 16B 大端计数器 +1（低 32 位为块内计数；全字 +1 语义等价）
                for (int k = 15; k >= 0; k--)
                    if (++counterArr[k] != 0) break;
            }
        }

        // Not used anyway, but might be in the future
        private void MaskAudio(ref byte[] data, uint size)
        {
            const uint dataOffset = 0x140;
            size -= dataOffset;
            for (int i = 0; i < size; i++)  // To be confirmed, could start at the current index of data as well...
            {
                data[i + dataOffset] ^= _audioMask[i & 0x1F];
            }
        }

        // ===== 7.1+ @UTF 流头解析（移植自 charlotte read_utf，仅需 nonce 字段）=====

        /// <summary>@UTF 列类型（低 4 位）对应的字节宽度；与 charlotte UTF_TYPES 一一对应。</summary>
        private static int UtfTypeWidth(int type) => type switch
        {
            0 or 1 => 1,                     // B / b
            2 or 3 => 2,                     // H / h
            4 or 5 or 8 or 0xA => 4,         // I / i / f / 4字节串
            6 or 7 or 9 or 0xB => 8,         // Q / q / d / 8字节串
            _ => throw new InvalidDataException($"Unknown @UTF column type {type}")
        };

        /// <summary>按列类型从大端序读取一个整数（nonce 实际为 u64，这里覆盖所有整型宽度以防类型标记差异）。</summary>
        private static ulong ReadUtfValue(byte[] table, int type, int offset) => type switch
        {
            0 => table[offset],
            1 => (ulong)(sbyte)table[offset],
            2 => BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(offset)),
            3 => (ulong)BinaryPrimitives.ReadInt16BigEndian(table.AsSpan(offset)),
            4 => BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(offset)),
            5 => (ulong)BinaryPrimitives.ReadInt32BigEndian(table.AsSpan(offset)),
            6 or 7 => BinaryPrimitives.ReadUInt64BigEndian(table.AsSpan(offset)),
            _ => throw new InvalidDataException($"Non-integer @UTF value type {type}")
        };

        /// <summary>
        /// 从 @SFV 头块（@UTF 表）中读取 nonce。
        /// 表头偏移（相对 table 起点，与 Python struct 布局一致）：
        /// rowAt@2(u16)、stringsAt@4(u32)、保留@8/u32、保留@12/u32、columns@16(u16)；列定义从 24 起。
        /// </summary>
        private static ulong ReadUtfNonce(byte[] payload)
        {
            int tableSize = (int)BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(4));
            int copyLen = Math.Min(tableSize, payload.Length - 8);
            byte[] table = new byte[tableSize];
            Array.Copy(payload, 8, table, 0, copyLen);

            int rowCursor = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(2));
            uint stringsAt = BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(4));
            ushort columns = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(16));

            int at = 24;
            for (int i = 0; i < columns; i++)
            {
                byte flags = table[at];
                uint nameAt = BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(at + 1));
                int type = flags & 0x0F;
                int storage = flags & 0xF0;
                int width = UtfTypeWidth(type);

                ulong value;
                if (storage == 0x10)                // UTF_ZERO：值恒为 0
                {
                    value = 0;
                    at += 5;
                }
                else if (storage == 0x30)           // UTF_CONSTANT：值紧跟列定义
                {
                    int valueAt = at + 5;
                    value = ReadUtfValue(table, type, valueAt);
                    at = valueAt + width;
                }
                else if (storage == 0x50)           // UTF_PER_ROW：值在行数据区（游标需随逐行列推进）
                {
                    value = ReadUtfValue(table, type, rowCursor);
                    rowCursor += width;
                    at += 5;
                }
                else throw new InvalidDataException($"Unsupported @UTF storage flag 0x{storage:X2}");

                // 列名位于字符串区（偏移为 stringsAt + nameAt），NUL 结尾
                int namePos = (int)(stringsAt + nameAt);
                int nameEnd = Array.IndexOf(table, (byte)0, namePos);
                if (nameEnd < 0) throw new InvalidDataException("Malformed @UTF column name");
                string name = Encoding.ASCII.GetString(table, namePos, nameEnd - namePos);
                if (name == "nonce") return value;
            }
            throw new InvalidDataException("'nonce' column not found in @UTF video header");
        }

        /// <summary>
        /// 只打开文件扫描到第一个 @SFV 块，判定加密世代并取 nonce。
        /// dataType &amp; 0x3 == 0（数据块打头）→ 旧体系，返回 null；
        /// 否则为 @UTF 头块 → 解析并返回 nonce。对应 charlotte video_nonce()。
        /// </summary>
        public static ulong? ReadVideoNonce(string path)
        {
            using FileStream fp = File.OpenRead(path);
            long fileSize = fp.Length;
            byte[] head = new byte[32];
            while (fileSize >= 32)
            {
                if (fp.Read(head, 0, 32) != 32) break;
                fileSize -= 32;

                uint signature = BinaryPrimitives.ReadUInt32BigEndian(head.AsSpan(0));
                uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(head.AsSpan(4));
                byte dataOffset = head[9];
                ushort paddingSize = BinaryPrimitives.ReadUInt16BigEndian(head.AsSpan(10));
                byte dataType = head[15];

                int size = (int)(dataSize - dataOffset - paddingSize);
                fp.Seek(dataOffset - 0x18, SeekOrigin.Current);
                byte[] payload = new byte[size];
                fp.Read(payload, 0, size);
                fp.Seek(paddingSize, SeekOrigin.Current);
                fileSize -= dataSize - 0x18;

                if (signature != 0x40534656) continue;       // 等第一个 @SFV

                if ((dataType & 0x3) == 0) return null;       // 数据块 → 旧体系
                return ReadUtfNonce(payload);                  // @UTF 头块 → nonce
            }
            return null;
        }

        public Dictionary<string, List<string>> Demux(bool videoExtract, bool audioExtract, string outputDir)
        {

            FileStream filePointer = File.OpenRead(_path);  // TODO: Use a binary reader
            long fileSize = filePointer.Length;
            Info info = new();
            Console.WriteLine($"Demuxing {_filename} : extracting video and audio...");

            Dictionary<string, BinaryWriter> fileStreams = new(); // File paths as keys
            Dictionary<string, List<string>> filePaths = new();
            string path;
            while (fileSize > 0)
            {
                byte[] byteBlock = new byte[32];
                filePointer.Read(byteBlock, 0, byteBlock.Length);
                fileSize -= 32;

                info.signature = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 0));
                info.dataSize = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 4));
                info.dataOffset = byteBlock[9];
                info.paddingSize = Tools.Bswap(BitConverter.ToUInt16(byteBlock, 10));
                info.chno = byteBlock[12];
                info.dataType = byteBlock[15];
                info.frameTime = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 16));
                info.frameRate = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 20));

                int size = (int)(info.dataSize - info.dataOffset - info.paddingSize);
                filePointer.Seek(info.dataOffset - 0x18, SeekOrigin.Current);
                byte[] data = new byte[size];
                filePointer.Read(data);
                filePointer.Seek(info.paddingSize, SeekOrigin.Current);
                fileSize -= info.dataSize - 0x18;

                switch (info.signature)
                {
                    case 0x43524944: // CRID

                        break;
                    case 0x40534656: // @SFV    Video block
                        switch (info.dataType)
                        {
                            case 0:
                                if (videoExtract)
                                {
                                    bool writeVideo;
                                    if (_aesKey != null)
                                    {
                                        DecryptStream(ref data, info.frameTime);  // 7.1 AES-CTR
                                        writeVideo = true;
                                    }
                                    else if (_videoEncrypted)
                                    {
                                        MaskVideo(ref data, size);                // 旧版链式 XOR
                                        writeVideo = true;
                                    }
                                    else
                                    {
                                        // 7.1 音频-only 组：无 AES 密钥，跳过视频，不产出花屏 ivf
                                        writeVideo = false;
                                    }

                                    if (writeVideo)
                                    {
                                        path = Path.Combine(outputDir, _filename[..^4] + ".ivf");
                                        if (!fileStreams.ContainsKey(path))
                                        {
                                            fileStreams.Add(path, new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write)));
                                            if (!filePaths.ContainsKey("ivf")) filePaths.Add("ivf", new List<string>{path});
                                            else filePaths["ivf"].Add(path);
                                        }
                                        fileStreams[path].Write(data);
                                    }
                                }
                                break;
                            default: // Not implemented, we don't have any uses for it
                                break;
                        }
                        break;

                    case 0x40534641: // @SFA    Audio block
                        switch (info.dataType)
                        {
                            case 0:
                                if (audioExtract)
                                {
                                    // Might need some extra work if the audio has to be decrypted during the demuxing
                                    // (hello AudioMask)
                                    path = Path.Combine(outputDir, _filename[..^4] + $"_{info.chno}.hca");
                                    if (!fileStreams.ContainsKey(path))
                                    {
                                        fileStreams.Add(path, new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write)));
                                        if (!filePaths.ContainsKey("hca")) filePaths.Add("hca", new List<string> { path });
                                        else filePaths["hca"].Add(path);
                                    }
                                    fileStreams[path].Write(data);
                                }
                                break;
                            default: // No need to implement it, we lazy
                                break;
                        }
                        break;

                    case 0x40435545: // @CUE - Might be used to play a certain part of the video, but shouldn't be needed anyway (appears in cutscene Cs_Sumeru_AQ30161501_DT)
                        Console.WriteLine("@CUE field detected in USM, skipping as we don't need it");
                        break;
                    default:
                        Console.WriteLine("Signature {0} unknown, skipping...", info.signature);
                        break;
                }
            }
            // Closing Streams
            filePointer.Close();
            foreach (BinaryWriter stream in fileStreams.Values) stream.Close();
            _ctrEncryptor?.Dispose();   // 仅新体系创建过，旧体系为 null
            _aesAlgorithm?.Dispose();
            return filePaths;
        }
    }

}
