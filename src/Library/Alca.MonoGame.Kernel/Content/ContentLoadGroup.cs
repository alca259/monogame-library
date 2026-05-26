namespace Alca.MonoGame.Kernel.Content;

/// <summary>Groups multiple typed assets for batch async loading with unified progress reporting.</summary>
public sealed class ContentLoadGroup
{
    private readonly List<(string Name, Func<AsyncContentLoader, CancellationToken, Task> Loader)> _assetNames = new(16);

    /// <summary>Gets the number of registered assets.</summary>
    public int Count => _assetNames.Count;

    /// <summary>Registers a typed asset for batch loading.</summary>
    /// <typeparam name="T">Content type (e.g. <see cref="Texture2D"/>, <see cref="SpriteFont"/>).</typeparam>
    /// <param name="assetName">Content pipeline path of the asset.</param>
    public void Add<T>(string assetName)
        => _assetNames.Add((assetName, (loader, ct) => loader.LoadAsync<T>(assetName, null, ct)));

    /// <summary>
    /// Loads all registered assets sequentially, reporting progress after each one completes.
    /// Each asset awaits the prior one before starting; progress is <c>(completed / total)</c>.
    /// </summary>
    /// <param name="loader">The async loader that manages the main-thread upload queue.</param>
    /// <param name="progress">Optional progress sink receiving values in [0, 1].</param>
    /// <param name="ct">Token to cancel loading.</param>
    public async Task LoadAllAsync(AsyncContentLoader loader, IProgress<float>? progress, CancellationToken ct)
    {
        int total = _assetNames.Count;
        if (total == 0) return;

        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            await _assetNames[i].Loader(loader, ct).ConfigureAwait(false);
            progress?.Report((float)(i + 1) / total);
        }
    }
}
