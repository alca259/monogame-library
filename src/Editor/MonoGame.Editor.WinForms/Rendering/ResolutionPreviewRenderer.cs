using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGame.Editor.WinForms.Rendering;

/// <summary>
/// Dibuja barras de pillarbox/letterbox sobre el viewport para visualizar la resolución virtual del juego
/// en relación con el tamaño actual del viewport del editor.
/// Usa renderizado en espacio de pantalla (sin transformación de cámara).
/// </summary>
public sealed class ResolutionPreviewRenderer : IDisposable
{
    private static readonly XnaColor BarColor    = new(0,   0,   0,   200);
    private static readonly XnaColor BorderColor = new(0, 220, 220, 180);

    private SpriteBatch? _spriteBatch;
    private Texture2D?   _pixel;

    /// <summary>Devuelve <c>true</c> una vez que se ha llamado a <see cref="Initialize"/>.</summary>
    public bool IsInitialized => _spriteBatch != null;

    /// <summary>Reserva recursos de GPU. Debe llamarse desde el hilo de renderizado.</summary>
    public void Initialize(GraphicsDevice gd)
    {
        _spriteBatch = new SpriteBatch(gd);
        _pixel       = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { XnaColor.White });
    }

    /// <summary>
    /// Dibuja las barras de letterbox/pillarbox y un borde para el área de resolución virtual.
    /// No hace nada cuando <see cref="IsInitialized"/> es false o las dimensiones virtuales son cero.
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
            // Viewport más ancho → pillarbox (barras verticales a los lados)
            drawH   = viewportHeight;
            drawW   = (int)MathF.Round(viewportHeight * virtualAspect);
            offsetX = (viewportWidth - drawW) / 2;
            offsetY = 0;
        }
        else
        {
            // Viewport más alto → letterbox (barras horizontales arriba/abajo)
            drawW   = viewportWidth;
            drawH   = (int)MathF.Round(viewportWidth / virtualAspect);
            offsetX = 0;
            offsetY = (viewportHeight - drawH) / 2;
        }

        _spriteBatch.Begin();

        // Barra izquierda
        if (offsetX > 0)
            FillRect(0, 0, offsetX, viewportHeight, BarColor);

        // Barra derecha
        if (offsetX > 0)
            FillRect(offsetX + drawW, 0, viewportWidth - offsetX - drawW, viewportHeight, BarColor);

        // Barra superior
        if (offsetY > 0)
            FillRect(0, 0, viewportWidth, offsetY, BarColor);

        // Barra inferior
        if (offsetY > 0)
            FillRect(0, offsetY + drawH, viewportWidth, viewportHeight - offsetY - drawH, BarColor);

        // Contorno del borde alrededor del área virtual
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
