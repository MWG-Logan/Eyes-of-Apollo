using MWG.EyesOfApollo.Desktop.Services;
using MWG.EyesOfApollo.Desktop.ViewModels;

namespace MWG.EyesOfApollo.Desktop
{
    /// <summary>
    /// Configures the MAUI app and dependency injection.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates the configured <see cref="MauiApp"/> instance.
        /// </summary>
        /// <returns>The configured MAUI app.</returns>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

			builder.Services.AddSingleton<ThemeService>();
			builder.Services.AddSingleton<IAudioCaptureService, AudioCaptureService>();
			builder.Services.AddSingleton<MainViewModel>();
			builder.Services.AddSingleton<MainPage>();
			builder.Services.AddSingleton<AppShell>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
