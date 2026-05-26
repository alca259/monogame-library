using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.StateMachine;

/// <summary>
/// ECS behaviour that owns and drives a <see cref="StateMachine{TState}"/>.
/// Subclasses register states in <see cref="ConfigureStates"/> and trigger transitions
/// via <see cref="Transition"/>.
/// </summary>
/// <typeparam name="TState">The enum type that uniquely identifies each state.</typeparam>
public abstract class StateMachineBehaviour<TState> : GameBehaviour where TState : Enum
{
    /// <summary>Gets the state machine owned by this behaviour.</summary>
    public StateMachine<TState> FSM { get; } = new();

    /// <inheritdoc/>
    public override void Awake() => ConfigureStates();

    /// <summary>
    /// Override to register all states with <see cref="FSM"/>.
    /// Called once during <see cref="GameBehaviour.Awake"/>.
    /// </summary>
    protected abstract void ConfigureStates();

    /// <inheritdoc/>
    public override void Update(GameTime gameTime) => FSM.Update(gameTime);

    /// <summary>Convenience shorthand for <c>FSM.Transition(state)</c>.</summary>
    protected void Transition(TState state) => FSM.Transition(state);
}
