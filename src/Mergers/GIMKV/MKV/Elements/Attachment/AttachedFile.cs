using GICutscenes.Mergers.GIMKV.MKV.Generics;

namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Attachment
{
    internal class AttachedFile
    {
        private static readonly byte[] Signature = Signatures.AttachedFile;
        public MKVElement<string> FileName;
        public MKVElement<string> FileMimeType;
        private readonly FileInfo _file;
        public MKVElement<string> FileDescription;

        // Utils classes, to not have to recompute the byte arrays
        private readonly List<byte> _firstFields;
        public AttachedFile(string fontPath, string description)
        {
            if (!File.Exists(fontPath)) throw new FileNotFoundException($"Font at path {fontPath} doesn't exist.");
            FileName = new MKVElement<string>(Signatures.FileName, Path.GetFileName(fontPath));
            FileMimeType = new MKVElement<string>(Signatures.FileMimeType, "font/ttf");
            _file = new FileInfo(fontPath);
            FileDescription = new MKVElement<string>(Signatures.FileDescription, description);
            _firstFields = new List<byte>();
        }

        // Also generates the bytes to be written after, to save some actions and time
        public uint Length()
        {
            _firstFields.AddRange(FileName.ToBytes());
            _firstFields.AddRange(FileMimeType.ToBytes());
            _firstFields.AddRange(FileDescription.ToBytes());
            _firstFields.AddRange(Signatures.FileData);
            _firstFields.AddRange(GIMKV.FieldLength((uint)_file.Length));

            uint length = (uint) (_firstFields.Count + _file.Length);
            _firstFields.InsertRange(0, GIMKV.FieldLength(length));
            _firstFields.InsertRange(0, Signature);
            return (uint) (_firstFields.Count + _file.Length);
        }

        public void WriteBytes(FileStream fs)
        {
            fs.Write(_firstFields.ToArray());
            _file.OpenRead().CopyTo(fs);
        }
    }
}