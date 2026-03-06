using MWG.EyesOfApollo.Desktop.ViewModels;

namespace MWG.EyesOfApollo.Desktop
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;
        private readonly IDispatcherTimer _renderTimer;

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

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
            StartRenderTimer();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _renderTimer.Stop();
        }

        private void StartRenderTimer()
        {
            var targetFps = Math.Max(30, _viewModel.SelectedFrameRate);
            _renderTimer.Interval = TimeSpan.FromMilliseconds(1000d / targetFps);
            _renderTimer.Start();
        }
    }
}
