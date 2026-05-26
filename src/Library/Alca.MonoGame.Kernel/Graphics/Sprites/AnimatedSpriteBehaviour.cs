using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Graphics.Sprites;

/// <summary>ECS behaviour that drives an <see cref="Sprites.AnimatedSprite"/> using the entity's transform position.</summary>
public sealed class AnimatedSpriteBehaviour : GameBehaviour
{
    /// <summary>Gets the animated sprite managed by this behaviour.</summary>
    public AnimatedSprite Sprite { get; } = new();

    /// <summary>Assigns the animation and starts playback in one call.</summary>
    /// <param name="animation">The animation to play.</param>
    public void Play(Animation animation)
    {
        Sprite.Animation = animation;
        Sprite.Play();
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime) => Sprite.Update(gameTime);

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        => Sprite.Draw(spriteBatch, Entity.Transform.Position2d);
}
