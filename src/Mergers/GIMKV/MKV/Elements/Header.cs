using GICutscenes.Mergers.GIMKV.MKV.Generics;

namespace GICutscenes.Mergers.GIMKV.MKV.Elements
{
    internal class Header : MKVContainerElement
    {
        public MKVElement<byte> EBMLVersion;
        public MKVElement<byte> EBMLReadVersion;
        public MKVElement<byte> EBMLMaxIDLength;
        public MKVElement<byte> EBMLMaxSizeLength;
        public MKVElement<string> Doctype;
        public MKVElement<byte> DoctypeVersion;
        public MKVElement<byte> DoctypeReadVersion;

        public Header() : base(Signatures.Header)
        {
            EBMLVersion = new MKVElement<byte>(Signatures.EBMLVersion, 0x01);
            EBMLReadVersion = new MKVElement<byte>(Signatures.EBMLReadVersion, 0x01);
            EBMLMaxIDLength = new MKVElement<byte>(Signatures.EBMLMaxIDLength, 0x04);
            EBMLMaxSizeLength = new MKVElement<byte>(Signatures.EBMLMaxSizeLength, 0x08);
            Doctype = new MKVElement<string>(Signatures.Doctype, "matroska");
            DoctypeVersion = new MKVElement<byte>(Signatures.DoctypeVersion, 0x04);
            DoctypeReadVersion = new MKVElement<byte>(Signatures.DoctypeReadVersion, 0x02);
        }
    }
}