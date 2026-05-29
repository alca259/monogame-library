namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>
/// Material that replaces all opaque pixels with a flat silhouette color.
/// Useful for displaying hidden characters behind walls or for selection highlights.
/// Shader parameters: <c>SilhouetteColor</c> (float4), <c>AlphaThreshold</c> (float).
/// </summary>
public sealed class SilhouetteMaterial : Material
{
    private readonly EffectParameter? _silhouetteColorParam;
    private readonly EffectParameter? _alphaThresholdParam;

    /// <summary>Gets or sets the silhouette fill color. Default is <see cref="Color.Black"/>.</summary>
    public Color SilhouetteColor { get; set; } = Color.Black;

    /// <summary>Gets or sets the alpha threshold below which a pixel is considered transparent. Default is 0.1.</summary>
    public float AlphaThreshold { get; set; } = 0.1f;

    /// <summary>Initializes a <see cref="SilhouetteMaterial"/> from a compiled silhouette <see cref="Effect"/>.</summary>
    public SilhouetteMaterial(Effect effect) : base(effect)
    {
        _silhouetteColorParam = GetParameter("SilhouetteColor");
        _alphaThresholdParam  = GetParameter("AlphaThreshold");
    }

    /// <inheritdoc/>
    public override void Apply()
    {
        _silhouetteColorParam?.SetValue(SilhouetteColor.ToVector4());
        _alphaThresholdParam?.SetValue(AlphaThreshold);
    }
}
