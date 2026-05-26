namespace Alca.MonoGame.Kernel.Graphics.Camera;

/// <summary>3D camera with a constant position and target. View is built once and never updated.</summary>
public sealed class FixedCamera3D : Camera3D
{
    private Matrix _view;
    private readonly Matrix _projection;

    /// <inheritdoc/>
    public override Matrix View => _view;

    /// <inheritdoc/>
    public override Matrix Projection => _projection;

    /// <summary>Creates a fixed camera.</summary>
    /// <param name="position">Camera eye position in world space.</param>
    /// <param name="target">The point the camera looks at.</param>
    /// <param name="fieldOfView">Vertical field of view in degrees.</param>
    /// <param name="aspectRatio">Viewport width divided by height.</param>
    /// <param name="nearPlane">Near clipping distance.</param>
    /// <param name="farPlane">Far clipping distance.</param>
    public FixedCamera3D(
        Vector3 position,
        Vector3 target,
        float fieldOfView,
        float aspectRatio,
        float nearPlane = 0.1f,
        float farPlane = 10000f)
    {
        Position = position;
        Target = target;
        _view = BuildView(position, target);
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(fieldOfView), aspectRatio, nearPlane, farPlane);
    }

    /// <summary>Updates the camera position and target, rebuilding the view matrix.</summary>
    public void SetPositionAndTarget(Vector3 position, Vector3 target)
    {
        Position = position;
        Target = target;
        _view = BuildView(position, target);
        InvalidateFrustum();
    }
}
