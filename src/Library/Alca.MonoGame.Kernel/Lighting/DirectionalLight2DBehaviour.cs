namespace Alca.MonoGame.Kernel.Lighting;

/// <summary>
/// Uniform directional light with no distance attenuation. Illuminates the entire scene
/// equally regardless of position. Use for sun, moon, or level-wide ceiling lights.
/// The <see cref="Direction"/> property is exposed for shader use but does not affect
/// the CPU-side lighting accumulation.
/// </summary>
public sealed class DirectionalLight2DBehaviour : LightBehaviour
{
    /// <summary>Gets or sets the normalized light direction. Defaults to <see cref="Vector2.UnitX"/> (right).</summary>
    public Vector2 Direction { get; set; } = Vector2.UnitX;

    /// <inheritdoc/>
    public override void Contribute(ref LightContribution accumulator, Vector2 worldPosition)
        => accumulator.Add(Color, Intensity);
}
