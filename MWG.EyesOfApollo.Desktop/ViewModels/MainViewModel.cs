using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MWG.EyesOfApollo.Desktop.Models;
using MWG.EyesOfApollo.Desktop.Rendering;
using MWG.EyesOfApollo.Desktop.Services;

namespace MWG.EyesOfApollo.Desktop.ViewModels
{
    /// <summary>
    /// View model that orchestrates audio capture, visualization state, and settings.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private const double DefaultDbMin = -90;
        private const double DefaultDbMax = -6;
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
        private const string BandNormalizationPreferenceKey = "BandNormalization";
        private const string BinModePreferenceKey = "FrequencyBinMode";
        private const string SettingsPanelVisiblePreferenceKey = "SettingsPanelVisible";

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
        private bool _enableBandNormalization;
        private FrequencyWeightingMode _selectedWeightingMode;
        private FrequencyBinMode _selectedBinMode;
        private string _statsText = string.Empty;
        private int _frameCount;
        private double _latestLatency;
        private bool _isInitialized;
        private float[] _smoothedMagnitudes = Array.Empty<float>();
        private float[] _peakHoldMagnitudes = Array.Empty<float>();
        private float[] _bandPeakMagnitudes = Array.Empty<float>();
        private float _autoGainPeak = 0.1f;
        private bool _isSettingsPanelVisible = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="audioCaptureService">The audio capture service.</param>
        /// <param name="themeService">The theme service.</param>
        public MainViewModel(IAudioCaptureService audioCaptureService, ThemeService themeService)
        {
            _audioCaptureService = audioCaptureService;
            _themeService = themeService;
            _visualizerState = new VisualizerState();
            VisualizerDrawable = new VisualizerDrawable(_visualizerState);
            VisualizerDrawable.DbMin = (float)DefaultDbMin;
            VisualizerDrawable.DbMax = (float)DefaultDbMax;

            SourceOptions = new ObservableCollection<AudioSourceType>(Enum.GetValues<AudioSourceType>());
            ModeOptions = new ObservableCollection<VisualizerMode>(Enum.GetValues<VisualizerMode>());
            FrameRateOptions = new ObservableCollection<int>(new[] { 30, 60, 120, 144, 240 });
            ScaleOptions = new ObservableCollection<AmplitudeScaleMode>(Enum.GetValues<AmplitudeScaleMode>());
            WeightingOptions = new ObservableCollection<FrequencyWeightingMode>(Enum.GetValues<FrequencyWeightingMode>());
            BinModeOptions = new ObservableCollection<FrequencyBinMode>(Enum.GetValues<FrequencyBinMode>());

            _selectedSourceType = AudioSourceType.Output;
            _selectedMode = VisualizerMode.Bars;
            _selectedFrameRate = 60;
            _selectedScaleMode = AmplitudeScaleMode.Dbfs;
            _selectedWeightingMode = FrequencyWeightingMode.AWeighting;
            _selectedBinMode = FrequencyBinMode.Logarithmic;
            _enableAutoGain = true;
            _enableSmoothing = true;
            _enablePeakHold = true;
            _enableBandNormalization = true;

            ToggleSettingsPanelCommand = new Command(() => IsSettingsPanelVisible = !IsSettingsPanelVisible);

            _audioCaptureService.AudioBufferAvailable += OnAudioBufferAvailable;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the available audio devices.
        /// </summary>
        public ObservableCollection<AudioDeviceInfo> Devices { get; } = new();
        /// <summary>
        /// Gets the available themes.
        /// </summary>
        public ObservableCollection<ThemeDefinition> Themes { get; } = new();
        /// <summary>
        /// Gets the available audio source options.
        /// </summary>
        public ObservableCollection<AudioSourceType> SourceOptions { get; }
        /// <summary>
        /// Gets the available visualizer modes.
        /// </summary>
        public ObservableCollection<VisualizerMode> ModeOptions { get; }
        /// <summary>
        /// Gets the available frame rate options.
        /// </summary>
        public ObservableCollection<int> FrameRateOptions { get; }
        /// <summary>
        /// Gets the available amplitude scale modes.
        /// </summary>
        public ObservableCollection<AmplitudeScaleMode> ScaleOptions { get; }
        /// <summary>
        /// Gets the available weighting options.
        /// </summary>
        public ObservableCollection<FrequencyWeightingMode> WeightingOptions { get; }
        /// <summary>
        /// Gets the available bin modes.
        /// </summary>
        public ObservableCollection<FrequencyBinMode> BinModeOptions { get; }

        /// <summary>
        /// Gets the command that collapses or expands the settings panel.
        /// </summary>
        public ICommand ToggleSettingsPanelCommand { get; }

        /// <summary>
        /// Gets the drawable used by the view.
        /// </summary>
        public VisualizerDrawable VisualizerDrawable { get; }

        /// <summary>
        /// Gets or sets the selected audio device.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether the settings panel is visible.
        /// </summary>
        public bool IsSettingsPanelVisible
        {
            get => _isSettingsPanelVisible;
            set
            {
                if (SetProperty(ref _isSettingsPanelVisible, value))
                {
                    Preferences.Set(SettingsPanelVisiblePreferenceKey, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected bin mode.
        /// </summary>
        public FrequencyBinMode SelectedBinMode
        {
            get => _selectedBinMode;
            set
            {
                if (SetProperty(ref _selectedBinMode, value))
                {
                    Preferences.Set(BinModePreferenceKey, value.ToString());
                    VisualizerDrawable.BinMode = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether band normalization is enabled.
        /// </summary>
        public bool EnableBandNormalization
        {
            get => _enableBandNormalization;
            set
            {
                if (SetProperty(ref _enableBandNormalization, value))
                {
                    Preferences.Set(BandNormalizationPreferenceKey, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected weighting mode.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the selected theme.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the selected audio source type.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the selected visualizer mode.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the selected frame rate.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether stats are shown.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether axis indicators are shown.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the selected amplitude scale mode.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether auto gain is enabled.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether smoothing is enabled.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether peak hold is enabled.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the stats text.
        /// </summary>
        public string StatsText
        {
            get => _statsText;
            set => SetProperty(ref _statsText, value);
        }

        /// <summary>
        /// Gets the background color from the selected theme.
        /// </summary>
        public Color ThemeBackground => SelectedTheme?.BackgroundColor ?? Colors.Black;

        /// <summary>
        /// Gets the primary color from the selected theme.
        /// </summary>
        public Color ThemePrimary => SelectedTheme?.PrimaryColor ?? Colors.White;

        /// <summary>
        /// Initializes themes, preferences, and devices.
        /// </summary>
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

        /// <summary>
        /// Updates frame stats and latency text.
        /// </summary>
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
                    var latency = Volatile.Read(ref _latestLatency);
                    StatsText = $"FPS {fps:0} | Latency {latency:0.0} ms | Mode {SelectedMode}";
                }
            }
        }

        /// <inheritdoc />
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
            try
            {
                var devices = SelectedSourceType == AudioSourceType.Input
                    ? await _audioCaptureService.GetInputDevicesAsync()
                    : await _audioCaptureService.GetOutputDevicesAsync();

                foreach (var device in devices)
                {
                    Devices.Add(device);
                }

                if (Devices.Count == 0)
                {
                    SelectedDevice = null;
                    await _audioCaptureService.StopAsync();
                    return;
                }

                var persistedDeviceId = Preferences.Get(DevicePreferenceKey, string.Empty);
                SelectedDevice = Devices.FirstOrDefault(device => device.Id == persistedDeviceId) ?? Devices.FirstOrDefault();
            }
            catch (Exception ex)
            {
                DiagnosticsLogger.LogError("Failed to enumerate audio devices.", ex);
                SelectedDevice = null;
                await _audioCaptureService.StopAsync();
            }
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

            if (Enum.TryParse(Preferences.Get(ScalePreferenceKey, AmplitudeScaleMode.Dbfs.ToString()), out AmplitudeScaleMode scaleMode))
            {
                SelectedScaleMode = scaleMode;
            }

            EnableAutoGain = Preferences.Get(AutoGainPreferenceKey, true);
            EnableSmoothing = Preferences.Get(SmoothingPreferenceKey, true);
            EnablePeakHold = Preferences.Get(PeakHoldPreferenceKey, true);
            EnableBandNormalization = Preferences.Get(BandNormalizationPreferenceKey, true);

            if (Enum.TryParse(Preferences.Get(WeightingPreferenceKey, FrequencyWeightingMode.AWeighting.ToString()), out FrequencyWeightingMode weightingMode))
            {
                SelectedWeightingMode = weightingMode;
            }

            if (Enum.TryParse(Preferences.Get(BinModePreferenceKey, FrequencyBinMode.Logarithmic.ToString()), out FrequencyBinMode binMode))
            {
                SelectedBinMode = binMode;
            }

            IsSettingsPanelVisible = Preferences.Get(SettingsPanelVisiblePreferenceKey, true);

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

            var magnitudes = AudioAnalyzer.ComputeSpectrum(
                e.Samples,
                e.SampleRate,
                e.Channels,
                SelectedTheme.BarCount,
                SelectedScaleMode,
                SelectedWeightingMode,
                SelectedBinMode,
                DefaultDbMin,
                DefaultDbMax);
            var processed = ApplyDynamics(magnitudes);
            var peaks = EnablePeakHold ? UpdatePeakHold(processed) : Array.Empty<float>();
            _visualizerState.Update(processed, peaks, e.SampleRate);

            var latency = (Stopwatch.GetTimestamp() - e.TimestampTicks) * 1000.0 / Stopwatch.Frequency;
            Interlocked.Exchange(ref _latestLatency, latency);
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

            if (EnableBandNormalization && SelectedScaleMode == AmplitudeScaleMode.Normalized)
            {
                if (_bandPeakMagnitudes.Length != result.Length)
                {
                    _bandPeakMagnitudes = new float[result.Length];
                }

                const float decay = 0.985f;
                for (var i = 0; i < result.Length; i++)
                {
                    var value = result[i];
                    var peak = Math.Max(value, _bandPeakMagnitudes[i] * decay);
                    peak = Math.Max(peak, 0.0001f);
                    _bandPeakMagnitudes[i] = peak;
                    result[i] = Math.Clamp(value / peak, 0, 1);
                }
            }

            if (EnableAutoGain && SelectedScaleMode == AmplitudeScaleMode.Normalized)
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
