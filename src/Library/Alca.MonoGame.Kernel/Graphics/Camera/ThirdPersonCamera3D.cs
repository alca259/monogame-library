namespace Alca.MonoGame.Kernel.Graphics.Camera;

/// <summary>3D third-person camera that follows a target with spring-damper smoothing.</summary>
public sealed class ThirdPersonCamera3D : Camera3D
{
    private Matrix _view;
    private readonly Matrix _projection;
    private Vector3 _velocity;
    private Vector3 _currentPosition;

    /// <summary>Gets or sets the spring stiffness. Higher values produce a snappier follow.</summary>
    public float SpringStiffness { get; set; } = 1800f;

    /// <summary>Gets or sets the spring damping. Higher values reduce oscillation.</summary>
    public float DampingRatio { get; set; } = 600f;

    /// <summary>Gets or sets the effective mass of the camera. Higher values slow the response.</summary>
    public float Mass { get; set; } = 50f;

    /// <summary>Gets or sets the offset from the target to the desired camera position in target-local space.</summary>
    public Vector3 Offset { get; set; } = new Vector3(0f, 150f, 500f);

    /// <inheritdoc/>
    public override Matrix View => _view;

    /// <inheritdoc/>
    public override Matrix Projection => _projection;

    /// <summary>Creates a third-person camera.</summary>
    /// <param name="initialTargetPosition">Starting world-space position of the followed target.</param>
    /// <param name="fieldOfView">Vertical field of view in degrees.</param>
    /// <param name="aspectRatio">Viewport width divided by height.</param>
    /// <param name="nearPlane">Near clipping distance.</param>
    /// <param name="farPlane">Far clipping distance.</param>
    public ThirdPersonCamera3D(
        Vector3 initialTargetPosition,
        float fieldOfView,
        float aspectRatio,
        float nearPlane = 0.1f,
        float farPlane = 10000f)
    {
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(fieldOfView), aspectRatio, nearPlane, farPlane);

        // Initialize at the desired position to prevent a snap on the first frame.
        _currentPosition = initialTargetPosition + Offset;
        Position = _currentPosition;
        Target = initialTargetPosition;
        _view = BuildView(Position, Target);
    }

    /// <summary>Advances the spring-damper simulation and rebuilds the view. Call once per frame from Update.</summary>
    /// <param name="targetPosition">Current world-space position of the followed entity.</param>
    /// <param name="targetRotationY">Current Y-axis rotation of the followed entity in radians.</param>
    /// <param name="elapsed">Elapsed time in seconds since last frame.</param>
    public void Update(Vector3 targetPosition, float targetRotationY, float elapsed)
    {
        Matrix rotation = Matrix.CreateRotationY(targetRotationY);
        Vector3 desiredPos = Vector3.Transform(Offset, rotation) + targetPosition;

        Vector3 stretch = _currentPosition - desiredPos;
        Vector3 force = -SpringStiffness * stretch - DampingRatio * _velocity;
        Vector3 acceleration = force / Mass;

        _velocity += acceleration * elapsed;
        _currentPosition += _velocity * elapsed;

        Position = _currentPosition;
        Target = targetPosition;
        _view = BuildView(Position, Target);
        InvalidateFrustum();
    }

    /// <summary>Snaps the camera to the desired position instantly, bypassing the spring.</summary>
    public void SnapTo(Vector3 targetPosition, float targetRotationY)
    {
        Matrix rotation = Matrix.CreateRotationY(targetRotationY);
        _currentPosition = Vector3.Transform(Offset, rotation) + targetPosition;
        _velocity = Vector3.Zero;
        Position = _currentPosition;
        Target = targetPosition;
        _view = BuildView(Position, Target);
        InvalidateFrustum();
    }
}
