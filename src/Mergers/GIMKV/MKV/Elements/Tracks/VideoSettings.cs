using GICutscenes.Mergers.GIMKV.MKV.Generics;

namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Tracks
{
    internal class VideoSettings : MKVContainerElement
    {
        public MKVElement<uint> PixelWidth;
        public MKVElement<uint> PixelHeight;
        public MKVElement<byte[]> DisplayWidth; // !!!! Keep the zeroes in front of it (we should try without it, but there should be some zeroes before
        public MKVElement<byte[]> DisplayHeight; // If it doesn't work, then we should put a byte array to fix it (Big Endian of course)

        public VideoSettings(uint pixelWidth, uint pixelHeight, uint? displayWidth = null, uint? displayHeight = null) : base(Signatures.VideoSettings)
        {
            displayWidth ??= pixelWidth;
            displayHeight ??= pixelHeight;

            if (displayWidth == 0 || displayHeight == 0) throw new Exception("Display width and height can't be zero");  // According to the spec
            PixelWidth = new MKVElement<uint>(Signatures.PixelWidth, pixelWidth);
            PixelHeight = new MKVElement<uint>(Signatures.PixelHeight, pixelHeight);

            byte[] disWidth = BitConverter.GetBytes((uint)displayWidth);
            byte[] disHeight = BitConverter.GetBytes((uint)displayHeight);
            Array.Reverse(disWidth);
            Array.Reverse(disHeight);

            DisplayWidth = new MKVElement<byte[]>(Signatures.DisplayWidth, disWidth);
            DisplayHeight = new MKVElement<byte[]>(Signatures.DisplayHeight, disHeight);
        }
    }
}