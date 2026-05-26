namespace Alca.MonoGame.Kernel.Graphics.Sprites;

/// <summary>Manages a set of named <see cref="Animation"/> clips and drives a single <see cref="AnimatedSprite"/>, switching between states on demand.</summary>
public sealed class AnimationStateMachine
{
    private readonly Dictionary<string, Animation> _states = new(8);
    private readonly AnimatedSprite _sprite = new();

    /// <summary>Gets the name of the currently active animation state, or <c>null</c> if none has been played yet.</summary>
    public string? CurrentState { get; private set; }

    /// <summary>Registers a named animation state. Throws if the name is already registered.</summary>
    /// <param name="name">Unique state name.</param>
    /// <param name="animation">The animation to associate with this state.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> already exists.</exception>
    public void Register(string name, Animation animation)
    {
        if (!_states.TryAdd(name, animation))
            throw new ArgumentException($"An animation state with the name '{name}' already exists.", nameof(name));
    }

    /// <summary>Removes a named animation state. Does not throw if the name is not found.</summary>
    /// <param name="name">State name to remove.</param>
    public void Unregister(string name) => _states.Remove(name);

    /// <summary>
    /// Switches to the named state and starts playback from frame 0.
    /// If the state is already current, this is a no-op.
    /// </summary>
    /// <param name="name">State name to activate.</param>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="name"/> has not been registered.</exception>
    public void Play(string name)
    {
        if (CurrentState == name)
            return;

        if (!_states.TryGetValue(name, out Animation? animation))
            throw new KeyNotFoundException($"No animation state with the name '{name}' has been registered.");

        CurrentState = name;
        _sprite.Animation = animation;
        _sprite.Play();
    }

    /// <summary>Advances the active animation by one frame tick.</summary>
    /// <param name="gameTime">A snapshot of the game timing values provided by the framework.</param>
    public void Update(GameTime gameTime) => _sprite.Update(gameTime);

    /// <summary>Draws the current animation frame at the specified position.</summary>
    /// <param name="spriteBatch">The sprite batch to draw with.</param>
    /// <param name="position">World-space position at which to render the sprite.</param>
    public void Draw(SpriteBatch spriteBatch, Vector2 position) => _sprite.Draw(spriteBatch, position);
}
