namespace Alca.MonoGame.Kernel.ECS;

/// <summary>
/// Pure-data spatial component automatically attached to every entity.
/// Supports parent-child hierarchies with local and world-space coordinate transforms.
/// Equivalent to Unity's Transform.
/// </summary>
public sealed class TransformBehaviour : GameBehaviour
{
    private Vector3 _localPosition;
    private Vector3 _localRotation;
    private Vector3 _localScale = Vector3.One;

    private TransformBehaviour? ParentTransformInternal => EntityOrNull?.Parent?.Transform;

    #region Local Space

    /// <summary>Gets or sets the position relative to the parent (or world origin if no parent).</summary>
    public Vector3 LocalPosition
    {
        get => _localPosition;
        set => _localPosition = value;
    }

    /// <summary>Gets or sets the rotation as Euler angles in radians relative to the parent transform (pitch=X, yaw=Y, roll=Z).</summary>
    public Vector3 LocalRotation
    {
        get => _localRotation;
        set => _localRotation = value;
    }

    /// <summary>Gets or sets the scale relative to the parent transform. Defaults to (1, 1, 1).</summary>
    public Vector3 LocalScale
    {
        get => _localScale;
        set => _localScale = value;
    }

    /// <summary>Gets or sets the XY plane local position. Z is preserved on set.</summary>
    public Vector2 LocalPosition2d
    {
        get => new(_localPosition.X, _localPosition.Y);
        set => _localPosition = new Vector3(value, _localPosition.Z);
    }

    /// <summary>Gets or sets the Z-axis local rotation (roll) in radians.</summary>
    public float LocalRotation2d
    {
        get => _localRotation.Z;
        set => _localRotation = new Vector3(_localRotation.X, _localRotation.Y, value);
    }

    /// <summary>Gets or sets the XY plane local scale. Z is preserved on set.</summary>
    public Vector2 LocalScale2d
    {
        get => new(_localScale.X, _localScale.Y);
        set => _localScale = new Vector3(value, _localScale.Z);
    }

    #endregion

    #region World Space

    /// <summary>Gets or sets the world-space position. Equals <see cref="LocalPosition"/> when there is no parent.</summary>
    public Vector3 Position
    {
        get
        {
            var parent = ParentTransformInternal;
            return parent is null
                ? _localPosition
                : Vector3.Transform(_localPosition, parent.LocalToWorldMatrix);
        }
        set
        {
            var parent = ParentTransformInternal;
            _localPosition = parent is null
                ? value
                : Vector3.Transform(value, parent.WorldToLocalMatrix);
        }
    }

    /// <summary>Gets or sets the world-space rotation as Euler angles in radians.</summary>
    public Vector3 Rotation
    {
        get
        {
            var parent = ParentTransformInternal;
            return parent is null ? _localRotation : parent.Rotation + _localRotation;
        }
        set
        {
            var parent = ParentTransformInternal;
            _localRotation = parent is null ? value : value - parent.Rotation;
        }
    }

    /// <summary>Gets or sets the local scale. Alias for <see cref="LocalScale"/>; preserved for backward compatibility.</summary>
    public Vector3 Scale
    {
        get => _localScale;
        set => _localScale = value;
    }

    /// <summary>Gets the global scale of the object, computed as the product of all scales in the hierarchy. Read-only.</summary>
    public Vector3 LossyScale
    {
        get
        {
            var parent = ParentTransformInternal;
            return parent is null ? _localScale : parent.LossyScale * _localScale;
        }
    }

    /// <summary>Gets or sets the XY plane world position. Z is preserved on set.</summary>
    public Vector2 Position2d
    {
        get { var p = Position; return new(p.X, p.Y); }
        set { var cur = Position; Position = new Vector3(value, cur.Z); }
    }

    /// <summary>Gets or sets the world Z-axis rotation (roll) in radians — the standard 2D rotation angle.</summary>
    public float Rotation2d
    {
        get => Rotation.Z;
        set { var r = Rotation; Rotation = new Vector3(r.X, r.Y, value); }
    }

    /// <summary>Gets the XY plane world scale. Derived from <see cref="LossyScale"/>.</summary>
    public Vector2 Scale2d
    {
        get { var s = LossyScale; return new(s.X, s.Y); }
    }

    #endregion

    #region Velocity (not part of Unity Transform; kept as a convenience field)

    /// <summary>Gets or sets the velocity used by movement behaviours.</summary>
    public Vector3 Velocity { get; set; }

    /// <summary>Gets or sets the XY plane velocity. Z is preserved on set.</summary>
    public Vector2 Velocity2d
    {
        get => new(Velocity.X, Velocity.Y);
        set => Velocity = new Vector3(value, Velocity.Z);
    }

    #endregion

    #region Matrices

