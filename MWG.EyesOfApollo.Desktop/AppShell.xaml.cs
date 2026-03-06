namespace MWG.EyesOfApollo.Desktop
{
    public partial class AppShell : Shell
    {
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
