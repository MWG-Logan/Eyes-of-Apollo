#if !WINDOWS && !ANDROID
using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    /// <summary>
    /// Placeholder audio capture implementation for non-Windows platforms.
    /// </summary>
    public partial class AudioCaptureService
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<AudioDeviceInfo>> GetInputDevicesAsync()
        {
            return Task.FromResult<IReadOnlyList<AudioDeviceInfo>>(Array.Empty<AudioDeviceInfo>());
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<AudioDeviceInfo>> GetOutputDevicesAsync()
        {
            return Task.FromResult<IReadOnlyList<AudioDeviceInfo>>(Array.Empty<AudioDeviceInfo>());
        }

        /// <inheritdoc />
        public Task StartAsync(AudioDeviceInfo device, AudioSourceType sourceType)
        {
            IsRunning = false;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            IsRunning = false;
            return Task.CompletedTask;
        }
    }
}
#endif
