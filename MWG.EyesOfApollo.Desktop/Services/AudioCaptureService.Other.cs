#if !WINDOWS
using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    public partial class AudioCaptureService
    {
        public Task<IReadOnlyList<AudioDeviceInfo>> GetInputDevicesAsync()
        {
            return Task.FromResult<IReadOnlyList<AudioDeviceInfo>>(Array.Empty<AudioDeviceInfo>());
        }

        public Task<IReadOnlyList<AudioDeviceInfo>> GetOutputDevicesAsync()
        {
            return Task.FromResult<IReadOnlyList<AudioDeviceInfo>>(Array.Empty<AudioDeviceInfo>());
        }

        public Task StartAsync(AudioDeviceInfo device, AudioSourceType sourceType)
        {
            IsRunning = false;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            IsRunning = false;
            return Task.CompletedTask;
        }
    }
}
#endif
