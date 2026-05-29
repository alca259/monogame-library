namespace Alca.MonoGame.Kernel.UI;

/// <summary>Defines the four independent border insets and rendering mode for a 9-slice sprite.</summary>
public readonly struct NineSliceBorderData
{
    /// <summary>Pixels from the left edge of the source texture treated as a fixed border.</summary>
    public int Left { get; init; }

    /// <summary>Pixels from the right edge of the source texture treated as a fixed border.</summary>
    public int Right { get; init; }

    /// <summary>Pixels from the top edge of the source texture treated as a fixed border.</summary>
    public int Top { get; init; }

    /// <summary>Pixels from the bottom edge of the source texture treated as a fixed border.</summary>
    public int Bottom { get; init; }

    /// <summary>When <see langword="true"/>, edge regions are tiled instead of stretched.</summary>
    public bool TileEdges { get; init; }

    /// <summary>When <see langword="true"/>, the center region is tiled instead of stretched.</summary>
    public bool TileCenter { get; init; }

    /// <summary>Returns a <see cref="NineSliceBorderData"/> with identical insets on all four sides.</summary>
    public static NineSliceBorderData Uniform(int border) =>
        new() { Left = border, Right = border, Top = border, Bottom = border };
}
