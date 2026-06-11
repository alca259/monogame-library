using System.Threading.Channels;

namespace Alca.MonoGame.Kernel.Navigation;

/// <summary>
/// Off-thread wrapper around <see cref="Pathfinder"/> that moves path searches to a dedicated
/// background worker thread via <see cref="System.Threading.Channels"/>.
/// Assign to <see cref="ECS.GameWorld.AsyncPathfinder"/> so <see cref="NavAgent"/> can use it automatically.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FindPathAsync"/> posts a request to a bounded channel (capacity 32) and returns a
/// <see cref="Task{NavPath}"/> the caller can <c>await</c> on the main thread.
/// The <see cref="NavPath"/> returned is newly allocated per request and safe to read after the task completes.
/// </para>
/// <para>
/// <strong>Thread safety:</strong> <see cref="NavGrid"/> must not be mutated while a request referencing it is
/// in-flight. If the grid is modified by <see cref="NavGridPhysicsSync"/>, ensure no pending requests reference
/// that grid instance during mutation, or synchronize with external locks.
/// </para>
/// </remarks>
public sealed class AsyncPathfinder : IDisposable
{
    private const int ChannelCapacity = 32;

    private readonly Pathfinder _pathfinder;
    private readonly Channel<PathRequest> _requestChannel;
    private readonly Task _workerTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance with its own dedicated <see cref="Pathfinder"/> and background worker.
    /// </summary>
    public AsyncPathfinder() : this(new Pathfinder()) { }

    /// <summary>
    /// Initializes with a shared <paramref name="pathfinder"/> instance.
    /// The caller owns the lifetime of <paramref name="pathfinder"/>.
    /// </summary>
    public AsyncPathfinder(Pathfinder pathfinder)
    {
        _pathfinder = pathfinder;
        _requestChannel = Channel.CreateBounded<PathRequest>(
            new BoundedChannelOptions(ChannelCapacity)
            {
                SingleWriter = false,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });
        _workerTask = Task.Run(RunWorkerAsync);
    }

    /// <summary>
    /// Posts a path request to the background worker and returns a task that completes with the result.
    /// Returns <c>null</c> if no path exists.
    /// </summary>
    /// <param name="grid">The navigation grid to search. Must not be mutated while the request is in-flight.</param>
    /// <param name="from">World-space start position.</param>
    /// <param name="to">World-space destination.</param>
    /// <param name="profile">Agent navigation profile.</param>
    /// <param name="ct">Cancellation token. If cancelled, the returned task is also cancelled.</param>
    public Task<NavPath?> FindPathAsync(NavGrid grid, Vector2 from, Vector2 to,
        NavAgentProfile profile, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AsyncPathfinder));

        var tcs = new TaskCompletionSource<NavPath?>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (ct.CanBeCanceled)
            ct.Register(() => tcs.TrySetCanceled(ct));

        var request = new PathRequest(grid, from, to, profile, tcs);
        _requestChannel.Writer.TryWrite(request);

        return tcs.Task;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _requestChannel.Writer.TryComplete();
        _workerTask.Wait(TimeSpan.FromSeconds(5));
    }

    #region Worker
    private async Task RunWorkerAsync()
    {
        await foreach (PathRequest request in _requestChannel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            if (request.Tcs.Task.IsCanceled) continue;

            try
            {
                var result = new NavPath();
                bool found = _pathfinder.FindPath(request.Grid, request.From, request.To, result, request.Profile);
                request.Tcs.TrySetResult(found ? result : null);
            }
            catch (Exception ex)
            {
                request.Tcs.TrySetException(ex);
            }
        }
    }
    #endregion

    #region Internal types
    private readonly struct PathRequest
    {
        internal readonly NavGrid _grid;
        internal readonly Vector2 _from;
        internal readonly Vector2 _to;
        internal readonly NavAgentProfile _profile;
        internal readonly TaskCompletionSource<NavPath?> _tcs;

        internal PathRequest(NavGrid grid, Vector2 from, Vector2 to,
            NavAgentProfile profile, TaskCompletionSource<NavPath?> tcs)
        {
            _grid = grid;
            _from = from;
            _to = to;
            _profile = profile;
            _tcs = tcs;
        }

        internal NavGrid Grid => _grid;
        internal Vector2 From => _from;
        internal Vector2 To => _to;
        internal NavAgentProfile Profile => _profile;
        internal TaskCompletionSource<NavPath?> Tcs => _tcs;
    }
    #endregion
}
