namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Cluster
{
    internal class SimpleBlock : IBlockContainer
    {
        public byte[] Signature;
        private readonly byte _trackNumber;
        private readonly ushort _timestamp; // Relative timestamp
        private readonly byte _flags;
        private readonly byte[] _data;
        private readonly byte? _frames;

        public SimpleBlock(int trackNumber, int timestamp, byte flags, byte[] data, byte? frameNumber = null)
        {
            Signature = Signatures.SimpleBlock;
            _trackNumber = (byte)(trackNumber + 0x80);
            _timestamp = (ushort)timestamp;  // Always of size two
            _flags = flags;
            _data = data;
            _frames = (byte?)(frameNumber - 0x01);
        }

        public virtual uint Length()
        {
            byte[] size = BitConverter.GetBytes(_timestamp); // Timestamp is BE, should also be trimmed
            Array.Reverse(size);
            // Data + timestamp + frames (if not null) + flags + tracknumber
            uint payloadLength = (uint)_data.Length + 2 + (uint)(_frames != null ? 1 : 0) + 2;
            return (uint)(Signature.Length + GIMKV.FieldLength(payloadLength).Length + payloadLength);
        }

        public virtual byte[] ToBytes()
        {
            List<byte> fieldBytes = new(_data);

            if (_frames != null) fieldBytes.Insert(0, (byte)_frames);
            fieldBytes.Insert(0, _flags);
            // Timestamp to bytes
            byte[] size = BitConverter.GetBytes(_timestamp); // Timestamp is BE
            Array.Reverse(size);
            fieldBytes.InsertRange(0, size);

            fieldBytes.Insert(0, _trackNumber);

            byte[] fieldLength = GIMKV.FieldLength((uint)fieldBytes.Count);
            fieldBytes.InsertRange(0, fieldLength);
            fieldBytes.InsertRange(0, Signature);
            return fieldBytes.ToArray();
        }
    }
}