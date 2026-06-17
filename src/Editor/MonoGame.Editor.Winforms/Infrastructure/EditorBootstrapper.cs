using MonoGame.Editor.Core;
using Serilog;

namespace MonoGame.Editor.Winforms.Infrastructure;

/// <summary>Inicializa los singletons del editor antes de que abra la ventana principal.</summary>
internal static class EditorBootstrapper
{
    internal static void Init()
    {
        // Fuerza la creación del singleton (lazy) y registra el inicio.
        EditorContext.Instance.Logger.Log("Editor iniciado.");
        Log.Information("EditorBootstrapper: EditorContext inicializado.");
    }
}
