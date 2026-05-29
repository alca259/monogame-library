namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>
/// Material that animates a dissolve/disintegration transition using a noise texture.
/// Shader parameters: <c>Progress</c> (float), <c>EdgeColor</c> (float4),
/// <c>EdgeWidth</c> (float), <c>NoiseTexture</c> (sampler).
/// </summary>
public sealed class DissolveMaterial : Material
{
    private readonly EffectParameter? _progressParam;
    private readonly EffectParameter? _edgeColorParam;
    private readonly EffectParameter? _edgeWidthParam;
    private readonly EffectParameter? _noiseTextureParam;

    private float _progress;
    private float _edgeWidth = 0.05f;

    /// <summary>
    /// Gets or sets the dissolve progress in [0, 1]. 0 = fully visible, 1 = fully dissolved.
    /// Value is automatically clamped.
    /// </summary>
    public float Progress
    {
        get => _progress;
        set => _progress = MathHelper.Clamp(value, 0f, 1f);
    }

    /// <summary>Gets or sets the color of the dissolve edge. Default is <see cref="Color.White"/>.</summary>
    public Color EdgeColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the width of the glowing edge in [0, 1] normalized noise-space. Default is 0.05.
    /// Value is automatically clamped.
    /// </summary>
    public float EdgeWidth
    {
        get => _edgeWidth;
        set => _edgeWidth = MathHelper.Clamp(value, 0f, 1f);
    }

    /// <summary>Gets or sets the noise texture used for the dissolve pattern. May be null.</summary>
    public Texture2D? NoiseTexture { get; set; }

    /// <summary>Initializes a <see cref="DissolveMaterial"/> from a compiled dissolve <see cref="Effect"/>.</summary>
    public DissolveMaterial(Effect effect) : base(effect)
    {
        _progressParam     = GetParameter("Progress");
        _edgeColorParam    = GetParameter("EdgeColor");
        _edgeWidthParam    = GetParameter("EdgeWidth");
        _noiseTextureParam = GetParameter("NoiseTexture");
    }

    /// <inheritdoc/>
    public override void Apply()
    {
        _progressParam?.SetValue(_progress);
        _edgeColorParam?.SetValue(EdgeColor.ToVector4());
        _edgeWidthParam?.SetValue(_edgeWidth);

        if (NoiseTexture is not null)
            _noiseTextureParam?.SetValue(NoiseTexture);
    }
}
