using System.Diagnostics.CodeAnalysis;

namespace Alca.MonoGame.Kernel.Graphics.Sprites;

/// <summary>Represents a sprite that can be animated.</summary>
public sealed class AnimatedSprite : Sprite
{
    private int _currentFrame;
    private TimeSpan _elapsed;
    private Animation? _animation;

    /// <summary>Gets or Sets the animation to use for this animated sprite.</summary>
    public Animation? Animation
    {
        get => _animation;
        set
        {
            _animation = value;
            // Starting with the first frame when setting a new animation
            // ensures consistent behavior when switching between different animations.
            if (_animation?.Frames.Count > 0)
            {
                Region = _animation.Frames[0];
            }
        }
    }

    /// <summary>Creates a new animated sprite.</summary>
    public AnimatedSprite() { }

    /// <summary>Creates a new animated sprite with the specified frames and delay.</summary>
    /// <param name="animation">The animation for this animated sprite.</param>
    [SetsRequiredMembers]
    public AnimatedSprite(Animation animation)
    {
        Animation = animation;
    }

    /// <summary>Updates this animated sprite.</summary>
    /// <param name="gameTime">A snapshot of the game timing values provided by the framework.</param>
    public void Update(GameTime gameTime)
    {
        if (_animation == null)
        {
            // Resetting the elapsed time and current frame when there is no animation
            _elapsed = gameTime.ElapsedGameTime;
            _currentFrame = 0;
            return;
        }

        _elapsed += gameTime.ElapsedGameTime;

        if (_elapsed >= _animation.Delay)
        {
            _elapsed -= _animation.Delay;
            _currentFrame++;

            if (_currentFrame >= _animation.Frames.Count)
            {
                _currentFrame = 0;
            }

            Region = _animation.Frames[_currentFrame];
        }
    }
}
