namespace MWG.EyesOfApollo.Desktop
{
    /// <summary>
    /// Application entry point for the MAUI app.
    /// </summary>
    public partial class App
    {
        private readonly AppShell _appShell;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        /// <param name="appShell">The application shell.</param>
        public App(AppShell appShell)
        {
            InitializeComponent();
            _appShell = appShell;
        }

        /// <inheritdoc />
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_appShell);
        }
    }
}