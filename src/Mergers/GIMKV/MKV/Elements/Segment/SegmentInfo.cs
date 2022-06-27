using GICutscenes.Mergers.GIMKV.MKV.Generics;

namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Segment
{
    internal class SegmentInfo : MKVContainerElement
    {
        //private byte[] _signature;
        public MKVElement<uint> TimestampScale;

        public MKVElement<string> MuxingApp;
        public MKVElement<string> WritingApp;
        public MKVElement<float> Duration;
        public MKVElement<long> DateUTC;
        public MKVElement<byte[]> SegmentUID;

        public SegmentInfo(string appName, float fileDuration, long dateUTCLong, byte[] segmentUid) : base(Signatures.SegmentInfo)
        {
            TimestampScale = new MKVElement<uint>(Signatures.TimestampScale, GIMKV.TimestampScale);
            MuxingApp = new MKVElement<string>(Signatures.MuxingApp, appName);
            WritingApp = new MKVElement<string>(Signatures.WritingApp, appName);
            Duration = new MKVElement<float>(Signatures.Duration, fileDuration);
            DateUTC = new MKVElement<long>(Signatures.DateUTC, dateUTCLong);
            SegmentUID = new MKVElement<byte[]>(Signatures.SegmentUID, segmentUid);
        }
    }
}