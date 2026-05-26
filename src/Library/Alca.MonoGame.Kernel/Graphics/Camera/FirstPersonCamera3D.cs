namespace Alca.MonoGame.Kernel.Graphics.Camera;

/// <summary>3D first-person camera steered by yaw and pitch, attached to a player position.</summary>
public sealed class FirstPersonCamera3D : Camera3D
{
    private const float MaxPitchDegrees = 89f;

    private Matrix _view;
    private readonly Matrix _projection;
    private float _yaw;
    private float _pitch;

    /// <inheritdoc/>
    public override Matrix View => _view;

    /// <inheritdoc/>
    public override Matrix Projection => _projection;

    /// <summary>Gets the current horizontal rotation angle in radians.</summary>
    public float Yaw => _yaw;

    /// <summary>Gets the current vertical rotation angle in radians, clamped to ±89°.</summary>
    public float Pitch => _pitch;

    /// <summary>Creates a first-person camera at the given starting position.</summary>
    /// <param name="startPosition">Initial eye position in world space.</param>
    /// <param name="fieldOfView">Vertical field of view in degrees.</param>
    /// <param name="aspectRatio">Viewport width divided by height.</param>
    /// <param name="nearPlane">Near clipping distance.</param>
    /// <param name="farPlane">Far clipping distance.</param>
    public FirstPersonCamera3D(
        Vector3 startPosition,
        float fieldOfView,
        float aspectRatio,
        float nearPlane = 0.1f,
        float farPlane = 10000f)
    {
        Position = startPosition;
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(fieldOfView), aspectRatio, nearPlane, farPlane);
        RebuildView();
    }

    /// <summary>Applies yaw and pitch deltas from mouse or stick input.</summary>
    public void Look(float deltaYaw, float deltaPitch)
    {
        _yaw += deltaYaw;
        _pitch = MathHelper.Clamp(
            _pitch + deltaPitch,
            MathHelper.ToRadians(-MaxPitchDegrees),
            MathHelper.ToRadians(MaxPitchDegrees));
        RebuildView();
    }

    /// <summary>Moves the camera forward or backward along its look direction.</summary>
    public void MoveForward(float speed)
    {
        Vector3 forward = Vector3.Normalize(Target - Position);
        Position += forward * speed;
        Target = Position + forward;
        InvalidateFrustum();
    }

    /// <summary>Moves the camera sideways perpendicular to its look direction.</summary>
    public void Strafe(float speed)
    {
        Vector3 forward = Vector3.Normalize(Target - Position);
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.Up));
        Position += right * speed;
        Target = Position + forward;
        InvalidateFrustum();
    }

    /// <summary>Teleports the camera to a new world-space position, preserving the current look direction.</summary>
    public void SetPosition(Vector3 position)
    {
        Vector3 forward = Vector3.Normalize(Target - Position);
        Position = position;
        Target = position + forward;
        _view = BuildView(Position, Target);
        InvalidateFrustum();
    }

    private void RebuildView()
    {
        Matrix rotation = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);
        Vector3 forward = Vector3.Transform(Vector3.Forward, rotation);
        Target = Position + forward;
        _view = BuildView(Position, Target);
        InvalidateFrustum();
    }
}
