using MonoGame.Editor.Maui.Platforms.Windows;
using Serilog;

namespace MonoGame.Editor.Maui;

static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MonoGameEditor", "logs", "editor-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += static (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Log.Fatal(ex, "Unhandled exception: {Message}", ex.Message);
        };

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            })
            .ConfigureMauiHandlers(h =>
            {
                h.AddHandler<MonoGameView, MonoGameViewHandler>();
            });

        return builder.Build();
    }
}
