namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>
/// Controls how a <see cref="StandardMaterial"/> handles transparency and blending.
/// Mirrors Unity's Standard Shader Rendering Mode.
/// </summary>
public enum RenderingMode
{
    /// <summary>Fully opaque surface. Alpha channel of the albedo texture is ignored.</summary>
    Opaque = 0,

    /// <summary>
    /// Hard-edge transparency: pixels with alpha below <see cref="StandardMaterial.AlphaCutoff"/>
    /// are discarded; all other pixels are fully opaque.
    /// </summary>
    Cutout = 1,

    /// <summary>
    /// Soft fade-in/fade-out transparency. The full albedo alpha drives the blend. Intended for
    /// fading objects in and out — does not write to the depth buffer.
    /// </summary>
    Fade = 2,

    /// <summary>
    /// Physically correct transparency (glass, water). Maintains specular highlights at full
    /// brightness even when nearly transparent.
    /// </summary>
    Transparent = 3,
}
