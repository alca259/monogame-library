using Alca.MonoGame.Kernel.Graphics.Shaders;
using Alca.MonoGame.Kernel.Lighting;

namespace Alca.MonoGame.Kernel.Graphics.Sprites;

/// <summary>
/// A sprite material that supports normal-map lighting.
/// Set <see cref="NormalMap"/> and call <see cref="SyncLights"/> each frame before rendering.
/// </summary>
public sealed class NormalMapSpriteMaterial : Material
{
    private readonly EffectParameter? _normalMapParam;
    private readonly EffectParameter? _normalStrengthParam;
    private readonly EffectParameter? _ambientColorParam;

    private float _normalStrength = 1f;

    /// <summary>Gets or sets the normal map texture. When <see langword="null"/> the shader receives no normal data.</summary>
    public Texture2D? NormalMap { get; set; }

    /// <summary>
    /// Gets or sets the normal map intensity clamped to [0, 1].
    /// A value of 0 disables normal-map shading; a value of 1 applies full effect.
    /// </summary>
    public float NormalStrength
    {
        get => _normalStrength;
        set => _normalStrength = MathHelper.Clamp(value, 0f, 1f);
    }

    /// <summary>Gets or sets the ambient color multiplied onto the sprite before directional lights are applied.</summary>
    public Color AmbientColor { get; set; } = Color.White;

    /// <summary>
    /// Creates a new <see cref="NormalMapSpriteMaterial"/> and caches shader parameter references.
    /// </summary>
    /// <param name="effect">The compiled HLSL/GLSL effect that implements normal-map shading.</param>
    public NormalMapSpriteMaterial(Effect effect) : base(effect)
    {
        _normalMapParam = GetParameter("NormalMap");
        _normalStrengthParam = GetParameter("NormalStrength");
        _ambientColorParam = GetParameter("AmbientColor");
    }

    /// <summary>
    /// Copies the ambient color from <paramref name="lightingWorld"/> into <see cref="AmbientColor"/>.
    /// Full point-light / directional-light synchronization requires a shader that accepts per-light arrays.
    /// </summary>
    public void SyncLights(LightingWorld lightingWorld)
    {
        AmbientColor = lightingWorld.AmbientColor;
    }

    /// <inheritdoc/>
    public override void Apply()
    {
        if (NormalMap is not null)
            _normalMapParam?.SetValue(NormalMap);

        _normalStrengthParam?.SetValue(_normalStrength);
        _ambientColorParam?.SetValue(AmbientColor.ToVector4());
    }
}
