using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGame.Editor.WinForms.Rendering;

/// <summary>
/// Dibuja una superposición wireframe de la cuadrícula de navegación de la escena en el viewport del editor.
/// Lee las dimensiones de la cuadrícula directamente desde <see cref="EditorWorldConfig"/> sin instanciar
/// el NavGrid en tiempo de ejecución — seguro de usar en modo edición.
/// </summary>
public sealed class NavGridPreviewRenderer : IDisposable
{
    private static readonly XnaColor GridLineColor  = new(0, 220, 220, 80);
    private static readonly XnaColor BorderColor    = new(0, 220, 220, 180);

    private SpriteBatch?  _spriteBatch;
    private Texture2D?    _pixel;

    /// <summary>Devuelve <c>true</c> una vez que se ha llamado a <see cref="Initialize"/>.</summary>
    public bool IsInitialized => _spriteBatch != null;

    /// <summary>Reserva recursos de GPU. Debe llamarse desde el hilo de renderizado.</summary>
    public void Initialize(GraphicsDevice gd)
    {
        _spriteBatch = new SpriteBatch(gd);
        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { XnaColor.White });
    }

    /// <summary>
    /// Dibuja la superposición de la cuadrícula de navegación usando coordenadas en espacio mundo.
    /// No hace nada cuando <see cref="IsInitialized"/> es false o
    /// <paramref name="config"/> no tiene la navegación habilitada.
    /// </summary>
    public void Draw(EditorWorldConfig? config, Matrix cameraTransform)
    {
        if (_spriteBatch is null || _pixel is null) return;
        if (config is null || !config.UseNavigation) return;

        int   cols     = Math.Max(1, config.NavGridWidth);
        int   rows     = Math.Max(1, config.NavGridHeight);
        float cellSize = Math.Max(1f, config.NavGridCellSize);
        float ox       = config.NavGridOriginX;
        float oy       = config.NavGridOriginY;
        float totalW   = cols * cellSize;
        float totalH   = rows * cellSize;

        _spriteBatch.Begin(transformMatrix: cameraTransform,
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend);

        // ── Líneas interiores de la cuadrícula ────────────────────────────────────────────────
        // Verticales
        for (int c = 1; c < cols; c++)
        {
            float x = ox + c * cellSize;
            DrawLineWorld(x, oy, x, oy + totalH, GridLineColor, 1f);
        }

        // Horizontales
        for (int r = 1; r < rows; r++)
        {
            float y = oy + r * cellSize;
            DrawLineWorld(ox, y, ox + totalW, y, GridLineColor, 1f);
        }

        // ── Borde exterior ───────────────────────────────────────────────────────
        DrawLineWorld(ox,          oy,          ox + totalW, oy,          BorderColor, 2f);
        DrawLineWorld(ox + totalW, oy,          ox + totalW, oy + totalH, BorderColor, 2f);
        DrawLineWorld(ox + totalW, oy + totalH, ox,          oy + totalH, BorderColor, 2f);
        DrawLineWorld(ox,          oy + totalH, ox,          oy,          BorderColor, 2f);

        _spriteBatch.End();
    }

    private void DrawLineWorld(float x1, float y1, float x2, float y2, XnaColor color, float thickness)
    {
        if (_spriteBatch is null || _pixel is null) return;

        float dx  = x2 - x1;
        float dy  = y2 - y1;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.001f) return;

        float angle = MathF.Atan2(dy, dx);
        _spriteBatch.Draw(
            _pixel,
            new Vector2(x1, y1),
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(len, thickness),
            SpriteEffects.None,
            0f);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _spriteBatch?.Dispose();
        _pixel?.Dispose();
    }
}
