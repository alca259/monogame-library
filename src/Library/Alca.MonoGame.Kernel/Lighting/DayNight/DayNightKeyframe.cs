namespace Alca.MonoGame.Kernel.Lighting.DayNight;

/// <summary>
/// Defines the sky and lighting state at a specific time of day.
/// Used as a control point for <see cref="DayNightProfile"/> interpolation.
/// </summary>
public readonly struct DayNightKeyframe
{
    /// <summary>Gets the time of day at which this keyframe is anchored.</summary>
    public TimeOfDay Time { get; }

    /// <summary>Gets the ambient light color at this keyframe.</summary>
    public Color AmbientColor { get; }

    /// <summary>Gets the ambient light intensity multiplier at this keyframe.</summary>
    public float AmbientIntensity { get; }

    /// <summary>Gets the sun angle in degrees at this keyframe (0 = east, 90 = overhead, 180 = west).</summary>
    public float SunAngleDegrees { get; }

    /// <summary>
    /// Initializes a new <see cref="DayNightKeyframe"/>.
    /// </summary>
    /// <param name="time">The time of day for this keyframe.</param>
    /// <param name="ambientColor">The ambient color at this time.</param>
    /// <param name="ambientIntensity">The ambient intensity multiplier (default 1).</param>
    /// <param name="sunAngleDegrees">The sun angle in degrees (default 0).</param>
    public DayNightKeyframe(TimeOfDay time, Color ambientColor, float ambientIntensity = 1f, float sunAngleDegrees = 0f)
    {
        Time = time;
        AmbientColor = ambientColor;
        AmbientIntensity = ambientIntensity;
        SunAngleDegrees = sunAngleDegrees;
    }
}
