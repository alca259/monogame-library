namespace MonoGame.Editor.Core.Models;

/// <summary>Describes the data type stored in an <see cref="EditorMaterialProperty"/>.</summary>
public enum EditorMaterialPropertyType
{
    /// <summary>Single float scalar.</summary>
    Float,
    /// <summary>Two-component float vector.</summary>
    Vector2,
    /// <summary>Three-component float vector.</summary>
    Vector3,
    /// <summary>Four-component float vector.</summary>
    Vector4,
    /// <summary>RGBA color (four floats).</summary>
    Color,
    /// <summary>Reference to a texture asset by content-relative path.</summary>
    Texture2D,
}

/// <summary>A single editable property in an <see cref="EditorMaterial"/>.</summary>
public sealed class EditorMaterialProperty
{
    /// <summary>Gets or sets the shader parameter name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type.</summary>
    public EditorMaterialPropertyType Type { get; set; }

    /// <summary>Numeric data (Float=1, Vector2=2, Vector3=3, Vector4/Color=4 elements). Null for Texture2D.</summary>
    public float[]? Data { get; set; }

    /// <summary>Content-relative texture path (no extension). Null for non-texture types.</summary>
    public string? TexturePath { get; set; }
}

/// <summary>
/// Editor-side model for a material asset (.mat.json).
/// Mirrors <c>MaterialDescriptor</c> from the Kernel library but uses editor-friendly types.
/// </summary>
public sealed class EditorMaterial
{
    /// <summary>Gets or sets the display name of the material.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content-relative path (no extension) to the compiled shader (.xnb).
    /// Example: <c>"Shaders/StandardEffect"</c>.
    /// </summary>
    public string ShaderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rendering mode for transparency blending.
    /// One of: <c>"Opaque"</c>, <c>"Cutout"</c>, <c>"Fade"</c>, <c>"Transparent"</c>.
    /// </summary>
    public string RenderingMode { get; set; } = "Opaque";

    /// <summary>Gets or sets the secondary UV set index for detail maps (0 = UV0, 1 = UV1).</summary>
    public int UVSet { get; set; } = 0;

    /// <summary>Gets or sets the shader parameter overrides keyed by parameter name.</summary>
    public Dictionary<string, EditorMaterialProperty> Properties { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Returns a new, empty material ready for editing.</summary>
    public static EditorMaterial CreateEmpty(string name = "New Material") =>
        new() { Name = name, ShaderPath = string.Empty };

    /// <summary>
    /// Returns a new Standard material pre-populated with all Standard shader properties and their defaults.
    /// The <see cref="ShaderPath"/> is set to <c>"Shaders/StandardEffect"</c>.
    /// </summary>
    public static EditorMaterial CreateStandard(string name = "New Material")
    {
        var mat = new EditorMaterial
        {
            Name          = name,
            ShaderPath    = "Shaders/StandardEffect",
            RenderingMode = "Opaque",
            UVSet         = 0,
        };

        // Main maps — scalar defaults
        mat.SetFloat("Metallic",          0f);
        mat.SetFloat("Smoothness",        0.5f);
        mat.SetFloat("NormalScale",       1f);
        mat.SetFloat("HeightScale",       0.02f);
        mat.SetFloat("OcclusionStrength", 1f);
        mat.SetFloat("EmissionIntensity", 0f);
        mat.SetColor("AlbedoColor",       [1f, 1f, 1f, 1f]);
        mat.SetColor("EmissionColor",     [0f, 0f, 0f, 0f]);
        mat.SetVector2("Tiling",          [1f, 1f]);
        mat.SetVector2("Offset",          [0f, 0f]);

        // Secondary maps — scalar defaults
        mat.SetFloat("DetailNormalScale", 1f);
        mat.SetVector2("DetailTiling",    [1f, 1f]);
        mat.SetVector2("DetailOffset",    [0f, 0f]);

        return mat;
    }

    // ── Fluent helpers ──────────────────────────────────────────────────────

    private void SetFloat(string key, float value) =>
        Properties[key] = new EditorMaterialProperty
        {
            Name = key,
            Type = EditorMaterialPropertyType.Float,
            Data = [value],
        };

    private void SetColor(string key, float[] rgba) =>
        Properties[key] = new EditorMaterialProperty
        {
            Name = key,
            Type = EditorMaterialPropertyType.Color,
            Data = rgba,
        };

    private void SetVector2(string key, float[] xy) =>
        Properties[key] = new EditorMaterialProperty
        {
            Name = key,
            Type = EditorMaterialPropertyType.Vector2,
            Data = xy,
        };
}
