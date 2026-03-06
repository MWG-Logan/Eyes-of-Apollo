using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    /// <summary>
    /// Abstraction for audio capture implementations.
    /// </summary>
    public interface IAudioCaptureService
    {
        /// <summary>
        /// Raised when a new audio buffer is available.
        /// </summary>
        event EventHandler<AudioBufferEventArgs>? AudioBufferAvailable;

        /// <summary>
        /// Gets a value indicating whether capture is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Lists available input devices.
        /// </summary>
        Task<IReadOnlyList<AudioDeviceInfo>> GetInputDevicesAsync();
        /// <summary>
        /// Lists available output devices.
        /// </summary>
        Task<IReadOnlyList<AudioDeviceInfo>> GetOutputDevicesAsync();
        /// <summary>
        /// Starts capture from the specified device.
        /// </summary>
        /// <param name="device">The device to capture from.</param>
        /// <param name="sourceType">The source type.</param>
        Task StartAsync(AudioDeviceInfo device, AudioSourceType sourceType);
        /// <summary>
        /// Stops capture.
        /// </summary>
        Task StopAsync();
    }
}
