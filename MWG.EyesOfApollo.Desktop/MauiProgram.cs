using Microsoft.Extensions.Logging;
using MWG.EyesOfApollo.Desktop.Services;
using MWG.EyesOfApollo.Desktop.ViewModels;

namespace MWG.EyesOfApollo.Desktop
{
    public static class MauiProgram
    {
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
