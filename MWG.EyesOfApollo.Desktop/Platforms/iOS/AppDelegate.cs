using Foundation;

namespace MWG.EyesOfApollo.Desktop.Platforms.iOS
{
    /// <summary>
    /// iOS application delegate for MAUI.
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        /// <inheritdoc />
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
