using Alca.MonoGame.Kernel.Mathematics;

namespace Alca.MonoGame.Kernel.Graphics.Camera;

/// <summary>
/// A camera designed for isometric scenes. Internally wraps a <see cref="Camera2D"/> and an
/// <see cref="IsometricHelper"/> so that world↔screen conversions automatically account for
/// the isometric projection.
/// </summary>
public sealed class IsometricCamera
{
    private readonly Camera2D _camera;
    private readonly IsometricHelper _helper;

    /// <summary>Gets the underlying <see cref="Camera2D"/> for direct manipulation or SpriteBatch matrix use.</summary>
    public Camera2D Camera => _camera;

    /// <summary>Gets the <see cref="IsometricHelper"/> used for tile-space conversions.</summary>
    public IsometricHelper TileHelper => _helper;

    /// <summary>Gets or sets the zoom level. Delegates to <see cref="Camera2D.Zoom"/>.</summary>
    public float Zoom
    {
        get => _camera.Zoom;
        set => _camera.Zoom = value;
    }

    /// <summary>
    /// Creates a new <see cref="IsometricCamera"/> with the specified tile dimensions.
    /// </summary>
    /// <param name="tileWidth">Width of a single isometric tile in pixels. Defaults to 64.</param>
    /// <param name="tileHeight">Height of a single isometric tile in pixels. Defaults to 32.</param>
    public IsometricCamera(float tileWidth = IsometricHelper.DefaultTileWidth, float tileHeight = IsometricHelper.DefaultTileHeight)
    {
        _camera = new Camera2D();
        _helper = new IsometricHelper(tileWidth, tileHeight);
    }

    /// <summary>Returns the transform matrix to pass to <see cref="SpriteBatch.Begin"/>.</summary>
    public Matrix GetTransformMatrix(Viewport viewport)
        => _camera.GetTransformMatrix(viewport);

    /// <summary>
    /// Converts a raw screen-space position to isometric world coordinates.
    /// First applies the camera's inverse transform, then converts the result from screen-isometric to world space.
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screenPos, Viewport viewport)
    {
        Vector2 cameraWorld = _camera.ScreenToWorld(screenPos, viewport);
        return _helper.ScreenToWorld(cameraWorld);
    }

    /// <summary>
    /// Centers the camera on the given isometric world position by projecting it to screen space first.
    /// </summary>
    public void CenterOn(Vector2 worldPos)
    {
        _camera.Position = _helper.WorldToScreen(worldPos);
    }
}
