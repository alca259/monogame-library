using System.Drawing;

namespace MonoGame.Editor.Winforms.Rendering;

/// <summary>
/// Cámara 2D para el viewport del editor: pan y zoom con conversión Y-up entre espacio de mundo y pantalla.
/// Port directo de <c>MonoGame.Editor.Maui.Rendering.EditorCamera2D</c>; sin dependencias de MAUI.
/// </summary>
public sealed class EditorCamera2D
{
    private const float MinZoom = 0.001f;
    private const float MaxZoom = 5000f;

    /// <summary>Zoom inicial: 50 px por unidad de mundo (1 m = 50 px).</summary>
    public const float DefaultZoom = 50f;

    /// <summary>Posición en espacio mundo del centro de la cámara.</summary>
    public PointF Position { get; set; } = PointF.Empty;

    /// <summary>Factor de escala del zoom, limitado a [0.001, 5000].</summary>
    public float Zoom { get; private set; } = DefaultZoom;

    /// <summary>
    /// Convierte un punto de espacio mundo a coordenadas de pantalla.
    /// El eje Y se invierte para que Y positivo apunte hacia arriba (convención Y-up).
    /// </summary>
    public PointF WorldToScreen(PointF worldPos, SizeF viewportSize)
    {
        float x =  (worldPos.X - Position.X) * Zoom + viewportSize.Width  * 0.5f;
        float y = -(worldPos.Y - Position.Y) * Zoom + viewportSize.Height * 0.5f;
        return new PointF(x, y);
    }

    /// <summary>Convierte un punto de pantalla a coordenadas de espacio mundo (Y-up).</summary>
    public PointF ScreenToWorld(PointF screenPos, SizeF viewportSize)
    {
        float x =  (screenPos.X - viewportSize.Width  * 0.5f) / Zoom + Position.X;
        float y = -(screenPos.Y - viewportSize.Height * 0.5f) / Zoom + Position.Y;
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
