using System.Runtime.InteropServices;

namespace CRIDemuxer.FileTypes
{
    public struct WAVEriff // size 36
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] riff;
        public uint riffSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] wave;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] fmt;
        public uint fmtSize;
        public ushort fmtType;
        public ushort fmtChannelCount;
        public uint fmtSamplingRate;
        public uint fmtSamplesPerSec;
        public ushort fmtSamplingSize;
        public ushort fmtBitCount;

        public WAVEriff()
        {
            riff = "RIFF".ToCharArray();
            riffSize = 0;
            wave = "WAVE".ToCharArray();
            fmt = "fmt ".ToCharArray();
            fmtSize = 0x10;
            fmtType = 0;
            fmtChannelCount = 0;
            fmtSamplingRate = 0;
            fmtSamplesPerSec = 0;
            fmtSamplingSize = 0;
            fmtBitCount = 0;
        }
    }

    public struct WAVEsmpl // size 68
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] smpl;
        public uint smplSize;
        public uint manufacturer;
        public uint product;
        public uint samplePeriod;
        public uint MIDIUnityNote;
        public uint MIDIPitchFraction;
        public uint SMPTEFormat;
        public uint SMPTEOffset;
        public uint sampleLoops;
        public uint samplerData;
        public uint loop_Identifier;
        public uint loop_Type;
        public uint loop_Start;
        public uint loop_End;
        public uint loop_Fraction;
        public uint loop_PlayCount;

        public WAVEsmpl()
        {
            smpl = "smpl".ToCharArray();
            smplSize = 0x3C;
            manufacturer = 0;
            product = 0;
            samplePeriod = 0;
            MIDIUnityNote = 0x3C;
            MIDIPitchFraction = 0;
            SMPTEFormat = 0;
            SMPTEOffset = 0;
            sampleLoops = 1;
            samplerData = 0x18;
            loop_Identifier = 0;
            loop_Type = 0;
            loop_Start = 0;
            loop_End = 0;
            loop_Fraction = 0;
            loop_PlayCount = 0;
        }
    }
    public struct WAVEnote // size 12
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] note;
        public uint noteSize;
        public uint dwName;

        public WAVEnote()
        {
            note = "note".ToCharArray();
            noteSize = 0;
            dwName = 0;
        }
    }
    public struct WAVEdata // size 8
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] data;
        public uint dataSize;

        public WAVEdata()
        {
            data = "data".ToCharArray();
            dataSize = 0;
        }
    }

    public class WAV
    {
        public static byte[] ToByteArray(dynamic h)  // Should be one of the structs above
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
