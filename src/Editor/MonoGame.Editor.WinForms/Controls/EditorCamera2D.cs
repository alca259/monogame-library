namespace MonoGame.Editor.WinForms.Controls;

/// <summary>
/// Cámara 2D ligera para el viewport del editor.
/// Soporta paneo (botón central del ratón) y zoom (rueda del ratón) de forma independiente de cualquier referencia al Kernel.
/// </summary>
public sealed class EditorCamera2D
{
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 10f;

    /// <summary>Posición en el espacio mundo del centro de la cámara.</summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>Factor de escala del zoom, limitado a [0.1, 10].</summary>
    public float Zoom { get; private set; } = 1f;

    /// <summary>
    /// Devuelve la matriz de transformación para usar en <c>SpriteBatch.Begin(transformMatrix: ...)</c>.
    /// Traslada por -Position, escala por Zoom y luego vuelve a centrar en el viewport.
    /// </summary>
    public Matrix GetTransformMatrix(Viewport viewport) =>
        Matrix.CreateTranslation(-Position.X, -Position.Y, 0f)
        * Matrix.CreateScale(Zoom, Zoom, 1f)
        * Matrix.CreateTranslation(viewport.Width * 0.5f, viewport.Height * 0.5f, 0f);

    /// <summary>Convierte un punto en espacio de pantalla a coordenadas en espacio mundo.</summary>
    public Vector2 ScreenToWorld(Vector2 screenPos, Viewport viewport)
    {
        Matrix inverse = Matrix.Invert(GetTransformMatrix(viewport));
        return Vector2.Transform(screenPos, inverse);
    }

    /// <summary>Desplaza la cámara <paramref name="worldDelta"/> unidades en el espacio mundo.</summary>
    public void Pan(Vector2 worldDelta) => Position += worldDelta;

    /// <summary>
    /// Aplica zoom por <paramref name="factor"/> manteniendo estacionario el punto en <paramref name="screenFocus"/>.
    /// </summary>
    public void ZoomAt(float factor, Vector2 screenFocus, Viewport viewport)
    {
        Vector2 worldBefore = ScreenToWorld(screenFocus, viewport);
        Zoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
        Vector2 worldAfter = ScreenToWorld(screenFocus, viewport);
        Position += worldBefore - worldAfter;
    }
}
