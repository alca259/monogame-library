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
    /// Example: <c>"Shaders/SpriteTint"</c>.
    /// </summary>
    public string ShaderPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the shader parameter overrides keyed by parameter name.</summary>
    public Dictionary<string, EditorMaterialProperty> Properties { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Returns a new, empty material ready for editing.</summary>
    public static EditorMaterial CreateEmpty(string name = "New Material") =>
        new() { Name = name, ShaderPath = string.Empty };
}
