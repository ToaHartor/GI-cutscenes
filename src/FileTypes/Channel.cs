namespace GICutscenes.FileTypes
{
    internal class Channel
    {
        public float[] block;
        public float[] baseTable;
        public sbyte[] value;
        public sbyte[] scale;
        public sbyte[] value2;
        public int type;
        public uint value3I;   // stores an index where the char pointer is in value
        public uint count;
        public float[] wav1;
        public float[] wav2;
        public float[] wav3;
        public float[][] wave;

        public Channel()
        {
            block = new float[0x80];
            baseTable = new float[0x80];
            value = new sbyte[0x80];
            scale = new sbyte[0x80];
            value2 = new sbyte[8];
            type = 0;
            value3I = 0;
            count = 0;
            wav1 = new float[0x80];
            wav2 = new float[0x80];
            wav3 = new float[0x80];
            wave = new float[8][];
            for (int i = 0; i < 8; i++)
            {
                wave[i] = new float[0x80];
            }
        }

        public void Decode1(ref ClData data, uint a, int b, byte[] athTable)
        {
            byte[] scalelist = {
            // v2.0
            0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0D, 0x0D,
            0x0D, 0x0D, 0x0D, 0x0D, 0x0C, 0x0C, 0x0C, 0x0C,
            0x0C, 0x0C, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B,
            0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x09,
            0x09, 0x09, 0x09, 0x09, 0x09, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x07, 0x06, 0x06, 0x05, 0x04,
            0x04, 0x04, 0x03, 0x03, 0x03, 0x02, 0x02, 0x02,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // v1.3
            //0x0E,0x0E,0x0E,0x0E,0x0E,0x0E,0x0D,0x0D,
            //0x0D,0x0D,0x0D,0x0D,0x0C,0x0C,0x0C,0x0C,
            //0x0C,0x0C,0x0B,0x0B,0x0B,0x0B,0x0B,0x0B,
            //0x0A,0x0A,0x0A,0x0A,0x0A,0x0A,0x0A,0x09,
            //0x09,0x09,0x09,0x09,0x09,0x08,0x08,0x08,
            //0x08,0x08,0x08,0x07,0x06,0x06,0x05,0x04,
            //0x04,0x04,0x03,0x03,0x03,0x02,0x02,0x02,
            //0x02,0x01,0x01,0x01,0x01,0x01,0x01,0x01,
            };

            uint[] valueInt = {
            0x342A8D26, 0x34633F89, 0x3497657D, 0x34C9B9BE, 0x35066491, 0x353311C4, 0x356E9910, 0x359EF532,
            0x35D3CCF1, 0x360D1ADF, 0x363C034A, 0x367A83B3, 0x36A6E595, 0x36DE60F5, 0x371426FF, 0x3745672A,
            0x37838359, 0x37AF3B79, 0x37E97C38, 0x381B8D3A, 0x384F4319, 0x388A14D5, 0x38B7FBF0, 0x38F5257D,
            0x3923520F, 0x39599D16, 0x3990FA4D, 0x39C12C4D, 0x3A00B1ED, 0x3A2B7A3A, 0x3A647B6D, 0x3A9837F0,
            0x3ACAD226, 0x3B071F62, 0x3B340AAF, 0x3B6FE4BA, 0x3B9FD228, 0x3BD4F35B, 0x3C0DDF04, 0x3C3D08A4,
            0x3C7BDFED, 0x3CA7CD94, 0x3CDF9613, 0x3D14F4F0, 0x3D467991, 0x3D843A29, 0x3DB02F0E, 0x3DEAC0C7,
            0x3E1C6573, 0x3E506334, 0x3E8AD4C6, 0x3EB8FBAF, 0x3EF67A41, 0x3F243516, 0x3F5ACB94, 0x3F91C3D3,
            0x3FC238D2, 0x400164D2, 0x402C6897, 0x4065B907, 0x40990B88, 0x40CBEC15, 0x4107DB35, 0x413504F3,
            };

            uint[] scaleInt = {
            0x00000000, 0x3F2AAAAB, 0x3ECCCCCD, 0x3E924925, 0x3E638E39, 0x3E3A2E8C, 0x3E1D89D9, 0x3E088889,
            0x3D842108, 0x3D020821, 0x3C810204, 0x3C008081, 0x3B804020, 0x3B002008, 0x3A801002, 0x3A000801,
            };

            //static auto* valueFloat = (float*)valueInt;
            uint valueFloatI = 0;
            //static auto* scaleFloat = (float*)scaleInt;
            uint scaleFloatI = 0;
            int v = data.GetBit(3);
            if (v >= 6)
            {
                for (uint i = 0; i < count; i++) value[i] = (sbyte)data.GetBit(6);
            }
            else if (v != 0)
            {
                int v1 = data.GetBit(6), v2 = (1 << v) - 1, v3 = v2 >> 1;
                value[0] = (sbyte)v1;
                for (uint i = 1; i < count; i++)
                {
                    int v4 = data.GetBit(v);
                    if (v4 != v2) { v1 += v4 - v3; }
                    else { v1 = data.GetBit(6); }
                    value[i] = (sbyte)v1;
                }
            }
            else
            {
                Array.Clear(value);
            }
            if (type == 2)
            {
                v = data.CheckBit(4);
                value2[0] = (sbyte)v;
                if (v < 15)
                {
                    //for (sbyte & i : value2) i = (sbyte)(data.GetBit(4));
                    for (int i = 0; i < value2.Length; i++)
                    {
                        value2[i] = (sbyte)data.GetBit(4);
                    }
                }
            }
            else // Not executed when a < i
            {
                for (uint i = 0; i < a; i++) value[value3I + i] = (sbyte)data.GetBit(6);
            }

            for (int i = 0; i < count; i++)
            {
                v = value[i];
                if (v != 0)
                {
                    v = athTable[i] + (b + i >> 8) - v * 5 / 2 + 1;
                    if (v < 0) v = 15;
                    else if (v >= 0x39) v = 1;
                    else v = scalelist[v];
                }
                scale[i] = (sbyte)v;
            }
            Array.Clear(scale, (int)count, (int)(0x80 - count));
            for (uint i = 0; i < count; i++)
            {
                float mul = 0.0f;
                if (value[i] is < 64 and >= 0) mul = BitConverter.UInt32BitsToSingle(valueInt[valueFloatI + value[i]]);
                baseTable[i] = mul * BitConverter.UInt32BitsToSingle(scaleInt[scaleFloatI + scale[i]]);
            }
        }

        public void Decode2(ref ClData data)
        {
            sbyte[] list1 = {
            0, 2, 3, 3, 4, 4, 4, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            };
            sbyte[] list2 = {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            2, 2, 2, 2, 2, 2, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0,
            2, 2, 3, 3, 3, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4,
            3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            };
            float[] list3 = {
            +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0,
            +0, +0, +1, -1, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0,
            +0, +0, +1, +1, -1, -1, +2, -2, +0, +0, +0, +0, +0, +0, +0, +0,
            +0, +0, +1, -1, +2, -2, +3, -3, +0, +0, +0, +0, +0, +0, +0, +0,
            +0, +0, +1, +1, -1, -1, +2, +2, -2, -2, +3, +3, -3, -3, +4, -4,
            +0, +0, +1, +1, -1, -1, +2, +2, -2, -2, +3, -3, +4, -4, +5, -5,
            +0, +0, +1, +1, -1, -1, +2, -2, +3, -3, +4, -4, +5, -5, +6, -6,
            +0, +0, +1, -1, +2, -2, +3, -3, +4, -4, +5, -5, +6, -6, +7, -7,
    };
            for (uint i = 0; i < count; i++)
            {
                float f;
                int s = scale[i];
                int bitSize = list1[s];
                int v = data.GetBit(bitSize);
                if (s < 8)
                {
                    v += s << 4;
                    data.AddBit(list2[v] - bitSize);
                    f = list3[v];
                }
                else
                {
                    v = (1 - ((v & 1) << 1)) * (v / 2);
                    if (v == 0) data.AddBit(-1);
                    f = v;
                }
                block[i] = baseTable[i] * f;
            }
            Array.Clear(block, (int)count, (int)(4 * (0x80 - count)));
        }

        public void Decode3(uint a, uint b, uint c, uint d)
        {
            if (type == 2 || b <= 0) return;
            uint[][] listInt = new uint[2][];
            listInt[0] = new uint[] {
                0x00000000, 0x00000000, 0x32A0B051, 0x32D61B5E, 0x330EA43A, 0x333E0F68, 0x337D3E0C, 0x33A8B6D5,
                0x33E0CCDF, 0x3415C3FF, 0x34478D75, 0x3484F1F6, 0x34B123F6, 0x34EC0719, 0x351D3EDA, 0x355184DF,
                0x358B95C2, 0x35B9FCD2, 0x35F7D0DF, 0x36251958, 0x365BFBB8, 0x36928E72, 0x36C346CD, 0x370218AF,
                0x372D583F, 0x3766F85B, 0x3799E046, 0x37CD078C, 0x3808980F, 0x38360094, 0x38728177, 0x38A18FAF,
                0x38D744FD, 0x390F6A81, 0x393F179A, 0x397E9E11, 0x39A9A15B, 0x39E2055B, 0x3A16942D, 0x3A48A2D8,
                0x3A85AAC3, 0x3AB21A32, 0x3AED4F30, 0x3B1E196E, 0x3B52A81E, 0x3B8C57CA, 0x3BBAFF5B, 0x3BF9295A,
                0x3C25FED7, 0x3C5D2D82, 0x3C935A2B, 0x3CC4563F, 0x3D02CD87, 0x3D2E4934, 0x3D68396A, 0x3D9AB62B,
                0x3DCE248C, 0x3E0955EE, 0x3E36FD92, 0x3E73D290, 0x3EA27043, 0x3ED87039, 0x3F1031DC, 0x3F40213B
            };
            listInt[1] = new uint[] {
                0x3F800000, 0x3FAA8D26, 0x3FE33F89, 0x4017657D, 0x4049B9BE, 0x40866491, 0x40B311C4, 0x40EE9910,
                0x411EF532, 0x4153CCF1, 0x418D1ADF, 0x41BC034A, 0x41FA83B3, 0x4226E595, 0x425E60F5, 0x429426FF,
                0x42C5672A, 0x43038359, 0x432F3B79, 0x43697C38, 0x439B8D3A, 0x43CF4319, 0x440A14D5, 0x4437FBF0,
                0x4475257D, 0x44A3520F, 0x44D99D16, 0x4510FA4D, 0x45412C4D, 0x4580B1ED, 0x45AB7A3A, 0x45E47B6D,
                0x461837F0, 0x464AD226, 0x46871F62, 0x46B40AAF, 0x46EFE4BA, 0x471FD228, 0x4754F35B, 0x478DDF04,
                0x47BD08A4, 0x47FBDFED, 0x4827CD94, 0x485F9613, 0x4894F4F0, 0x48C67991, 0x49043A29, 0x49302F0E,
                0x496AC0C7, 0x499C6573, 0x49D06334, 0x4A0AD4C6, 0x4A38FBAF, 0x4A767A41, 0x4AA43516, 0x4ADACB94,
                0x4B11C3D3, 0x4B4238D2, 0x4B8164D2, 0x4BAC6897, 0x4BE5B907, 0x4C190B88, 0x4C4BEC15, 0x00000000
            };
            uint[] listFloat = listInt[1];
            for (uint i = 0; i < a; i++)
            {
                for (uint j = 0, k = c, l = c - 1; j < b && k < d; j++, l--)
                {
                    block[k++] = BitConverter.UInt32BitsToSingle(listFloat[value[value3I + i] - value[l]]) * block[l];
                }
            }
            block[0x80 - 1] = 0;
        }

        public void Decode4(int index, uint a, uint b, uint c, Channel c1)
        {
            if (type != 1 || c == 0) return;

            uint[] listInt = {
                0x40000000, 0x3FEDB6DB, 0x3FDB6DB7, 0x3FC92492, 0x3FB6DB6E, 0x3FA49249, 0x3F924925, 0x3F800000,
                0x3F5B6DB7, 0x3F36DB6E, 0x3F124925, 0x3EDB6DB7, 0x3E924925, 0x3E124925, 0x00000000, 0x00000000,
            };
            float f1 = BitConverter.UInt32BitsToSingle(listInt[c1.value2[index]]);
            float f2 = f1 - 2.0f;
            uint sIndex = b;
            //float* s = &block[b];
            uint dIndex = b;
            //float* d = c1.block[b];
            for (uint i = 0; i < a; i++)
            {
                c1.block[dIndex++] = block[sIndex] * f2;
                //*(d++) = *s * f2;
                block[sIndex++] = block[sIndex] * f1;
                //*(s++) = *s * f1;
            }
        }

        public void Decode5(int index)
        {
            uint[][] list1Int = new uint[7][];
            list1Int[0] = new uint[]
                {
                    0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75,
                    0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75,
                    0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75,
                    0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75,
                    0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75,
                    0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75,
                    0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75,
                    0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75, 0x3DA73D75,
                };
            list1Int[1] = new uint[]
            {
                    0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31,
                    0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31,
                    0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31,
                    0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31,
                    0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31,
                    0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31,
                    0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31,
                    0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31, 0x3F7B14BE, 0x3F54DB31,
            };
            list1Int[2] = new uint[]
            {
                    0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403, 0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403,
                    0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403, 0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403,
                    0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403, 0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403,
                    0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403, 0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403,
                    0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403, 0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403,
                    0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403, 0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403,
                    0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403, 0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403,
                    0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403, 0x3F7EC46D, 0x3F74FA0B, 0x3F61C598, 0x3F45E403,
            };
            list1Int[3] = new uint[]
            {
                    0x3F7FB10F, 0x3F7D3AAC, 0x3F7853F8, 0x3F710908, 0x3F676BD8, 0x3F5B941A, 0x3F4D9F02, 0x3F3DAEF9,
                    0x3F7FB10F, 0x3F7D3AAC, 0x3F7853F8, 0x3F710908, 0x3F676BD8, 0x3F5B941A, 0x3F4D9F02, 0x3F3DAEF9,
                    0x3F7FB10F, 0x3F7D3AAC, 0x3F7853F8, 0x3F710908, 0x3F676BD8, 0x3F5B941A, 0x3F4D9F02, 0x3F3DAEF9,
                    0x3F7FB10F, 0x3F7D3AAC, 0x3F7853F8, 0x3F710908, 0x3F676BD8, 0x3F5B941A, 0x3F4D9F02, 0x3F3DAEF9,
                    0x3F7FB10F, 0x3F7D3AAC, 0x3F7853F8, 0x3F710908, 0x3F676BD8, 0x3F5B941A, 0x3F4D9F02, 0x3F3DAEF9,
                    0x3F7FB10F, 0x3F7D3AAC, 0x3F7853F8, 0x3F710908, 0x3F676BD8, 0x3F5B941A, 0x3F4D9F02, 0x3F3DAEF9,
                    0x3F7FB10F, 0x3F7D3AAC, 0x3F7853F8, 0x3F710908, 0x3F676BD8, 0x3F5B941A, 0x3F4D9F02, 0x3F3DAEF9,
                    0x3F7FB10F, 0x3F7D3AAC, 0x3F7853F8, 0x3F710908, 0x3F676BD8, 0x3F5B941A, 0x3F4D9F02, 0x3F3DAEF9,
            };
            list1Int[4] = new uint[]
            {
                0x3F7FEC43, 0x3F7F4E6D, 0x3F7E1324, 0x3F7C3B28, 0x3F79C79D, 0x3F76BA07, 0x3F731447, 0x3F6ED89E,
                    0x3F6A09A7, 0x3F64AA59, 0x3F5EBE05, 0x3F584853, 0x3F514D3D, 0x3F49D112, 0x3F41D870, 0x3F396842,
                    0x3F7FEC43, 0x3F7F4E6D, 0x3F7E1324, 0x3F7C3B28, 0x3F79C79D, 0x3F76BA07, 0x3F731447, 0x3F6ED89E,
                    0x3F6A09A7, 0x3F64AA59, 0x3F5EBE05, 0x3F584853, 0x3F514D3D, 0x3F49D112, 0x3F41D870, 0x3F396842,
                    0x3F7FEC43, 0x3F7F4E6D, 0x3F7E1324, 0x3F7C3B28, 0x3F79C79D, 0x3F76BA07, 0x3F731447, 0x3F6ED89E,
                    0x3F6A09A7, 0x3F64AA59, 0x3F5EBE05, 0x3F584853, 0x3F514D3D, 0x3F49D112, 0x3F41D870, 0x3F396842,
                    0x3F7FEC43, 0x3F7F4E6D, 0x3F7E1324, 0x3F7C3B28, 0x3F79C79D, 0x3F76BA07, 0x3F731447, 0x3F6ED89E,
                    0x3F6A09A7, 0x3F64AA59, 0x3F5EBE05, 0x3F584853, 0x3F514D3D, 0x3F49D112, 0x3F41D870, 0x3F396842,
            };
            list1Int[5] = new uint[]
            {
                0x3F7FFB11, 0x3F7FD397, 0x3F7F84AB, 0x3F7F0E58, 0x3F7E70B0, 0x3F7DABCC, 0x3F7CBFC9, 0x3F7BACCD,
                    0x3F7A7302, 0x3F791298, 0x3F778BC5, 0x3F75DEC6, 0x3F740BDD, 0x3F721352, 0x3F6FF573, 0x3F6DB293,
                    0x3F6B4B0C, 0x3F68BF3C, 0x3F660F88, 0x3F633C5A, 0x3F604621, 0x3F5D2D53, 0x3F59F26A, 0x3F5695E5,
                    0x3F531849, 0x3F4F7A1F, 0x3F4BBBF8, 0x3F47DE65, 0x3F43E200, 0x3F3FC767, 0x3F3B8F3B, 0x3F373A23,
                    0x3F7FFB11, 0x3F7FD397, 0x3F7F84AB, 0x3F7F0E58, 0x3F7E70B0, 0x3F7DABCC, 0x3F7CBFC9, 0x3F7BACCD,
                    0x3F7A7302, 0x3F791298, 0x3F778BC5, 0x3F75DEC6, 0x3F740BDD, 0x3F721352, 0x3F6FF573, 0x3F6DB293,
                    0x3F6B4B0C, 0x3F68BF3C, 0x3F660F88, 0x3F633C5A, 0x3F604621, 0x3F5D2D53, 0x3F59F26A, 0x3F5695E5,
                    0x3F531849, 0x3F4F7A1F, 0x3F4BBBF8, 0x3F47DE65, 0x3F43E200, 0x3F3FC767, 0x3F3B8F3B, 0x3F373A23,
            };
            list1Int[6] = new uint[]
            {
                0x3F7FFEC4, 0x3F7FF4E6, 0x3F7FE129, 0x3F7FC38F, 0x3F7F9C18, 0x3F7F6AC7, 0x3F7F2F9D, 0x3F7EEA9D,
                    0x3F7E9BC9, 0x3F7E4323, 0x3F7DE0B1, 0x3F7D7474, 0x3F7CFE73, 0x3F7C7EB0, 0x3F7BF531, 0x3F7B61FC,
                    0x3F7AC516, 0x3F7A1E84, 0x3F796E4E, 0x3F78B47B, 0x3F77F110, 0x3F772417, 0x3F764D97, 0x3F756D97,
                    0x3F748422, 0x3F73913F, 0x3F7294F8, 0x3F718F57, 0x3F708066, 0x3F6F6830, 0x3F6E46BE, 0x3F6D1C1D,
                    0x3F6BE858, 0x3F6AAB7B, 0x3F696591, 0x3F6816A8, 0x3F66BECC, 0x3F655E0B, 0x3F63F473, 0x3F628210,
                    0x3F6106F2, 0x3F5F8327, 0x3F5DF6BE, 0x3F5C61C7, 0x3F5AC450, 0x3F591E6A, 0x3F577026, 0x3F55B993,
                    0x3F53FAC3, 0x3F5233C6, 0x3F5064AF, 0x3F4E8D90, 0x3F4CAE79, 0x3F4AC77F, 0x3F48D8B3, 0x3F46E22A,
                    0x3F44E3F5, 0x3F42DE29, 0x3F40D0DA, 0x3F3EBC1B, 0x3F3CA003, 0x3F3A7CA4, 0x3F385216, 0x3F36206C,
            };
            uint[][] list2Int = new uint[7][];
            list2Int[0] = new uint[]
                {
                    0xBD0A8BD4, 0x3D0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4,
                    0x3D0A8BD4, 0xBD0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4,
                    0x3D0A8BD4, 0xBD0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4,
                    0xBD0A8BD4, 0x3D0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4,
                    0x3D0A8BD4, 0xBD0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4,
                    0xBD0A8BD4, 0x3D0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4,
                    0xBD0A8BD4, 0x3D0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4,
                    0x3D0A8BD4, 0xBD0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4, 0x3D0A8BD4, 0x3D0A8BD4, 0xBD0A8BD4,
            };
            list2Int[1] = new uint[]
            {
                    0xBE47C5C2, 0xBF0E39DA, 0x3E47C5C2, 0x3F0E39DA, 0x3E47C5C2, 0x3F0E39DA, 0xBE47C5C2, 0xBF0E39DA,
                    0x3E47C5C2, 0x3F0E39DA, 0xBE47C5C2, 0xBF0E39DA, 0xBE47C5C2, 0xBF0E39DA, 0x3E47C5C2, 0x3F0E39DA,
                    0x3E47C5C2, 0x3F0E39DA, 0xBE47C5C2, 0xBF0E39DA, 0xBE47C5C2, 0xBF0E39DA, 0x3E47C5C2, 0x3F0E39DA,
                    0xBE47C5C2, 0xBF0E39DA, 0x3E47C5C2, 0x3F0E39DA, 0x3E47C5C2, 0x3F0E39DA, 0xBE47C5C2, 0xBF0E39DA,
                    0x3E47C5C2, 0x3F0E39DA, 0xBE47C5C2, 0xBF0E39DA, 0xBE47C5C2, 0xBF0E39DA, 0x3E47C5C2, 0x3F0E39DA,
                    0xBE47C5C2, 0xBF0E39DA, 0x3E47C5C2, 0x3F0E39DA, 0x3E47C5C2, 0x3F0E39DA, 0xBE47C5C2, 0xBF0E39DA,
                    0xBE47C5C2, 0xBF0E39DA, 0x3E47C5C2, 0x3F0E39DA, 0x3E47C5C2, 0x3F0E39DA, 0xBE47C5C2, 0xBF0E39DA,
                    0x3E47C5C2, 0x3F0E39DA, 0xBE47C5C2, 0xBF0E39DA, 0xBE47C5C2, 0xBF0E39DA, 0x3E47C5C2, 0x3F0E39DA,
            };
            list2Int[2] = new uint[]
            {
                    0xBDC8BD36, 0xBE94A031, 0xBEF15AEA, 0xBF226799, 0x3DC8BD36, 0x3E94A031, 0x3EF15AEA, 0x3F226799,
                    0x3DC8BD36, 0x3E94A031, 0x3EF15AEA, 0x3F226799, 0xBDC8BD36, 0xBE94A031, 0xBEF15AEA, 0xBF226799,
                    0x3DC8BD36, 0x3E94A031, 0x3EF15AEA, 0x3F226799, 0xBDC8BD36, 0xBE94A031, 0xBEF15AEA, 0xBF226799,
                    0xBDC8BD36, 0xBE94A031, 0xBEF15AEA, 0xBF226799, 0x3DC8BD36, 0x3E94A031, 0x3EF15AEA, 0x3F226799,
                    0x3DC8BD36, 0x3E94A031, 0x3EF15AEA, 0x3F226799, 0xBDC8BD36, 0xBE94A031, 0xBEF15AEA, 0xBF226799,
                    0xBDC8BD36, 0xBE94A031, 0xBEF15AEA, 0xBF226799, 0x3DC8BD36, 0x3E94A031, 0x3EF15AEA, 0x3F226799,
                    0xBDC8BD36, 0xBE94A031, 0xBEF15AEA, 0xBF226799, 0x3DC8BD36, 0x3E94A031, 0x3EF15AEA, 0x3F226799,
                    0x3DC8BD36, 0x3E94A031, 0x3EF15AEA, 0x3F226799, 0xBDC8BD36, 0xBE94A031, 0xBEF15AEA, 0xBF226799,
            };
            list2Int[3] = new uint[]
            {
                    0xBD48FB30, 0xBE164083, 0xBE78CFCC, 0xBEAC7CD4, 0xBEDAE880, 0xBF039C3D, 0xBF187FC0, 0xBF2BEB4A,
                    0x3D48FB30, 0x3E164083, 0x3E78CFCC, 0x3EAC7CD4, 0x3EDAE880, 0x3F039C3D, 0x3F187FC0, 0x3F2BEB4A,
                    0x3D48FB30, 0x3E164083, 0x3E78CFCC, 0x3EAC7CD4, 0x3EDAE880, 0x3F039C3D, 0x3F187FC0, 0x3F2BEB4A,
                    0xBD48FB30, 0xBE164083, 0xBE78CFCC, 0xBEAC7CD4, 0xBEDAE880, 0xBF039C3D, 0xBF187FC0, 0xBF2BEB4A,
                    0x3D48FB30, 0x3E164083, 0x3E78CFCC, 0x3EAC7CD4, 0x3EDAE880, 0x3F039C3D, 0x3F187FC0, 0x3F2BEB4A,
                    0xBD48FB30, 0xBE164083, 0xBE78CFCC, 0xBEAC7CD4, 0xBEDAE880, 0xBF039C3D, 0xBF187FC0, 0xBF2BEB4A,
                    0xBD48FB30, 0xBE164083, 0xBE78CFCC, 0xBEAC7CD4, 0xBEDAE880, 0xBF039C3D, 0xBF187FC0, 0xBF2BEB4A,
                    0x3D48FB30, 0x3E164083, 0x3E78CFCC, 0x3EAC7CD4, 0x3EDAE880, 0x3F039C3D, 0x3F187FC0, 0x3F2BEB4A,
            };
            list2Int[4] = new uint[]
            {
                    0xBCC90AB0, 0xBD96A905, 0xBDFAB273, 0xBE2F10A2, 0xBE605C13, 0xBE888E93, 0xBEA09AE5, 0xBEB8442A,
                    0xBECF7BCA, 0xBEE63375, 0xBEFC5D27, 0xBF08F59B, 0xBF13682A, 0xBF1D7FD1, 0xBF273656, 0xBF3085BB,
                    0x3CC90AB0, 0x3D96A905, 0x3DFAB273, 0x3E2F10A2, 0x3E605C13, 0x3E888E93, 0x3EA09AE5, 0x3EB8442A,
                    0x3ECF7BCA, 0x3EE63375, 0x3EFC5D27, 0x3F08F59B, 0x3F13682A, 0x3F1D7FD1, 0x3F273656, 0x3F3085BB,
                    0x3CC90AB0, 0x3D96A905, 0x3DFAB273, 0x3E2F10A2, 0x3E605C13, 0x3E888E93, 0x3EA09AE5, 0x3EB8442A,
                    0x3ECF7BCA, 0x3EE63375, 0x3EFC5D27, 0x3F08F59B, 0x3F13682A, 0x3F1D7FD1, 0x3F273656, 0x3F3085BB,
                    0xBCC90AB0, 0xBD96A905, 0xBDFAB273, 0xBE2F10A2, 0xBE605C13, 0xBE888E93, 0xBEA09AE5, 0xBEB8442A,
                    0xBECF7BCA, 0xBEE63375, 0xBEFC5D27, 0xBF08F59B, 0xBF13682A, 0xBF1D7FD1, 0xBF273656, 0xBF3085BB,
            };
            list2Int[5] = new uint[]
            {
                    0xBC490E90, 0xBD16C32C, 0xBD7B2B74, 0xBDAFB680, 0xBDE1BC2E, 0xBE09CF86, 0xBE22ABB6, 0xBE3B6ECF,
                    0xBE541501, 0xBE6C9A7F, 0xBE827DC0, 0xBE8E9A22, 0xBE9AA086, 0xBEA68F12, 0xBEB263EF, 0xBEBE1D4A,
                    0xBEC9B953, 0xBED53641, 0xBEE0924F, 0xBEEBCBBB, 0xBEF6E0CB, 0xBF00E7E4, 0xBF064B82, 0xBF0B9A6B,
                    0xBF10D3CD, 0xBF15F6D9, 0xBF1B02C6, 0xBF1FF6CB, 0xBF24D225, 0xBF299415, 0xBF2E3BDE, 0xBF32C8C9,
                    0x3C490E90, 0x3D16C32C, 0x3D7B2B74, 0x3DAFB680, 0x3DE1BC2E, 0x3E09CF86, 0x3E22ABB6, 0x3E3B6ECF,
                    0x3E541501, 0x3E6C9A7F, 0x3E827DC0, 0x3E8E9A22, 0x3E9AA086, 0x3EA68F12, 0x3EB263EF, 0x3EBE1D4A,
                    0x3EC9B953, 0x3ED53641, 0x3EE0924F, 0x3EEBCBBB, 0x3EF6E0CB, 0x3F00E7E4, 0x3F064B82, 0x3F0B9A6B,
                    0x3F10D3CD, 0x3F15F6D9, 0x3F1B02C6, 0x3F1FF6CB, 0x3F24D225, 0x3F299415, 0x3F2E3BDE, 0x3F32C8C9,
            };
            list2Int[6] = new uint[]
            {
                    0xBBC90F88, 0xBC96C9B6, 0xBCFB49BA, 0xBD2FE007, 0xBD621469, 0xBD8A200A, 0xBDA3308C, 0xBDBC3AC3,
                    0xBDD53DB9, 0xBDEE3876, 0xBE039502, 0xBE1008B7, 0xBE1C76DE, 0xBE28DEFC, 0xBE354098, 0xBE419B37,
                    0xBE4DEE60, 0xBE5A3997, 0xBE667C66, 0xBE72B651, 0xBE7EE6E1, 0xBE8586CE, 0xBE8B9507, 0xBE919DDD,
                    0xBE97A117, 0xBE9D9E78, 0xBEA395C5, 0xBEA986C4, 0xBEAF713A, 0xBEB554EC, 0xBEBB31A0, 0xBEC1071E,
                    0xBEC6D529, 0xBECC9B8B, 0xBED25A09, 0xBED8106B, 0xBEDDBE79, 0xBEE363FA, 0xBEE900B7, 0xBEEE9479,
                    0xBEF41F07, 0xBEF9A02D, 0xBEFF17B2, 0xBF0242B1, 0xBF04F484, 0xBF07A136, 0xBF0A48AD, 0xBF0CEAD0,
                    0xBF0F8784, 0xBF121EB0, 0xBF14B039, 0xBF173C07, 0xBF19C200, 0xBF1C420C, 0xBF1EBC12, 0xBF212FF9,
                    0xBF239DA9, 0xBF26050A, 0xBF286605, 0xBF2AC082, 0xBF2D1469, 0xBF2F61A5, 0xBF31A81D, 0xBF33E7BC,
            };
            uint[][] list3Int = new uint[2][];  // TODO : Merge arrays
            list3Int[0] = new uint[]
                {
                    0x3A3504F0, 0x3B0183B8, 0x3B70C538, 0x3BBB9268, 0x3C04A809, 0x3C308200, 0x3C61284C, 0x3C8B3F17,
                    0x3CA83992, 0x3CC77FBD, 0x3CE91110, 0x3D0677CD, 0x3D198FC4, 0x3D2DD35C, 0x3D434643, 0x3D59ECC1,
                    0x3D71CBA8, 0x3D85741E, 0x3D92A413, 0x3DA078B4, 0x3DAEF522, 0x3DBE1C9E, 0x3DCDF27B, 0x3DDE7A1D,
                    0x3DEFB6ED, 0x3E00D62B, 0x3E0A2EDA, 0x3E13E72A, 0x3E1E00B1, 0x3E287CF2, 0x3E335D55, 0x3E3EA321,
                    0x3E4A4F75, 0x3E56633F, 0x3E62DF37, 0x3E6FC3D1, 0x3E7D1138, 0x3E8563A2, 0x3E8C72B7, 0x3E93B561,
                    0x3E9B2AEF, 0x3EA2D26F, 0x3EAAAAAB, 0x3EB2B222, 0x3EBAE706, 0x3EC34737, 0x3ECBD03D, 0x3ED47F46,
                    0x3EDD5128, 0x3EE6425C, 0x3EEF4EFF, 0x3EF872D7, 0x3F00D4A9, 0x3F0576CA, 0x3F0A1D3B, 0x3F0EC548,
                    0x3F136C25, 0x3F180EF2, 0x3F1CAAC2, 0x3F213CA2, 0x3F25C1A5, 0x3F2A36E7, 0x3F2E9998, 0x3F32E705,
            };
            list3Int[1] = new uint[]
            {
                    0xBF371C9E, 0xBF3B37FE, 0xBF3F36F2, 0xBF431780, 0xBF46D7E6, 0xBF4A76A4, 0xBF4DF27C, 0xBF514A6F,
                    0xBF547DC5, 0xBF578C03, 0xBF5A74EE, 0xBF5D3887, 0xBF5FD707, 0xBF6250DA, 0xBF64A699, 0xBF66D908,
                    0xBF68E90E, 0xBF6AD7B1, 0xBF6CA611, 0xBF6E5562, 0xBF6FE6E7, 0xBF715BEF, 0xBF72B5D1, 0xBF73F5E6,
                    0xBF751D89, 0xBF762E13, 0xBF7728D7, 0xBF780F20, 0xBF78E234, 0xBF79A34C, 0xBF7A5397, 0xBF7AF439,
                    0xBF7B8648, 0xBF7C0ACE, 0xBF7C82C8, 0xBF7CEF26, 0xBF7D50CB, 0xBF7DA88E, 0xBF7DF737, 0xBF7E3D86,
                    0xBF7E7C2A, 0xBF7EB3CC, 0xBF7EE507, 0xBF7F106C, 0xBF7F3683, 0xBF7F57CA, 0xBF7F74B6, 0xBF7F8DB6,
                    0xBF7FA32E, 0xBF7FB57B, 0xBF7FC4F6, 0xBF7FD1ED, 0xBF7FDCAD, 0xBF7FE579, 0xBF7FEC90, 0xBF7FF22E,
                    0xBF7FF688, 0xBF7FF9D0, 0xBF7FFC32, 0xBF7FFDDA, 0xBF7FFEED, 0xBF7FFF8F, 0xBF7FFFDF, 0xBF7FFFFC,
            };

            //float* s, *d, *s1, *s2;
            int s = 0;
            ref float[] sTable = ref block;
            //s = block;
            int d = 0;
            ref float[] dTable = ref wav1;
            //d = wav1;
            for (int i = 0, count1 = 1, count2 = 0x40; i < 7; i++, count1 <<= 1, count2 >>= 1)
            {
                //float* d1 = d;
                int d1 = d;  // wav1
                //float* d2 = &d[count2];
                int d2 = d + count2;   // wav1[count2]
                for (int j = 0; j < count1; j++)
                {
                    for (int k = 0; k < count2; k++)
                    {
                        float a = sTable[s++];
                        float b = sTable[s++];
                        dTable[d1++] = b + a;
                        dTable[d2++] = a - b;
                    }
                    d1 += count2;
                    d2 += count2;
                }
                //float* w = &s[-0x80];
                int w = s - 0x80;
                ref float[] wTable = ref sTable;
                s = d;
                sTable = ref dTable;
                d = w;
                dTable = ref wTable;
            }
            s = 0;
            sTable = ref wav1;
            //s = wav1;
            d = 0;
            dTable = ref block;
            //d = block;
            for (int i = 0, count1 = 0x40, count2 = 1; i < 7; i++, count1 >>= 1, count2 <<= 1)
            {
                //auto* list1Float = (float*)list1Int[i];
                uint list1FloatI = 0;
                //auto* list2Float = (float*)list2Int[i];
                uint list2FloatI = 0;
                //float* p1 = s;
                int p1 = s;
                //float* p2 = &p1[count2];
                int p2 = p1 + count2;
                //float* d1 = d;
                int d1 = d;
                //float* d2 = &d1[count2 * 2 - 1];
                int d2 = d1 + (count2 * 2 - 1);
                for (int j = 0; j < count1; j++)
                {
                    for (int k = 0; k < count2; k++)
                    {
                        float a = sTable[p1++];
                        float b = sTable[p2++];
                        float c = BitConverter.UInt32BitsToSingle(list1Int[i][list1FloatI++]);
                        float p = BitConverter.UInt32BitsToSingle(list2Int[i][list2FloatI++]);
                        dTable[d1++] = a * c - b * p;
                        dTable[d2--] = a * p + b * c;
                    }
                    p1 += count2;
                    p2 += count2;
                    d1 += count2;
                    d2 += count2 * 3;
                }
                // swapping table at each loop iteration
                int w = s;
                ref float[] wTable = ref sTable;
                s = d;
                sTable = ref dTable;
                d = w;
                dTable = ref wTable;
            }
            d = 0;
            for (int i = 0; i < 0x80; i++) wav2[d++] = sTable[s++];
            //s = (float*)list3Int;
            s = 0;
            //d = wave[index];
            d = 0;
            //s1 = &wav2[0x40];
            uint s1 = 0x40;
            //s2 = wav3;
            uint s2 = 0;
            for (int i = 0; i < 0x40; i++) wave[index][d++] = wav2[s1++] * BitConverter.UInt32BitsToSingle(list3Int[0][s++]) + wav3[s2++];
            s = 0;
            for (int i = 0; i < 0x40; i++) wave[index][d++] = BitConverter.UInt32BitsToSingle(list3Int[1][s++]) * wav2[--s1] - wav3[s2++];
            // s pointer is at the end of the list3Int[1], next for will decrease it
            s = 0x40;
            //s1 = &wav2[0x40 - 1];
            s1 = 0x40 - 1;
            //s2 = wav3;
            s2 = 0;
            for (int i = 0; i < 0x40; i++) wav3[s2++] = wav2[s1--] * BitConverter.UInt32BitsToSingle(list3Int[1][--s]);
            s = 0x40;
            for (int i = 0; i < 0x40; i++) wav3[s2++] = BitConverter.UInt32BitsToSingle(list3Int[0][--s]) * wav2[++s1];
        }
    }

    public class ClData
    {
        private readonly byte[] data;
        private readonly int size;
        private int bit;

        public ClData(byte[] data, int size)
        {
            this.data = data;
            this.size = size * 8 - 16;
            bit = 0;
        }

        public byte[] GetData()
        {
            return data;
        }

        public int CheckBit(int bitSize)
        {
            int v = 0;
            if (bit + bitSize > size) return v;
            int[] mask = { 0xFFFFFF, 0x7FFFFF, 0x3FFFFF, 0x1FFFFF, 0x0FFFFF, 0x07FFFF, 0x03FFFF, 0x01FFFF };
            int dataOffset = bit >> 3;
            v = data[dataOffset];
            v = v << 8 | (dataOffset + 1 < data.Length ? data[dataOffset + 1] : 0);  // Only because sometimes it happens
            v = v << 8 | (dataOffset + 2 < data.Length ? data[dataOffset + 2] : 0);
            v &= mask[bit & 7];
            v >>= 24 - (bit & 7) - bitSize;
            return v;
        }

        public int GetBit(int bitSize)
        {
            int v = CheckBit(bitSize);
            bit += bitSize;
            return v;
        }

        public void AddBit(int bitSize)
        {
            bit += bitSize;
        }
    }
}