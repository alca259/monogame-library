namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>
/// A single serializable property that overrides a shader parameter in a <see cref="MaterialDescriptor"/>.
/// Numeric values are stored in <see cref="Data"/>; texture assets are referenced by
/// <see cref="TexturePath"/> (content-relative path, no extension).
/// </summary>
public sealed class MaterialProperty
{
    /// <summary>Gets or sets the name of the shader parameter this property targets.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type of this property.</summary>
    public MaterialPropertyType Type { get; set; }

    /// <summary>
    /// Gets or sets the numeric data payload.
    /// Length conventions: Float = 1, Vector2 = 2, Vector3 = 3, Vector4/Color = 4.
    /// <see langword="null"/> when <see cref="Type"/> is <see cref="MaterialPropertyType.Texture2D"/>.
    /// </summary>
    public float[]? Data { get; set; }

    /// <summary>
    /// Gets or sets the content-relative path (no extension) for texture properties.
    /// <see langword="null"/> when <see cref="Type"/> is not <see cref="MaterialPropertyType.Texture2D"/>.
    /// </summary>
    public string? TexturePath { get; set; }
}
