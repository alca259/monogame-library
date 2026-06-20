namespace Alca.MonoGame.Kernel.UI.Core;

/// <summary>Horizontal alignment within a cell or container.</summary>
public enum HAlign
{
    /// <summary>Aligns content to the left edge; stretches if used as cell fill.</summary>
    Left,

    /// <summary>Centers content horizontally.</summary>
    Center,

    /// <summary>Aligns content to the right edge.</summary>
    Right
}

/// <summary>Vertical alignment within a cell or container.</summary>
public enum VAlign
{
    /// <summary>Aligns content to the top edge; stretches if used as cell fill.</summary>
    Top,

    /// <summary>Centers content vertically.</summary>
    Middle,

    /// <summary>Aligns content to the bottom edge.</summary>
    Bottom
}

/// <summary>Direction of layout flow for stack-based and progress controls.</summary>
public enum Orientation
{
    /// <summary>Children laid out left to right.</summary>
    Horizontal,

    /// <summary>Children laid out top to bottom.</summary>
    Vertical
}

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
