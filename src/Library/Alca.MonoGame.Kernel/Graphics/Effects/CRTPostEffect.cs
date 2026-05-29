namespace Alca.MonoGame.Kernel.Graphics.Effects;

/// <summary>
/// Post-process effect that simulates a CRT monitor display with scanlines,
/// barrel distortion, and a vignette. Apply via <see cref="PostProcessEffect.Apply"/>.
/// Shader parameters: <c>ScanlineIntensity</c>, <c>BarrelDistortion</c>,
/// <c>VignetteRadius</c>, <c>Resolution</c>.
/// </summary>
public sealed class CRTPostEffect : PostProcessEffect
{
    private readonly EffectParameter? _scanlineIntensityParam;
    private readonly EffectParameter? _barrelDistortionParam;
    private readonly EffectParameter? _vignetteRadiusParam;
    private readonly EffectParameter? _resolutionParam;

    private float _scanlineIntensity = 0.3f;
    private float _barrelDistortion  = 0.1f;
    private float _vignetteRadius    = 0.8f;

    /// <summary>
    /// Gets or sets the scanline darkness intensity in [0, 1]. Default is 0.3.
    /// Value is automatically clamped.
    /// </summary>
    public float ScanlineIntensity
    {
        get => _scanlineIntensity;
        set => _scanlineIntensity = MathHelper.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets or sets the barrel distortion strength in [0, 1]. Default is 0.1.
    /// Value is automatically clamped.
    /// </summary>
    public float BarrelDistortion
    {
        get => _barrelDistortion;
        set => _barrelDistortion = MathHelper.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets or sets the vignette falloff radius in [0, 1] (1 = no vignette). Default is 0.8.
    /// Value is automatically clamped.
    /// </summary>
    public float VignetteRadius
    {
        get => _vignetteRadius;
        set => _vignetteRadius = MathHelper.Clamp(value, 0f, 1f);
    }

    /// <summary>Gets or sets the render resolution passed to the shader. Default is 1280 × 720.</summary>
    public Vector2 Resolution { get; set; } = new Vector2(1280f, 720f);

    /// <summary>Initializes a <see cref="CRTPostEffect"/> from a compiled CRT <see cref="Effect"/>.</summary>
    public CRTPostEffect(Effect effect) : base(effect)
    {
        _scanlineIntensityParam = GetParameter("ScanlineIntensity");
        _barrelDistortionParam  = GetParameter("BarrelDistortion");
        _vignetteRadiusParam    = GetParameter("VignetteRadius");
        _resolutionParam        = GetParameter("Resolution");
    }

    /// <inheritdoc/>
    public override void SetParameters()
    {
        _scanlineIntensityParam?.SetValue(_scanlineIntensity);
        _barrelDistortionParam?.SetValue(_barrelDistortion);
        _vignetteRadiusParam?.SetValue(_vignetteRadius);
        _resolutionParam?.SetValue(Resolution);
    }

    // Convenience accessor for the base-class Effect.Parameters
    private EffectParameter? GetParameter(string name) => Effect.Parameters[name];
}
