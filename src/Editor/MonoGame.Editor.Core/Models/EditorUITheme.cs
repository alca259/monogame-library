namespace MonoGame.Editor.Core.Models;

/// <summary>
/// Configuración NineSlice para un único tipo de control.
/// Se serializa dentro de <see cref="EditorUITheme"/>.
/// </summary>
public sealed class EditorUIThemeEntry
{
    /// <summary>Ruta de textura relativa al Content (sin extensión). Null o vacío = sin textura NineSlice.</summary>
    public string TexturePath { get; set; } = string.Empty;

    /// <summary>Píxeles desde el borde izquierdo tratados como borde fijo.</summary>
    public int BorderLeft { get; set; } = 8;

    /// <summary>Píxeles desde el borde derecho tratados como borde fijo.</summary>
    public int BorderRight { get; set; } = 8;

    /// <summary>Píxeles desde el borde superior tratados como borde fijo.</summary>
    public int BorderTop { get; set; } = 8;

    /// <summary>Píxeles desde el borde inferior tratados como borde fijo.</summary>
    public int BorderBottom { get; set; } = 8;

    /// <summary>Cuando es true, las regiones de borde se tesean en lugar de estirarse.</summary>
    public bool TileEdges { get; set; }

    /// <summary>Cuando es true, la región central se tesea en lugar de estirarse.</summary>
    public bool TileCenter { get; set; }
}

/// <summary>
/// Modelo del lado del editor para un recurso de tema de UI (.uitheme.json).
/// Almacena rutas de textura NineSlice y márgenes de borde para cada tipo de control soportado.
/// </summary>
public sealed class EditorUITheme
{
    /// <summary>Nombre de visualización para este tema.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Configuración NineSlice para <c>Panel</c>.</summary>
    public EditorUIThemeEntry Panel { get; set; } = new();

    /// <summary>Configuración NineSlice para <c>Button</c>.</summary>
    public EditorUIThemeEntry Button { get; set; } = new();

    /// <summary>Configuración NineSlice para el encabezado de <c>Dropdown</c>.</summary>
    public EditorUIThemeEntry Dropdown { get; set; } = new();

    /// <summary>Configuración NineSlice para el borde de <c>ProgressBar</c>.</summary>
    public EditorUIThemeEntry ProgressBar { get; set; } = new() { BorderLeft = 4, BorderRight = 4, BorderTop = 4, BorderBottom = 4 };

    /// <summary>Configuración NineSlice para <c>TextBox</c> / <c>TextArea</c>.</summary>
    public EditorUIThemeEntry TextBox { get; set; } = new() { BorderLeft = 4, BorderRight = 4, BorderTop = 4, BorderBottom = 4 };

    /// <summary>Devuelve un nuevo tema vacío listo para edición.</summary>
    public static EditorUITheme CreateEmpty(string name = "New UI Theme") => new() { Name = name };
}
