using Microsoft.Extensions.Logging;
using PPQ.Singleton;

namespace PPQ
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
                    // Mis fuentes.
                    fonts.AddFont("CreatoDisplayBold.otf", "CreatoDisplay");
                    fonts.AddFont("Formula1.ttf", "F1");
                    fonts.AddFont("F1Regular.ttf", "F1Regular");
                    fonts.AddFont("911.ttf", "911");
                    fonts.AddFont("Presidency.ttf", "Presidency");
                    fonts.AddFont("UnageoMedium.ttf", "Unageo");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<MultiplayerService>();

            return builder.Build();
        }
    }
}
