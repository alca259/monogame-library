namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>
/// Typed wrapper for <c>StandardEffect.fx</c> — a PBR-lite material that mirrors Unity's Standard Shader
/// property set (Albedo, Metallic/Smoothness, Normal, Height, Occlusion, Emission, Detail maps).
/// </summary>
/// <remarks>
/// Load the compiled effect with <c>Content.Load&lt;Effect&gt;("Shaders/StandardEffect")</c>,
/// then construct this class once and reuse it every frame.
/// </remarks>
public sealed class StandardMaterial : Material
{
    #region Properties — Main Maps

    /// <summary>Albedo (base colour) tint. Multiplied by <see cref="AlbedoTexture"/> when present.</summary>
    public Color AlbedoColor { get; set; } = Color.White;

    /// <summary>Optional albedo texture. When set, the HasAlbedoMap shader flag is sent as 1.</summary>
    public Texture2D? AlbedoTexture { get; set; }

    /// <summary>Metallic value (0 = dielectric, 1 = metal). Used when no <see cref="MetallicTexture"/>.</summary>
    public float Metallic { get; set; } = 0f;

    /// <summary>Smoothness value (0 = rough, 1 = mirror). Used when no <see cref="MetallicTexture"/>.</summary>
    public float Smoothness { get; set; } = 0.5f;

    /// <summary>Optional metallic map (R = metallic, A = smoothness).</summary>
    public Texture2D? MetallicTexture { get; set; }

    /// <summary>Optional tangent-space normal map.</summary>
    public Texture2D? NormalTexture { get; set; }

    /// <summary>Normal map intensity multiplier.</summary>
    public float NormalScale { get; set; } = 1f;

    /// <summary>Optional height / parallax map (greyscale).</summary>
    public Texture2D? HeightTexture { get; set; }

    /// <summary>Parallax depth scale.</summary>
    public float HeightScale { get; set; } = 0.02f;

    /// <summary>Optional ambient occlusion map (R channel used).</summary>
    public Texture2D? OcclusionTexture { get; set; }

    /// <summary>Strength of the occlusion effect (0 = no occlusion, 1 = full).</summary>
    public float OcclusionStrength { get; set; } = 1f;

    /// <summary>Emission colour. Multiplied by <see cref="EmissionIntensity"/>.</summary>
    public Color EmissionColor { get; set; } = Color.Black;

    /// <summary>Emission intensity multiplier.</summary>
    public float EmissionIntensity { get; set; } = 0f;

    /// <summary>Optional emission texture (RGB modulates <see cref="EmissionColor"/>).</summary>
    public Texture2D? EmissionTexture { get; set; }

    /// <summary>Optional detail mask texture (controls secondary map blending).</summary>
    public Texture2D? DetailMaskTexture { get; set; }

    /// <summary>UV tiling for all main maps.</summary>
    public Vector2 Tiling { get; set; } = Vector2.One;

    /// <summary>UV offset for all main maps.</summary>
    public Vector2 Offset { get; set; } = Vector2.Zero;

    #endregion

    #region Properties — Secondary Maps

    /// <summary>Optional detail albedo texture (2× overlay blending).</summary>
    public Texture2D? DetailAlbedoTexture { get; set; }

    /// <summary>Optional detail tangent-space normal map.</summary>
    public Texture2D? DetailNormalTexture { get; set; }

    /// <summary>Detail normal map intensity multiplier.</summary>
    public float DetailNormalScale { get; set; } = 1f;

    /// <summary>UV tiling for all secondary (detail) maps.</summary>
    public Vector2 DetailTiling { get; set; } = Vector2.One;

    /// <summary>UV offset for all secondary (detail) maps.</summary>
    public Vector2 DetailOffset { get; set; } = Vector2.Zero;

    #endregion

    #region Properties — Rendering

    /// <summary>Rendering mode — controls the technique and blend state used during <see cref="Apply"/>.</summary>
    public RenderingMode RenderingMode { get; set; } = RenderingMode.Opaque;

    /// <summary>Alpha threshold for <see cref="RenderingMode.Cutout"/> mode.</summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    #endregion

    #region Properties — Per-frame transforms (set before Apply each frame)

    /// <summary>Combined World × View × Projection matrix.</summary>
    public Matrix WorldViewProjection { get; set; } = Matrix.Identity;

    /// <summary>World matrix (used for normal transformation).</summary>
    public Matrix World { get; set; } = Matrix.Identity;

    /// <summary>Camera world-space position (used for specular calculation).</summary>
    public Vector3 CameraPosition { get; set; } = Vector3.Backward;

