namespace Alca.MonoGame.Kernel.UI.Layout;

/// <summary>Anchor position for a child element within an <see cref="AnchorLayout"/>.</summary>
public enum Anchor
{
    /// <summary>Top-left corner of the container.</summary>
    TopLeft,

    /// <summary>Top edge, horizontally centered.</summary>
    TopCenter,

    /// <summary>Top-right corner of the container.</summary>
    TopRight,

    /// <summary>Left edge, vertically centered.</summary>
    MiddleLeft,

    /// <summary>Horizontally and vertically centered.</summary>
    Center,

    /// <summary>Right edge, vertically centered.</summary>
    MiddleRight,

    /// <summary>Bottom-left corner of the container.</summary>
    BottomLeft,

    /// <summary>Bottom edge, horizontally centered.</summary>
    BottomCenter,

    /// <summary>Bottom-right corner of the container.</summary>
    BottomRight,

    /// <summary>Fills the entire container bounds; offset is applied as inset margin on each axis.</summary>
    Fill
}
