using Serilog;

namespace MonoGame.Editor.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MonoGameEditor", "logs", "editor-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Application.ThreadException += OnThreadException;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.SetColorMode(SystemColorMode.Dark);

        try
        {
            Application.Run(new EditorForm(EditorContext.Instance));
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = (Exception)e.ExceptionObject;
        Log.Fatal(ex, "Unhandled non-UI exception");
        MessageBox.Show(
            $"A fatal error occurred:\n\n{ex.Message}",
            "MonoGame Editor — Fatal Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI thread exception");
        MessageBox.Show(
            $"An error occurred:\n\n{e.Exception.Message}",
            "MonoGame Editor — Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
