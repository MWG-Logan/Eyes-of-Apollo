using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    /// <summary>
    /// Base audio capture service with shared event plumbing.
    /// </summary>
    public partial class AudioCaptureService : IAudioCaptureService
    {
        /// <inheritdoc />
        public event EventHandler<AudioBufferEventArgs>? AudioBufferAvailable;

        /// <inheritdoc />
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// Raises the audio buffer event.
        /// </summary>
        /// <param name="samples">Captured samples.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="channels">Channel count.</param>
        /// <param name="timestampTicks">Timestamp in ticks.</param>
        /// <param name="sourceType">Source type.</param>
        protected void RaiseBuffer(float[] samples, int sampleRate, int channels, long timestampTicks, AudioSourceType sourceType)
        {
            AudioBufferAvailable?.Invoke(this, new AudioBufferEventArgs(samples, sampleRate, channels, timestampTicks, sourceType));
        }
    }
}
