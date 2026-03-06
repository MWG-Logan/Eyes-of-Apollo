using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    public interface IAudioCaptureService
    {
        event EventHandler<AudioBufferEventArgs>? AudioBufferAvailable;

        bool IsRunning { get; }

        Task<IReadOnlyList<AudioDeviceInfo>> GetInputDevicesAsync();
        Task<IReadOnlyList<AudioDeviceInfo>> GetOutputDevicesAsync();
        Task StartAsync(AudioDeviceInfo device, AudioSourceType sourceType);
        Task StopAsync();
    }
}
