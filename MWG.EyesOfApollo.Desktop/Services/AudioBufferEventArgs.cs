using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    /// <summary>
    /// Provides audio buffer data for capture events.
    /// </summary>
    public sealed class AudioBufferEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBufferEventArgs"/> class.
        /// </summary>
        /// <param name="samples">The captured audio samples.</param>
        /// <param name="sampleRate">The sample rate of the audio buffer.</param>
        /// <param name="channels">The number of channels in the buffer.</param>
        /// <param name="timestampTicks">The timestamp for the buffer in ticks.</param>
        /// <param name="sourceType">The audio source type.</param>
        public AudioBufferEventArgs(float[] samples, int sampleRate, int channels, long timestampTicks, AudioSourceType sourceType)
        {
            Samples = samples;
            SampleRate = sampleRate;
            Channels = channels;
            TimestampTicks = timestampTicks;
            SourceType = sourceType;
        }

        /// <summary>
        /// Gets the captured audio samples.
        /// </summary>
        public float[] Samples { get; }
        /// <summary>
        /// Gets the sample rate of the audio buffer.
        /// </summary>
        public int SampleRate { get; }
        /// <summary>
        /// Gets the number of channels in the buffer.
        /// </summary>
        public int Channels { get; }
        /// <summary>
        /// Gets the timestamp for the buffer in ticks.
        /// </summary>
        public long TimestampTicks { get; }
        /// <summary>
        /// Gets the audio source type.
        /// </summary>
        public AudioSourceType SourceType { get; }
    }
}
