namespace Alca.MonoGame.Kernel.Graphics.Camera;

/// <summary>2D camera with position, zoom, rotation, and world/screen space conversion.</summary>
public sealed class Camera2D
{
    private Matrix _transform;
    private Matrix _inverseTransform;
    private bool _dirty = true;

    private Vector2 _position;
    private float _zoom = 1f;
    private float _rotation;
    private float _minZoom = 0.1f;
    private float _maxZoom = 10f;

    /// <summary>Gets or sets the world-space position of the camera center.</summary>
    public Vector2 Position
    {
        get => _position;
        set { _position = value; _dirty = true; }
    }

    /// <summary>Gets or sets the zoom level. Clamped between <see cref="MinZoom"/> and <see cref="MaxZoom"/>.</summary>
    public float Zoom
    {
        get => _zoom;
        set { _zoom = MathHelper.Clamp(value, _minZoom, _maxZoom); _dirty = true; }
    }

    /// <summary>Gets or sets the camera rotation in radians.</summary>
    public float Rotation
    {
        get => _rotation;
        set { _rotation = value; _dirty = true; }
    }

    /// <summary>Gets or sets the minimum allowed zoom level.</summary>
    public float MinZoom
    {
        get => _minZoom;
        set { _minZoom = value; _zoom = MathHelper.Clamp(_zoom, _minZoom, _maxZoom); _dirty = true; }
    }

    /// <summary>Gets or sets the maximum allowed zoom level.</summary>
    public float MaxZoom
    {
        get => _maxZoom;
        set { _maxZoom = value; _zoom = MathHelper.Clamp(_zoom, _minZoom, _maxZoom); _dirty = true; }
    }

    /// <summary>Returns the transform matrix to pass to <see cref="SpriteBatch.Begin"/>.</summary>
    public Matrix GetTransformMatrix(Viewport viewport)
    {
        if (_dirty)
            Recalculate(viewport);
        return _transform;
    }

    /// <summary>Converts a screen-space position to a world-space position.</summary>
    public Vector2 ScreenToWorld(Vector2 screenPos, Viewport viewport)
    {
        if (_dirty)
            Recalculate(viewport);
        return Vector2.Transform(screenPos, _inverseTransform);
    }

    /// <summary>Converts a world-space position to a screen-space position.</summary>
    public Vector2 WorldToScreen(Vector2 worldPos, Viewport viewport)
    {
        if (_dirty)
            Recalculate(viewport);
        return Vector2.Transform(worldPos, _transform);
    }

    /// <summary>Smoothly moves the camera toward a target position. Call once per frame from Update.</summary>
    public void Follow(Vector2 target, float lerpFactor)
    {
        _position.X = MathHelper.Lerp(_position.X, target.X, lerpFactor);
        _position.Y = MathHelper.Lerp(_position.Y, target.Y, lerpFactor);
        _dirty = true;
    }

    /// <summary>Clamps the camera position so it stays within the given world bounds.</summary>
    public void ClampToBounds(Rectangle worldBounds)
    {
        _position.X = MathHelper.Clamp(_position.X, worldBounds.Left, worldBounds.Right);
        _position.Y = MathHelper.Clamp(_position.Y, worldBounds.Top, worldBounds.Bottom);
        _dirty = true;
    }

    private void Recalculate(Viewport viewport)
    {
        float centerX = viewport.Width * 0.5f;
        float centerY = viewport.Height * 0.5f;

        _transform =
            Matrix.CreateTranslation(-_position.X, -_position.Y, 0f) *
            Matrix.CreateRotationZ(_rotation) *
            Matrix.CreateScale(_zoom, _zoom, 1f) *
            Matrix.CreateTranslation(centerX, centerY, 0f);

        _inverseTransform = Matrix.Invert(_transform);
        _dirty = false;
    }
}
