#if WINDOWS
using System.Diagnostics;
using MWG.EyesOfApollo.Desktop.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MWG.EyesOfApollo.Desktop.Services
{
    public partial class AudioCaptureService
    {
        private readonly object _syncRoot = new();
        private WaveInEvent? _inputCapture;
        private WasapiLoopbackCapture? _loopbackCapture;
        private MMDeviceEnumerator? _deviceEnumerator;

        public Task<IReadOnlyList<AudioDeviceInfo>> GetInputDevicesAsync()
        {
            var devices = new List<AudioDeviceInfo>();
            for (var index = 0; index < WaveIn.DeviceCount; index++)
            {
                var caps = WaveIn.GetCapabilities(index);
                devices.Add(new AudioDeviceInfo(index.ToString(), caps.ProductName));
            }

            return Task.FromResult<IReadOnlyList<AudioDeviceInfo>>(devices);
        }

        public Task<IReadOnlyList<AudioDeviceInfo>> GetOutputDevicesAsync()
        {
            _deviceEnumerator ??= new MMDeviceEnumerator();
            var endpoints = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            var devices = endpoints
                .Select(device => new AudioDeviceInfo(device.ID, device.FriendlyName))
                .ToList();

            return Task.FromResult<IReadOnlyList<AudioDeviceInfo>>(devices);
        }

        public async Task StartAsync(AudioDeviceInfo device, AudioSourceType sourceType)
        {
            await StopAsync();

            lock (_syncRoot)
            {

                if (sourceType == AudioSourceType.Input)
                {
                    var waveIn = new WaveInEvent
                    {
                        DeviceNumber = int.Parse(device.Id),
                        BufferMilliseconds = 20,
                        WaveFormat = new WaveFormat(44100, 16, 2)
                    };

                    waveIn.DataAvailable += (_, args) =>
                        RaiseBuffer(ConvertToFloat(args.Buffer, args.BytesRecorded, waveIn.WaveFormat), waveIn.WaveFormat.SampleRate, waveIn.WaveFormat.Channels, Stopwatch.GetTimestamp(), sourceType);
                    waveIn.StartRecording();
                    _inputCapture = waveIn;
                }
                else
                {
                    _deviceEnumerator ??= new MMDeviceEnumerator();
                    var deviceInstance = _deviceEnumerator.GetDevice(device.Id);
                    var loopback = new WasapiLoopbackCapture(deviceInstance)
                    {
                        ShareMode = AudioClientShareMode.Shared
                    };

                    loopback.DataAvailable += (_, args) =>
                        RaiseBuffer(ConvertToFloat(args.Buffer, args.BytesRecorded, loopback.WaveFormat), loopback.WaveFormat.SampleRate, loopback.WaveFormat.Channels, Stopwatch.GetTimestamp(), sourceType);
                    loopback.StartRecording();
                    _loopbackCapture = loopback;
                }

                IsRunning = true;
            }

            return;
        }

        public Task StopAsync()
        {
            lock (_syncRoot)
            {
                _inputCapture?.StopRecording();
                _inputCapture?.Dispose();
                _inputCapture = null;

                _loopbackCapture?.StopRecording();
                _loopbackCapture?.Dispose();
                _loopbackCapture = null;

                IsRunning = false;
            }

            return Task.CompletedTask;
        }

        private static float[] ConvertToFloat(byte[] buffer, int bytesRecorded, WaveFormat format)
        {
            if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
            {
                var sampleCount = bytesRecorded / sizeof(float);
                var samples = new float[sampleCount];
                Buffer.BlockCopy(buffer, 0, samples, 0, bytesRecorded);
                return samples;
            }

            if (format.BitsPerSample == 16)
            {
                var sampleCount = bytesRecorded / sizeof(short);
                var samples = new float[sampleCount];
                for (var index = 0; index < sampleCount; index++)
                {
                    var sample = BitConverter.ToInt16(buffer, index * sizeof(short));
                    samples[index] = sample / 32768f;
                }

                return samples;
            }

            return Array.Empty<float>();
        }
    }
}
#endif
