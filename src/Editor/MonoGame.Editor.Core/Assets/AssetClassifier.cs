namespace MonoGame.Editor.Core.Assets;

/// <summary>Asigna extensiones de archivo a valores de <see cref="AssetType"/> y crea instancias de <see cref="AssetInfo"/>.</summary>
public static class AssetClassifier
{
    private static readonly Dictionary<string, AssetType> ExtensionMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = AssetType.Texture,
            [".jpg"] = AssetType.Texture,
            [".jpeg"] = AssetType.Texture,
            [".bmp"] = AssetType.Texture,
            [".gif"] = AssetType.Texture,
            [".tga"] = AssetType.Texture,
            [".wav"] = AssetType.Audio,
            [".mp3"] = AssetType.Audio,
            [".ogg"] = AssetType.Audio,
            [".wma"] = AssetType.Audio,
            [".spritefont"] = AssetType.Font,
            [".fnt"] = AssetType.Font,
            [".tmx"] = AssetType.TiledMap,
            [".tsx"] = AssetType.TiledMap,
            [".cs"] = AssetType.Script,
        };

    // Sufijos compuestos resueltos antes de la búsqueda por extensión simple (primero el más largo)
    private static readonly (string Suffix, AssetType Type)[] CompoundSuffixes =
    [
        (".scene.json",     AssetType.Scene),
        (".prefab.json",    AssetType.Prefab),
        (".particles.json", AssetType.Particles),
        (".anim.json",      AssetType.Animation),
        (".input.json",     AssetType.InputMap),
        (".sprite.json",    AssetType.Sprite),
        (".mat.json",       AssetType.Material),
        (".uitheme.json",   AssetType.UITheme),
    ];

    /// <summary>Devuelve el <see cref="AssetType"/> para <paramref name="filePath"/> en función de su(s) extensión(es).</summary>
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

    /// <summary>Crea un <see cref="AssetInfo"/> para <paramref name="absolutePath"/> relativo a <paramref name="rootPath"/>.</summary>
    public static AssetInfo CreateInfo(string absolutePath, string rootPath)
    {
        string relative = Path.GetRelativePath(rootPath, absolutePath);
        string name = GetDisplayName(absolutePath);
        string extension = Path.GetExtension(absolutePath).ToLowerInvariant();
        AssetType type = Classify(absolutePath);
        long size = File.Exists(absolutePath) ? new FileInfo(absolutePath).Length : 0L;

        return new AssetInfo(absolutePath, relative, name, type, extension, size);
    }

    private static string GetDisplayName(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        for (int i = 0; i < CompoundSuffixes.Length; i++)
        {
            if (fileName.EndsWith(CompoundSuffixes[i].Suffix, StringComparison.OrdinalIgnoreCase))
                return fileName[..^CompoundSuffixes[i].Suffix.Length];
        }
        return Path.GetFileNameWithoutExtension(filePath);
    }
}
