namespace Alca.MonoGame.Kernel.Content;

/// <summary>Fluent builder for constructing a <see cref="ContentLoadGroup"/>.</summary>
public sealed class ContentGroupBuilder
{
    private readonly ContentLoadGroup _group = new();

    /// <summary>Registers a typed asset by name.</summary>
    /// <typeparam name="T">Content type (e.g. <see cref="Texture2D"/>, <see cref="SpriteFont"/>).</typeparam>
    /// <param name="assetName">Content pipeline path of the asset.</param>
    public ContentGroupBuilder Add<T>(string assetName)
    {
        _group.Add<T>(assetName);
        return this;
    }

    /// <summary>
    /// Registers multiple typed assets. Only call this outside of the game loop,
    /// as it iterates over an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">Content type shared by all assets in <paramref name="names"/>.</typeparam>
    /// <param name="names">Asset names to register.</param>
    public ContentGroupBuilder AddRange<T>(IEnumerable<string> names)
    {
        foreach (string name in names)
            _group.Add<T>(name);
        return this;
    }

    /// <summary>Returns the built <see cref="ContentLoadGroup"/>.</summary>
    public ContentLoadGroup Build() => _group;
}
