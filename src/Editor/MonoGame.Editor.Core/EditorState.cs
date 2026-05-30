namespace MonoGame.Editor.Core;

/// <summary>Representa el estado operativo actual del editor.</summary>
public enum EditorState
{
    /// <summary>El editor está activo; el bucle de juego está detenido, los gizmos son visibles.</summary>
    Editing,

    /// <summary>El bucle de juego está en ejecución con la cámara propia del juego.</summary>
    Playing,

    /// <summary>El bucle de juego está detenido temporalmente; la escena permanece congelada en el último fotograma.</summary>
    Paused,

}