    /// <summary>Gets the matrix that transforms a point from local space to world space.</summary>
    public Matrix LocalToWorldMatrix
    {
        get
        {
            var local = Matrix.CreateScale(_localScale)
                * Matrix.CreateFromYawPitchRoll(_localRotation.Y, _localRotation.X, _localRotation.Z)
                * Matrix.CreateTranslation(_localPosition);
            var parent = ParentTransformInternal;
            return parent is null ? local : local * parent.LocalToWorldMatrix;
        }
    }

    /// <summary>Gets the matrix that transforms a point from world space to local space.</summary>
    public Matrix WorldToLocalMatrix => Matrix.Invert(LocalToWorldMatrix);

    #endregion

    #region Coordinate Transforms

    /// <summary>Transforms a point from local space to world space (applies scale, rotation, and translation).</summary>
    public Vector3 TransformPoint(Vector3 localPoint) =>
        Vector3.Transform(localPoint, LocalToWorldMatrix);

    /// <summary>Transforms a point from world space to local space.</summary>
    public Vector3 InverseTransformPoint(Vector3 worldPoint) =>
        Vector3.Transform(worldPoint, WorldToLocalMatrix);

    /// <summary>Transforms a direction from local to world space (rotation only; no translation).</summary>
    public Vector3 TransformDirection(Vector3 localDirection) =>
        Vector3.TransformNormal(localDirection, LocalToWorldMatrix);

    /// <summary>Transforms a direction from world to local space (rotation only; no translation).</summary>
    public Vector3 InverseTransformDirection(Vector3 worldDirection) =>
        Vector3.TransformNormal(worldDirection, WorldToLocalMatrix);

    #endregion

    #region Transform Operations

    /// <summary>Moves the transform by the given vector.</summary>
    /// <param name="delta">Amount to move.</param>
    /// <param name="worldSpace">When true, moves in world space; otherwise in local space.</param>
    public void Translate(Vector3 delta, bool worldSpace = false)
    {
        if (worldSpace)
            Position += delta;
        else
            _localPosition += delta;
    }

    /// <summary>Applies the given Euler-angle rotation.</summary>
    /// <param name="eulerDelta">Rotation to apply, in radians.</param>
    /// <param name="worldSpace">When true, rotates in world space; otherwise in local space.</param>
    public void Rotate(Vector3 eulerDelta, bool worldSpace = false)
    {
        if (worldSpace)
            Rotation += eulerDelta;
        else
            _localRotation += eulerDelta;
    }

    /// <summary>Rotates the transform so its forward axis (2D: positive X) points at the given world target.</summary>
    public void LookAt(Vector3 worldTarget)
    {
        var dir = worldTarget - Position;
        LocalRotation2d = MathF.Atan2(dir.Y, dir.X);
    }

    /// <summary>Sets world-space position and rotation in a single call.</summary>
    public void SetPositionAndRotation(Vector3 worldPosition, Vector3 worldRotation)
    {
        Position = worldPosition;
        Rotation = worldRotation;
    }

    /// <summary>Sets local-space position and rotation in a single call.</summary>
    public void SetLocalPositionAndRotation(Vector3 localPosition, Vector3 localRotation)
    {
        _localPosition = localPosition;
        _localRotation = localRotation;
    }

    /// <summary>Gets the world-space position and rotation via out parameters.</summary>
    public void GetPositionAndRotation(out Vector3 position, out Vector3 rotation)
    {
        position = Position;
        rotation = Rotation;
    }

    /// <summary>Gets the local-space position and rotation via out parameters.</summary>
    public void GetLocalPositionAndRotation(out Vector3 localPosition, out Vector3 localRotation)
    {
        localPosition = _localPosition;
        localRotation = _localRotation;
    }

    #endregion

    #region Hierarchy Navigation

    /// <summary>Gets the parent transform, or null if this is the root.</summary>
    public TransformBehaviour? ParentTransform => EntityOrNull?.Parent?.Transform;

    /// <summary>Gets the root transform at the top of the hierarchy.</summary>
    public TransformBehaviour Root => Entity.Root.Transform;

    /// <summary>Gets the number of direct children.</summary>
    public int ChildCount => Entity.ChildCount;

    /// <summary>Returns the child transform at the given index, or null if the index is out of range.</summary>
    public TransformBehaviour? GetChild(int index)
    {
        var children = Entity.Children;
        return index >= 0 && index < children.Count ? children[index].Transform : null;
    }

    /// <summary>Returns true if this transform's entity is a direct or indirect child of <paramref name="other"/>'s entity.</summary>
    public bool IsChildOf(TransformBehaviour other) => Entity.IsChildOf(other.Entity);

    #endregion

    #region Constructors

    /// <summary>Creates a TransformBehaviour at the specified 3D position.</summary>
    public TransformBehaviour(Vector3 position = default) => _localPosition = position;

    /// <summary>Creates a TransformBehaviour at the specified 2D position (Z = 0).</summary>
    public TransformBehaviour(Vector2 position) => _localPosition = new Vector3(position, 0f);

    #endregion
}
