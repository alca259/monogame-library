namespace Alca.MonoGame.Kernel.StateMachine;

/// <summary>
/// Generic finite state machine with lifecycle callbacks (Enter, Update, Exit).
/// Allocates no heap memory during <see cref="Transition"/> or <see cref="Update"/>.
/// </summary>
/// <typeparam name="TState">The enum type that uniquely identifies each state.</typeparam>
public sealed class StateMachine<TState> where TState : Enum
{
    private readonly Dictionary<TState, IState<TState>> _states = new(8);
    private IState<TState>? _currentStateObj;

    /// <summary>Gets the currently active state identifier.</summary>
    public TState CurrentState { get; private set; } = default!;

    /// <summary>Gets the state that was active before the most recent transition.</summary>
    public TState PreviousState { get; private set; } = default!;

    /// <summary>Gets a value indicating whether a transition is in progress this tick.</summary>
    public bool IsTransitioning { get; private set; }

    /// <summary>
    /// Registers a state with the machine. Throws <see cref="ArgumentException"/> if the state id is already registered.
    /// </summary>
    public void Register(TState id, IState<TState> state)
    {
        if (_states.ContainsKey(id))
            throw new ArgumentException($"State '{id}' is already registered.", nameof(id));
        _states[id] = state;
    }

    /// <summary>
    /// Transitions to <paramref name="newState"/>. No-op if <paramref name="newState"/> equals <see cref="CurrentState"/>.
    /// Throws <see cref="KeyNotFoundException"/> if the state has not been registered.
    /// </summary>
    public void Transition(TState newState)
    {
        if (EqualityComparer<TState>.Default.Equals(newState, CurrentState) && _currentStateObj is not null)
            return;

        if (!_states.TryGetValue(newState, out var nextStateObj))
            throw new KeyNotFoundException($"State '{newState}' is not registered. Call Register() before Transition().");

        IsTransitioning = true;

        _currentStateObj?.Exit(newState);
        PreviousState = CurrentState;
        CurrentState = newState;
        _currentStateObj = nextStateObj;
        _currentStateObj.Enter(PreviousState);

        IsTransitioning = false;
    }

    /// <summary>Calls <see cref="IState{TState}.Update"/> on the currently active state.</summary>
    public void Update(GameTime gameTime)
    {
        _currentStateObj?.Update(gameTime);
    }

    /// <summary>Returns <c>true</c> if a state with the given identifier has been registered.</summary>
    public bool HasState(TState id) => _states.ContainsKey(id);
}
