namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>
/// Material that applies a bloom/glow halo around opaque sprite areas.
/// Shader parameters: <c>GlowColor</c> (float4), <c>GlowIntensity</c> (float),
/// <c>GlowRadius</c> (float — pixel radius).
/// </summary>
public sealed class GlowMaterial : Material
{
    private readonly EffectParameter? _glowColorParam;
    private readonly EffectParameter? _glowIntensityParam;
    private readonly EffectParameter? _glowRadiusParam;

    /// <summary>Gets or sets the glow color. Default is <see cref="Color.White"/>.</summary>
    public Color GlowColor { get; set; } = Color.White;

    /// <summary>Gets or sets the glow intensity multiplier. Default is 1.</summary>
    public float GlowIntensity { get; set; } = 1f;

    /// <summary>Gets or sets the glow radius in pixels. Default is 4.</summary>
    public int GlowRadius { get; set; } = 4;

    /// <summary>Initializes a <see cref="GlowMaterial"/> from a compiled glow <see cref="Effect"/>.</summary>
    public GlowMaterial(Effect effect) : base(effect)
    {
        _glowColorParam     = GetParameter("GlowColor");
        _glowIntensityParam = GetParameter("GlowIntensity");
        _glowRadiusParam    = GetParameter("GlowRadius");
    }

    /// <inheritdoc/>
    public override void Apply()
    {
        _glowColorParam?.SetValue(GlowColor.ToVector4());
        _glowIntensityParam?.SetValue(GlowIntensity);
        _glowRadiusParam?.SetValue((float)GlowRadius);
    }
}
