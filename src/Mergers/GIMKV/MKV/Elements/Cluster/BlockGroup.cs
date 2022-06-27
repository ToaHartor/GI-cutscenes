using GICutscenes.Mergers.GIMKV.MKV.Generics;

namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Cluster
{
    internal class BlockGroup : IBlockContainer
    {
        private static readonly byte[] _signature = Signatures.BlockGroup;
        public Block Block;
        public MKVElement<int> BlockDuration;

        public BlockGroup(int trackNumber, int timestamp, byte[] data, int blockDuration)
        {
            Block = new Block(trackNumber, timestamp, 0x00, data);
            BlockDuration = new MKVElement<int>(Signatures.BlockDuration, blockDuration);
        }

        public byte[] ToBytes()
        {
            List<byte> resBytes = new List<byte>(Block.ToBytes());
            resBytes.AddRange(BlockDuration.ToBytes());

            resBytes.InsertRange(0, GIMKV.FieldLength((uint)resBytes.Count));
            resBytes.InsertRange(0, _signature);
            return resBytes.ToArray();
        }

        public uint Length()
        {
            return Block.Length() + (uint)BlockDuration.ToBytes().Length;
        }
    }
}