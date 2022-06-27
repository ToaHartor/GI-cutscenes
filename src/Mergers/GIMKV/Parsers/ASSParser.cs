using System.Text;

namespace GICutscenes.Mergers.GIMKV.Parsers
{
    internal class ASSParser : BaseParser
    {
        public string Header;
        private readonly List<Dialogue> _dialogues;
        private readonly StreamReader _stream;

        public ASSParser(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"File {path} does not exist.");
            _stream = new StreamReader(new FileStream(path, FileMode.Open), Encoding.UTF8);
            _dialogues = new List<Dialogue>();

            string ln;
            List<string> headerList = new();
            while ((ln = _stream.ReadLine()) != null && !ln.StartsWith("Dialogue:")) headerList.Add(ln);
            if (headerList.Count == 0) throw new Exception($"Subtitle file {path} has no header.");

            Header = string.Join(Environment.NewLine, headerList);
            // Entering the Dialogue section
            _dialogues.Add(new Dialogue(ln));
            while ((ln = _stream.ReadLine()) != null && ln.StartsWith("Dialogue:")) _dialogues.Add(new Dialogue(ln));

            Duration = (ulong)Math.Round(_dialogues[^1].End.TotalMilliseconds - _dialogues[0].Begin.TotalMilliseconds); // Difference between first timestamp and last one
        }

        public override int PeekTimestamp(uint? index = null) => HasRemainingFrames(index) ? (int)Math.Round(_dialogues[(int)(index ?? FramesRead)].Begin.TotalMilliseconds) : -1;

        public int PeekEndTimestamp(uint? index = null) => HasRemainingFrames(index) ? (int)Math.Round(_dialogues[(int)(index ?? FramesRead)].End.TotalMilliseconds) : -1;

        public uint GetDialogueLength(uint? index = null) => (uint)_dialogues[(int)(index ?? FramesRead)].ToBytes(index ?? FramesRead).Length;

        public override uint DataSizeUntilTimestamp(int timestamp, out int newFrames)
        {
            newFrames = 0;
            uint currentIndex = FramesRead;
            uint length = 0;
            int ts = PeekTimestamp();
            while (timestamp > ts && currentIndex < _dialogues.Count) // When ts will be out of bound, it will be equal to -1 but  currentIndex will be equal to dialogues.Count
            {
                length += GetDialogueLength(currentIndex);
                newFrames++;
                currentIndex++;
                ts = PeekTimestamp(currentIndex);
            }
            return length;
        }

        public override bool HasRemainingFrames(uint? index = null) => _dialogues.Count > (index ?? FramesRead);

        public override bool IsKeyframe() => true;

        public override byte[] ReadBlock()
        {
            if (!HasRemainingFrames()) throw new Exception("No lines left can be read");
            byte[] block = _dialogues[(int)FramesRead].ToBytes(FramesRead);
            FramesRead++;
            TotalDataBytes += (uint)block.Length;
            return block;
        }

        public override uint PeekLength()
        {
            if (!HasRemainingFrames()) throw new Exception("Tried to peek the next block but we reached the end of the file");
            return (uint)_dialogues[(int)FramesRead].ToBytes(FramesRead).Length;
        }

        public override void FreeParser() => _stream.Dispose();
    }

    internal readonly struct Dialogue
    {
        public readonly TimeSpan Begin;
        public readonly TimeSpan End;

        // Final fields
        private readonly string _layer;

        private readonly string _style;
        private readonly string _name;
        private readonly string _marginL;
        private readonly string _marginR;
        private readonly string _marginV;
        private readonly string _effect;
        private readonly string _text;

        public Dialogue(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 10) throw new Exception($"Provided subtitles file has a line wrongly formatted. Line : {line}");
            _layer = parts[0][10..];
            Begin = TimeSpan.Parse(parts[1]);
            End = TimeSpan.Parse(parts[2]);
            _style = parts[3];
            _name = parts[4];
            _marginL = parts[5];
            _marginR = parts[6];
            _marginV = parts[7];
            _effect = parts[8];
            _text = string.Join(',', parts[9..]);
        }

        public byte[] ToBytes(uint readOrder)
        {
            string line = $"{readOrder},{_layer},{_style},{_name},{_marginL},{_marginR},{_marginV},{_effect},{_text}";
            return Encoding.UTF8.GetBytes(line);
        }
    }
}