    /// <summary>Light direction (world space, pointing toward the light source).</summary>
    public Vector3 LightDirection { get; set; } = new(0f, -0.7f, 0.7f);

    /// <summary>Directional light colour.</summary>
    public Color LightColor { get; set; } = Color.White;

    /// <summary>Ambient sky colour.</summary>
    public Color AmbientColor { get; set; } = new Color(51, 51, 51);

    #endregion

    #region Cached EffectParameter references

    private readonly EffectParameter _pWorldViewProjection;
    private readonly EffectParameter _pWorld;
    private readonly EffectParameter _pCameraPosition;
    private readonly EffectParameter _pLightDirection;
    private readonly EffectParameter _pLightColor;
    private readonly EffectParameter _pAmbientColor;

    private readonly EffectParameter _pAlbedoTexture;
    private readonly EffectParameter _pAlbedoColor;
    private readonly EffectParameter _pHasAlbedoMap;
    private readonly EffectParameter _pMetallicTexture;
    private readonly EffectParameter _pMetallic;
    private readonly EffectParameter _pSmoothness;
    private readonly EffectParameter _pHasMetallicMap;
    private readonly EffectParameter _pNormalTexture;
    private readonly EffectParameter _pNormalScale;
    private readonly EffectParameter _pHasNormalMap;
    private readonly EffectParameter _pHeightTexture;
    private readonly EffectParameter _pHeightScale;
    private readonly EffectParameter _pHasHeightMap;
    private readonly EffectParameter _pOcclusionTexture;
    private readonly EffectParameter _pOcclusionStrength;
    private readonly EffectParameter _pHasOcclusionMap;
    private readonly EffectParameter _pEmissionTexture;
    private readonly EffectParameter _pEmissionColor;
    private readonly EffectParameter _pEmissionIntensity;
    private readonly EffectParameter _pHasEmissionMap;
    private readonly EffectParameter _pTiling;
    private readonly EffectParameter _pOffset;
    private readonly EffectParameter _pDetailAlbedoTexture;
    private readonly EffectParameter _pHasDetailAlbedoMap;
    private readonly EffectParameter _pDetailNormalTexture;
    private readonly EffectParameter _pDetailNormalScale;
    private readonly EffectParameter _pHasDetailNormalMap;
    private readonly EffectParameter _pDetailTiling;
    private readonly EffectParameter _pDetailOffset;
    private readonly EffectParameter _pAlphaCutoff;

    #endregion

    #region Constructor

    /// <summary>
    /// Initialises the material. <paramref name="effect"/> must be the compiled
    /// <c>StandardEffect.fx</c> loaded via the content pipeline.
    /// </summary>
    public StandardMaterial(Effect effect) : base(effect)
    {
        _pWorldViewProjection = Require("WorldViewProjection");
        _pWorld               = Require("World");
        _pCameraPosition      = Require("CameraPosition");
        _pLightDirection      = Require("LightDirection");
        _pLightColor          = Require("LightColor");
        _pAmbientColor        = Require("AmbientColor");

        _pAlbedoTexture       = Require("AlbedoTexture");
        _pAlbedoColor         = Require("AlbedoColor");
        _pHasAlbedoMap        = Require("HasAlbedoMap");
        _pMetallicTexture     = Require("MetallicTexture");
        _pMetallic            = Require("Metallic");
        _pSmoothness          = Require("Smoothness");
        _pHasMetallicMap      = Require("HasMetallicMap");
        _pNormalTexture       = Require("NormalTexture");
        _pNormalScale         = Require("NormalScale");
        _pHasNormalMap        = Require("HasNormalMap");
        _pHeightTexture       = Require("HeightTexture");
        _pHeightScale         = Require("HeightScale");
        _pHasHeightMap        = Require("HasHeightMap");
        _pOcclusionTexture    = Require("OcclusionTexture");
        _pOcclusionStrength   = Require("OcclusionStrength");
        _pHasOcclusionMap     = Require("HasOcclusionMap");
        _pEmissionTexture     = Require("EmissionTexture");
        _pEmissionColor       = Require("EmissionColor");
        _pEmissionIntensity   = Require("EmissionIntensity");
        _pHasEmissionMap      = Require("HasEmissionMap");
        _pTiling              = Require("Tiling");
        _pOffset              = Require("Offset");
        _pDetailAlbedoTexture = Require("DetailAlbedoTexture");
        _pHasDetailAlbedoMap  = Require("HasDetailAlbedoMap");
        _pDetailNormalTexture = Require("DetailNormalTexture");
        _pDetailNormalScale   = Require("DetailNormalScale");
        _pHasDetailNormalMap  = Require("HasDetailNormalMap");
        _pDetailTiling        = Require("DetailTiling");
        _pDetailOffset        = Require("DetailOffset");
        _pAlphaCutoff         = Require("AlphaCutoff");
    }

