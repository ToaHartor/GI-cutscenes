namespace GICutscenes.Mergers.GIMKV.MKV.Elements
{
    internal class VoidElement
    {
        private long _elementSize;

        public VoidElement(long elementSize)
        {
            // 0x1037 - 0x79
            _elementSize = elementSize;
        }

        public byte[] VoidBytes()
        {
            List<byte> vBytes = new List<byte>(Signatures.EBMLVoidElement);
            // Data length
            byte[] voidArray = new byte[_elementSize - 3];
            byte[] lengthBytes = BitConverter.GetBytes(0x4000 + _elementSize - 3);
            Array.Reverse(lengthBytes);
            vBytes.AddRange(GIMKV.TrimZeroes(lengthBytes));
            vBytes.AddRange(voidArray);
            return vBytes.ToArray();
        }
    }
}