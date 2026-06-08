namespace MonoGame.Editor.Core;

/// <summary>
/// Panel/área del editor que tiene el foco de entrada. Determina qué atajos de teclado
/// responden: los globales actúan siempre, el resto solo en su contexto.
/// </summary>
public enum EditorFocusContext
{
    /// <summary>Sin panel concreto enfocado (menú superior, barra de herramientas, fuera de paneles).</summary>
    Global,

    /// <summary>Viewport de la escena: tools de cámara y gizmos (Q/W/E/R/T/H, snap, delete, focus).</summary>
    Viewport,

    /// <summary>Panel de jerarquía de la escena.</summary>
    Hierarchy,

    /// <summary>Panel inspector.</summary>
    Inspector,

    /// <summary>Pestaña Assets del dock.</summary>
    Assets,

    /// <summary>Pestaña Console del dock.</summary>
    Console,

    /// <summary>Pestaña Scenes del dock.</summary>
    Scenes,
}
