using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    public sealed class AudioBufferEventArgs : EventArgs
    {
        public AudioBufferEventArgs(float[] samples, int sampleRate, int channels, long timestampTicks, AudioSourceType sourceType)
        {
            Samples = samples;
            SampleRate = sampleRate;
            Channels = channels;
            TimestampTicks = timestampTicks;
            SourceType = sourceType;
        }

        public float[] Samples { get; }
        public int SampleRate { get; }
        public int Channels { get; }
        public long TimestampTicks { get; }
        public AudioSourceType SourceType { get; }
    }
}
