using GICutscenes.Mergers.GIMKV.MKV.Generics;

namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Cluster
{
    internal class Cluster
    {
        public MKVElement<int> Timestamp;
        public List<IBlockContainer> Blocks;

        private uint currentSize;
        private readonly int timestamp;

        public Cluster(int timestamp)
        {
            Timestamp = new MKVElement<int>(Signatures.Timestamp, timestamp);
            Blocks = new List<IBlockContainer>();
            currentSize = (uint)Timestamp.ToBytes().Length; // Cluster currently only contains the timestamp
            this.timestamp = timestamp;
        }

        public byte[] ToBytes()
        {
            List<byte> byteRes = new List<byte>(Timestamp.ToBytes());
            foreach (IBlockContainer b in Blocks) byteRes.AddRange(b.ToBytes());
            byteRes.InsertRange(0, GIMKV.FieldLength((uint)byteRes.Count));
            byteRes.InsertRange(0, Signatures.Cluster);

            return byteRes.ToArray();
        }

        // Returns the blocks position in the cluster
        public uint AddSimpleBlock(int trackNumber, int trackTimestamp, byte flags, byte[] data, byte? frameNumber = null)
        {
            uint position = currentSize;
            int relativeTimestamp = trackTimestamp - timestamp;
            SimpleBlock newS = new(trackNumber, relativeTimestamp, flags, data, frameNumber);
            Blocks.Add(newS);
            currentSize += newS.Length();
            return position;
        }

        public uint AddBlockGroup(int trackNumber, int trackTimestamp, byte[] data, int duration)
        {
            uint position = currentSize;
            int relativeTimestamp = trackTimestamp - timestamp;
            BlockGroup newBG = new(trackNumber, relativeTimestamp, data, duration);
            Blocks.Add(newBG);
            currentSize += newBG.Length();
            return position;
        }

        // Verify that the cluster won't exceed 5MB or have a length exceeding 5s
        public bool CanBeAdded(int finalTimestamp, uint totalDataSize, int videoFrames = 0, int audioFrames = 0, int subsFrames = 0) => (finalTimestamp - timestamp) < GIMKV.ClusterSize && (totalDataSize + currentSize + 9 * videoFrames + 10 * audioFrames + 10 * subsFrames) < GIMKV.ClusterTimeLength;

        // simple block is 5 (4 for the size), 4 for the data header of video, 5 for audio / subtitles will be 10 because of the blockGroup headers
    }
}