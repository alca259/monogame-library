namespace MonoGame.Editor.WinForms.Controls;

/// <summary>
/// Lightweight 2D camera for the editor viewport.
/// Supports pan (middle mouse) and zoom (scroll wheel) independently of any Kernel reference.
/// </summary>
public sealed class EditorCamera2D
{
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 10f;

    /// <summary>World-space position of the camera center.</summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>Zoom scale factor, clamped to [0.1, 10].</summary>
    public float Zoom { get; private set; } = 1f;

    /// <summary>
    /// Returns the transform matrix to use in <c>SpriteBatch.Begin(transformMatrix: ...)</c>.
    /// Translates by -Position, scales by Zoom, then re-centers on the viewport.
    /// </summary>
    public Matrix GetTransformMatrix(Viewport viewport) =>
        Matrix.CreateTranslation(-Position.X, -Position.Y, 0f)
        * Matrix.CreateScale(Zoom, Zoom, 1f)
        * Matrix.CreateTranslation(viewport.Width * 0.5f, viewport.Height * 0.5f, 0f);

    /// <summary>Converts a screen-space point to world-space coordinates.</summary>
    public Vector2 ScreenToWorld(Vector2 screenPos, Viewport viewport)
    {
        Matrix inverse = Matrix.Invert(GetTransformMatrix(viewport));
        return Vector2.Transform(screenPos, inverse);
    }

    /// <summary>Pans the camera by <paramref name="worldDelta"/> world units.</summary>
    public void Pan(Vector2 worldDelta) => Position += worldDelta;

    /// <summary>
    /// Zooms by <paramref name="factor"/> keeping the point at <paramref name="screenFocus"/> stationary.
    /// </summary>
    public void ZoomAt(float factor, Vector2 screenFocus, Viewport viewport)
    {
        Vector2 worldBefore = ScreenToWorld(screenFocus, viewport);
        Zoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
        Vector2 worldAfter = ScreenToWorld(screenFocus, viewport);
        Position += worldBefore - worldAfter;
    }
}
