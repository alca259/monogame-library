namespace Alca.MonoGame.Kernel.Weather;

/// <summary>
/// String-based identifier for a weather type. Extensible by design — developers can define
/// custom types using <c>new WeatherTypeId("my_weather")</c> and register them with
/// <see cref="WeatherWorld.RegisterCustomWeather"/>.
/// </summary>
public readonly struct WeatherTypeId : IEquatable<WeatherTypeId>
{
    /// <summary>Gets the underlying string value.</summary>
    public string Value { get; init; }

    /// <summary>Initializes a new <see cref="WeatherTypeId"/> with the given string key.</summary>
    public WeatherTypeId(string value) => Value = value ?? string.Empty;

    #region Predefined types
    /// <summary>Clear sky with sunlight. Warm light, minimal wind.</summary>
    public static readonly WeatherTypeId Sunny = new("sunny");

    /// <summary>Extreme heat. Very warm light, light wind, no precipitation.</summary>
    public static readonly WeatherTypeId HeatWave = new("heat_wave");

    /// <summary>Overcast sky, no rain. Cool light, light wind.</summary>
    public static readonly WeatherTypeId Cloudy = new("cloudy");

    /// <summary>Dense fog. Cool light, zero wind, heavy fog particles.</summary>
    public static readonly WeatherTypeId Fog = new("fog");

    /// <summary>Rain storm. Cold light, light wind, heavy rain particles.</summary>
    public static readonly WeatherTypeId Storm = new("storm");

    /// <summary>Electrical storm with lightning. Cold light, moderate wind, lightning strikes.</summary>
    public static readonly WeatherTypeId Thunderstorm = new("thunderstorm");

    /// <summary>Hail storm. Cold light, moderate wind, hail particles.</summary>
    public static readonly WeatherTypeId HailStorm = new("hail_storm");

    /// <summary>Snowstorm. Cold light, light wind, heavy snow particles.</summary>
    public static readonly WeatherTypeId Blizzard = new("blizzard");

    /// <summary>Extreme cold snap. Cold light, very strong wind, no precipitation.</summary>
    public static readonly WeatherTypeId ColdSnap = new("cold_snap");

    /// <summary>Orange-alert wind event. Neutral light, very strong wind, wind sprite particles.</summary>
    public static readonly WeatherTypeId OrangeWind = new("orange_wind");
    #endregion

    #region Equality
    /// <inheritdoc/>
    public bool Equals(WeatherTypeId other) =>
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is WeatherTypeId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value ?? string.Empty;

    /// <summary>Equality operator.</summary>
    public static bool operator ==(WeatherTypeId left, WeatherTypeId right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(WeatherTypeId left, WeatherTypeId right) => !left.Equals(right);
    #endregion
}
