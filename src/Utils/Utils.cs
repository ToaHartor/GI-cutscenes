
namespace CRIDemuxer.Utils
{
    internal class Tools
    {
        public static ushort Bswap(ushort v)
        {
            ushort r = (ushort)(v & 0xFF);
            r <<= 8;
            v >>= 8;
            r |= (ushort)(v & 0xFF);
            return r;
        }

        public static short Bswap(short v)
        {
            short r = (short)(v & 0xFF);
            r <<= 8;
            v >>= 8;
            r |= (short)(v & 0xFF);
            return r;
        }

        public static int Bswap(int v)
        {
            int r = v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            return r;
        }

        public static uint Bswap(uint v)
        {
            uint r = v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            return r;
        }

        public static long Bswap(long v)
        {
            long r = v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            return r;
        }

        public static ulong Bswap(ulong v)
        {
            ulong r = v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            r <<= 8;
            v >>= 8;
            r |= v & 0xFF;
            return r;
        }

        public static float Bswap(float v)
        {
            uint i = Bswap(BitConverter.SingleToUInt32Bits(v));
            return BitConverter.UInt32BitsToSingle(i);
        }
        public static uint Ceil2(uint a, uint b) { return (uint)(b > 0 ? a / b + (a % b != 0 ? 1 : 0) : 0); }
    }
}
