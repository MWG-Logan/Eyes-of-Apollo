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
        private string _statsText = string.Empty;
        private int _frameCount;
        private double _latestLatency;
        private bool _isInitialized;

        public MainViewModel(IAudioCaptureService audioCaptureService, ThemeService themeService)
        {
            _audioCaptureService = audioCaptureService;
            _themeService = themeService;
            _visualizerState = new VisualizerState();
            VisualizerDrawable = new VisualizerDrawable(_visualizerState);

            SourceOptions = new ObservableCollection<AudioSourceType>(Enum.GetValues<AudioSourceType>());
            ModeOptions = new ObservableCollection<VisualizerMode>(Enum.GetValues<VisualizerMode>());
            FrameRateOptions = new ObservableCollection<int>(new[] { 30, 60, 120, 144, 240 });

            _selectedSourceType = AudioSourceType.Output;
            _selectedMode = VisualizerMode.Bars;
            _selectedFrameRate = 60;

            _audioCaptureService.AudioBufferAvailable += OnAudioBufferAvailable;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<AudioDeviceInfo> Devices { get; } = new();
        public ObservableCollection<ThemeDefinition> Themes { get; } = new();
        public ObservableCollection<AudioSourceType> SourceOptions { get; }
        public ObservableCollection<VisualizerMode> ModeOptions { get; }
        public ObservableCollection<int> FrameRateOptions { get; }

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

            var magnitudes = AudioAnalyzer.ComputeSpectrum(e.Samples, SelectedTheme.BarCount);
            _visualizerState.Update(magnitudes);

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
    }
}
