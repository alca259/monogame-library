using Alca.MonoGame.Kernel.Content;
using Microsoft.Xna.Framework.Content;

namespace Alca.MonoGame.Kernel.UnitTests.Content;

public sealed class AsyncContentLoaderTests
{
    #region FlushPending — empty queue

    [Fact]
    public void FlushPending_EmptyQueue_DoesNotCallContentLoad()
    {
        using var loader = new AsyncContentLoader();
        var content = new RecordingContentManager();

        loader.FlushPending(content);

        Assert.Equal(0, content.LoadCallCount);
    }

    [Fact]
    public void FlushPending_EmptyQueue_DoesNotThrow()
    {
        using var loader = new AsyncContentLoader();
        var content = new RecordingContentManager();

        Exception? ex = Record.Exception(() => loader.FlushPending(content));

        Assert.Null(ex);
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_AbortsCurrentCancellationToken()
    {
        using var loader = new AsyncContentLoader();
        CancellationToken tokenBefore = loader.Token;

        loader.Cancel();

        Assert.True(tokenBefore.IsCancellationRequested);
    }

    [Fact]
    public void Cancel_IssuesFreshToken_ThatIsNotCancelled()
    {
        using var loader = new AsyncContentLoader();
        loader.Cancel();

        Assert.False(loader.Token.IsCancellationRequested);
    }

    [Fact]
    public void Cancel_TokenAfter_IsDifferentFromTokenBefore()
    {
        using var loader = new AsyncContentLoader();
        CancellationToken tokenBefore = loader.Token;

        loader.Cancel();

        Assert.NotEqual(tokenBefore, loader.Token);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_CancelsToken()
    {
        var loader = new AsyncContentLoader();
        CancellationToken token = loader.Token;

        loader.Dispose();

        Assert.True(token.IsCancellationRequested);
    }

    #endregion

    #region ContentLoadGroup — incremental progress

    [Fact]
    public async Task LoadAllAsync_WithThreeAssets_ReportsThreeProgressValues()
    {
        using var loader = new AsyncContentLoader();
        loader.MaxAssetsPerFrame = 10;
        var content = new RecordingContentManager();

        var group = new ContentLoadGroup();
        group.Add<object>("asset1");
        group.Add<object>("asset2");
        group.Add<object>("asset3");

        var progressValues = new List<float>();
        var progress = new Progress<float>(p =>
        {
            lock (progressValues) progressValues.Add(p);
        });

        Task loadTask = group.LoadAllAsync(loader, progress, CancellationToken.None);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!loadTask.IsCompleted && !timeout.Token.IsCancellationRequested)
        {
            loader.FlushPending(content);
            await Task.Delay(10, CancellationToken.None);
        }

        await loadTask;

        Assert.Equal(3, progressValues.Count);
        Assert.Equal(1f / 3f, progressValues[0], 4);
        Assert.Equal(2f / 3f, progressValues[1], 4);
        Assert.Equal(1f, progressValues[2], 4);
    }

    [Fact]
    public async Task LoadAllAsync_EmptyGroup_CompletesImmediately()
    {
        using var loader = new AsyncContentLoader();
        var group = new ContentLoadGroup();

        Task loadTask = group.LoadAllAsync(loader, null, CancellationToken.None);
        await loadTask;

        Assert.True(loadTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task LoadAllAsync_ReportsProgressMonotonicallyIncreasing()
    {
        using var loader = new AsyncContentLoader();
        loader.MaxAssetsPerFrame = 10;
        var content = new RecordingContentManager();

        var group = new ContentLoadGroup();
        group.Add<object>("a1");
        group.Add<object>("a2");
        group.Add<object>("a3");
        group.Add<object>("a4");

        var progressValues = new List<float>();
        var progress = new Progress<float>(p =>
        {
            lock (progressValues) progressValues.Add(p);
        });

        Task loadTask = group.LoadAllAsync(loader, progress, CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!loadTask.IsCompleted && !timeout.Token.IsCancellationRequested)
        {
            loader.FlushPending(content);
            await Task.Delay(10, CancellationToken.None);
        }

        await loadTask;

        for (int i = 1; i < progressValues.Count; i++)
            Assert.True(progressValues[i] > progressValues[i - 1]);
    }

    #endregion

    #region MaxAssetsPerFrame

    [Fact]
    public async Task FlushPending_MaxAssetsPerFrame_LimitsProcessedCount()
    {
        using var loader = new AsyncContentLoader();
        loader.MaxAssetsPerFrame = 1;
        var content = new RecordingContentManager();

        Task t1 = loader.LoadAsync<object>("a1", null, CancellationToken.None);
        Task t2 = loader.LoadAsync<object>("a2", null, CancellationToken.None);

        // Allow Task.Run bodies time to enqueue both pending loads
        await Task.Delay(100);

        loader.FlushPending(content);

        Assert.Equal(1, content.LoadCallCount);
    }

    #endregion

    #region ContentGroupBuilder

    [Fact]
    public void ContentGroupBuilder_Add_RegistersAssets()
    {
        ContentLoadGroup group = new ContentGroupBuilder()
            .Add<object>("asset1")
            .Add<object>("asset2")
            .Build();

        Assert.Equal(2, group.Count);
    }

    [Fact]
    public void ContentGroupBuilder_AddRange_RegistersAllAssets()
    {
        ContentLoadGroup group = new ContentGroupBuilder()
            .AddRange<object>(new[] { "a1", "a2", "a3" })
            .Build();

        Assert.Equal(3, group.Count);
    }

    #endregion

    // ── test doubles ─────────────────────────────────────────────────────────

    private sealed class RecordingContentManager : ContentManager
    {
        public int LoadCallCount { get; private set; }

        public RecordingContentManager() : base(new StubServiceProvider())
        {
            RootDirectory = "Content";
        }

        public override T Load<T>(string assetName)
        {
            LoadCallCount++;
            return default!;
        }

        private sealed class StubServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }
    }
}
