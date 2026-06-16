namespace Alca.MonoGame.Kernel.Lighting;

/// <summary>
/// Mutable accumulator for light color contributions. Allocated on the stack — no heap allocation.
/// Start from <see cref="Color.Black"/> (no light) or a custom ambient color.
/// </summary>
public struct LightContribution
{
    private Color _accumulated;

    /// <summary>Gets the accumulated light color.</summary>
    public readonly Color Accumulated => _accumulated;

    /// <summary>Initializes a new contribution accumulator starting from <paramref name="baseColor"/>.</summary>
    public LightContribution(Color baseColor) => _accumulated = baseColor;

    /// <summary>
    /// Blends <paramref name="color"/> into the accumulator using <see cref="Color.Lerp"/> weighted by <paramref name="weight"/>.
    /// <paramref name="weight"/> is clamped to [0, 1].
    /// </summary>
    public void Add(Color color, float weight)
        => _accumulated = Color.Lerp(_accumulated, color, MathHelper.Clamp(weight, 0f, 1f));
}
