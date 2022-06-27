namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Cluster
{
    internal class Block : SimpleBlock
    {
        public Block(int trackNumber, int timestamp, byte flag, byte[] data) : base(trackNumber, timestamp, flag, data)
        {
            Signature = Signatures.Block;
        }
    }
}