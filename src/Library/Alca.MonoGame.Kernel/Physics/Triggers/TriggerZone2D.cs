using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Physics.Triggers;

/// <summary>
/// ECS component that defines a 2D trigger volume without physics simulation.
/// Supports AABB and Circle shapes. Subscribe to <see cref="OnEnter"/>, <see cref="OnStay"/>,
/// and <see cref="OnExit"/> to respond to overlap events dispatched by <see cref="TriggerWorld"/>.
/// </summary>
public sealed class TriggerZone2D : GameBehaviour
{
    #region Configuration
    /// <summary>Gets or sets the collision shape. Default is <see cref="TriggerShapeType.AABB"/>.</summary>
    public TriggerShapeType Shape { get; set; } = TriggerShapeType.AABB;

    /// <summary>Gets or sets the width of the AABB shape in pixels. Default is 64.</summary>
    public int Width { get; set; } = 64;

    /// <summary>Gets or sets the height of the AABB shape in pixels. Default is 64.</summary>
    public int Height { get; set; } = 64;

    /// <summary>Gets or sets the radius of the Circle shape in pixels. Default is 32.</summary>
    public float Radius { get; set; } = 32f;

    /// <summary>Gets or sets the local offset applied to the trigger center relative to the entity position.</summary>
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the layer mask used for overlap filtering. Only zones that share at least one
    /// mask bit will be tested against each other. Default is -1 (all layers).
    /// </summary>
    public int LayerMask { get; set; } = -1;
    #endregion

    #region Computed bounds
    /// <summary>
    /// Gets the world-space axis-aligned bounding rectangle, centered on <see cref="WorldCenter"/>.
    /// Used for AABB shape overlap tests.
    /// </summary>
    public Rectangle WorldBounds
    {
        get
        {
            Vector2 pos = Entity.Transform.Position2d + Offset;
            return new Rectangle(
                (int)(pos.X - Width * 0.5f),
                (int)(pos.Y - Height * 0.5f),
                Width,
                Height);
        }
    }

    /// <summary>Gets the world-space center of this trigger zone (entity position + offset).</summary>
    public Vector2 WorldCenter => Entity.Transform.Position2d + Offset;
    #endregion

    #region Events
    /// <summary>Raised on the first frame that another zone begins overlapping this zone.</summary>
    public Action<TriggerOverlapInfo>? OnEnter;

    /// <summary>Raised every frame while another zone continues to overlap this zone.</summary>
    public Action<TriggerOverlapInfo>? OnStay;

    /// <summary>Raised on the frame that another zone stops overlapping this zone.</summary>
    public Action<TriggerOverlapInfo>? OnExit;
    #endregion

    #region Lifecycle
    /// <inheritdoc/>
    public override void Awake()
    {
        Entity.World.TriggerWorld?.Register(this);
    }

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        Entity.World.TriggerWorld?.Unregister(this);
    }
    #endregion

    #region Internal event dispatch (called by TriggerWorld)
    /// <summary>Dispatches the Enter event. Called by <see cref="TriggerWorld"/>.</summary>
    internal void RaiseEnter(TriggerZone2D other) => OnEnter?.Invoke(new TriggerOverlapInfo(this, other));

    /// <summary>Dispatches the Stay event. Called by <see cref="TriggerWorld"/>.</summary>
    internal void RaiseStay(TriggerZone2D other) => OnStay?.Invoke(new TriggerOverlapInfo(this, other));

    /// <summary>Dispatches the Exit event. Called by <see cref="TriggerWorld"/>.</summary>
    internal void RaiseExit(TriggerZone2D other) => OnExit?.Invoke(new TriggerOverlapInfo(this, other));
    #endregion
}
