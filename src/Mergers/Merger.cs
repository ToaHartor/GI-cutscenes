namespace GICutscenes.Mergers
{
    internal interface Merger
    {

        void AddVideoTrack(string videoFile);

        void AddAudioTrack(string audioFile, int lang);

        void AddSubtitlesTrack(string subFile, string language);

        void AddAttachment(string attachment, string description);

        void Merge();

        void Merge(string audioFormat, string videoFormat, string preset, string crf) { }

    }
}
