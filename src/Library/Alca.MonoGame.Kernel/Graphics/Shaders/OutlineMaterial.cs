namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>
/// Material that renders an outline around a sprite by sampling adjacent pixels.
/// Shader parameters: <c>OutlineColor</c> (float4), <c>OutlineThickness</c> (float),
/// <c>AlphaThreshold</c> (float).
/// </summary>
public sealed class OutlineMaterial : Material
{
    private readonly EffectParameter? _outlineColorParam;
    private readonly EffectParameter? _outlineThicknessParam;
    private readonly EffectParameter? _alphaThresholdParam;

    /// <summary>Gets or sets the color of the outline. Default is <see cref="Color.White"/>.</summary>
    public Color OutlineColor { get; set; } = Color.White;

    /// <summary>Gets or sets the outline thickness in pixels. Default is 1.</summary>
    public float OutlineThickness { get; set; } = 1f;

    /// <summary>Gets or sets the alpha threshold below which a pixel is considered transparent. Default is 0.1.</summary>
    public float AlphaThreshold { get; set; } = 0.1f;

    /// <summary>Initializes an <see cref="OutlineMaterial"/> from a compiled outline <see cref="Effect"/>.</summary>
    public OutlineMaterial(Effect effect) : base(effect)
    {
        _outlineColorParam     = GetParameter("OutlineColor");
        _outlineThicknessParam = GetParameter("OutlineThickness");
        _alphaThresholdParam   = GetParameter("AlphaThreshold");
    }

    /// <inheritdoc/>
    public override void Apply()
    {
        _outlineColorParam?.SetValue(OutlineColor.ToVector4());
        _outlineThicknessParam?.SetValue(OutlineThickness);
        _alphaThresholdParam?.SetValue(AlphaThreshold);
    }
}
