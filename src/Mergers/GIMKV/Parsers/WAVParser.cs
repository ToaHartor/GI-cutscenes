namespace GICutscenes.Mergers.GIMKV.Parsers
{
    internal class WAVParser : BaseParser
    {
        public struct Header
        {
            public uint riffSize;
            public uint fmtSize;
            public ushort type;
            public ushort channelCount;
            public uint samplingRate;
            public uint samplesPerSec;
            public ushort samplingSize;
            public ushort bitCount;
            public uint dataSize;
        }

        public static readonly int MaxFramesInSameBlock = 8;
        public static readonly uint FPS = 25;

        public Header WavHeader;

        private readonly BinaryReader _stream;
        private long _bytesRead;
        public readonly uint FrameSize;

        public WAVParser(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"File {path} does not exist.");
            _stream = new BinaryReader(new FileStream(path, FileMode.Open));
            // Matching RIFF
            if (_stream.ReadUInt32() != 0x46464952) throw new Exception("Wrong file type given : bad magic numbers.");
            WavHeader.riffSize = _stream.ReadUInt32();
            if (_stream.ReadUInt32() != 0x45564157) throw new Exception("Wrong file type given.");
            if (_stream.ReadUInt32() != 0x20746D66) throw new Exception("fmt field couldn't be properly found");
            WavHeader.fmtSize = _stream.ReadUInt32();
            WavHeader.type = _stream.ReadUInt16();
            WavHeader.channelCount = _stream.ReadUInt16();
            WavHeader.samplingRate = _stream.ReadUInt32();
            WavHeader.samplesPerSec = _stream.ReadUInt32();
            WavHeader.samplingSize = _stream.ReadUInt16();
            WavHeader.bitCount = _stream.ReadUInt16();
            if (_stream.ReadUInt32() != 0x61746164) throw new Exception("data field couldn't be properly found");
            WavHeader.dataSize = _stream.ReadUInt32();

            Duration = (uint)Math.Round(WavHeader.dataSize * 8f / (WavHeader.bitCount * WavHeader.samplingRate * WavHeader.channelCount) * 1000f);
            FrameSize = WavHeader.samplesPerSec / FPS;
            FrameDuration = 1f / FPS * 1000;
            TotalDataBytes = WavHeader.dataSize;
        }

        public override bool HasRemainingFrames(uint? index = null) => (index ?? _bytesRead) < WavHeader.dataSize;

        public int PossibleFullFramesNumber(int audioFrames) // Returns only full frames
        {
            while (_stream.BaseStream.Position + audioFrames * FrameSize > _stream.BaseStream.Length) audioFrames--;
            return audioFrames;
        }

        // audioFrames here is always checked with PossibleFullFramesNumber before
        public uint NextFullAudioFramesLength(int audioFrames) => (uint)audioFrames * FrameSize;

        public override byte[] ReadBlock()
        {
            if (!HasRemainingFrames()) throw new Exception("No bytes left can be read");
            _bytesRead += FrameSize;
            FramesRead++;
            return _stream.ReadBytes((int)FrameSize); // If we reach the end, we will have less bytes than usual
        }

        public byte[] ReadMultipleBlocks(int blockNumber)
        {
            FramesRead += (uint)blockNumber;
            _bytesRead += FrameSize * blockNumber;
            return _stream.ReadBytes((int)(FrameSize * blockNumber));
        }

        public override uint PeekLength()
        {
            if (!HasRemainingFrames()) throw new Exception("Tried to peek the next block but we reached the end of the file");
            long remainingLength = _stream.BaseStream.Length - _stream.BaseStream.Position;
            return (uint)((remainingLength < FrameSize) ? remainingLength : FrameSize);
        }
        public override void FreeParser() => _stream.Dispose();

    }
}