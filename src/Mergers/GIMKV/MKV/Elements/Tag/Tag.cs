using GICutscenes.Mergers.GIMKV.MKV.Generics;

namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Tag
{
    internal class Tag : MKVContainerElement
    {
        public Targets Target;
        public SimpleTag Bps;
        public SimpleTag Duration;
        public SimpleTag NumberOfFrames;
        public SimpleTag NumberOfBytes;
        public SimpleTag StatisticsWritingApp;
        public SimpleTag StatisticsWritingDateUTC;
        public SimpleTag StatisticsTags;

        public Tag(byte[] trackUid, ulong bps, ulong duration, uint numberOfFrames, ulong numberOfBytes, string writingApp, DateTime writingDateUTC) : base(Signatures.Tag)
        {
            Target = new Targets(trackUid);
            Bps = new SimpleTag("BPS", bps.ToString());
            Duration = new SimpleTag("DURATION", TimeSpan.FromMilliseconds(duration) + "00");
            NumberOfFrames = new SimpleTag("NUMBER_OF_FRAMES", numberOfFrames.ToString());
            NumberOfBytes = new SimpleTag("NUMBER_OF_BYTES", numberOfBytes.ToString());
            StatisticsWritingApp = new SimpleTag("_STATISTICS_WRITING_APP", writingApp);
            StatisticsWritingDateUTC = new SimpleTag("_STATISTICS_WRITING_DATE_UTC", writingDateUTC.ToString("yyyy-MM-dd HH:mm:ss"));
            StatisticsTags = new SimpleTag("_STATISTICS_TAGS", "BPS DURATION NUMBER_OF_FRAMES NUMBER_OF_BYTES");
        }
    }
}