namespace Alca.MonoGame.Kernel.Timers;

/// <summary>
/// Game-time scheduler backed by a pre-allocated timer pool.
/// Registered in <see cref="Core"/> and updated automatically each frame.
/// </summary>
public sealed class TimerManager
{
    private const int PoolCapacity = 32;

    private readonly GameTimer[] _pool = new GameTimer[PoolCapacity];
    private int _poolCount;
    private readonly GameTimer[] _active = new GameTimer[PoolCapacity];
    private int _activeCount;

    /// <summary>Initializes a new timer manager and pre-populates the pool.</summary>
    public TimerManager()
    {
        for (int i = 0; i < PoolCapacity; i++)
            _pool[i] = new GameTimer();
        _poolCount = PoolCapacity;
    }

    /// <summary>Schedules a one-shot callback to fire after <paramref name="seconds"/> of game time.</summary>
    /// <param name="seconds">Delay in seconds.</param>
    /// <param name="callback">Action invoked when the timer fires.</param>
    /// <returns>The <see cref="GameTimer"/> instance; call <see cref="GameTimer.Cancel"/> to cancel before it fires.</returns>
    public GameTimer Schedule(float seconds, Action callback)
        => ScheduleInternal(seconds, callback, repeating: false, maxFires: 1);

    /// <summary>
    /// Schedules a repeating callback to fire every <paramref name="seconds"/> of game time.
    /// </summary>
    /// <param name="seconds">Interval in seconds.</param>
    /// <param name="callback">Action invoked each time the timer fires.</param>
    /// <param name="maxFires">Maximum number of times to fire, or <c>null</c> for infinite.</param>
    public GameTimer ScheduleRepeating(float seconds, Action callback, int? maxFires = null)
        => ScheduleInternal(seconds, callback, repeating: true, maxFires: maxFires ?? 0);

    /// <summary>Cancels all active timers and returns them to the pool.</summary>
    public void CancelAll()
    {
        for (int i = 0; i < _activeCount; i++)
        {
            _active[i].Cancel();
            ReturnToPool(_active[i]);
        }
        _activeCount = 0;
    }

    /// <summary>Advances all active timers by the elapsed game time.</summary>
    public void Update(GameTime gameTime)
    {
        if (_activeCount == 0) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        int i = 0;

        while (i < _activeCount)
        {
            bool done = _active[i].Tick(dt);
            if (done || _active[i].IsDone)
            {
                ReturnToPool(_active[i]);
                _active[i] = _active[_activeCount - 1];
                _activeCount--;
            }
            else
            {
                i++;
            }
        }
    }

    #region Internal
    private GameTimer ScheduleInternal(float seconds, Action callback, bool repeating, int maxFires)
    {
        var timer = AcquireFromPool();
        timer.Configure(seconds, callback, repeating, maxFires);

        if (_activeCount < _active.Length)
            _active[_activeCount++] = timer;

        return timer;
    }

    private GameTimer AcquireFromPool()
    {
        if (_poolCount > 0)
            return _pool[--_poolCount];

        return new GameTimer();
    }

    private void ReturnToPool(GameTimer timer)
    {
        timer.Reset();
        if (_poolCount < _pool.Length)
            _pool[_poolCount++] = timer;
    }
    #endregion
}
