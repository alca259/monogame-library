namespace Alca.MonoGame.Kernel.Graphics.Camera;

/// <summary>3D top-down camera looking straight down. Supports both fixed look-at and smooth tracking modes.</summary>
public sealed class TopDownCamera3D : Camera3D
{
    private Matrix _view;
    private readonly Matrix _projection;
    private float _height;

    /// <summary>Gets or sets the Y-axis height at which the camera sits above the world.</summary>
    public float Height
    {
        get => _height;
        set { _height = value; Position = new Vector3(Position.X, _height, Position.Z); RebuildView(); }
    }

    /// <inheritdoc/>
    public override Matrix View => _view;

    /// <inheritdoc/>
    public override Matrix Projection => _projection;

    /// <summary>Creates a top-down camera.</summary>
    /// <param name="height">Camera height above the world (Y axis).</param>
    /// <param name="fieldOfView">Vertical field of view in degrees.</param>
    /// <param name="aspectRatio">Viewport width divided by height.</param>
    /// <param name="nearPlane">Near clipping distance.</param>
    /// <param name="farPlane">Far clipping distance.</param>
    public TopDownCamera3D(
        float height,
        float fieldOfView,
        float aspectRatio,
        float nearPlane = 0.1f,
        float farPlane = 100000f)
    {
        _height = height;
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(fieldOfView), aspectRatio, nearPlane, farPlane);

        // Z offset of 1 avoids gimbal lock when using Vector3.Up as the up vector.
        Position = new Vector3(0f, _height, 1f);
        Target = Vector3.Zero;
        _view = BuildView(Position, Target);
    }

    /// <summary>Centers the camera over a fixed world point.</summary>
    public void LookAt(Vector3 worldCenter)
    {
        Position = new Vector3(worldCenter.X, _height, worldCenter.Z + 1f);
        Target = worldCenter;
        RebuildView();
    }

    /// <summary>Smoothly follows a world-space target on the XZ plane. Call once per frame from Update.</summary>
    /// <param name="target">World-space position to follow.</param>
    /// <param name="lerpFactor">Interpolation factor (1 = instant snap, smaller = lag).</param>
    public void Follow(Vector3 target, float lerpFactor = 1f)
    {
        float newX = MathHelper.Lerp(Position.X, target.X, lerpFactor);
        float newZ = MathHelper.Lerp(Position.Z - 1f, target.Z, lerpFactor);
        Position = new Vector3(newX, _height, newZ + 1f);
        Target = new Vector3(newX, 0f, newZ);
        RebuildView();
    }

    private void RebuildView()
    {
        _view = BuildView(Position, Target);
        InvalidateFrustum();
    }
}
