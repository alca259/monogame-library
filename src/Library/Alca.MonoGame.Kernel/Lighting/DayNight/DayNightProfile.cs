namespace Alca.MonoGame.Kernel.Lighting.DayNight;

/// <summary>
/// Defines the full 24-hour ambient lighting curve via four keyframes.
/// Use <see cref="Sample"/> to obtain an interpolated <see cref="DayNightKeyframe"/> at any time of day.
/// </summary>
public sealed class DayNightProfile
{
    #region Keyframes
    /// <summary>Gets the keyframe at midnight (00:00).</summary>
    public DayNightKeyframe Midnight { get; }

    /// <summary>Gets the keyframe at sunrise (06:00).</summary>
    public DayNightKeyframe Sunrise { get; }

    /// <summary>Gets the keyframe at noon (12:00).</summary>
    public DayNightKeyframe Noon { get; }

    /// <summary>Gets the keyframe at sunset (20:00).</summary>
    public DayNightKeyframe Sunset { get; }

    /// <summary>Gets the real-time duration of a full in-game day in seconds. Default is 600.</summary>
    public float DayDurationSeconds { get; }
    #endregion

    #region Default profile
    /// <summary>Gets a default day/night profile with sensible ambient colors.</summary>
    public static readonly DayNightProfile Default = new();
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a <see cref="DayNightProfile"/> with the given keyframes and day duration.
    /// Default values produce a natural-looking 10-minute game day.
    /// </summary>
    public DayNightProfile(
        DayNightKeyframe? midnight = null,
        DayNightKeyframe? sunrise = null,
        DayNightKeyframe? noon = null,
        DayNightKeyframe? sunset = null,
        float dayDurationSeconds = 600f)
    {
        Midnight = midnight ?? new DayNightKeyframe(TimeOfDay.Midnight,  new Color(10,  10,  30),  0.1f, 0f);
        Sunrise  = sunrise  ?? new DayNightKeyframe(TimeOfDay.Sunrise,   new Color(255, 180, 100), 0.7f, 45f);
        Noon     = noon     ?? new DayNightKeyframe(TimeOfDay.Noon,      new Color(220, 220, 255), 1.0f, 90f);
        Sunset   = sunset   ?? new DayNightKeyframe(TimeOfDay.Sunset,    new Color(255, 100, 50),  0.6f, 135f);
        DayDurationSeconds = dayDurationSeconds;
    }
    #endregion

    #region Sampling
    /// <summary>
    /// Samples the profile at <paramref name="time"/> by finding the two adjacent keyframes
    /// and linearly interpolating between them. Wraps correctly between sunset and midnight.
    /// </summary>
    public DayNightKeyframe Sample(TimeOfDay time)
    {
        float h = time.Hours;

        // Keyframes ordered by hour: Midnight(0) → Sunrise(6) → Noon(12) → Sunset(20) → Midnight(24)
        DayNightKeyframe from, to;
        float t;

        if (h < 6f)
        {
            // Midnight → Sunrise
            from = Midnight;
            to   = Sunrise;
            t    = h / 6f;
        }
        else if (h < 12f)
        {
            // Sunrise → Noon
            from = Sunrise;
            to   = Noon;
            t    = (h - 6f) / 6f;
        }
        else if (h < 20f)
        {
            // Noon → Sunset
            from = Noon;
            to   = Sunset;
            t    = (h - 12f) / 8f;
        }
        else
        {
            // Sunset → Midnight (wraps: 20h → 24h)
            from = Sunset;
            to   = Midnight;
            t    = (h - 20f) / 4f;
        }

        return Interpolate(from, to, t);
    }
    #endregion

    #region Private helpers
    private static DayNightKeyframe Interpolate(in DayNightKeyframe a, in DayNightKeyframe b, float t)
    {
        byte r = (byte)MathHelper.Lerp(a.AmbientColor.R, b.AmbientColor.R, t);
        byte g = (byte)MathHelper.Lerp(a.AmbientColor.G, b.AmbientColor.G, t);
        byte bv = (byte)MathHelper.Lerp(a.AmbientColor.B, b.AmbientColor.B, t);
        byte alpha = (byte)MathHelper.Lerp(a.AmbientColor.A, b.AmbientColor.A, t);

        float intensity   = MathHelper.Lerp(a.AmbientIntensity, b.AmbientIntensity, t);
        float sunAngle    = MathHelper.Lerp(a.SunAngleDegrees,  b.SunAngleDegrees,  t);
        TimeOfDay midTime = TimeOfDay.Lerp(a.Time, b.Time, t);

        return new DayNightKeyframe(midTime, new Color(r, g, bv, alpha), intensity, sunAngle);
    }
    #endregion
}
