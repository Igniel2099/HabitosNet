using CommunityToolkit.Maui;
using HabitosNet.Data;
using HabitosNet.ViewModels;
using HabitosNet.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HabitosNet
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "habitos.db");
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<StudyViewModel>();
            builder.Services.AddSingleton<HistoryViewModel>();
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddSingleton<StudyPage>();
            builder.Services.AddSingleton<HistoryPage>();

            var app = builder.Build();

            // v1 simple: crear la BD si no existe (sin migraciones).
            using (var scope = app.Services.CreateScope())
            {
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var db = factory.CreateDbContext();
                db.Database.EnsureCreated();
            }

            return app;
        }
    }
}
