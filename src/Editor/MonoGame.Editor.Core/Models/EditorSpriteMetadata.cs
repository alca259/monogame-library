namespace MonoGame.Editor.Core.Models;

/// <summary>
/// Metadatos serializables para un recurso de sprite (.sprite.json).
/// Almacena los márgenes de borde 9-slice y el modo de tesela para una textura, relativos a la raíz de Content.
/// </summary>
public sealed class EditorSpriteMetadata
{
    /// <summary>Obtiene o establece la ruta relativa al Content (con extensión) del archivo de textura fuente.</summary>
    public string TextureRelativePath { get; set; } = string.Empty;

    /// <summary>Píxeles desde el borde izquierdo de la textura fuente tratados como esquina/borde fijo.</summary>
    public int BorderLeft { get; set; }

    /// <summary>Píxeles desde el borde derecho de la textura fuente tratados como esquina/borde fijo.</summary>
    public int BorderRight { get; set; }

    /// <summary>Píxeles desde el borde superior de la textura fuente tratados como esquina/borde fijo.</summary>
    public int BorderTop { get; set; }

    /// <summary>Píxeles desde el borde inferior de la textura fuente tratados como esquina/borde fijo.</summary>
    public int BorderBottom { get; set; }

    /// <summary>Cuando es <see langword="true"/>, las regiones de borde se tesean en lugar de estirarse en tiempo de ejecución.</summary>
    public bool TileEdges { get; set; }

    /// <summary>Cuando es <see langword="true"/>, la región central se tesea en lugar de estirarse en tiempo de ejecución.</summary>
    public bool TileCenter { get; set; }
}
