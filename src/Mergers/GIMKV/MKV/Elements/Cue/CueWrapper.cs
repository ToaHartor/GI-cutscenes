namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Cue
{
    internal class CueWrapper
    {
        private readonly int _track;
        private readonly int _timestamp; // Millisecond
        public readonly int ClusterIndex;
        private readonly uint _positionInCluster;
        private readonly int? _duration;

        public CueWrapper(int track, int timestamp, int clusterIndex, uint positionInCluster, int? duration = null)
        {
            _track = track;
            _timestamp = timestamp;
            ClusterIndex = clusterIndex;
            _positionInCluster = positionInCluster;
            if (duration != null) _duration = duration;
        }

        public CuePoint GenerateCuePoint(long clusterPosition) => new(_timestamp, _track, clusterPosition, _positionInCluster, _duration);
    }
}