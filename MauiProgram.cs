using Microsoft.Extensions.Logging;
using WeightRecall.Data;
//using WeightRecall.Repository;
//using WeightRecall.Services;

namespace WeightRecall
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

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // Add these to your MauiProgram.cs CreateMauiApp method
            builder.Services.AddSingleton<DatabaseContext>();
            //builder.Services.AddSingleton<RoutineRepository>();
            //builder.Services.AddSingleton<RoutineService>();

            return builder.Build();
        }
    }
}