    #endregion

    #region Apply

    /// <summary>
    /// Pushes all properties to the GPU and selects the appropriate technique.
    /// Call once per draw call, then iterate <see cref="Effect.CurrentTechnique"/>.
    /// </summary>
    public override void Apply()
    {
        // Select technique
        string techniqueName = RenderingMode switch
        {
            RenderingMode.Cutout      => "Standard_Cutout",
            RenderingMode.Fade        => "Standard_Fade",
            RenderingMode.Transparent => "Standard_Transparent",
            _                         => "Standard_Opaque",
        };
        Effect.CurrentTechnique = Effect.Techniques[techniqueName];

        // Matrices + camera
        _pWorldViewProjection.SetValue(WorldViewProjection);
        _pWorld.SetValue(World);
        _pCameraPosition.SetValue(CameraPosition);

        // Lighting
        _pLightDirection.SetValue(LightDirection);
        _pLightColor.SetValue(LightColor.ToVector3());
        _pAmbientColor.SetValue(AmbientColor.ToVector3());

        // Albedo
        _pAlbedoColor.SetValue(AlbedoColor.ToVector4());
        if (AlbedoTexture is not null)
        {
            _pAlbedoTexture.SetValue(AlbedoTexture);
            _pHasAlbedoMap.SetValue(1f);
        }
        else
        {
            _pHasAlbedoMap.SetValue(0f);
        }

        // Metallic / Smoothness
        _pMetallic.SetValue(Metallic);
        _pSmoothness.SetValue(Smoothness);
        if (MetallicTexture is not null)
        {
            _pMetallicTexture.SetValue(MetallicTexture);
            _pHasMetallicMap.SetValue(1f);
        }
        else
        {
            _pHasMetallicMap.SetValue(0f);
        }

        // Normal
        _pNormalScale.SetValue(NormalScale);
        if (NormalTexture is not null)
        {
            _pNormalTexture.SetValue(NormalTexture);
            _pHasNormalMap.SetValue(1f);
        }
        else
        {
            _pHasNormalMap.SetValue(0f);
        }

        // Height
        _pHeightScale.SetValue(HeightScale);
        if (HeightTexture is not null)
        {
            _pHeightTexture.SetValue(HeightTexture);
            _pHasHeightMap.SetValue(1f);
        }
        else
        {
            _pHasHeightMap.SetValue(0f);
        }

        // Occlusion
        _pOcclusionStrength.SetValue(OcclusionStrength);
        if (OcclusionTexture is not null)
        {
            _pOcclusionTexture.SetValue(OcclusionTexture);
            _pHasOcclusionMap.SetValue(1f);
        }
        else
        {
            _pHasOcclusionMap.SetValue(0f);
        }

        // Emission
        _pEmissionColor.SetValue(EmissionColor.ToVector4());
        _pEmissionIntensity.SetValue(EmissionIntensity);
        if (EmissionTexture is not null)
        {
            _pEmissionTexture.SetValue(EmissionTexture);
            _pHasEmissionMap.SetValue(1f);
        }
        else
        {
            _pHasEmissionMap.SetValue(0f);
        }

        // UV
        _pTiling.SetValue(Tiling);
        _pOffset.SetValue(Offset);

        // Detail maps
        if (DetailAlbedoTexture is not null)
        {
            _pDetailAlbedoTexture.SetValue(DetailAlbedoTexture);
            _pHasDetailAlbedoMap.SetValue(1f);
        }
        else
        {
            _pHasDetailAlbedoMap.SetValue(0f);
        }

        _pDetailNormalScale.SetValue(DetailNormalScale);
        if (DetailNormalTexture is not null)
        {
            _pDetailNormalTexture.SetValue(DetailNormalTexture);
            _pHasDetailNormalMap.SetValue(1f);
        }
        else
        {
            _pHasDetailNormalMap.SetValue(0f);
        }

        _pDetailTiling.SetValue(DetailTiling);
        _pDetailOffset.SetValue(DetailOffset);

        // Alpha cutoff
        _pAlphaCutoff.SetValue(AlphaCutoff);

        Effect.CurrentTechnique.Passes[0].Apply();
    }

    #endregion

    #region Helpers

    private EffectParameter Require(string name) =>
        Effect.Parameters[name]
            ?? throw new InvalidOperationException(
                $"StandardMaterial: shader is missing required parameter '{name}'. " +
                "Ensure the effect was compiled from StandardEffect.fx.");

    #endregion
}
