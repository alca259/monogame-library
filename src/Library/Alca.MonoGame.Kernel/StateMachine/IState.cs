namespace Alca.MonoGame.Kernel.StateMachine;

/// <summary>
/// Defines the lifecycle callbacks for a single state in a <see cref="StateMachine{TState}"/>.
/// </summary>
/// <typeparam name="TState">The enum type that identifies states.</typeparam>
public interface IState<TState> where TState : Enum
{
    /// <summary>Called when the machine transitions into this state.</summary>
    /// <param name="previousState">The state that was active before the transition.</param>
    void Enter(TState previousState);

    /// <summary>Called every frame while this state is active.</summary>
    void Update(GameTime gameTime);

    /// <summary>Called when the machine transitions out of this state.</summary>
    /// <param name="nextState">The state that will become active after the transition.</param>
    void Exit(TState nextState);
}
