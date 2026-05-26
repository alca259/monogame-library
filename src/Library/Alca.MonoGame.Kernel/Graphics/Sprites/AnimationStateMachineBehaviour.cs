using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Graphics.Sprites;

/// <summary>ECS behaviour that drives an <see cref="Sprites.AnimationStateMachine"/> using the entity's transform position.</summary>
public sealed class AnimationStateMachineBehaviour : GameBehaviour
{
    /// <summary>Gets the state machine managed by this behaviour.</summary>
    public AnimationStateMachine StateMachine { get; } = new();

    /// <summary>Gets the currently active state name. Delegates to <see cref="AnimationStateMachine.CurrentState"/>.</summary>
    public string? CurrentState => StateMachine.CurrentState;

    /// <summary>Switches to the named animation state. Delegates to <see cref="AnimationStateMachine.Play"/>.</summary>
    /// <param name="stateName">State name to activate.</param>
    public void Play(string stateName) => StateMachine.Play(stateName);

    /// <inheritdoc/>
    public override void Update(GameTime gameTime) => StateMachine.Update(gameTime);

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        => StateMachine.Draw(spriteBatch, Entity.Transform.Position2d);
}
