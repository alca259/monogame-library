namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>
/// Serializable descriptor for a material asset (.mat.json).
/// Contains only data; no runtime GPU references — use <see cref="DynamicMaterial"/> to obtain a GPU-ready material.
/// </summary>
public sealed class MaterialDescriptor
{
    /// <summary>Gets or sets the display name of the material.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content-relative path (no extension) to the compiled shader effect (.xnb).
    /// Example: <c>"Shaders/SpriteTint"</c>.
    /// </summary>
    public string ShaderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rendering mode that controls transparency blending.
    /// Stored as a string for human-readable JSON (e.g. <c>"Opaque"</c>, <c>"Cutout"</c>).
    /// </summary>
    public string RenderingMode { get; set; } = nameof(Shaders.RenderingMode.Opaque);

    /// <summary>
    /// Gets or sets the secondary UV set index used for detail maps (0 = UV0, 1 = UV1).
    /// </summary>
    public int UVSet { get; set; } = 0;

    /// <summary>
    /// Gets or sets the shader parameter overrides keyed by parameter name.
    /// Parameters not listed here use the shader's compiled defaults.
    /// </summary>
    public Dictionary<string, MaterialProperty> Properties { get; set; } = new(StringComparer.Ordinal);
}
