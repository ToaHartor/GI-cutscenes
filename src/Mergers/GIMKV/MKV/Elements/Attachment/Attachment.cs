namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Attachment
{
    internal class Attachment
    {
        private static readonly byte[] Signature = Signatures.Attachment;
        private readonly List<AttachedFile> _attachedFiles;
        public Attachment()
        {
            _attachedFiles = new List<AttachedFile>();
        }

        public int Count() => _attachedFiles.Count;

        public void AddAttachment(string path, string description) =>
            _attachedFiles.Add(new AttachedFile(path, description));

        public void WriteBytes(FileStream fs)
        {
            uint totalLength = 0;
            foreach (AttachedFile file in _attachedFiles) totalLength+= file.Length();

            // Writing in file sequentially
            fs.Write(Signature);
            fs.Write(GIMKV.FieldLength(totalLength));
            foreach (AttachedFile file in _attachedFiles) file.WriteBytes(fs);
        }
    }
}