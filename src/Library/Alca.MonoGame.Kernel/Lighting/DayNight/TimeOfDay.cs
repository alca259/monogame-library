namespace Alca.MonoGame.Kernel.Lighting.DayNight;

/// <summary>
/// Represents a time of day on a [0, 24) hour scale.
/// Immutable value type; arithmetic wraps correctly across midnight.
/// </summary>
public readonly struct TimeOfDay : IEquatable<TimeOfDay>
{
    private readonly float _hours;

    /// <summary>Gets the time expressed in hours [0, 24).</summary>
    public float Hours => _hours;

    /// <summary>Gets the fractional minutes component [0, 60).</summary>
    public float Minutes => (_hours % 1f) * 60f;

    /// <summary>Gets the total elapsed seconds since midnight.</summary>
    public float TotalSeconds => _hours * 3600f;

    /// <summary>Returns true if the time is between 06:00 and 19:59 (daytime).</summary>
    public bool IsDaytime => _hours >= 6f && _hours < 20f;

    /// <summary>Returns true if the time is outside daytime hours.</summary>
    public bool IsNighttime => !IsDaytime;

    private TimeOfDay(float hours) => _hours = hours;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="TimeOfDay"/> from the given hour value, wrapping into [0, 24).
    /// Handles negative values correctly.
    /// </summary>
    public static TimeOfDay FromHours(float hours) => new(((hours % 24f) + 24f) % 24f);

    /// <summary>Gets midnight (00:00).</summary>
    public static TimeOfDay Midnight => new(0f);

    /// <summary>Gets sunrise (06:00).</summary>
    public static TimeOfDay Sunrise => new(6f);

    /// <summary>Gets noon (12:00).</summary>
    public static TimeOfDay Noon => new(12f);

    /// <summary>Gets sunset (20:00).</summary>
    public static TimeOfDay Sunset => new(20f);

    // ── Interpolation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="t"/>,
    /// taking the shortest path around the 24-hour clock.
    /// </summary>
    public static TimeOfDay Lerp(TimeOfDay a, TimeOfDay b, float t)
    {
        float diff = b._hours - a._hours;

        // Choose the shorter arc
        if (diff > 12f)  diff -= 24f;
        if (diff < -12f) diff += 24f;

        return FromHours(a._hours + diff * t);
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool Equals(TimeOfDay other) => _hours == other._hours;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TimeOfDay other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _hours.GetHashCode();

    /// <summary>Returns true when both values represent the same hour.</summary>
    public static bool operator ==(TimeOfDay left, TimeOfDay right) => left.Equals(right);

    /// <summary>Returns true when the values differ.</summary>
    public static bool operator !=(TimeOfDay left, TimeOfDay right) => !left.Equals(right);

    /// <summary>Returns the time formatted as HH:MM.</summary>
    public override string ToString() => $"{(int)_hours:D2}:{(int)Minutes:D2}";
}
