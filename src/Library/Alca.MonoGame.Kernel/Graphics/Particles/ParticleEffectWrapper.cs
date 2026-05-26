using MonoGame.Extended.Particles;

namespace Alca.MonoGame.Kernel.Graphics.Particles;

/// <summary>Wraps a MonoGame.Extended ParticleEffect providing simplified lifecycle management.</summary>
public sealed class ParticleEffectWrapper
{
    private ParticleEffect? _effect;

    /// <summary>Initializes a new empty wrapper. Call <see cref="LoadFromFile"/> before using.</summary>
    public ParticleEffectWrapper() { }

    /// <summary>Initializes the wrapper with an already-constructed effect (used for testing).</summary>
    internal ParticleEffectWrapper(ParticleEffect effect)
    {
        _effect = effect;
    }

    /// <summary>Gets the underlying ParticleEffect, or null if not yet loaded.</summary>
    public ParticleEffect? Effect => _effect;

    /// <summary>Loads a particle effect from the content pipeline using MonoGame.Extended's JSON format.</summary>
    public void LoadFromFile(ContentManager content, string assetName)
    {
        _effect = ParticleEffect.FromFile(assetName, content);
    }

    /// <summary>Updates the particle simulation and repositions the emitter origin.</summary>
    public void Update(GameTime gameTime, Vector2 emitterPosition)
    {
        if (_effect is null)
            return;

        _effect.Position = emitterPosition;
        _effect.Update(gameTime);
    }

    /// <summary>Draws all active particles using the specified blend state. Manages Begin/End internally.</summary>
    public void Draw(SpriteBatch spriteBatch, BlendState blendState)
    {
        if (_effect is null)
            return;

        spriteBatch.Begin(blendState: blendState);
        SpriteBatchExtensions.Draw(spriteBatch, _effect);
        spriteBatch.End();
    }

    /// <summary>Triggers a manual particle burst at the given world position.</summary>
    public void Trigger(Vector2 position)
    {
        _effect?.Trigger(position, 0f);
    }
}
