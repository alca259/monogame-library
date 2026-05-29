namespace Alca.MonoGame.Kernel.Scenes.Transitions;

/// <summary>
/// Defines the contract for a visual transition effect applied when switching between scenes.
/// Transitions have two phases: <em>out</em> (covering the old scene) and <em>in</em> (uncovering the new scene).
/// </summary>
public interface ISceneTransition
{
    /// <summary>Begins the transition-out phase with the specified duration in seconds.</summary>
    void BeginTransitionOut(float durationSeconds);

    /// <summary>Begins the transition-in phase with the specified duration in seconds.</summary>
    void BeginTransitionIn(float durationSeconds);

    /// <summary>Advances the transition timer by <paramref name="deltaTime"/> seconds.</summary>
    void Update(float deltaTime);

    /// <summary>Gets a value indicating whether the transition-out phase has completed (old scene is fully hidden).</summary>
    bool IsTransitionOutComplete { get; }

    /// <summary>Gets a value indicating whether the transition-in phase has completed (new scene is fully revealed).</summary>
    bool IsTransitionInComplete { get; }

    /// <summary>Draws the visual transition effect as an overlay on top of the current scene.</summary>
    void Draw(SpriteBatch spriteBatch, Viewport viewport);

    /// <summary>Resets the transition to its initial state so it can be reused.</summary>
    void Reset();
}
