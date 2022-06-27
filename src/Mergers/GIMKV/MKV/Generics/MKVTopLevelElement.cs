namespace GICutscenes.Mergers.GIMKV.MKV.Generics
{
    internal abstract class MKVTopLevelElement : MKVContainerElement
    {
        public List<MKVContainerElement> Containers;

        protected MKVTopLevelElement(byte[] signature) : base(signature)
        {
            Containers = new List<MKVContainerElement>();
        }

        public new virtual byte[] ToBytes()
        {
            List<byte> byteRes = new();
            foreach (MKVContainerElement c in Containers) byteRes.AddRange(c.ToBytes());

            byteRes.InsertRange(0, GIMKV.FieldLength((uint)byteRes.Count));
            byteRes.InsertRange(0, _signature);
            return byteRes.ToArray();
        }
    }
}