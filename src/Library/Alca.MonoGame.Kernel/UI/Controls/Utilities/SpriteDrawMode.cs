namespace Alca.MonoGame.Kernel.UI.Controls.Utilities;

/// <summary>Determines how a UISprite texture is scaled or tiled within its Bounds.</summary>
public enum SpriteDrawMode
{
    /// <summary>Scales the texture to fill Bounds exactly, ignoring aspect ratio.</summary>
    Stretch,

    /// <summary>Scales uniformly to fit within Bounds, preserving aspect ratio.</summary>
    Fit,

    /// <summary>Draws at natural size from the top-left corner; content outside Bounds must be clipped by the caller.</summary>
    Crop,

    /// <summary>Tiles the texture; requires SamplerState.LinearWrap in the active SpriteBatch.Begin call.</summary>
    Tile,
}
