using Microsoft.Xna.Framework;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGame.Editor.Maui.Rendering;

/// <summary>
/// Renderiza la cuadrícula del viewport en espacio de pantalla (líneas de 1px fijas
/// independientemente del nivel de zoom) usando un único pixel blanco como textura base.
/// </summary>
internal sealed class GridRenderer : IDisposable
{
    private Texture2D?   _pixel;
    private SpriteBatch? _spriteBatch;

    /// <summary>Reserva los recursos de GPU. Llamar desde el hilo de render.</summary>
    public void Initialize(GraphicsDevice gd)
    {
        _pixel?.Dispose();
        _spriteBatch?.Dispose();

        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { XnaColor.White });
        _spriteBatch = new SpriteBatch(gd);
    }

    /// <summary>
    /// Dibuja la cuadrícula en espacio de pantalla. Omite líneas si la separación
    /// en pantalla sería menor de 4 píxeles (evita ruido visual a zoom muy bajo).
    /// </summary>
    public void DrawGrid(EditorCamera2D camera, Viewport viewport, float cellSize)
    {
        if (_pixel is null || _spriteBatch is null) return;
        if (cellSize < 1f) return;

        float screenCellSize = cellSize * camera.Zoom;
        if (screenCellSize < 4f) return;

        XnaColor lineColor = new XnaColor(55, 55, 60, 180);

        // Calcular el rect del mundo visible en pantalla
        Matrix inv = Matrix.Invert(camera.GetTransformMatrix(viewport));
        Vector2 worldTL = Vector2.Transform(Vector2.Zero, inv);
        Vector2 worldBR = Vector2.Transform(new Vector2(viewport.Width, viewport.Height), inv);

        float worldLeft   = MathF.Min(worldTL.X, worldBR.X);
        float worldRight  = MathF.Max(worldTL.X, worldBR.X);
        float worldTop    = MathF.Min(worldTL.Y, worldBR.Y);
        float worldBottom = MathF.Max(worldTL.Y, worldBR.Y);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        try
        {
            // Líneas verticales (x = n * cellSize)
            float startX = MathF.Floor(worldLeft / cellSize) * cellSize;
            for (float wx = startX; wx <= worldRight + cellSize; wx += cellSize)
            {
                float screenX = (wx - camera.Position.X) * camera.Zoom + viewport.Width * 0.5f;
                int sx = (int)MathF.Round(screenX);
                if (sx < 0 || sx >= viewport.Width) continue;
                _spriteBatch.Draw(_pixel, new Rectangle(sx, 0, 1, viewport.Height), lineColor);
            }

            // Líneas horizontales (y = n * cellSize)
            float startY = MathF.Floor(worldTop / cellSize) * cellSize;
            for (float wy = startY; wy <= worldBottom + cellSize; wy += cellSize)
            {
                float screenY = (wy - camera.Position.Y) * camera.Zoom + viewport.Height * 0.5f;
                int sy = (int)MathF.Round(screenY);
                if (sy < 0 || sy >= viewport.Height) continue;
                _spriteBatch.Draw(_pixel, new Rectangle(0, sy, viewport.Width, 1), lineColor);
            }
        }
        catch { }
        finally
        {
            try { _spriteBatch.End(); }
            catch { }
        }
    }

    public void Dispose()
    {
        _pixel?.Dispose();
        _spriteBatch?.Dispose();
        _pixel = null;
        _spriteBatch = null;
    }
}
