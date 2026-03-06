using MWG.EyesOfApollo.Desktop.ViewModels;

namespace MWG.EyesOfApollo.Desktop
{
    /// <summary>
    /// Main visualizer page.
    /// </summary>
    public partial class MainPage
    {
        private readonly MainViewModel _viewModel;
        private readonly IDispatcherTimer _renderTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        /// <param name="viewModel">The main view model.</param>
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            BindingContext = _viewModel;
            VisualizerView.Drawable = _viewModel.VisualizerDrawable;

            _viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.SelectedFrameRate))
                {
                    StartRenderTimer();
                }
            };

            _renderTimer = Dispatcher.CreateTimer();
            _renderTimer.Tick += (_, _) =>
            {
                _viewModel.TickFrame();
                VisualizerView.Invalidate();
            };
        }

        /// <inheritdoc />
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
            StartRenderTimer();
        }

        /// <inheritdoc />
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _renderTimer.Stop();
        }

        private void StartRenderTimer()
        {
            var targetFps = Math.Max(30, _viewModel.SelectedFrameRate);
            _renderTimer.Interval = TimeSpan.FromMilliseconds(1000d / targetFps);
            _renderTimer.Stop();
            _renderTimer.Start();
        }
    }
}
