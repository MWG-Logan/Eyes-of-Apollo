using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using MWG.EyesOfApollo.Desktop.Models;
using MWG.EyesOfApollo.Desktop.Rendering;
using MWG.EyesOfApollo.Desktop.Services;

namespace MWG.EyesOfApollo.Desktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private const string ThemePreferenceKey = "SelectedTheme";
        private const string DevicePreferenceKey = "SelectedDevice";
        private const string SourcePreferenceKey = "SelectedSource";
        private const string ModePreferenceKey = "SelectedMode";
        private const string FrameRatePreferenceKey = "SelectedFrameRate";
        private const string StatsPreferenceKey = "ShowStats";
        private const string AxisPreferenceKey = "ShowAxisIndicators";
        private const string ScalePreferenceKey = "AmplitudeScale";
        private const string AutoGainPreferenceKey = "AutoGain";
        private const string SmoothingPreferenceKey = "Smoothing";
        private const string PeakHoldPreferenceKey = "PeakHold";
        private const string WeightingPreferenceKey = "FrequencyWeighting";

        private readonly IAudioCaptureService _audioCaptureService;
        private readonly ThemeService _themeService;
        private readonly VisualizerState _visualizerState;
        private readonly Stopwatch _fpsStopwatch = Stopwatch.StartNew();

        private AudioDeviceInfo? _selectedDevice;
        private ThemeDefinition? _selectedTheme;
        private AudioSourceType _selectedSourceType;
        private VisualizerMode _selectedMode;
        private int _selectedFrameRate;
        private bool _showStats;
        private bool _showAxisIndicators;
        private AmplitudeScaleMode _selectedScaleMode;
        private bool _enableAutoGain;
        private bool _enableSmoothing;
        private bool _enablePeakHold;
        private FrequencyWeightingMode _selectedWeightingMode;
        private string _statsText = string.Empty;
        private int _frameCount;
        private double _latestLatency;
        private bool _isInitialized;
        private float[] _smoothedMagnitudes = Array.Empty<float>();
        private float[] _peakHoldMagnitudes = Array.Empty<float>();
        private float _autoGainPeak = 0.1f;

        public MainViewModel(IAudioCaptureService audioCaptureService, ThemeService themeService)
        {
            _audioCaptureService = audioCaptureService;
            _themeService = themeService;
            _visualizerState = new VisualizerState();
            VisualizerDrawable = new VisualizerDrawable(_visualizerState);

            SourceOptions = new ObservableCollection<AudioSourceType>(Enum.GetValues<AudioSourceType>());
            ModeOptions = new ObservableCollection<VisualizerMode>(Enum.GetValues<VisualizerMode>());
            FrameRateOptions = new ObservableCollection<int>(new[] { 30, 60, 120, 144, 240 });
            ScaleOptions = new ObservableCollection<AmplitudeScaleMode>(Enum.GetValues<AmplitudeScaleMode>());
            WeightingOptions = new ObservableCollection<FrequencyWeightingMode>(Enum.GetValues<FrequencyWeightingMode>());

            _selectedSourceType = AudioSourceType.Output;
            _selectedMode = VisualizerMode.Bars;
            _selectedFrameRate = 60;
            _selectedScaleMode = AmplitudeScaleMode.Normalized;
            _selectedWeightingMode = FrequencyWeightingMode.Flat;
            _enableAutoGain = true;
            _enableSmoothing = true;
            _enablePeakHold = true;

            _audioCaptureService.AudioBufferAvailable += OnAudioBufferAvailable;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<AudioDeviceInfo> Devices { get; } = new();
        public ObservableCollection<ThemeDefinition> Themes { get; } = new();
        public ObservableCollection<AudioSourceType> SourceOptions { get; }
        public ObservableCollection<VisualizerMode> ModeOptions { get; }
        public ObservableCollection<int> FrameRateOptions { get; }
        public ObservableCollection<AmplitudeScaleMode> ScaleOptions { get; }
        public ObservableCollection<FrequencyWeightingMode> WeightingOptions { get; }

        public VisualizerDrawable VisualizerDrawable { get; }

        public AudioDeviceInfo? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    Preferences.Set(DevicePreferenceKey, value?.Id ?? string.Empty);
                    _ = RestartCaptureAsync();
                }
            }
        }

        public FrequencyWeightingMode SelectedWeightingMode
        {
            get => _selectedWeightingMode;
            set
            {
                if (SetProperty(ref _selectedWeightingMode, value))
                {
                    Preferences.Set(WeightingPreferenceKey, value.ToString());
                }
            }
        }

        public ThemeDefinition? SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (SetProperty(ref _selectedTheme, value))
                {
                    Preferences.Set(ThemePreferenceKey, value?.Name ?? string.Empty);
                    UpdateTheme();
                }
            }
        }

        public AudioSourceType SelectedSourceType
        {
            get => _selectedSourceType;
            set
            {
                if (SetProperty(ref _selectedSourceType, value))
                {
                    Preferences.Set(SourcePreferenceKey, value.ToString());
                    _ = LoadDevicesAsync();
                }
            }
        }

        public VisualizerMode SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (SetProperty(ref _selectedMode, value))
                {
                    Preferences.Set(ModePreferenceKey, value.ToString());
                    VisualizerDrawable.Mode = value;
                }
            }
        }

        public int SelectedFrameRate
        {
            get => _selectedFrameRate;
            set
            {
                if (SetProperty(ref _selectedFrameRate, value))
                {
                    Preferences.Set(FrameRatePreferenceKey, value);
                }
            }
        }

        public bool ShowStats
        {
            get => _showStats;
            set
            {
                if (SetProperty(ref _showStats, value))
                {
                    Preferences.Set(StatsPreferenceKey, value);
                    if (!value)
                    {
                        StatsText = string.Empty;
                    }
                }
            }
        }

        public bool ShowAxisIndicators
        {
            get => _showAxisIndicators;
            set
            {
                if (SetProperty(ref _showAxisIndicators, value))
                {
                    Preferences.Set(AxisPreferenceKey, value);
                    VisualizerDrawable.ShowAxisIndicators = value;
                }
            }
        }

        public AmplitudeScaleMode SelectedScaleMode
        {
            get => _selectedScaleMode;
            set
            {
                if (SetProperty(ref _selectedScaleMode, value))
                {
                    Preferences.Set(ScalePreferenceKey, value.ToString());
                    VisualizerDrawable.ScaleMode = value;
                }
            }
        }

        public bool EnableAutoGain
        {
            get => _enableAutoGain;
            set
            {
                if (SetProperty(ref _enableAutoGain, value))
                {
                    Preferences.Set(AutoGainPreferenceKey, value);
                }
            }
        }

        public bool EnableSmoothing
        {
            get => _enableSmoothing;
            set
            {
                if (SetProperty(ref _enableSmoothing, value))
                {
                    Preferences.Set(SmoothingPreferenceKey, value);
                }
            }
        }

        public bool EnablePeakHold
        {
            get => _enablePeakHold;
            set
            {
                if (SetProperty(ref _enablePeakHold, value))
                {
                    Preferences.Set(PeakHoldPreferenceKey, value);
                    VisualizerDrawable.ShowPeakHold = value;
                }
            }
        }

        public string StatsText
        {
            get => _statsText;
            set => SetProperty(ref _statsText, value);
        }

        public Color ThemeBackground => SelectedTheme?.BackgroundColor ?? Colors.Black;

        public Color ThemePrimary => SelectedTheme?.PrimaryColor ?? Colors.White;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            await LoadThemesAsync();
            RestorePreferences();
            await LoadDevicesAsync();
        }

        public void TickFrame()
        {
            _frameCount++;

            if (_fpsStopwatch.ElapsedMilliseconds >= 500)
            {
                var fps = _frameCount / _fpsStopwatch.Elapsed.TotalSeconds;
                _frameCount = 0;
                _fpsStopwatch.Restart();

                if (ShowStats)
                {
                    StatsText = $"FPS {fps:0} | Latency {_latestLatency:0.0} ms | Mode {SelectedMode}";
                }
            }
        }

        public void Dispose()
        {
            _audioCaptureService.AudioBufferAvailable -= OnAudioBufferAvailable;
            _ = _audioCaptureService.StopAsync();
        }

        private async Task LoadThemesAsync()
        {
            Themes.Clear();
            var themes = await _themeService.LoadThemesAsync();
            foreach (var theme in themes)
            {
                Themes.Add(theme);
            }

            SelectedTheme = Themes.FirstOrDefault();
        }

        private async Task LoadDevicesAsync()
        {
            Devices.Clear();
            var devices = SelectedSourceType == AudioSourceType.Input
                ? await _audioCaptureService.GetInputDevicesAsync()
                : await _audioCaptureService.GetOutputDevicesAsync();

            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            var persistedDeviceId = Preferences.Get(DevicePreferenceKey, string.Empty);
            SelectedDevice = Devices.FirstOrDefault(device => device.Id == persistedDeviceId) ?? Devices.FirstOrDefault();
        }

        private async Task RestartCaptureAsync()
        {
            if (SelectedDevice == null)
            {
                await _audioCaptureService.StopAsync();
                return;
            }

            await _audioCaptureService.StartAsync(SelectedDevice, SelectedSourceType);
        }

        private void UpdateTheme()
        {
            if (SelectedTheme == null)
            {
                return;
            }

            VisualizerDrawable.Theme = SelectedTheme;
            SelectedMode = SelectedTheme.Mode;
            OnPropertyChanged(nameof(ThemeBackground));
            OnPropertyChanged(nameof(ThemePrimary));
        }

        private void RestorePreferences()
        {
            if (Enum.TryParse(Preferences.Get(SourcePreferenceKey, AudioSourceType.Output.ToString()), out AudioSourceType source))
            {
                SelectedSourceType = source;
            }

            if (Enum.TryParse(Preferences.Get(ModePreferenceKey, VisualizerMode.Bars.ToString()), out VisualizerMode mode))
            {
                SelectedMode = mode;
            }

            SelectedFrameRate = Preferences.Get(FrameRatePreferenceKey, 60);
            ShowStats = Preferences.Get(StatsPreferenceKey, false);
            ShowAxisIndicators = Preferences.Get(AxisPreferenceKey, false);

            if (Enum.TryParse(Preferences.Get(ScalePreferenceKey, AmplitudeScaleMode.Normalized.ToString()), out AmplitudeScaleMode scaleMode))
            {
                SelectedScaleMode = scaleMode;
            }

            EnableAutoGain = Preferences.Get(AutoGainPreferenceKey, true);
            EnableSmoothing = Preferences.Get(SmoothingPreferenceKey, true);
            EnablePeakHold = Preferences.Get(PeakHoldPreferenceKey, true);

            if (Enum.TryParse(Preferences.Get(WeightingPreferenceKey, FrequencyWeightingMode.Flat.ToString()), out FrequencyWeightingMode weightingMode))
            {
                SelectedWeightingMode = weightingMode;
            }

            var themeName = Preferences.Get(ThemePreferenceKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(themeName))
            {
                SelectedTheme = Themes.FirstOrDefault(theme => theme.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase))
                    ?? Themes.FirstOrDefault();
            }
        }

        private void OnAudioBufferAvailable(object? sender, AudioBufferEventArgs e)
        {
            if (SelectedTheme == null)
            {
                return;
            }

            var magnitudes = AudioAnalyzer.ComputeSpectrum(e.Samples, e.SampleRate, e.Channels, SelectedTheme.BarCount, SelectedScaleMode, SelectedWeightingMode);
            var processed = ApplyDynamics(magnitudes);
            var peaks = EnablePeakHold ? UpdatePeakHold(processed) : Array.Empty<float>();
            _visualizerState.Update(processed, peaks);

            var latency = (Stopwatch.GetTimestamp() - e.TimestampTicks) * 1000.0 / Stopwatch.Frequency;
            _latestLatency = latency;
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private float[] ApplyDynamics(float[] magnitudes)
        {
            var result = magnitudes.ToArray();

            if (EnableAutoGain)
            {
                var currentPeak = result.Length == 0 ? 0f : result.Max();
                _autoGainPeak = Math.Max(currentPeak, _autoGainPeak * 0.98f);
                var gain = _autoGainPeak > 0.0001f ? 1f / _autoGainPeak : 1f;

                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = Math.Clamp(result[i] * gain, 0, 1);
                }
            }

            if (EnableSmoothing)
            {
                if (_smoothedMagnitudes.Length != result.Length)
                {
                    _smoothedMagnitudes = new float[result.Length];
                }

                const float attack = 0.6f;
                const float release = 0.2f;

                for (var i = 0; i < result.Length; i++)
                {
                    var previous = _smoothedMagnitudes[i];
                    var target = result[i];
                    var coefficient = target > previous ? attack : release;
                    var smoothed = previous + (target - previous) * coefficient;
                    _smoothedMagnitudes[i] = smoothed;
                    result[i] = smoothed;
                }
            }

            return result;
        }

        private float[] UpdatePeakHold(float[] magnitudes)
        {
            if (_peakHoldMagnitudes.Length != magnitudes.Length)
            {
                _peakHoldMagnitudes = new float[magnitudes.Length];
            }

            const float decay = 0.96f;
            for (var i = 0; i < magnitudes.Length; i++)
            {
                var value = magnitudes[i];
                var decayed = _peakHoldMagnitudes[i] * decay;
                _peakHoldMagnitudes[i] = Math.Max(value, decayed);
            }

            return _peakHoldMagnitudes.ToArray();
        }
    }
}
