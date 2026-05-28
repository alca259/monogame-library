using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Weather;

/// <summary>
/// Opt-in ECS component that connects an entity to the <see cref="WeatherWorld"/> simulation.
/// Attach to any entity whose physics body should respond to wind forces or receive impulses from lightning strikes.
/// Auto-registers and unregisters with the world service via <see cref="Awake"/> and <see cref="OnDestroy"/>.
/// </summary>
public sealed class WeatherBehaviour : GameBehaviour
{
    private Physics.RigidBody2D? _rigidBody;

    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets whether this entity's <see cref="Physics.RigidBody2D"/> receives wind forces each frame.
    /// Requires a sibling <see cref="Physics.RigidBody2D"/> component; silently no-ops if absent.
    /// </summary>
    public bool ReceivesWind { get; set; } = false;

    /// <summary>Gets or sets the multiplier applied to the wind force vector before passing it to <see cref="Physics.RigidBody2D.ApplyForce"/>. Default 1.</summary>
    public float WindForceMultiplier { get; set; } = 1f;

    /// <summary>Gets or sets whether this entity can receive a radial impulse from nearby lightning strikes.</summary>
    public bool ReceivesLightningImpulse { get; set; } = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Awake()
    {
        _rigidBody = Entity.GetComponent<Physics.RigidBody2D>();
        Entity.World.WeatherWorld?.Register(this);
    }

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        Entity.World.WeatherWorld?.Unregister(this);
    }

    // ── Internal dispatch (called by WeatherWorld, not user code) ─────────────

    /// <summary>
    /// Applies <paramref name="windForce"/> to the sibling <see cref="Physics.RigidBody2D"/> this frame.
    /// No-op when <see cref="ReceivesWind"/> is false or no <see cref="Physics.RigidBody2D"/> is present.
    /// </summary>
    internal void ApplyWindForce(Vector2 windForce)
    {
        if (!ReceivesWind || _rigidBody is null) return;
        _rigidBody.ApplyForce(windForce * WindForceMultiplier);
    }

    /// <summary>
    /// Applies an outward radial impulse centered on <paramref name="strikePosition"/> to the sibling body.
    /// No-op when <see cref="ReceivesLightningImpulse"/> is false, the body is outside the radius, or no body is present.
    /// </summary>
    internal void ApplyLightningImpulse(Vector2 strikePosition, float radius, float strength)
    {
        if (!ReceivesLightningImpulse || _rigidBody is null) return;

        Vector2 pos = Entity.Transform.Position2d;
        float dist = Vector2.Distance(pos, strikePosition);
        if (dist >= radius || dist < 0.001f) return;

        // Linear falloff: full strength at center, zero at edge
        float falloff = 1f - (dist / radius);
        Vector2 direction = Vector2.Normalize(pos - strikePosition);
        _rigidBody.ApplyImpulse(direction * strength * falloff);
    }
}
