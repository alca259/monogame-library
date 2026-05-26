using System.Diagnostics.CodeAnalysis;

namespace Alca.MonoGame.Kernel.Graphics.Sprites;

/// <summary>Represents a sprite that can be animated.</summary>
public sealed class AnimatedSprite : Sprite
{
    private int _currentFrame;
    private TimeSpan _elapsed;
    private Animation? _animation;
    private bool _isPlaying = true;
    private bool _isComplete;
    private bool _onCompleteFired;

    /// <summary>Gets or Sets the animation to use for this animated sprite.</summary>
    public Animation? Animation
    {
        get => _animation;
        set
        {
            _animation = value;
            _currentFrame = 0;
            _elapsed = TimeSpan.Zero;
            _isComplete = false;
            _onCompleteFired = false;
            if (_animation?.Frames.Count > 0)
                Region = _animation.Frames[0];
        }
    }

    /// <summary>Gets whether the animation is currently advancing frames.</summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>Gets whether a non-looping animation has reached its last frame and stopped.</summary>
    public bool IsComplete => _isComplete;

    /// <summary>Gets or sets a global speed multiplier applied on top of <see cref="Animation.SpeedMultiplier"/>. Default is <c>1.0</c>.</summary>
    public float PlaybackSpeed { get; set; } = 1.0f;

    /// <summary>Invoked once when a non-looping animation finishes playing.</summary>
    public Action? OnComplete { get; set; }

    /// <summary>Creates a new animated sprite.</summary>
    [SetsRequiredMembers]
    public AnimatedSprite() { }

    /// <summary>Creates a new animated sprite with the specified animation.</summary>
    /// <param name="animation">The animation for this animated sprite.</param>
    [SetsRequiredMembers]
    public AnimatedSprite(Animation animation)
    {
        Animation = animation;
    }

    /// <summary>Starts or resumes playback. If the animation was complete, resets to frame 0.</summary>
    public void Play()
    {
        if (_isComplete)
        {
            _currentFrame = 0;
            _elapsed = TimeSpan.Zero;
            _isComplete = false;
            _onCompleteFired = false;
            if (_animation?.Frames.Count > 0)
                Region = _animation.Frames[0];
        }

        _isPlaying = true;
    }

    /// <summary>Freezes the current frame without resetting position.</summary>
    public void Pause() => _isPlaying = false;

    /// <summary>Resumes playback from the current frame. Alias for <see cref="Play"/> when not complete.</summary>
    public void Resume() => Play();

    /// <summary>Stops playback and resets to frame 0.</summary>
    public void Stop()
    {
        _isPlaying = false;
        _isComplete = false;
        _onCompleteFired = false;
        _currentFrame = 0;
        _elapsed = TimeSpan.Zero;
        if (_animation?.Frames.Count > 0)
            Region = _animation.Frames[0];
    }

    /// <summary>Updates this animated sprite, advancing frames based on elapsed time.</summary>
    /// <param name="gameTime">A snapshot of the game timing values provided by the framework.</param>
    public void Update(GameTime gameTime)
    {
        if (_animation == null)
        {
            _elapsed = TimeSpan.Zero;
            _currentFrame = 0;
            return;
        }

        if (!_isPlaying)
            return;

        _elapsed += gameTime.ElapsedGameTime;

        double combinedSpeed = _animation.SpeedMultiplier * PlaybackSpeed;
        if (combinedSpeed <= 0.0)
            combinedSpeed = 1.0;

        TimeSpan effectiveDelay = TimeSpan.FromTicks((long)(_animation.Delay.Ticks / combinedSpeed));
        if (effectiveDelay <= TimeSpan.Zero)
            effectiveDelay = TimeSpan.FromTicks(1);

        if (_elapsed < effectiveDelay)
            return;

        _elapsed -= effectiveDelay;
        _currentFrame++;

        if (_currentFrame >= _animation.Frames.Count)
        {
            if (_animation.IsLooping)
            {
                _currentFrame = 0;
            }
            else
            {
                _currentFrame = _animation.Frames.Count - 1;
                _isPlaying = false;
                _isComplete = true;
                if (!_onCompleteFired)
                {
                    _onCompleteFired = true;
                    OnComplete?.Invoke();
                }
            }
        }

        Region = _animation.Frames[_currentFrame];
    }
}
