using MonoGame.Editor.Winforms.Forms;
using MonoGame.Editor.Winforms.Infrastructure;
using Serilog;

namespace MonoGame.Editor.Winforms;

static class Program
{
    [STAThread]
    static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MonoGameEditor", "logs", "editor-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            ApplicationConfiguration.Initialize();
            Application.SetColorMode(SystemColorMode.Dark);

            UiDispatcher.Capture();
            EditorBootstrapper.Init();

            Application.Run(new MainForm());
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
