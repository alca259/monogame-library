namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>A sprite-compatible material that exposes Alpha and TintColor as shader parameters.</summary>
public sealed class SpriteMaterial : Material
{
    private readonly EffectParameter? _alphaParam;
    private readonly EffectParameter? _tintColorParam;
    private float _alpha = 1f;
    private Color _tintColor = Color.White;

    /// <summary>Gets or sets the global alpha multiplier sent to the shader (0–1).</summary>
    public float Alpha
    {
        get => _alpha;
        set => _alpha = MathHelper.Clamp(value, 0f, 1f);
    }

    /// <summary>Gets or sets the tint color multiplied onto each pixel by the shader.</summary>
    public Color TintColor
    {
        get => _tintColor;
        set => _tintColor = value;
    }

    /// <summary>Initializes the sprite material with the given effect. Caches shader parameter references.</summary>
    public SpriteMaterial(Effect effect) : base(effect)
    {
        _alphaParam     = GetParameter("Alpha");
        _tintColorParam = GetParameter("TintColor");
    }

    /// <inheritdoc/>
    public override void Apply()
    {
        _alphaParam?.SetValue(_alpha);
        _tintColorParam?.SetValue(_tintColor.ToVector4());
    }
}
