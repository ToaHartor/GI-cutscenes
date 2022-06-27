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
        public USM(string filename, byte[] key1, byte[] key2)
        {
            _path = filename;
            _filename = Path.GetFileName(filename);
            _key1 = key1;
            _key2 = key2;
            Console.WriteLine($"key1={Convert.ToHexString(_key1)} key2={Convert.ToHexString(_key2)}");
            InitMask(key1, key2);
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

        public void Demux(bool videoExtract, bool audioExtract, string outputDir)
        {

            FileStream filePointer = File.OpenRead(_path);  // TODO: Use a binary reader
            long fileSize = filePointer.Length;
            Info info = new();
            Console.WriteLine($"Demuxing {_filename} : extracting video and audio...");

            Dictionary<string, BinaryWriter> fileStreams = new(); // File paths as keys
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
                                    MaskVideo(ref data, size);
                                    path = Path.Combine(outputDir, _filename[..^4] + ".ivf");
                                    if (!fileStreams.ContainsKey(path)) fileStreams.Add(path, new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write)));
                                    fileStreams[path].Write(data);
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
                                    if (!fileStreams.ContainsKey(path)) fileStreams.Add(path, new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write)));
                                    fileStreams[path].Write(data);
                                }
                                break;
                            default: // No need to implement it, we lazy
                                break;
                        }
                        break;

                    default:
                        Console.WriteLine("Signature {0} unknown, skipping...", info.signature);
                        break;
                }
            }

            // Closing Streams
            filePointer.Close();
            foreach (BinaryWriter stream in fileStreams.Values) stream.Close();
        }
    }

}
