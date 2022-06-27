namespace GICutscenes.Mergers.GIMKV.MKV
{
    // Field signatures according to the matroska v4 spec
    internal static class Signatures
    {
        // Header
        public static readonly byte[] Header = { 0x1A, 0x45, 0xDF, 0xA3 };

        public static readonly byte[] EBMLVersion = { 0x42, 0x86 };
        public static readonly byte[] EBMLReadVersion = { 0x42, 0xF7 };
        public static readonly byte[] EBMLMaxIDLength = { 0x42, 0xF2 };
        public static readonly byte[] EBMLMaxSizeLength = { 0x42, 0xF3 };
        public static readonly byte[] Doctype = { 0x42, 0x82 };
        public static readonly byte[] DoctypeVersion = { 0x42, 0x87 };
        public static readonly byte[] DoctypeReadVersion = { 0x42, 0x85 };

        // Void Element
        public static readonly byte[] EBMLVoidElement = { 0xEC };

        // Segment
        public static readonly byte[] Segment = { 0x18, 0x53, 0x80, 0x67 };

        public static readonly byte[] SeekHead = { 0x11, 0x4D, 0x9B, 0x74 };
        public static readonly byte[] Seek = { 0x4D, 0xBB };
        public static readonly byte[] SeekId = { 0x53, 0xAB };
        public static readonly byte[] SeekPosition = { 0x53, 0xAC };

        // Segment Info
        public static readonly byte[] SegmentInfo = { 0x15, 0x49, 0xA9, 0x66 };

        public static readonly byte[] TimestampScale = { 0x2A, 0xD7, 0xB1 };
        public static readonly byte[] MuxingApp = { 0x4D, 0x80 };
        public static readonly byte[] WritingApp = { 0x57, 0x41 };
        public static readonly byte[] Duration = { 0x44, 0x89 };
        public static readonly byte[] DateUTC = { 0x44, 0x61 };
        public static readonly byte[] SegmentUID = { 0x73, 0xA4 };

        // Tracks
        public static readonly byte[] Tracks = { 0x16, 0x54, 0xAE, 0x6B };

        public static readonly byte[] TrackEntry = { 0xAE };
        public static readonly byte[] TrackNumber = { 0xD7 };
        public static readonly byte[] TrackUID = { 0x73, 0xC5 };
        public static readonly byte[] TrackType = { 0x83 };
        public static readonly byte[] FlagDefault = { 0x88 };
        public static readonly byte[] FlagLacing = { 0x9C };
        public static readonly byte[] CodecID = { 0x86 };
        public static readonly byte[] CodecPrivate = { 0x63, 0xA2 };
        public static readonly byte[] DefaultDuration = { 0x23, 0xE3, 0x83 };
        public static readonly byte[] LanguageIETF = { 0x22, 0xB5, 0x9D };
        public static readonly byte[] Language = { 0x22, 0xB5, 0x9C };
        public static readonly byte[] Name = { 0x53, 0x6E };

        // Video Specific
        public static readonly byte[] VideoSettings = { 0xE0 };

        public static readonly byte[] PixelWidth = { 0xB0 };
        public static readonly byte[] PixelHeight = { 0xBA };
        public static readonly byte[] DisplayWidth = { 0x54, 0xB0 };
        public static readonly byte[] DisplayHeight = { 0x54, 0xBA };

        // Audio Specific
        public static readonly byte[] AudioSettings = { 0xE1 };

        public static readonly byte[] SamplingFrequency = { 0xB5 };
        public static readonly byte[] Channels = { 0x9F };
        public static readonly byte[] BitDepth = { 0x62, 0x64 };

        // Attachments
        public static readonly byte[] Attachment = { 0x19, 0x41, 0xA4, 0x69 };

        public static readonly byte[] AttachedFile = { 0x61, 0xA7 };
        public static readonly byte[] FileName = { 0x46, 0x6E };
        public static readonly byte[] FileMimeType = { 0x46, 0x60 };
        public static readonly byte[] FileData = { 0x46, 0x5C };
        public static readonly byte[] FileDescription = { 0x46, 0x7E };

        // Cluster
        public static readonly byte[] Cluster = { 0x1F, 0x43, 0xB6, 0x75 };

        public static readonly byte[] Timestamp = { 0xE7 };
        public static readonly byte[] SimpleBlock = { 0xA3 };
        public static readonly byte[] BlockGroup = { 0xA0 };
        public static readonly byte[] Block = { 0xA1 };
        public static readonly byte[] BlockDuration = { 0x9B };

        // Cues
        public static readonly byte[] Cues = { 0x1C, 0x53, 0xBB, 0x6B };

        public static readonly byte[] CuePoint = { 0xBB };
        public static readonly byte[] CueTime = { 0xB3 };
        public static readonly byte[] CueTrackPositions = { 0xB7 };
        public static readonly byte[] CueTrack = { 0xF7 };
        public static readonly byte[] CueClusterPosition = { 0xF1 };
        public static readonly byte[] CueRelativePosition = { 0xF0 };
        public static readonly byte[] CueDuration = { 0xB2 };

        // Tags
        public static readonly byte[] Tags = { 0x12, 0x54, 0xC3, 0x67 };

        public static readonly byte[] Tag = { 0x73, 0x73 };
        public static readonly byte[] Targets = { 0x63, 0xC0 };
        public static readonly byte[] TargetTypeValue = { 0x68, 0xCA };
        public static readonly byte[] TargetTypeUID = { 0x63, 0xC5 };
        public static readonly byte[] SimpleTag = { 0x67, 0xC8 };
        public static readonly byte[] TagName = { 0x45, 0xA3 };
        public static readonly byte[] TagString = { 0x44, 0x87 };
    }
}