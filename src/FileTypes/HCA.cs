using GICutscenes.Utils;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace GICutscenes.FileTypes
{
    /*
        * Header :   HCA\x00 or C8C3C100 if encrypted
        * 0x00C1C3C8 & 0x7F7F7F7F == HCA\x00    0x00414348
        * if encrypted, then headers have to be bitwise-anded by 0x7F7F7F7F to get the original ones
        * 2 bytes for version :
        * 0x01 0x03 -> v1.3
        * 0x02 0x00 -> v2.0
        * 0x03 0x00 -> v3.0
        * 2 bytes for header size
        *
        * Format info :
        * 4 bytes for block signature
        * "fmt\x00"   0x00746D66
        * 1 byte -> channel count
        * 3 bytes -> sample rate
        * 4 bytes -> block count
        * 2 bytes -> encoder delay
        * 2 bytes -> encoder padding
        *
        * Compression info:
        * 4 bytes for block signature
        * "comp"   0x706D6F63
        * ushort blockSize
        * byte r01->r08
        * byte reserve1-reserve2
        *
        * Decode info:
        * 4 bytes for block signature
        * "dec\x00"   0x00636564
        *
        *
        * VBR info:
        * "vbr\x00"   0x00726276
        *
        * ATH info:
        * "ath\x00"   0x00687461
        * ushort type  (0:全て0 1:テーブル1)
        *
        * Loop info:
        * "loop"   0x706F6F6C
        * uint start
        * uint end
        * ushort count
        * ushort r01
        *
        * Cipher info:
        * 4 bytes for block signature
        * "ciph"  0x68706963
        * ushort type (0:暗号化なし 1:鍵なし暗号化 0x38:鍵あり暗号化)
        *
        * RVA info:
        * "rva\x00   0x00617672
        * float volume
        *
        * Comment info:
        * "comm"   0x6D6D6F63
        * byte length
        */

    internal struct HCAHeader  // Only useful things here
    {
        // HCA
        public ushort version;

        public ushort dataOffset;

        // FMT
        public uint channelCount;

        public uint samplingRate; // 3 bytes
        public uint blockCount;

        // COMP | DEC
        public ushort blockSize;

        // all 1 byte
        public uint comp_r01;
        public uint comp_r02;
        public uint comp_r03;
        public uint comp_r04;
        public uint comp_r05;
        public uint comp_r06;
        public uint comp_r07;
        public uint comp_r08;
        public uint comp_r09;

        // VBR
        // ATH
        public ushort athType;

        // LOOP
        public bool loopFlg;

        // CIPH
        public ushort ciphType;

        // RVA
        public float volume;

        // COMM
        // PAD
    }

    public class Hca
    {
        private readonly string audioName;

        private readonly byte[] _key1;
        private readonly byte[] _key2;
        private readonly byte[] _ciphTable;
        private byte[] _athTable;
        private bool _encrypted = false;
        private HCAHeader _hcaHeader;
        private Channel[] _hcaChannel; // usually of size 0x10
        private byte[] _header;
        private byte[] _data;
        private int _ciperTypeOffset = 0; //used to export decrypt hca

        public Hca(string filename, byte[]? key1 = null, byte[]? key2 = null, byte[]? hcaData = null)
        {
            FileStream hcaFile;
            audioName = filename;
            int audioSize;
            byte[] headerMagic = new byte[8];
            if (key1 == null || key2 == null)  // if no key is provided, then attempt to automatically find the keys based to the filenames in versions.json
            {
                FileInfo f = new(filename);
                Match m = Regex.Match(f.Name, @"(.*?)_[0-3]\.hca");  // Matching a base name that could correspond to a USM file from versions.json
                if (!m.Success) throw new ArgumentException($"Unable to find key for the file {f.Name}, as it has to follow a specific naming convention when automatically demuxed...");
                var splitKeys = Demuxer.KeySplitter(Demuxer.EncryptionKey(m.Groups[1].Captures[0] + ".usm"));
                key1 = splitKeys.Item1;
                key2 = splitKeys.Item2;
            }
            _key1 = key1;
            _key2 = key2;
            _ciphTable = new byte[0x100];
            _hcaHeader = new HCAHeader();
            if (hcaData is null)
            {
                // Read data from file
                if (!File.Exists(audioName)) throw new FileNotFoundException();
                if (!audioName.EndsWith(".hca")) throw new FileLoadException();
                hcaFile = File.OpenRead(audioName);
                hcaFile.Read(headerMagic, 0, 8);
                checkHeader(headerMagic);
                hcaFile.Seek(0, SeekOrigin.Begin);
                _header = new byte[_hcaHeader.dataOffset];
                hcaFile.Read(_header, 0, _header.Length);
                LoadHeader(ref _header, _encrypted);
                audioSize = (int)(_hcaHeader.blockSize * _hcaHeader.blockCount);
                _data = new byte[audioSize];
                hcaFile.Read(_data, 0, audioSize);
                hcaFile.Close();
            }
            else 
            {
                checkHeader(hcaData.Take(8).ToArray());
                _header = hcaData.Take(_hcaHeader.dataOffset).ToArray();
                LoadHeader(ref _header, _encrypted);
                _data = hcaData.Skip(_hcaHeader.dataOffset).Take((int)(_hcaHeader.blockSize * _hcaHeader.blockCount)).ToArray();
            }
        }

        private byte[] Init56_CreateTable(byte key)
        {
            byte[] table = new byte[0x10];
            int mul = (key & 1) << 3 | 5;
            int add = key & 0xE | 1;
            key >>= 4;
            for (int i = 0; i < 0x10; i++)
            {
                key = (byte)(key * mul + add & 0xF);
                table[i] = key;
            }
            return table;
        }

        private void InitMask(int type)
        {
            switch (type)
            {
                case 0:
                    for (int i = 0; i < 0x100; i++) _ciphTable[i] = (byte)i;
                    break;

                case 1:
                    for (int i = 0, v = 0; i < 0xFF; i++)
                    {
                        v = v * 13 + 11 & 0xFF;
                        if (v is 0 or 0xFF) v = v * 13 + 11 & 0xFF;
                        _ciphTable[i] = (byte)v;
                    }
                    _ciphTable[0] = 0;
                    _ciphTable[0xFF] = 0xFF;
                    break;

                case 56:
                    byte[] t1 = new byte[8];
                    uint key1 = BitConverter.ToUInt32(_key1, 0);
                    uint key2 = BitConverter.ToUInt32(_key2, 0);

                    if (key1 == 0) key2--;
                    key1--;
                    for (int i = 0; i < 7; i++)
                    {
                        t1[i] = (byte)key1;
                        key1 = key1 >> 8 | key2 << 24;
                        key2 >>= 8;
                    }

                    byte[] t2 = new byte[] {
                        t1[1],
                        (byte)  (uint) (t1[1] ^ t1[6]),
                        (byte)  (uint) (t1[2] ^ t1[3]),
                        t1[2],
                        (byte)  (uint) (t1[2] ^ t1[1]),
                        (byte)  (uint) (t1[3] ^ t1[4]),
                        t1[3],
                        (byte)  (uint) (t1[3] ^ t1[2]),
                        (byte)  (uint) (t1[4] ^ t1[5]),
                        t1[4],
                        (byte)  (uint) (t1[4] ^ t1[3]),
                        (byte)  (uint) (t1[5] ^ t1[6]),
                        t1[5],
                        (byte)  (uint) (t1[5] ^ t1[4]),
                        (byte)  (uint) (t1[6] ^ t1[1]),
                        t1[6]
                    };

                    byte[] t3 = new byte[0x100];
                    byte[] t31 = Init56_CreateTable(t1[0]);
                    // Create Table
                    for (int i = 0; i < 0x10; i++)
                    {
                        byte[] t32 = Init56_CreateTable(t2[i]);
                        byte v = (byte)(t31[i] << 4);
                        int index = 0;
                        foreach (byte j in t32)
                        {
                            t3[i * 0x10 + index] = (byte)(v | j);
                            index++;
                        }
                    }

                    // CIPHテーブル
                    int iTable = 1;
                    for (int i = 0, v = 0; i < 0x100; i++)
                    {
                        v = v + 0x11 & 0xFF;
                        byte a = t3[v];
                        if (a != 0 && a != 0xFF) _ciphTable[iTable++] = a;
                    }
                    _ciphTable[0] = 0;
                    _ciphTable[0xFF] = 0xFF;
                    break;

                default:
                    break;
            }
        }

        private void Mask(ref byte[] data, int size)
        {
            if (_hcaHeader.ciphType == 0) { return; }
            for (int i = 0; size > 0; i++, size--)
            {
                byte d = data[i];
                data[i] = _ciphTable[d];
            }
        }

        private static ushort CheckSum(byte[] data, int size)
        {
            ushort[] v = {
            0x0000, 0x8005, 0x800F, 0x000A, 0x801B, 0x001E, 0x0014, 0x8011, 0x8033, 0x0036, 0x003C, 0x8039, 0x0028,
            0x802D, 0x8027, 0x0022,
            0x8063, 0x0066, 0x006C, 0x8069, 0x0078, 0x807D, 0x8077, 0x0072, 0x0050, 0x8055, 0x805F, 0x005A, 0x804B,
            0x004E, 0x0044, 0x8041,
            0x80C3, 0x00C6, 0x00CC, 0x80C9, 0x00D8, 0x80DD, 0x80D7, 0x00D2, 0x00F0, 0x80F5, 0x80FF, 0x00FA, 0x80EB,
            0x00EE, 0x00E4, 0x80E1,
            0x00A0, 0x80A5, 0x80AF, 0x00AA, 0x80BB, 0x00BE, 0x00B4, 0x80B1, 0x8093, 0x0096, 0x009C, 0x8099, 0x0088,
            0x808D, 0x8087, 0x0082,
            0x8183, 0x0186, 0x018C, 0x8189, 0x0198, 0x819D, 0x8197, 0x0192, 0x01B0, 0x81B5, 0x81BF, 0x01BA, 0x81AB,
            0x01AE, 0x01A4, 0x81A1,
            0x01E0, 0x81E5, 0x81EF, 0x01EA, 0x81FB, 0x01FE, 0x01F4, 0x81F1, 0x81D3, 0x01D6, 0x01DC, 0x81D9, 0x01C8,
            0x81CD, 0x81C7, 0x01C2,
            0x0140, 0x8145, 0x814F, 0x014A, 0x815B, 0x015E, 0x0154, 0x8151, 0x8173, 0x0176, 0x017C, 0x8179, 0x0168,
            0x816D, 0x8167, 0x0162,
            0x8123, 0x0126, 0x012C, 0x8129, 0x0138, 0x813D, 0x8137, 0x0132, 0x0110, 0x8115, 0x811F, 0x011A, 0x810B,
            0x010E, 0x0104, 0x8101,
            0x8303, 0x0306, 0x030C, 0x8309, 0x0318, 0x831D, 0x8317, 0x0312, 0x0330, 0x8335, 0x833F, 0x033A, 0x832B,
            0x032E, 0x0324, 0x8321,
            0x0360, 0x8365, 0x836F, 0x036A, 0x837B, 0x037E, 0x0374, 0x8371, 0x8353, 0x0356, 0x035C, 0x8359, 0x0348,
            0x834D, 0x8347, 0x0342,
            0x03C0, 0x83C5, 0x83CF, 0x03CA, 0x83DB, 0x03DE, 0x03D4, 0x83D1, 0x83F3, 0x03F6, 0x03FC, 0x83F9, 0x03E8,
            0x83ED, 0x83E7, 0x03E2,
            0x83A3, 0x03A6, 0x03AC, 0x83A9, 0x03B8, 0x83BD, 0x83B7, 0x03B2, 0x0390, 0x8395, 0x839F, 0x039A, 0x838B,
            0x038E, 0x0384, 0x8381,
            0x0280, 0x8285, 0x828F, 0x028A, 0x829B, 0x029E, 0x0294, 0x8291, 0x82B3, 0x02B6, 0x02BC, 0x82B9, 0x02A8,
            0x82AD, 0x82A7, 0x02A2,
            0x82E3, 0x02E6, 0x02EC, 0x82E9, 0x02F8, 0x82FD, 0x82F7, 0x02F2, 0x02D0, 0x82D5, 0x82DF, 0x02DA, 0x82CB,
            0x02CE, 0x02C4, 0x82C1,
            0x8243, 0x0246, 0x024C, 0x8249, 0x0258, 0x825D, 0x8257, 0x0252, 0x0270, 0x8275, 0x827F, 0x027A, 0x826B,
            0x026E, 0x0264, 0x8261,
            0x0220, 0x8225, 0x822F, 0x022A, 0x823B, 0x023E, 0x0234, 0x8231, 0x8213, 0x0216, 0x021C, 0x8219, 0x0208,
            0x820D, 0x8207, 0x0202 };
            ushort sum = 0;
            for (int i = 0; i < size; i++) sum = (ushort)(sum << 8 ^ v[sum >> 8 ^ data[i]]);
            return sum;
        }

        private void checkHeader(byte[] hcaBytes)
        {
            uint magic = 0xFFFFFFFF;
            uint sign = BitConverter.ToUInt32(hcaBytes, 0) & 0x7F7F7F7F;
            if (sign == 0x00414348u)
            {
                magic = 0x7F7F7F7F;
                _encrypted = true;
            }

            sign = BitConverter.ToUInt32(hcaBytes, 0) & magic;
            if (sign == 0x00414348u)
            {
                BitConverter.GetBytes(sign).CopyTo(hcaBytes, 0);
                _hcaHeader.version = Tools.Bswap(BitConverter.ToUInt16(hcaBytes, 4));
                _hcaHeader.dataOffset = Tools.Bswap(BitConverter.ToUInt16(hcaBytes, 6));
            }
            else
            {
                Console.WriteLine("Wrong header, exiting...");
                Environment.Exit(0);
            }
        }

        private void LoadHeader(ref byte[] header, bool _encrypted)
        {
            if (_encrypted)
            {
                for (int i = 0; i < 8; i++)
                {
                    header[i] &= 0x7f;
                }
            }

            int headerOffset = 8;

            // fmt
            uint sign = BitConverter.ToUInt32(header, headerOffset);
            if (_encrypted) { sign &= 0x7f7f7f7f;}

            if (sign == 0x00746D66u) // fmt
            {
                //Console.WriteLine("fmt");
                BitConverter.GetBytes(sign).CopyTo(header, headerOffset);
                _hcaHeader.channelCount = header[headerOffset + 4];
                byte[] samplingRate = new byte[4];
                Array.Copy(header, headerOffset + 5, samplingRate, 0, 3);
                samplingRate[3] = 0x00;
                _hcaHeader.samplingRate = Tools.Bswap(BitConverter.ToUInt32(samplingRate) << 8);
                _hcaHeader.blockCount = Tools.Bswap(BitConverter.ToUInt32(header, headerOffset + 8));
                headerOffset += 16;
            }
            else
            {
                Console.WriteLine("Unknown field, quitting...");
                Environment.Exit(0);
            }

            // comp or dec
            sign = BitConverter.ToUInt32(header, headerOffset);
            if (_encrypted) { sign &= 0x7f7f7f7f; }

            if (sign == 0x706D6F63u) // comp
            {
                //Console.WriteLine("comp");
                BitConverter.GetBytes(sign).CopyTo(header, headerOffset);
                _hcaHeader.blockSize = Tools.Bswap(BitConverter.ToUInt16(header, headerOffset + 4));
                _hcaHeader.comp_r01 = header[headerOffset + 6];
                _hcaHeader.comp_r02 = header[headerOffset + 7];
                _hcaHeader.comp_r03 = header[headerOffset + 8];
                _hcaHeader.comp_r04 = header[headerOffset + 9];
                _hcaHeader.comp_r05 = header[headerOffset + 10];
                _hcaHeader.comp_r06 = header[headerOffset + 11];
                _hcaHeader.comp_r07 = header[headerOffset + 12];
                _hcaHeader.comp_r08 = header[headerOffset + 13];
                if (_hcaHeader.blockSize is not (>= 8 and <= 0xFFFF or 0)) throw new Exception("Incorrect block size");
                if (!(_hcaHeader.comp_r01 >= 0 && _hcaHeader.comp_r01 <= _hcaHeader.comp_r02 && _hcaHeader.comp_r02 <= 0x1F)) throw new Exception("Incorrect comp values");
                headerOffset += 16;
            }
            else if (sign == 0x00636564u) // dec
            {
                //Console.WriteLine("dec");
                BitConverter.GetBytes(sign).CopyTo(header, headerOffset);
                _hcaHeader.blockSize = Tools.Bswap(BitConverter.ToUInt16(header, headerOffset + 4));
                _hcaHeader.comp_r01 = header[headerOffset + 6];
                _hcaHeader.comp_r02 = header[headerOffset + 7];
                // I hope there isn't any weird things with endianness
                _hcaHeader.comp_r03 = (byte)(header[headerOffset + 10] >> 4);  // 4 first bits
                _hcaHeader.comp_r04 = header[headerOffset + 10] & 0xFu;  // 4 last bits

                _hcaHeader.comp_r05 = header[headerOffset + 8];
                _hcaHeader.comp_r06 = (header[headerOffset + 11] > 0 ? header[headerOffset + 9] : header[headerOffset + 8]) + 1u;
                _hcaHeader.comp_r07 = _hcaHeader.comp_r05 - _hcaHeader.comp_r06;
                _hcaHeader.comp_r08 = 0;
                if (_hcaHeader.blockSize is not (>= 8 and <= 0xFFFF or 0)) throw new Exception("Incorrect block size");
                if (!(_hcaHeader.comp_r01 >= 0 && _hcaHeader.comp_r01 <= _hcaHeader.comp_r02 && _hcaHeader.comp_r02 <= 0x1F)) throw new Exception("Incorrect comp values");
                if (_hcaHeader.comp_r03 == 0) _hcaHeader.comp_r03 = 1;
                headerOffset += 12;
            }
            else
            {
                Console.WriteLine("Unknown field, quitting...");
                Environment.Exit(0);
            }

            // vbr
            sign = BitConverter.ToUInt32(header, headerOffset);
            if (_encrypted) { sign &= 0x7f7f7f7f; }
            if (sign == 0x00726276u) // vbr
            {
                //Console.WriteLine("vbr");
                BitConverter.GetBytes(sign).CopyTo(header, headerOffset);
                headerOffset += 8;
            }

            // ath
            sign = BitConverter.ToUInt32(header, headerOffset);
            if (_encrypted) { sign &= 0x7f7f7f7f; }

            if (sign == 0x00687461u) // ath
            {
                //Console.WriteLine("ath");
                BitConverter.GetBytes(sign).CopyTo(header, headerOffset);
                _hcaHeader.athType = Tools.Bswap(BitConverter.ToUInt16(header, headerOffset + 4));
                headerOffset += 6;
            }
            else
            {
                if (_hcaHeader.version < 0x200) _hcaHeader.athType = 1;
            }

            // loop
            sign = BitConverter.ToUInt32(header, headerOffset);
            if (_encrypted) { sign &= 0x7f7f7f7f; }

            if (sign == 0x706F6F6Cu) // loop
            {
                //Console.WriteLine("loop");
                BitConverter.GetBytes(sign).CopyTo(header, headerOffset);
                _hcaHeader.loopFlg = true;
                headerOffset += 16;
            }
            else
            {
                _hcaHeader.loopFlg = false;
            }

            // ciph
            sign = BitConverter.ToUInt32(header, headerOffset);
            if (_encrypted) { sign &= 0x7f7f7f7f; }

            if (sign == 0x68706963u) // ciph
            {
                //Console.WriteLine("ciph");
                BitConverter.GetBytes(sign).CopyTo(header, headerOffset);
                _ciperTypeOffset = headerOffset + 4;
                _hcaHeader.ciphType = Tools.Bswap(BitConverter.ToUInt16(header, _ciperTypeOffset));
                if (_hcaHeader.ciphType is not (0 or 1 or 0x38)) throw new Exception("Invalid cipher type: " + _hcaHeader.ciphType);
                headerOffset += 6;
            }
            else
            {
                _hcaHeader.ciphType = 0;
            }

            // rva
            sign = BitConverter.ToUInt32(header, headerOffset);
            if (_encrypted) { sign &= 0x7f7f7f7f; }

            if (sign == 0x00617672u) // rva
            {
                //Console.WriteLine("rva");
                BitConverter.GetBytes(sign).CopyTo(header, headerOffset);
                _hcaHeader.volume = Tools.Bswap(BitConverter.ToSingle(header, headerOffset + 4));
                headerOffset += 8;
            }
            else
            {
                _hcaHeader.volume = 1;
            }

            // comm
            sign = BitConverter.ToUInt32(header, headerOffset);
            if (_encrypted) { sign &= 0x7f7f7f7f; }

            if (sign == 0x6D6D6F63u) // comm
            {
                //Console.WriteLine("comm");
                BitConverter.GetBytes(sign).CopyTo(header, headerOffset);
                headerOffset += 5;
            }

            // pad
            sign = BitConverter.ToUInt32(header, headerOffset);
            if (_encrypted) { sign &= 0x7f7f7f7f; }

            if (sign == 0x00646170u) // pad
            {
                //Console.WriteLine("pad");
                BitConverter.GetBytes(sign).CopyTo(header, headerOffset);
                headerOffset += 4;
            }
            //Console.WriteLine("Finished parsing header...");
            BitConverter.GetBytes(Tools.Bswap(CheckSum(header, header.Length - 2))).CopyTo(header, header.Length - 2);

            ATHInit();
            InitMask(_hcaHeader.ciphType);

            if (_hcaHeader.comp_r03 == 0) _hcaHeader.comp_r03 = 1;

            ChannelInit();
        }

        public Task Decrypt(string? output=null) // I don't know how to use async in C#
        {
            if (output == null) { output = audioName; }
            FileStream exportFile = File.OpenWrite(output);

            //modify header to unencrypted mode
            byte[] header = new byte[_header.Length];
            Buffer.BlockCopy(_header, 0, header, 0, _header.Length);
            byte[] decryptType = { 0, 0 };
            decryptType.CopyTo(header, _ciperTypeOffset);
            BitConverter.GetBytes(Tools.Bswap(CheckSum(header, header.Length - 2))).CopyTo(header, header.Length - 2);
            exportFile.Write(header, 0, header.Length);

            //if (!File.Exists(filename)) throw new FileNotFoundException();
            //if (!filename.EndsWith(".hca")) throw new FileLoadException();
            //FileStream filePointer = File.OpenRead(filename);

            //byte[] content = new byte[hcaHeader.blockSize * hcaHeader.blockCount];
            byte[] data2 = new byte[_hcaHeader.blockSize];

            //InitMask(_hcaHeader.ciphType);
            //Console.WriteLine("Decrypting content...");
            if (_hcaHeader.ciphType != 0)
            { //a = hcaHeader.dataOffset
                for (uint i = 0, a = 0; i < _hcaHeader.blockCount; i++, a += _hcaHeader.blockSize)
                {
                    //Console.WriteLine($"{Path.GetFileNameWithoutExtension(audioName)}: i为{i}, blockCount为{_hcaHeader.blockCount}，{i < _hcaHeader.blockCount}");
                    Array.Copy(_data, a, data2, 0, _hcaHeader.blockSize);
                    //filePointer.Seek(a, SeekOrigin.Begin);
                    //filePointer.Read(data2, 0, hcaHeader.blockSize);
                    //Console.WriteLine(Convert.ToHexString(data2));
                    Mask(ref data2, _hcaHeader.blockSize);
                    BitConverter.GetBytes(Tools.Bswap(CheckSum(data2, _hcaHeader.blockSize - 2))).CopyTo(data2, _hcaHeader.blockSize - 2); // checksum inclusion
                    exportFile.Write(data2);
                    // a - hcaHeader.dataOffset
                }
            }
            else
            {
                exportFile.Write(_data);
            }
            exportFile.Close();
            return Task.CompletedTask;
            //filePointer.Close();
            //this.data = content;

            // For saving the file to the disk
            //string decryptedFile = String.Concat(filename.Substring(0, filename.Length - 4), "_decrypted.hca");
            //File.WriteAllBytes(decryptedFile, this.header.Concat(content).ToArray());
            //Console.WriteLine("File written : " + decryptedFile);
        }

        private void DecodeBlock(ref byte[] data)
        {
            Mask(ref data, _hcaHeader.blockSize);
            ClData d = new(data, _hcaHeader.blockSize);
            int magic = d.GetBit(16);//0xFFFF固定
            if (magic != 0xFFFF) return;
            int a = (d.GetBit(9) << 8) - d.GetBit(7);
                for (uint i = 0; i < _hcaHeader.channelCount; i++) _hcaChannel[i].Decode1(ref d, _hcaHeader.comp_r09, a, _athTable);
                for (int i = 0; i < 8; i++)
                {
                    for (uint j = 0; j < _hcaHeader.channelCount; j++) _hcaChannel[j].Decode2(ref d);
                    for (uint j = 0; j < _hcaHeader.channelCount; j++)
                        _hcaChannel[j].Decode3(_hcaHeader.comp_r09, _hcaHeader.comp_r08,
                            _hcaHeader.comp_r07 + _hcaHeader.comp_r06, _hcaHeader.comp_r05);
                    for (uint j = 0; j < _hcaHeader.channelCount - 1; j++)
                        _hcaChannel[j].Decode4(i, _hcaHeader.comp_r05 - _hcaHeader.comp_r06, _hcaHeader.comp_r06,
                            _hcaHeader.comp_r07, _hcaChannel[1]);
                    for (uint j = 0; j < _hcaHeader.channelCount; j++) _hcaChannel[j].Decode5(i);
                }
        }

        private void ATHInit()
        {
            switch (_hcaHeader.athType)
            {
                case 0:
                    _athTable = new byte[0x80];
                    break;

                case 1:
                    byte[] list = {
                            0x78, 0x5F, 0x56, 0x51, 0x4E, 0x4C, 0x4B, 0x49, 0x48, 0x48, 0x47, 0x46, 0x46, 0x45, 0x45, 0x45,
                            0x44, 0x44, 0x44, 0x44, 0x43, 0x43, 0x43, 0x43, 0x43, 0x43, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42,
                            0x42, 0x42, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x40, 0x40, 0x40, 0x40,
                            0x40, 0x40, 0x40, 0x40, 0x40, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F,
                            0x3F, 0x3F, 0x3F, 0x3E, 0x3E, 0x3E, 0x3E, 0x3E, 0x3E, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D,
                            0x3C, 0x3C, 0x3C, 0x3C, 0x3C, 0x3C, 0x3C, 0x3C, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B,
                            0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B,
                            0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3B, 0x3C, 0x3C, 0x3C, 0x3C, 0x3C, 0x3C, 0x3C, 0x3C,
                            0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3E, 0x3E, 0x3E, 0x3E, 0x3E, 0x3E, 0x3E, 0x3F,
                            0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F,
                            0x3F, 0x3F, 0x3F, 0x3F, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40,
                            0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
                            0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
                            0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42,
                            0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x42, 0x43, 0x43, 0x43,
                            0x43, 0x43, 0x43, 0x43, 0x43, 0x43, 0x43, 0x43, 0x43, 0x43, 0x43, 0x43, 0x43, 0x43, 0x44, 0x44,
                            0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x44, 0x45, 0x45, 0x45, 0x45,
                            0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x46, 0x46, 0x46, 0x46, 0x46, 0x46, 0x46, 0x46,
                            0x46, 0x46, 0x47, 0x47, 0x47, 0x47, 0x47, 0x47, 0x47, 0x47, 0x47, 0x47, 0x48, 0x48, 0x48, 0x48,
                            0x48, 0x48, 0x48, 0x48, 0x49, 0x49, 0x49, 0x49, 0x49, 0x49, 0x49, 0x49, 0x4A, 0x4A, 0x4A, 0x4A,
                            0x4A, 0x4A, 0x4A, 0x4A, 0x4B, 0x4B, 0x4B, 0x4B, 0x4B, 0x4B, 0x4B, 0x4C, 0x4C, 0x4C, 0x4C, 0x4C,
                            0x4C, 0x4D, 0x4D, 0x4D, 0x4D, 0x4D, 0x4D, 0x4E, 0x4E, 0x4E, 0x4E, 0x4E, 0x4E, 0x4F, 0x4F, 0x4F,
                            0x4F, 0x4F, 0x4F, 0x50, 0x50, 0x50, 0x50, 0x50, 0x51, 0x51, 0x51, 0x51, 0x51, 0x52, 0x52, 0x52,
                            0x52, 0x52, 0x53, 0x53, 0x53, 0x53, 0x54, 0x54, 0x54, 0x54, 0x54, 0x55, 0x55, 0x55, 0x55, 0x56,
                            0x56, 0x56, 0x56, 0x57, 0x57, 0x57, 0x57, 0x57, 0x58, 0x58, 0x58, 0x59, 0x59, 0x59, 0x59, 0x5A,
                            0x5A, 0x5A, 0x5A, 0x5B, 0x5B, 0x5B, 0x5B, 0x5C, 0x5C, 0x5C, 0x5D, 0x5D, 0x5D, 0x5D, 0x5E, 0x5E,
                            0x5E, 0x5F, 0x5F, 0x5F, 0x60, 0x60, 0x60, 0x61, 0x61, 0x61, 0x61, 0x62, 0x62, 0x62, 0x63, 0x63,
                            0x63, 0x64, 0x64, 0x64, 0x65, 0x65, 0x66, 0x66, 0x66, 0x67, 0x67, 0x67, 0x68, 0x68, 0x68, 0x69,
                            0x69, 0x6A, 0x6A, 0x6A, 0x6B, 0x6B, 0x6B, 0x6C, 0x6C, 0x6D, 0x6D, 0x6D, 0x6E, 0x6E, 0x6F, 0x6F,
                            0x70, 0x70, 0x70, 0x71, 0x71, 0x72, 0x72, 0x73, 0x73, 0x73, 0x74, 0x74, 0x75, 0x75, 0x76, 0x76,
                            0x77, 0x77, 0x78, 0x78, 0x78, 0x79, 0x79, 0x7A, 0x7A, 0x7B, 0x7B, 0x7C, 0x7C, 0x7D, 0x7D, 0x7E,
                            0x7E, 0x7F, 0x7F, 0x80, 0x80, 0x81, 0x81, 0x82, 0x83, 0x83, 0x84, 0x84, 0x85, 0x85, 0x86, 0x86,
                            0x87, 0x88, 0x88, 0x89, 0x89, 0x8A, 0x8A, 0x8B, 0x8C, 0x8C, 0x8D, 0x8D, 0x8E, 0x8F, 0x8F, 0x90,
                            0x90, 0x91, 0x92, 0x92, 0x93, 0x94, 0x94, 0x95, 0x95, 0x96, 0x97, 0x97, 0x98, 0x99, 0x99, 0x9A,
                            0x9B, 0x9B, 0x9C, 0x9D, 0x9D, 0x9E, 0x9F, 0xA0, 0xA0, 0xA1, 0xA2, 0xA2, 0xA3, 0xA4, 0xA5, 0xA5,
                            0xA6, 0xA7, 0xA7, 0xA8, 0xA9, 0xAA, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAE, 0xAF, 0xB0, 0xB1, 0xB1,
                            0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
                            0xC0, 0xC1, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD,
                            0xCE, 0xCF, 0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD,
                            0xDE, 0xDF, 0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xED, 0xEE,
                            0xEF, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFF, 0xFF,
                    };
                    for (uint i = 0, v = 0; i < 0x80; i++, v += _hcaHeader.samplingRate)
                    {
                        uint index = v >> 13;
                        if (index >= 0x28E)
                        {
                            Array.Fill<byte>(_athTable, 0xFF, (int)i, (int)(0x80 - i)); // Filling the remaining table with 0xFF
                            break;
                        }
                        _athTable[i] = list[index];
                    }
                    break;

                default:
                    throw new Exception("Wrong ATH type, unable to init...");
            }
        }

        private void ChannelInit()
        {
            _hcaChannel = new Channel[_hcaHeader.channelCount];
            for (int i = 0; i < _hcaHeader.channelCount; i++)
            {
                _hcaChannel[i] = new Channel();
            }
            if (!(_hcaHeader.comp_r01 == 1 && _hcaHeader.comp_r02 == 15)) throw new Exception("Comp values invalid");
            _hcaHeader.comp_r09 = Tools.Ceil2(_hcaHeader.comp_r05 - (_hcaHeader.comp_r06 + _hcaHeader.comp_r07), _hcaHeader.comp_r08);
            byte[] r = new byte[0x10];
            Array.Clear(r);
            uint b = _hcaHeader.channelCount / _hcaHeader.comp_r03;
            if (_hcaHeader.comp_r07 != 0 && b > 1)
            {
                uint c = 0;
                for (uint i = 0; i < _hcaHeader.comp_r03; i++, c += b)
                {
                    switch (b)
                    {
                        case 2:
                            r[c] = 1;
                            r[c + 1] = 2;
                            break;

                        case 3:
                            r[c] = 1;
                            r[c + 1] = 2;
                            break;

                        case 4:
                            r[c] = 1;
                            r[c + 1] = 2;
                            if (_hcaHeader.comp_r04 == 0)
                            {
                                r[c + 2] = 1;
                                r[c + 3] = 2;
                            }
                            break;

                        case 5:
                            r[c] = 1;
                            r[c + 1] = 2;
                            if (_hcaHeader.comp_r04 <= 2)
                            {
                                r[c + 3] = 1;
                                r[c + 4] = 2;
                            }
                            break;

                        case 6:
                            r[c] = 1;
                            r[c + 1] = 2;
                            r[c + 4] = 1;
                            r[c + 5] = 2;
                            break;

                        case 7:
                            r[c] = 1;
                            r[c + 1] = 2;
                            r[c + 4] = 1;
                            r[c + 5] = 2;
                            break;

                        case 8:
                            r[c + 0] = 1;
                            r[c + 1] = 2;
                            r[c + 4] = 1;
                            r[c + 5] = 2;
                            r[c + 6] = 1;
                            r[c + 7] = 2;
                            break;
                    }
                }
            }
            for (uint i = 0; i < _hcaHeader.channelCount; i++)
            {
                _hcaChannel[i].type = r[i];
                _hcaChannel[i].value3I = _hcaHeader.comp_r06 + _hcaHeader.comp_r07;
                _hcaChannel[i].count = _hcaHeader.comp_r06 + (r[i] != 2 ? _hcaHeader.comp_r07 : 0);
            }
        }

        public Task ConvertToWAV(string outputDir)
        {
            // default values
            uint volume = 1;
            uint mode = 16;
            uint loop = 0;

            WAVEriff wavRiff = new();
            WAVEsmpl wavSmpl = new();
            //WAVEnote wavNote = new();
            WAVEdata wavData = new();
            wavRiff.fmtType = (ushort)(mode > 0 ? 1 : 3);
            wavRiff.fmtChannelCount = (ushort)_hcaHeader.channelCount;
            wavRiff.fmtBitCount = (ushort)(mode > 0 ? mode : 32);
            wavRiff.fmtSamplingRate = _hcaHeader.samplingRate;
            wavRiff.fmtSamplingSize = (ushort)(wavRiff.fmtBitCount / 8 * wavRiff.fmtChannelCount);
            wavRiff.fmtSamplesPerSec = wavRiff.fmtSamplingRate * wavRiff.fmtSamplingSize;

            wavData.dataSize = _hcaHeader.blockCount * 0x80 * 8 * wavRiff.fmtSamplingSize + (wavSmpl.loop_End - wavSmpl.loop_Start) * loop;
            wavRiff.riffSize = (uint)(0x1C + Marshal.SizeOf(wavData) + wavData.dataSize);


            // We do skip wavSmpl and wavNote, as they aren't useful to convert GI's HCA to WAV
            byte[] riffBytes = WAV.ToByteArray(wavRiff);
            byte[] dataBytes = WAV.ToByteArray(wavData);
            byte[] header = riffBytes.Concat(dataBytes).ToArray();

            // Opening the new wav file
            string wavFile = Path.Combine(outputDir, audioName[..^4] + ".wav");
            FileStream fs = new FileStream(wavFile, FileMode.Create);
            fs.Write(header, 0, header.Length);
            Console.WriteLine($"Converting {Path.GetFileName(audioName)} to wav...");
            _hcaHeader.volume *= volume;

            byte[] buf = new byte[_hcaHeader.blockSize];
            // Skipping the block for the loop
            MemoryStream ms = new MemoryStream((int)(8 * 0x80 * _hcaHeader.channelCount * (mode / 8)));
            uint offset = 0;
            for (uint l = 0; l < _hcaHeader.blockCount; l++, offset += _hcaHeader.blockSize) // iterating through hca blocks
            {
                Array.Copy(_data, offset, buf, 0, _hcaHeader.blockSize);
                DecodeBlock(ref buf);
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 0x80; j++)
                    {
                        for (uint k = 0; k < _hcaHeader.channelCount; k++)
                        {
                            float f = _hcaChannel[k].wave[i][j] * _hcaHeader.volume;
                            if (f > 1) f = 1;
                            else if (f < -1) f = -1;
                            // Decoding mode
                            int v = (int)f;
                            int bLength = 4;
                            switch (mode)
                            {
                                case 0:  // float
                                    break;

                                case 8:  // 8bits
                                    v = (int)(f * 0x7F) + 0x80;
                                    bLength = 1;
                                    break;

                                case 16: // 16bits
                                    v = (int)(f * 0x7FFF);
                                    bLength = 2;
                                    break;

                                case 24: // 24bits
                                    v = (int)(f * 0x7FFFFF);
                                    bLength = 3;
                                    break;

                                case 32: // 32bits
                                    v = (int)(f * 0x7FFFFFFF);
                                    break;

                                default:
                                    throw new Exception("This mode is not handled");
                            }
                            byte[] byteV = BitConverter.GetBytes(v);
                            ms.WriteAsync(byteV, 0, bLength);
                        }
                    }
                }
                ms.WriteTo(fs);
                ms.Seek(0, SeekOrigin.Begin);
            }
            fs.Close();
            ms.Close();

            return Task.CompletedTask;
        }
    }
}