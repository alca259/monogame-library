namespace Alca.MonoGame.Kernel.Lighting;

/// <summary>Logical grouping layer for lights. Allows isolating lighting contributions by scene layer.</summary>
public enum LightingLayer
{
    /// <summary>Standard world-space lighting layer.</summary>
    World,

    /// <summary>UI overlay lighting layer.</summary>
    UI,

    /// <summary>Underground or subsurface lighting layer.</summary>
    Underground,

    /// <summary>Top-most overlay lighting layer.</summary>
    Overlay
}
