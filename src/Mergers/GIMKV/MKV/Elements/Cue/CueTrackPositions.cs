using GICutscenes.Mergers.GIMKV.MKV.Generics;

namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Cue
{
    internal class CueTrackPositions : MKVContainerElement
    {
        public MKVElement<int> CueTrack;
        public MKVElement<long> CueClusterPosition;
        public MKVElement<long> CueRelativePosition;
        public MKVElement<int>? CueDuration;

        public CueTrackPositions(int track, long clusterPosition, long relativePosition, int? duration = null) : base(Signatures.CueTrackPositions)
        {
            CueTrack = new MKVElement<int>(Signatures.CueTrack, track);
            CueClusterPosition = new MKVElement<long>(Signatures.CueClusterPosition, clusterPosition);
            CueRelativePosition = new MKVElement<long>(Signatures.CueRelativePosition, relativePosition);
            if (duration != null) CueDuration = new MKVElement<int>(Signatures.CueDuration, (int)duration);
        }
    }
}