namespace Alca.MonoGame.Kernel.Weather;

/// <summary>
/// Data payload raised when a lightning strike fires.
/// Passed by value to subscribers of <see cref="WeatherWorld.LightningStruck"/> — no heap allocation at the call site.
/// </summary>
public readonly struct LightningStrikeEvent
{
    /// <summary>Gets the world-space position of the strike.</summary>
    public Vector2 Position { get; init; }

    /// <summary>Gets the peak light flash intensity (0–1 scale).</summary>
    public float Intensity { get; init; }

    /// <summary>Gets the radius in world units within which physics impulses are applied.</summary>
    public float ImpulseRadius { get; init; }

    /// <summary>Gets the impulse magnitude applied to bodies at the strike center, falling off linearly to the edge of <see cref="ImpulseRadius"/>.</summary>
    public float ImpulseStrength { get; init; }
}
