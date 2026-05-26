namespace MonoGame.Editor.Core.Assets;

/// <summary>Immutable descriptor for a single asset file on disk.</summary>
/// <param name="AbsolutePath">Full path to the asset file.</param>
/// <param name="RelativePath">Path relative to the Content root folder.</param>
/// <param name="Name">Display name (filename without the outermost extension).</param>
/// <param name="Type">Classified asset type.</param>
/// <param name="Extension">File extension including the leading dot, lower-case.</param>
/// <param name="SizeBytes">File size in bytes, or 0 if the file no longer exists.</param>
public sealed record AssetInfo(
    string AbsolutePath,
    string RelativePath,
    string Name,
    AssetType Type,
    string Extension,
    long SizeBytes);
