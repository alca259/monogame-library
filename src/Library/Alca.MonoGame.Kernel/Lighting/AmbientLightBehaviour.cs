namespace Alca.MonoGame.Kernel.Lighting;

/// <summary>
/// Provides a uniform base-color illumination across the entire scene with no position
/// or distance dependency. Use for background ambient such as day sky, night darkness,
/// or neutral interior lighting.
/// </summary>
public sealed class AmbientLightBehaviour : LightBehaviour
{
    /// <inheritdoc/>
    public override void Contribute(ref LightContribution accumulator, Vector2 worldPosition)
        => accumulator.Add(Color, Intensity);
}
