
using System.Net;

namespace GICutscenes.Utils
{
    internal class Tools
    {
        public static ushort Bswap(ushort v)
        {
            return (ushort)IPAddress.HostToNetworkOrder((short)v);
        }

        public static short Bswap(short v)
        {
            return IPAddress.HostToNetworkOrder(v);
        }

        public static int Bswap(int v)
        {
            return IPAddress.HostToNetworkOrder(v);
        }

        public static uint Bswap(uint v)
        {
            return (uint)IPAddress.HostToNetworkOrder((int)v);
        }

        public static long Bswap(long v)
        {
            return IPAddress.HostToNetworkOrder(v);
        }

        public static ulong Bswap(ulong v)
        {
            return (ulong)IPAddress.HostToNetworkOrder((long)v);
        }

        public static float Bswap(float v)
        {
            if (!BitConverter.IsLittleEndian) return v;
            uint i = Bswap(BitConverter.SingleToUInt32Bits(v));
            return BitConverter.UInt32BitsToSingle(i);
        }

        public static uint Ceil2(uint a, uint b) { return (uint)(b > 0 ? a / b + (a % b != 0 ? 1 : 0) : 0); }
    }
}
