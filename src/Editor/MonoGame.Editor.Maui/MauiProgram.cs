using Microsoft.Maui.LifecycleEvents;
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

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windows => windows.OnLaunched((window, args) =>
                {
                    // Captura errores del hilo de la interfaz de WinUI 3 antes de que rompan el proceso
                    Microsoft.UI.Xaml.Application.Current.UnhandledException += (sender, e) =>
                    {
                        // Registramos el error en Serilog
                        Log.Fatal(e.Exception, "Excepción nativa en WinUI: {Message}", e.Message);

                        // Evitamos cierre.
                        e.Handled = true;
                    };
                }));
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        return builder.Build();
    }
}
