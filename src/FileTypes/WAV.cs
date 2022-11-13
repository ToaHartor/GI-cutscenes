using System.Runtime.InteropServices;

namespace GICutscenes.FileTypes
{
    // Common interface for these types to be able to correctly marshal them (instead of using a dynamic type)
    internal interface IWavStruct { }

    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 36)]
    internal struct WAVEriff : IWavStruct // size 36
    {
        public uint riff = BitConverter.ToUInt32("RIFF"u8);
        public uint riffSize;
        public uint wave = BitConverter.ToUInt32("WAVE"u8);
        public uint fmt = BitConverter.ToUInt32("fmt "u8);
        public uint fmtSize = 0x10;
        public ushort fmtType;
        public ushort fmtChannelCount;
        public uint fmtSamplingRate;
        public uint fmtSamplesPerSec;
        public ushort fmtSamplingSize;
        public ushort fmtBitCount;

        public WAVEriff()
        {

        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 68)]
    internal struct WAVEsmpl : IWavStruct // size 68
    {
        public uint smpl = BitConverter.ToUInt32("smpl"u8);
        public uint smplSize = 0x3C;
        public uint manufacturer;
        public uint product;
        public uint samplePeriod;
        public uint MIDIUnityNote = 0x3C;
        public uint MIDIPitchFraction;
        public uint SMPTEFormat;
        public uint SMPTEOffset;
        public uint sampleLoops = 1;
        public uint samplerData = 0x18;
        public uint loop_Identifier;
        public uint loop_Type;
        public uint loop_Start;
        public uint loop_End;
        public uint loop_Fraction;
        public uint loop_PlayCount;

        public WAVEsmpl()
        {

        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 12)]
    internal struct WAVEnote : IWavStruct // size 12
    {
        public uint note = BitConverter.ToUInt32("note"u8);
        public uint noteSize;
        public uint dwName;

        public WAVEnote()
        {

        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    internal struct WAVEdata : IWavStruct // size 8
    {
        public uint data = BitConverter.ToUInt32("data"u8);
        public uint dataSize;

        public WAVEdata()
        {

        }
    }

    internal class WAV
    {
        public static byte[] ToByteArray(IWavStruct h)  // Should be one of the structs above
        {
            int size = Marshal.SizeOf(h);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(h, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
    }
}
