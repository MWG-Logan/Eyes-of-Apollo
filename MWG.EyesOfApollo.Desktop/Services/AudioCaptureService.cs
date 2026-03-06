using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    public partial class AudioCaptureService : IAudioCaptureService
    {
        public event EventHandler<AudioBufferEventArgs>? AudioBufferAvailable;

        public bool IsRunning { get; protected set; }

        protected void RaiseBuffer(float[] samples, int sampleRate, int channels, long timestampTicks, AudioSourceType sourceType)
        {
            AudioBufferAvailable?.Invoke(this, new AudioBufferEventArgs(samples, sampleRate, channels, timestampTicks, sourceType));
        }
    }
}
