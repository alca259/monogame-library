using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGame.Editor.WinForms.Rendering;

/// <summary>
/// Draws pillarbox/letterbox bars over the viewport to visualise the game's virtual resolution
/// relative to the current editor viewport size.
/// Uses screen-space rendering (no camera transform).
/// </summary>
public sealed class ResolutionPreviewRenderer : IDisposable
{
    private static readonly XnaColor BarColor    = new(0,   0,   0,   200);
    private static readonly XnaColor BorderColor = new(0, 220, 220, 180);

    private SpriteBatch? _spriteBatch;
    private Texture2D?   _pixel;

    /// <summary>Returns <c>true</c> once <see cref="Initialize"/> has been called.</summary>
    public bool IsInitialized => _spriteBatch != null;

    /// <summary>Allocates GPU resources. Must be called from the render thread.</summary>
    public void Initialize(GraphicsDevice gd)
    {
        _spriteBatch = new SpriteBatch(gd);
        _pixel       = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { XnaColor.White });
    }

    /// <summary>
    /// Draws letterbox/pillarbox bars and a border for the virtual resolution area.
    /// No-op when <see cref="IsInitialized"/> is false or virtual dimensions are zero.
    /// </summary>
    public void Draw(int virtualWidth, int virtualHeight, int viewportWidth, int viewportHeight)
    {
        if (_spriteBatch is null || _pixel is null) return;
        if (virtualWidth <= 0 || virtualHeight <= 0) return;
        if (viewportWidth <= 0 || viewportHeight <= 0) return;

        float virtualAspect  = (float)virtualWidth  / virtualHeight;
        float viewportAspect = (float)viewportWidth / viewportHeight;

        int drawW, drawH, offsetX, offsetY;
        if (viewportAspect >= virtualAspect)
        {
            // Wider viewport → pillarbox (vertical bars on sides)
            drawH   = viewportHeight;
            drawW   = (int)MathF.Round(viewportHeight * virtualAspect);
            offsetX = (viewportWidth - drawW) / 2;
            offsetY = 0;
        }
        else
        {
            // Taller viewport → letterbox (horizontal bars top/bottom)
            drawW   = viewportWidth;
            drawH   = (int)MathF.Round(viewportWidth / virtualAspect);
            offsetX = 0;
            offsetY = (viewportHeight - drawH) / 2;
        }

        _spriteBatch.Begin();

        // Left bar
        if (offsetX > 0)
            FillRect(0, 0, offsetX, viewportHeight, BarColor);

        // Right bar
        if (offsetX > 0)
            FillRect(offsetX + drawW, 0, viewportWidth - offsetX - drawW, viewportHeight, BarColor);

        // Top bar
        if (offsetY > 0)
            FillRect(0, 0, viewportWidth, offsetY, BarColor);

        // Bottom bar
        if (offsetY > 0)
            FillRect(0, offsetY + drawH, viewportWidth, viewportHeight - offsetY - drawH, BarColor);

        // Border outline around the virtual area
        const int BorderThickness = 1;
        FillRect(offsetX,                  offsetY,                  drawW,            BorderThickness, BorderColor);
        FillRect(offsetX,                  offsetY + drawH - 1,      drawW,            BorderThickness, BorderColor);
        FillRect(offsetX,                  offsetY,                  BorderThickness,  drawH,           BorderColor);
        FillRect(offsetX + drawW - 1,      offsetY,                  BorderThickness,  drawH,           BorderColor);

        _spriteBatch.End();
    }

    private void FillRect(int x, int y, int w, int h, XnaColor color)
    {
        if (w <= 0 || h <= 0) return;
        _spriteBatch!.Draw(_pixel!, new Microsoft.Xna.Framework.Rectangle(x, y, w, h), color);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _pixel?.Dispose();
        _spriteBatch?.Dispose();
        _pixel       = null;
        _spriteBatch = null;
    }
}
