namespace Alca.MonoGame.Kernel.Lighting;

/// <summary>
/// Distance-attenuated point light that radiates from the entity's world position.
/// Requires <see cref="LightBehaviour.Range"/> > 0 to have any effect.
/// Use for torches, lamps, explosions, and campfires.
/// </summary>
public sealed class PointLight2DBehaviour : LightBehaviour
{
    /// <summary>Gets or sets the distance falloff exponent. Higher values create a sharper edge. Default is 2.</summary>
    public float FalloffExponent { get; set; } = 2f;

    /// <inheritdoc/>
    public override void Contribute(ref LightContribution accumulator, Vector2 worldPosition)
    {
        if (Range <= 0f) return;

        float dist = Vector2.Distance(Entity.Transform.Position2d, worldPosition);
        if (dist >= Range) return;

        float t = 1f - MathF.Pow(dist / Range, FalloffExponent);
        accumulator.Add(Color, Intensity * t);
    }
}
