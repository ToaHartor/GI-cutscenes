using GICutscenes.Mergers.GIMKV.MKV.Generics;

namespace GICutscenes.Mergers.GIMKV.MKV.Elements.Tracks
{
    internal class AudioSettings : MKVContainerElement
    {
        public MKVElement<float> SamplingFrequency;
        public MKVElement<ushort> Channels;
        public MKVElement<ushort> BitDepth;

        public AudioSettings(float samplingFreq, ushort channel, ushort bitDepth) : base(Signatures.AudioSettings)
        {
            SamplingFrequency = new MKVElement<float>(Signatures.SamplingFrequency, samplingFreq);
            Channels = new MKVElement<ushort>(Signatures.Channels, channel);
            BitDepth = new MKVElement<ushort>(Signatures.BitDepth, bitDepth);
        }
    }
}