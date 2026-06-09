namespace MonoGame.Editor.Core.Models;

/// <summary>Describe el tipo de dato almacenado en un <see cref="EditorMaterialProperty"/>.</summary>
public enum EditorMaterialPropertyType
{
    /// <summary>Escalar flotante único.</summary>
    Float,
    /// <summary>Vector flotante de dos componentes.</summary>
    Vector2,
    /// <summary>Vector flotante de tres componentes.</summary>
    Vector3,
    /// <summary>Vector flotante de cuatro componentes.</summary>
    Vector4,
    /// <summary>Color RGBA (cuatro flotantes).</summary>
    Color,
    /// <summary>Referencia a un recurso de textura por ruta relativa al Content.</summary>
    Texture2D,
}

/// <summary>Una única propiedad editable en un <see cref="EditorMaterial"/>.</summary>
public sealed class EditorMaterialProperty
{
    /// <summary>Obtiene o establece el nombre del parámetro del shader.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Obtiene o establece el tipo de dato.</summary>
    public EditorMaterialPropertyType Type { get; set; }

    /// <summary>Datos numéricos (Float=1, Vector2=2, Vector3=3, Vector4/Color=4 elementos). Null para Texture2D.</summary>
    public float[]? Data { get; set; }

    /// <summary>Ruta de textura relativa al Content (sin extensión). Null para tipos que no sean texturas.</summary>
    public string? TexturePath { get; set; }
}

/// <summary>
/// Modelo del lado del editor para un recurso de material (.mat.json).
/// Refleja <c>MaterialDescriptor</c> de la librería Kernel pero usa tipos amigables para el editor.
/// </summary>
public sealed class EditorMaterial
{
    /// <summary>Obtiene o establece el nombre de visualización del material.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la ruta relativa al Content (sin extensión) del shader compilado (.xnb).
    /// Ejemplo: <c>"Shaders/StandardEffect"</c>.
    /// </summary>
    public string ShaderPath { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el modo de renderizado para la mezcla de transparencia.
    /// Uno de: <c>"Opaque"</c>, <c>"Cutout"</c>, <c>"Fade"</c>, <c>"Transparent"</c>.
    /// </summary>
    public string RenderingMode { get; set; } = "Opaque";

    /// <summary>Obtiene o establece el índice del conjunto UV secundario para los mapas de detalle (0 = UV0, 1 = UV1).</summary>
    public int UVSet { get; set; } = 0;

    /// <summary>Obtiene o establece las sobreescrituras de parámetros del shader indexadas por nombre de parámetro.</summary>
    public Dictionary<string, EditorMaterialProperty> Properties { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Devuelve un nuevo material vacío listo para edición.</summary>
    public static EditorMaterial CreateEmpty(string name = "New Material") =>
        new() { Name = name, ShaderPath = string.Empty };

    /// <summary>
    /// Devuelve un nuevo material Estándar pre-poblado con todas las propiedades del shader Estándar y sus valores predeterminados.
    /// El <see cref="ShaderPath"/> se establece en <c>"Shaders/StandardEffect"</c>.
    /// </summary>
    public static EditorMaterial CreateStandard(string name = "New Material")
    {
        var mat = new EditorMaterial
        {
            Name = name,
            ShaderPath = "Shaders/StandardEffect",
            RenderingMode = "Opaque",
            UVSet = 0,
        };

        // Main maps
        mat.SetTexture2D("AlbedoTexture");
        mat.SetColor("AlbedoColor", [1f, 1f, 1f, 1f]);
        mat.SetTexture2D("MetallicTexture");
        mat.SetFloat("Metallic", 0f);
        mat.SetFloat("Smoothness", 0.5f);
        mat.SetTexture2D("NormalTexture");
        mat.SetFloat("NormalScale", 1f);
        mat.SetTexture2D("HeightTexture");
        mat.SetFloat("HeightScale", 0.02f);
        mat.SetTexture2D("OcclusionTexture");
        mat.SetFloat("OcclusionStrength", 1f);
        mat.SetTexture2D("EmissionTexture");
        mat.SetColor("EmissionColor", [0f, 0f, 0f, 0f]);
        mat.SetFloat("EmissionIntensity", 0f);
        mat.SetTexture2D("DetailMaskTexture");
        mat.SetVector2("Tiling", [1f, 1f]);
        mat.SetVector2("Offset", [0f, 0f]);

        // Secondary maps
        mat.SetTexture2D("DetailAlbedoTexture");
        mat.SetTexture2D("DetailNormalTexture");
        mat.SetFloat("DetailNormalScale", 1f);
        mat.SetVector2("DetailTiling", [1f, 1f]);
        mat.SetVector2("DetailOffset", [0f, 0f]);

        return mat;
    }

    /// <summary>Devuelve un material SpriteTint pre-poblado con color y alpha.</summary>
    public static EditorMaterial CreateSpriteTint(string name = "New Material")
    {
        var mat = new EditorMaterial
        {
            Name = name,
            ShaderPath = "Shaders/SpriteTint",
        };
        mat.SetColor("TintColor", [1f, 1f, 1f, 1f]);
        mat.SetFloat("Alpha", 1f);
        return mat;
    }

    /// <summary>Devuelve un material Grayscale pre-poblado con intensidad.</summary>
    public static EditorMaterial CreateGrayscale(string name = "New Material")
    {
        var mat = new EditorMaterial
        {
            Name = name,
            ShaderPath = "Shaders/Grayscale",
        };
        mat.SetFloat("Intensity", 1f);
        return mat;
    }

    /// <summary>Devuelve un material Vignette pre-poblado con intensidad.</summary>
    public static EditorMaterial CreateVignette(string name = "New Material")
    {
        var mat = new EditorMaterial
        {
            Name = name,
            ShaderPath = "Shaders/Vignette",
        };
        mat.SetFloat("Intensity", 1f);
        return mat;
    }

    // ── Métodos auxiliares encadenados ──────────────────────────────────────

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

    private void SetTexture2D(string key) =>
        Properties[key] = new EditorMaterialProperty
        {
            Name = key,
            Type = EditorMaterialPropertyType.Texture2D,
            TexturePath = string.Empty,
        };
}
