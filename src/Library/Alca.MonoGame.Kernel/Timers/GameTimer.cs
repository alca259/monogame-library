namespace Alca.MonoGame.Kernel.Timers;

/// <summary>
/// A recyclable game-time timer. Obtain instances from <see cref="TimerManager"/>; do not
/// instantiate directly.
/// </summary>
public sealed class GameTimer
{
    private float _elapsed;
    private float _interval;
    private Action? _callback;
    private int _maxFires;
    private int _fireCount;
    private bool _isActive;
    private bool _isPaused;
    private bool _isRepeating;

    /// <summary>Gets a value indicating whether this timer has completed or been cancelled.</summary>
    public bool IsDone => !_isActive;

    internal void Configure(float seconds, Action callback, bool repeating, int maxFires)
    {
        _elapsed = 0f;
        _interval = seconds;
        _callback = callback;
        _isRepeating = repeating;
        _maxFires = maxFires;
        _fireCount = 0;
        _isActive = true;
        _isPaused = false;
    }

    internal void Reset()
    {
        _elapsed = 0f;
        _interval = 0f;
        _callback = null;
        _maxFires = 0;
        _fireCount = 0;
        _isActive = false;
        _isPaused = false;
        _isRepeating = false;
    }

    /// <summary>Pauses this timer; elapsed time stops advancing until <see cref="Resume"/> is called.</summary>
    public void Pause() => _isPaused = true;

    /// <summary>Resumes a paused timer.</summary>
    public void Resume() => _isPaused = false;

    /// <summary>Cancels this timer and marks it for return to the pool.</summary>
    public void Cancel() => _isActive = false;

    /// <summary>Advances the timer by <paramref name="dt"/> seconds. Returns <c>true</c> when the timer is done.</summary>
    internal bool Tick(float dt)
    {
        if (!_isActive || _isPaused) return false;

        _elapsed += dt;

        while (_elapsed >= _interval)
        {
            _elapsed -= _interval;
            _callback?.Invoke();
            _fireCount++;

            if (!_isRepeating || (_maxFires > 0 && _fireCount >= _maxFires))
            {
                _isActive = false;
                return true;
            }
        }

        return false;
    }
}
