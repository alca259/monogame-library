namespace MonoGame.Editor.Core.Assets;

/// <summary>Maps file extensions to <see cref="AssetType"/> values and creates <see cref="AssetInfo"/> instances.</summary>
public static class AssetClassifier
{
    private static readonly Dictionary<string, AssetType> ExtensionMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"]         = AssetType.Texture,
            [".jpg"]         = AssetType.Texture,
            [".jpeg"]        = AssetType.Texture,
            [".bmp"]         = AssetType.Texture,
            [".gif"]         = AssetType.Texture,
            [".tga"]         = AssetType.Texture,
            [".wav"]         = AssetType.Audio,
            [".mp3"]         = AssetType.Audio,
            [".ogg"]         = AssetType.Audio,
            [".wma"]         = AssetType.Audio,
            [".spritefont"]  = AssetType.Font,
            [".fnt"]         = AssetType.Font,
            [".tmx"]         = AssetType.TiledMap,
            [".tsx"]         = AssetType.TiledMap,
            [".cs"]          = AssetType.Script,
        };

    // Compound suffixes resolved before single-extension lookup (longest match first)
    private static readonly (string Suffix, AssetType Type)[] CompoundSuffixes =
    [
        (".scene.json",     AssetType.Scene),
        (".prefab.json",    AssetType.Prefab),
        (".particles.json", AssetType.Particles),
        (".anim.json",      AssetType.Animation),
        (".input.json",     AssetType.InputMap),
    ];

    /// <summary>Returns the <see cref="AssetType"/> for <paramref name="filePath"/> based on its extension(s).</summary>
    public static AssetType Classify(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        for (int i = 0; i < CompoundSuffixes.Length; i++)
        {
            if (fileName.EndsWith(CompoundSuffixes[i].Suffix, StringComparison.OrdinalIgnoreCase))
                return CompoundSuffixes[i].Type;
        }

        string ext = Path.GetExtension(filePath);
        return ExtensionMap.TryGetValue(ext, out AssetType type) ? type : AssetType.Unknown;
    }

    /// <summary>Creates an <see cref="AssetInfo"/> for <paramref name="absolutePath"/> relative to <paramref name="rootPath"/>.</summary>
    public static AssetInfo CreateInfo(string absolutePath, string rootPath)
    {
        string relative  = Path.GetRelativePath(rootPath, absolutePath);
        string name      = Path.GetFileNameWithoutExtension(absolutePath);
        string extension = Path.GetExtension(absolutePath).ToLowerInvariant();
        AssetType type   = Classify(absolutePath);
        long size        = File.Exists(absolutePath) ? new FileInfo(absolutePath).Length : 0L;

        return new AssetInfo(absolutePath, relative, name, type, extension, size);
    }
}
