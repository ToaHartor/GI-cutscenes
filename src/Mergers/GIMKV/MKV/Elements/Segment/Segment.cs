namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Segment
{
    internal class Segment
    {
        private static byte[] _signature = Signatures.Segment;
        public SeekHead Head;
        private long _offset;
        private long _totalSize;

        public Segment(long position, long totalSize, long trackOffset, long cueOffset, long tagOffset, long attachmentOffset = 0)
        {
            _offset = position;
            _totalSize = totalSize;
            Head = new SeekHead();
            Head.Containers.Add(new Seek(Signatures.SegmentInfo, GIMKV.SegmentOffset - position - 12)); // removing header + signature + segment length + 1
            Head.Containers.Add(new Seek(Signatures.Tracks, trackOffset - position - 12));
            Head.Containers.Add(new Seek(Signatures.Cues, cueOffset - position - 12));
            Head.Containers.Add(new Seek(Signatures.Tags, tagOffset - position - 12));
            if (attachmentOffset > 0) Head.Containers.Add(new Seek(Signatures.SegmentInfo, attachmentOffset - position - 12));
        }

        public byte[] ToBytes()
        {
            List<byte> resBytes = new List<byte>(_signature);
            byte[] size = BitConverter.GetBytes(_totalSize - _offset);
            Array.Reverse(size);
            size[0] = 0x01;
            resBytes.AddRange(size);
            resBytes.AddRange(Head.ToBytes());
            return resBytes.ToArray();
        }
    }
}