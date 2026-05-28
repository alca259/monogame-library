namespace Alca.MonoGame.Kernel.Weather;

/// <summary>
/// Represents the wind state for a given frame — direction, current speed, and turbulence.
/// Produced by <see cref="WeatherWorld"/> each frame from the active <see cref="WeatherProfile"/>.
/// </summary>
public readonly struct WindState
{
    /// <summary>Gets the normalized wind direction in world space.</summary>
    public Vector2 Direction { get; init; }

    /// <summary>Gets the current wind speed in km/h (interpolated within the profile's min/max range).</summary>
    public float SpeedKmh { get; init; }

    /// <summary>Gets the turbulence amplitude as a fraction of <see cref="SpeedKmh"/> (0 = smooth, 1 = ±100% variation).</summary>
    public float Turbulence { get; init; }

    /// <summary>Returns true when the wind is effectively still (speed ≤ 0.01 km/h).</summary>
    public bool IsCalm => SpeedKmh <= 0.01f;

    /// <summary>
    /// Computes the effective wind force vector for the current <paramref name="totalElapsedSeconds"/>,
    /// applying two sinusoidal turbulence waves with coprime frequencies over the base wind.
    /// Multiplied by <paramref name="worldUnitsPerKmh"/> to convert km/h to world units per second.
    /// No heap allocations.
    /// </summary>
    /// <param name="totalElapsedSeconds">Total game time in seconds (used as sinusoid argument for deterministic turbulence).</param>
    /// <param name="worldUnitsPerKmh">Scale factor: how many world units correspond to 1 km/h. Default 1.</param>
    public Vector2 ComputeEffectiveForce(float totalElapsedSeconds, float worldUnitsPerKmh = 1f)
    {
        if (IsCalm) return Vector2.Zero;

        float baseSpeed = SpeedKmh * worldUnitsPerKmh;

        // Two sinusoidal turbulence components with coprime frequencies (3 and 7 Hz)
        float t1 = MathF.Sin(totalElapsedSeconds * 3f);
        float t2 = MathF.Sin(totalElapsedSeconds * 7f);
        float turbulenceOffset = (t1 * 0.6f + t2 * 0.4f) * Turbulence;

        float effectiveSpeed = baseSpeed * (1f + turbulenceOffset);

        return Direction * effectiveSpeed;
    }
}
