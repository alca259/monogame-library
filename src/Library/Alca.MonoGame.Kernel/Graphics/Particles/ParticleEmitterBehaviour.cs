using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Graphics.Particles;

/// <summary>ECS behaviour that drives a <see cref="ParticleEffectWrapper"/> from the entity's transform.</summary>
public sealed class ParticleEmitterBehaviour : GameBehaviour
{
    /// <summary>Gets the particle effect wrapper managed by this behaviour. Never null.</summary>
    public ParticleEffectWrapper Effect { get; }

    /// <summary>Gets or sets the blend state used when drawing particles. Default is <see cref="BlendState.AlphaBlend"/>.</summary>
    public BlendState BlendState { get; set; } = BlendState.AlphaBlend;

    /// <summary>Gets or sets a value indicating whether the emitter follows the entity's world position each frame.</summary>
    public bool UseEntityPosition { get; set; } = true;

    /// <summary>Gets or sets the world-space offset applied to the entity position when placing the emitter.</summary>
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>Initializes a new instance with a pre-allocated effect wrapper.</summary>
    public ParticleEmitterBehaviour()
    {
        Effect = new ParticleEffectWrapper();
    }

    /// <summary>For testing: injects a pre-configured wrapper.</summary>
    internal ParticleEmitterBehaviour(ParticleEffectWrapper effect)
    {
        Effect = effect;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (UseEntityPosition)
        {
            Effect.Update(gameTime, Entity.Transform.Position2d + Offset);
        }
        else
        {
            Effect.Update(gameTime, Effect.Effect?.Position ?? Vector2.Zero);
        }
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Effect.Draw(spriteBatch, BlendState);
    }

    /// <summary>Triggers a manual particle burst at the entity's current position plus the configured offset.</summary>
    public void Trigger()
    {
        Effect.Trigger(Entity.Transform.Position2d + Offset);
    }
}
