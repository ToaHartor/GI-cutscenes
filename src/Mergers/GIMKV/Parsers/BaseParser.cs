namespace GICutscenes.Mergers.GIMKV.Parsers
{
    internal abstract class BaseParser
    {
        public float FrameDuration;
        public ulong TotalDataBytes;
        public uint FramesRead;
        public ulong Duration;
        public BaseParser()
        {
            FramesRead = 0;
            TotalDataBytes = 0;
            Duration = 0;
        }

        public virtual int PeekTimestamp(uint? index = null) => HasRemainingFrames(index) ? (int)Math.Round(FrameDuration * (index ?? FramesRead)) : -1;

        public virtual bool HasRemainingFrames(uint? index = null) => false;

        public virtual uint PeekLength() => 0;

        public virtual bool IsKeyframe() => false;

        public virtual uint DataSizeUntilTimestamp(int timestamp, out int newFrames)
        {
            newFrames = 0;
            return 0;
        }

        public virtual byte[] ReadBlock() => new byte[] { };

        public abstract void FreeParser();
    }
}