namespace Alca.MonoGame.Kernel.Graphics.Camera;

/// <summary>Abstract base for all 3D camera modes. Provides View, Projection, and frustum culling.</summary>
public abstract class Camera3D
{
    private readonly BoundingFrustum _frustum = new(Matrix.Identity);
    private bool _frustumDirty = true;

    /// <summary>Up vector used when building the view matrix.</summary>
    protected Vector3 _up = Vector3.Up;

    /// <summary>Gets the current camera eye position in world space.</summary>
    public Vector3 Position { get; protected set; }

    /// <summary>Gets the point the camera is looking at in world space.</summary>
    public Vector3 Target { get; protected set; }

    /// <summary>Gets the View matrix for this frame.</summary>
    public abstract Matrix View { get; }

    /// <summary>Gets the Projection matrix.</summary>
    public abstract Matrix Projection { get; }

    /// <summary>Returns the <see cref="BoundingFrustum"/> for this frame, rebuilding it only when dirty.</summary>
    public BoundingFrustum GetFrustum()
    {
        if (_frustumDirty)
        {
            _frustum.Matrix = View * Projection;
            _frustumDirty = false;
        }

        return _frustum;
    }

    /// <summary>Marks the frustum dirty so it is rebuilt on the next <see cref="GetFrustum"/> call.</summary>
    protected void InvalidateFrustum() => _frustumDirty = true;

    /// <summary>Builds a View matrix from a position looking at a target using <see cref="_up"/>.</summary>
    protected Matrix BuildView(Vector3 position, Vector3 target) =>
        Matrix.CreateLookAt(position, target, _up);
}
