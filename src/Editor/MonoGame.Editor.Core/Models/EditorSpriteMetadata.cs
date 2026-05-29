namespace MonoGame.Editor.Core.Models;

/// <summary>
/// Serializable metadata for a sprite asset (.sprite.json).
/// Stores the 9-slice border insets and tile mode for a texture, relative to the Content root.
/// </summary>
public sealed class EditorSpriteMetadata
{
    /// <summary>Gets or sets the content-relative path (with extension) to the source texture file.</summary>
    public string TextureRelativePath { get; set; } = string.Empty;

    /// <summary>Pixels from the left edge of the source texture treated as a fixed corner/border.</summary>
    public int BorderLeft { get; set; }

    /// <summary>Pixels from the right edge of the source texture treated as a fixed corner/border.</summary>
    public int BorderRight { get; set; }

    /// <summary>Pixels from the top edge of the source texture treated as a fixed corner/border.</summary>
    public int BorderTop { get; set; }

    /// <summary>Pixels from the bottom edge of the source texture treated as a fixed corner/border.</summary>
    public int BorderBottom { get; set; }

    /// <summary>When <see langword="true"/>, edge regions are tiled instead of stretched at runtime.</summary>
    public bool TileEdges { get; set; }

    /// <summary>When <see langword="true"/>, the center region is tiled instead of stretched at runtime.</summary>
    public bool TileCenter { get; set; }
}
