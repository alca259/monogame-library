using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Lighting;

/// <summary>
/// Base class for all 2D light types. Attach to a <see cref="GameEntity"/> to participate
/// in lighting calculations. Automatically registers/unregisters with <see cref="LightingWorld"/>
/// if one is set on the owning <see cref="ECS.GameWorld"/>.
/// </summary>
public abstract class LightBehaviour : GameBehaviour
{
    /// <summary>Gets or sets the light color. Default is <see cref="Color.White"/>.</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>Gets or sets the light intensity in range [0, 1]. Default is 1.</summary>
    public float Intensity { get; set; } = 1f;

    /// <summary>Gets or sets the lighting layer this light contributes to. Default is <see cref="LightingLayer.World"/>.</summary>
    public LightingLayer LightingLayer { get; set; } = LightingLayer.World;

    /// <summary>
    /// Gets or sets the maximum influence radius. 0 means unlimited (intended for
    /// <see cref="AmbientLight"/> and <see cref="DirectionalLight2D"/>).
    /// </summary>
    public float Range { get; set; } = 0f;

    /// <summary>Returns true when this light is active, enabled, and has positive intensity.</summary>
    public bool IsContributing => Entity.Active && Enabled && Intensity > 0f;

    /// <summary>
    /// Accumulates this light's contribution at <paramref name="worldPosition"/> into <paramref name="accumulator"/>.
    /// Only called when <see cref="IsContributing"/> is true and the layer matches.
    /// </summary>
    public abstract void Contribute(ref LightContribution accumulator, Vector2 worldPosition);

    /// <inheritdoc/>
    public override void Awake()
    {
        Entity.World.LightingWorld?.Register(this);
    }

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        Entity.World.LightingWorld?.Unregister(this);
    }
}
