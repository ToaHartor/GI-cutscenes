using GICutscenes.Mergers.GIMKV.MKV.Generics;

namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Tracks
{
    internal class TrackEntry : MKVContainerElement
    {
        public MKVElement<int> TrackNumber;
        public MKVElement<byte[]> TrackUID;
        public MKVElement<byte> TrackType;
        public MKVElement<byte> FlagDefault;
        public MKVElement<byte> FlagLacing;
        public MKVElement<string> CodecID;
        public MKVElement<byte[]> CodecPrivate;
        public MKVElement<ulong> DefaultDuration;
        public MKVElement<string> Language;
        public MKVElement<string> LanguageIETF;
        public MKVElement<string> Name;
        public MKVContainerElement SpecificSettings;  // Specific to audio / video

        //private int _trackNumber;

        public TrackEntry(int trackNumber, byte[] trackUid, string trackType, string codec, string name, string langIETF, ulong? duration = null, string? lang = null, byte[]? codecPrivate = null, MKVContainerElement? specSettings = null) : base(Signatures.TrackEntry)
        {
            TrackNumber = new MKVElement<int>(Signatures.TrackNumber, trackNumber);
            TrackUID = new MKVElement<byte[]>(Signatures.TrackUID, trackUid);

            byte trackTypeID = TrackTypeInt(trackType);

            TrackType = new MKVElement<byte>(Signatures.TrackType, trackTypeID);
            FlagDefault = new MKVElement<byte>(Signatures.FlagDefault, 0x00);
            if (trackTypeID == 0x11)
            {
                FlagLacing = new MKVElement<byte>(Signatures.FlagLacing, 0x00);  // No lacing
            }
            CodecID = new MKVElement<string>(Signatures.CodecID, codec);
            if (trackTypeID is 0x01 or 0x02) SpecificSettings = specSettings ?? throw new Exception("Specific settings for a Track Entry is null, while it has to be entered according to the track type.");

            if (trackTypeID is 0x01 or 0x11 && codecPrivate != null) CodecPrivate = new MKVElement<byte[]>(Signatures.CodecPrivate, codecPrivate);

            if (duration != null) DefaultDuration = new MKVElement<ulong>(Signatures.DefaultDuration, (ulong)duration);
            LanguageIETF = new MKVElement<string>(Signatures.LanguageIETF, langIETF);
            if (langIETF != "en" && lang != null) Language = new MKVElement<string>(Signatures.Language, lang);

            Name = new MKVElement<string>(Signatures.Name, name);
        }

        public byte[] GetTrackUID() => TrackUID.ToBytes()[3..];

        // Track types according to the Matroska V4 spec
        private static byte TrackTypeInt(string trackType)
        {
            return trackType switch
            {
                "video" => 0x01,
                "audio" => 0x02,
                "complex" => 0x03,
                "logo" => 0x10,
                "subtitle" => 0x11,
                "buttons" => 0x12,
                "control" => 0x20,
                "metadata" => 0x21,
                _ => throw new Exception($"Track type {trackType} doesn't exist.")
            };
        }
    }
}