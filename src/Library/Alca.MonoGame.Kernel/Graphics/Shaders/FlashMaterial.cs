namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>
/// Material that applies a flash/hit-flash effect by blending the sprite toward a solid color.
/// Shader parameters: <c>FlashColor</c> (float4), <c>FlashIntensity</c> (float).
/// </summary>
public sealed class FlashMaterial : Material
{
    private readonly EffectParameter? _flashColorParam;
    private readonly EffectParameter? _flashIntensityParam;

    private float _flashIntensity;

    /// <summary>Gets or sets the flash color to blend toward. Default is <see cref="Color.White"/>.</summary>
    public Color FlashColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the flash intensity in [0, 1]. 0 = no flash, 1 = fully solid flash color.
    /// Value is automatically clamped.
    /// </summary>
    public float FlashIntensity
    {
        get => _flashIntensity;
        set => _flashIntensity = MathHelper.Clamp(value, 0f, 1f);
    }

    /// <summary>Initializes a <see cref="FlashMaterial"/> from a compiled flash <see cref="Effect"/>.</summary>
    public FlashMaterial(Effect effect) : base(effect)
    {
        _flashColorParam     = GetParameter("FlashColor");
        _flashIntensityParam = GetParameter("FlashIntensity");
    }

    /// <inheritdoc/>
    public override void Apply()
    {
        _flashColorParam?.SetValue(FlashColor.ToVector4());
        _flashIntensityParam?.SetValue(_flashIntensity);
    }
}
