namespace Alca.MonoGame.Kernel.Content;

/// <summary>Loads content assets asynchronously, deferring GPU uploads to the main thread via <see cref="FlushPending"/>.</summary>
public sealed class AsyncContentLoader : IDisposable
{
    private readonly Queue<IPendingLoad> _pendingAssets = new(16);
    private readonly object _queueLock = new();
    private CancellationTokenSource _cancelSource = new();
    private bool _disposed;

    /// <summary>Maximum number of assets processed per <see cref="FlushPending"/> call. Defaults to 1.</summary>
    public int MaxAssetsPerFrame { get; set; } = 1;

    /// <summary>Gets a cancellation token tied to this loader's lifetime.</summary>
    public CancellationToken Token => _cancelSource.Token;

    /// <summary>
    /// Starts loading an asset asynchronously. The background phase enqueues the load request;
    /// the actual <see cref="ContentManager.Load{T}"/> call happens on the main thread via <see cref="FlushPending"/>.
    /// </summary>
    /// <param name="assetName">Content pipeline path of the asset.</param>
    /// <param name="progress">Optional progress sink. Receives 0.5 when background phase completes.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    public Task<T> LoadAsync<T>(string assetName, IProgress<float>? progress, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = Task.Run(() =>
        {
            if (ct.IsCancellationRequested)
            {
                tcs.TrySetCanceled(ct);
                return;
            }

            lock (_queueLock)
            {
                _pendingAssets.Enqueue(new PendingLoad<T>(assetName, tcs, ct));
            }

            progress?.Report(0.5f);
        }, ct).ContinueWith(
            t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception!.GetBaseException());
                else if (t.IsCanceled)
                    tcs.TrySetCanceled(ct);
            },
            CancellationToken.None,
            TaskContinuationOptions.NotOnRanToCompletion,
            TaskScheduler.Default);

        return tcs.Task;
    }

    /// <summary>
    /// Processes pending assets on the main thread. Call this from <c>Update()</c>.
    /// Processes up to <see cref="MaxAssetsPerFrame"/> assets per call.
    /// </summary>
    public void FlushPending(ContentManager content)
    {
        int processed = 0;
        while (processed < MaxAssetsPerFrame)
        {
            IPendingLoad? pending;
            lock (_queueLock)
            {
                if (_pendingAssets.Count == 0) break;
                pending = _pendingAssets.Dequeue();
            }

            pending.Execute(content);
            processed++;
        }
    }

    /// <summary>Cancels all pending operations and issues a fresh cancellation token.</summary>
    public void Cancel()
    {
        _cancelSource.Cancel();
        _cancelSource.Dispose();
        _cancelSource = new CancellationTokenSource();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cancelSource.Cancel();
        _cancelSource.Dispose();
    }

    private interface IPendingLoad
    {
        void Execute(ContentManager content);
    }

    private sealed class PendingLoad<T> : IPendingLoad
    {
        private readonly string _assetName;
        private readonly TaskCompletionSource<T> _tcs;
        private readonly CancellationToken _ct;

        public PendingLoad(string assetName, TaskCompletionSource<T> tcs, CancellationToken ct)
        {
            _assetName = assetName;
            _tcs = tcs;
            _ct = ct;
        }

        public void Execute(ContentManager content)
        {
            if (_ct.IsCancellationRequested)
            {
                _tcs.TrySetCanceled(_ct);
                return;
            }

            try
            {
                T result = content.Load<T>(_assetName);
                _tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
            }
        }
    }
}
