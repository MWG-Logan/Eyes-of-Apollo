namespace MWG.EyesOfApollo.Desktop
{
    /// <summary>
    /// Shell configuration for the application.
    /// </summary>
    public partial class AppShell
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppShell"/> class.
        /// </summary>
        /// <param name="mainPage">The main page.</param>
        public AppShell(MainPage mainPage)
        {
            InitializeComponent();

            Items.Add(new ShellContent
            {
                Title = "Visualizer",
                Content = mainPage,
                Route = nameof(MainPage)
            });
        }
    }
}
