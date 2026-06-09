namespace MonoGame.Editor.Maui.Rendering;

/// <summary>
/// Cámara 2D para el viewport del editor. Pan y zoom se aplican desde los
/// gesture recognizers del GraphicsView; esta clase solo contiene la matemática.
/// </summary>
public sealed class EditorCamera2D
{
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 10f;

    /// <summary>Posición en espacio mundo del centro de la cámara.</summary>
    public PointF Position { get; set; } = PointF.Zero;

    /// <summary>Factor de escala del zoom, limitado a [0.1, 10].</summary>
    public float Zoom { get; private set; } = 1f;

    /// <summary>Convierte un punto de espacio mundo a coordenadas de pantalla.</summary>
    public PointF WorldToScreen(PointF worldPos, SizeF viewportSize)
    {
        float x = (worldPos.X - Position.X) * Zoom + viewportSize.Width * 0.5f;
        float y = (worldPos.Y - Position.Y) * Zoom + viewportSize.Height * 0.5f;
        return new PointF(x, y);
    }

    /// <summary>Convierte un punto de pantalla a coordenadas de espacio mundo.</summary>
    public PointF ScreenToWorld(PointF screenPos, SizeF viewportSize)
    {
        float x = (screenPos.X - viewportSize.Width * 0.5f) / Zoom + Position.X;
        float y = (screenPos.Y - viewportSize.Height * 0.5f) / Zoom + Position.Y;
        return new PointF(x, y);
    }

    /// <summary>Desplaza la cámara <paramref name="worldDelta"/> unidades en espacio mundo.</summary>
    public void Pan(PointF worldDelta) =>
        Position = new PointF(Position.X + worldDelta.X, Position.Y + worldDelta.Y);

    /// <summary>
    /// Aplica zoom por <paramref name="factor"/> manteniendo estacionario
    /// el punto <paramref name="screenFocus"/> en pantalla.
    /// </summary>
    public void ZoomAt(float factor, PointF screenFocus, SizeF viewportSize)
    {
        PointF worldBefore = ScreenToWorld(screenFocus, viewportSize);
        Zoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
        PointF worldAfter = ScreenToWorld(screenFocus, viewportSize);
        Position = new PointF(
            Position.X + worldBefore.X - worldAfter.X,
            Position.Y + worldBefore.Y - worldAfter.Y);
    }
}
