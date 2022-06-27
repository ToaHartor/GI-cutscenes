namespace GICutscenes.Mergers.GIMKV.Parsers
{
    internal class IVFParser : BaseParser
    {
        public struct Header
        {
            public ushort version;
            public ushort headerLength;
            public char[] codec;  // VP90
            public ushort width;
            public ushort height;
            public uint framerate;  // framerate / timescale = fps -> 30000 / 1000 -> 30fps
            public uint timescale;
            public uint frames;
        }

        public Header IVFHeader;
        public readonly uint FPS;

        //public float FrameDuration; // In milliseconds
        private readonly BinaryReader _stream;

        public IVFParser(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"File {path} does not exist.");
            _stream = new BinaryReader(new FileStream(path, FileMode.Open));
            if (_stream.ReadUInt32() != 0x46494B44) throw new Exception("Wrong file type given.");
            IVFHeader.version = _stream.ReadUInt16();
            IVFHeader.headerLength = _stream.ReadUInt16();
            IVFHeader.codec = _stream.ReadChars(4);
            IVFHeader.width = _stream.ReadUInt16();
            IVFHeader.height = _stream.ReadUInt16();
            IVFHeader.framerate = _stream.ReadUInt32();
            IVFHeader.timescale = _stream.ReadUInt32();
            IVFHeader.frames = _stream.ReadUInt32();
            _stream.ReadBytes(IVFHeader.headerLength - 28); // _stream.BaseStream.Position | Skipping unused bytes at the end

            FPS = IVFHeader.framerate / IVFHeader.timescale;
            Duration = (ulong)Math.Round((float)IVFHeader.frames / FPS * 1000f);
            FrameDuration = 1f / FPS * 1000;
        }

        public override byte[] ReadBlock()
        {
            if (!HasRemainingFrames()) throw new Exception("Reached end of file before parsing every frame.");
            uint length = _stream.ReadUInt32();
            _stream.ReadUInt64();  // Timestamp
            FramesRead++;
            TotalDataBytes += length;
            return _stream.ReadBytes((int)length);
        }

        public override uint PeekLength()
        {
            if (!HasRemainingFrames()) throw new Exception("Tried to peek the next block but we reached the end of the file");
            uint length = _stream.ReadUInt32();
            _stream.BaseStream.Position -= 4;
            return length;
        }

        public override uint DataSizeUntilTimestamp(int timestamp, out int newFrames)
        {
            newFrames = 0;
            long currentPos = _stream.BaseStream.Position;
            uint length = 0;
            int ts = PeekTimestamp();
            while (timestamp > ts && _stream.BaseStream.Length > _stream.BaseStream.Position)
            {
                uint len = _stream.ReadUInt32();
                length += len;
                newFrames++;
                ts += (int)Math.Round(FrameDuration);
                _stream.BaseStream.Position += len + 8; // + 8 because it's the size in byte of the timestamp
            }
            // Coming back to former pos
            _stream.BaseStream.Position = currentPos;
            return length;
        }

        public override bool HasRemainingFrames(uint? index = null) => (index ?? FramesRead) < IVFHeader.frames;

        public override bool IsKeyframe() => FramesRead % (FPS * 2) == 0;  // Every two seconds, we consider that the frame is a keyframe

        public override void FreeParser() => _stream.Dispose();

    }

    //internal struct IVFBlock
    //{
    //    public uint Length { get; }
    //    public ulong FrameNumber { get; } // Frame number
    //    public byte[] Data { get; }

    //    public IVFBlock(uint length, ulong frameNumber, byte[] data)
    //    {
    //        Length = length;
    //        FrameNumber = frameNumber;
    //        Data = data;
    //    }
    //}
}