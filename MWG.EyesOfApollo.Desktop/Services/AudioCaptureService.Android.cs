#if ANDROID
using Android.Content;
using Android.Media;
using System.Diagnostics;
using MauiAudioDeviceInfo = MWG.EyesOfApollo.Desktop.Models.AudioDeviceInfo;

namespace MWG.EyesOfApollo.Desktop.Services
{
    /// <summary>
    /// Android-specific audio capture implementation using AudioRecord and AudioManager.
    /// </summary>
    public partial class AudioCaptureService
    {
        // Minimum reasonable PCM buffer size in bytes when the platform reports an invalid minimum.
        private const int FallbackBufferSize = 4096;
        // Number of audio channels matching ChannelIn.Stereo.
        private const int StereoChannels = 2;
        // Divisor used to normalise a 16-bit signed PCM sample to the [-1, 1] range.
        private const float Pcm16MaxAmplitude = 32768f;

        private AudioRecord? _audioRecord;
        private CancellationTokenSource? _captureCts;
        private Task? _captureTask;

        /// <inheritdoc />
        public Task<IReadOnlyList<MauiAudioDeviceInfo>> GetInputDevicesAsync()
        {
            var devices = new List<MauiAudioDeviceInfo>();

            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                var audioManager = Android.App.Application.Context.GetSystemService(Context.AudioService) as AudioManager;
                var androidDevices = audioManager?.GetDevices(GetDevicesTargets.Inputs);
                if (androidDevices != null)
                {
                    foreach (var d in androidDevices)
                    {
                        devices.Add(new MauiAudioDeviceInfo(d.Id.ToString(), $"{d.ProductName} ({d.Type})"));
                    }
                }
            }

            if (devices.Count == 0)
            {
                devices.Add(new MauiAudioDeviceInfo("0", "Default Microphone"));
            }

            return Task.FromResult<IReadOnlyList<MauiAudioDeviceInfo>>(devices);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<MauiAudioDeviceInfo>> GetOutputDevicesAsync()
        {
            var devices = new List<MauiAudioDeviceInfo>();

            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                var audioManager = Android.App.Application.Context.GetSystemService(Context.AudioService) as AudioManager;
                var androidDevices = audioManager?.GetDevices(GetDevicesTargets.Outputs);
                if (androidDevices != null)
                {
                    foreach (var d in androidDevices)
                    {
                        devices.Add(new MauiAudioDeviceInfo(d.Id.ToString(), $"{d.ProductName} ({d.Type})"));
                    }
                }
            }

            if (devices.Count == 0)
            {
                devices.Add(new MauiAudioDeviceInfo("0", "Default Speaker"));
            }

            return Task.FromResult<IReadOnlyList<MauiAudioDeviceInfo>>(devices);
        }

        /// <inheritdoc />
        public async Task StartAsync(MauiAudioDeviceInfo device, Models.AudioSourceType sourceType)
        {
            await StopAsync();

            if (sourceType == Models.AudioSourceType.Input)
            {
                const int sampleRate = 44100;
                const ChannelIn channelConfig = ChannelIn.Stereo;
                const Encoding audioFormat = Encoding.Pcm16bit;

                var bufferSize = AudioRecord.GetMinBufferSize(sampleRate, channelConfig, audioFormat);
                if (bufferSize <= 0)
                {
                    bufferSize = FallbackBufferSize;
                }

                _audioRecord = new AudioRecord(AudioSource.Mic, sampleRate, channelConfig, audioFormat, bufferSize);

                if (OperatingSystem.IsAndroidVersionAtLeast(23) && device.Id != "0")
                {
                    if (int.TryParse(device.Id, out var deviceId))
                    {
                        var audioManager = Android.App.Application.Context.GetSystemService(Context.AudioService) as AudioManager;
                        var androidDevices = audioManager?.GetDevices(GetDevicesTargets.Inputs);
                        if (androidDevices != null)
                        {
                            foreach (var d in androidDevices)
                            {
                                if (d.Id == deviceId)
                                {
                                    _audioRecord.SetPreferredDevice(d);
                                    break;
                                }
                            }
                        }
                    }
                }

                if (_audioRecord.State != Android.Media.State.Initialized)
                {
                    _audioRecord.Release();
                    _audioRecord.Dispose();
                    _audioRecord = null;
                    IsRunning = false;
                    return;
                }

                _audioRecord.StartRecording();
                IsRunning = true;

                _captureCts = new CancellationTokenSource();
                var token = _captureCts.Token;
                var localRecord = _audioRecord;
                var capturedSampleRate = sampleRate;
                var buffer = new byte[bufferSize];

                _captureTask = Task.Run(() =>
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            var read = localRecord?.Read(buffer, 0, buffer.Length) ?? -1;
                            if (read <= 0)
                            {
                                break;
                            }

                            var samples = ConvertPcm16ToFloat(buffer, read);
                            RaiseBuffer(samples, capturedSampleRate, StereoChannels, Stopwatch.GetTimestamp(), sourceType);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected on cancellation; suppress.
                    }
                }, token);
            }
            else
            {
                // System-wide output loopback capture is not available on Android without
                // special OEM or root permissions. Devices are listed for UI selection only.
                IsRunning = false;
            }
        }

        /// <inheritdoc />
        public async Task StopAsync()
        {
            _captureCts?.Cancel();

            if (_captureTask != null)
            {
                try
                {
                    await _captureTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected on cancellation; suppress.
                }

                _captureTask = null;
            }

            _captureCts?.Dispose();
            _captureCts = null;

            _audioRecord?.Stop();
            _audioRecord?.Release();
            _audioRecord?.Dispose();
            _audioRecord = null;

            IsRunning = false;
        }

        private static float[] ConvertPcm16ToFloat(byte[] buffer, int bytesRead)
        {
            var sampleCount = bytesRead / sizeof(short);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var sample = BitConverter.ToInt16(buffer, i * sizeof(short));
                samples[i] = sample / Pcm16MaxAmplitude;
            }

            return samples;
        }
    }
}
#endif
