namespace Alca.MonoGame.Kernel.Weather;

/// <summary>
/// Immutable data record describing the complete state of a weather condition:
/// temperature range, wind, lighting, fog, precipitation, lightning, audio volumes, and optional custom data.
/// Used both as a configuration target and as the live interpolated state during transitions.
/// </summary>
public readonly struct WeatherProfile
{
    #region Temperature

    /// <summary>Gets the minimum temperature of this weather state in degrees Celsius.</summary>
    public float TemperatureMin { get; init; }

    /// <summary>Gets the maximum temperature of this weather state in degrees Celsius.</summary>
    public float TemperatureMax { get; init; }

    #endregion

    #region Wind

    /// <summary>Gets the minimum wind speed for this state in km/h.</summary>
    public float WindSpeedMinKmh { get; init; }

    /// <summary>Gets the maximum wind speed for this state in km/h.</summary>
    public float WindSpeedMaxKmh { get; init; }

    /// <summary>Gets the predominant normalized wind direction in world space. Default is rightward.</summary>
    public Vector2 WindDirection { get; init; }

    /// <summary>Gets the turbulence amplitude as a fraction of the current wind speed (0 = smooth, 1 = ±100%).</summary>
    public float WindTurbulence { get; init; }

    #endregion

    #region Lighting

    /// <summary>Gets the ambient light color applied to <see cref="Lighting.LightingWorld.AmbientColor"/> during this weather.</summary>
    public Color AmbientColor { get; init; }

    /// <summary>Gets the ambient light intensity multiplier in range [0, 1].</summary>
    public float AmbientIntensity { get; init; }

    /// <summary>Gets the sky / background clear color.</summary>
    public Color SkyColor { get; init; }

    #endregion

    #region Fog

    /// <summary>Gets the fog overlay color (alpha channel encodes base density).</summary>
    public Color FogColor { get; init; }

    /// <summary>
    /// Gets the fog density in range [0, 1].
    /// Automatically forced to 0 when <see cref="WindSpeedMaxKmh"/> > 0 (wind and fog are mutually exclusive).
    /// </summary>
    public float FogDensity { get; init; }

    #endregion

    #region Precipitation

    /// <summary>Gets whether precipitation particle emitters should be active.</summary>
    public bool HasPrecipitation { get; init; }

    /// <summary>Gets the precipitation intensity, which scales the particle emission rate and maximum count.</summary>
    public PrecipitationIntensity PrecipitationLevel { get; init; }

    #endregion

    #region Lightning

    /// <summary>Gets whether lightning strikes should occur.</summary>
    public bool HasLightning { get; init; }

    /// <summary>Gets the minimum number of seconds between lightning strikes.</summary>
    public float LightningMinInterval { get; init; }

    /// <summary>Gets the maximum number of seconds between lightning strikes.</summary>
    public float LightningMaxInterval { get; init; }

    #endregion

    #region Audio volumes (0–1)

    /// <summary>Gets the target volume for the rain ambient loop.</summary>
    public float RainVolume { get; init; }

    /// <summary>Gets the target volume for the wind ambient loop.</summary>
    public float WindVolume { get; init; }

    /// <summary>Gets the target volume for the distant thunder ambient loop.</summary>
    public float ThunderVolume { get; init; }

    #endregion

    #region Custom data

    /// <summary>
    /// Gets an optional JSON string with arbitrary developer-defined attributes for this weather state.
    /// The library does not parse this field; consuming code deserializes it as needed.
    /// Example: <c>{"DañaVida":true,"Cantidad":10}</c>
    /// </summary>
    public string? CustomData { get; init; }

    #endregion

    #region Factory

    /// <summary>
    /// Linearly interpolates every numeric and color field between <paramref name="from"/> and <paramref name="to"/> by <paramref name="t"/> in [0, 1].
    /// Bool fields switch to <paramref name="to"/> values when t ≥ 0.5.
    /// <see cref="PrecipitationIntensity"/> switches at t ≥ 0.5.
    /// <see cref="CustomData"/> is inherited from <paramref name="to"/>.
    /// No heap allocations.
    /// </summary>
    public static WeatherProfile Lerp(in WeatherProfile from, in WeatherProfile to, float t)
    {
        bool useTo = t >= 0.5f;

        return new WeatherProfile
        {
            TemperatureMin = MathHelper.Lerp(from.TemperatureMin, to.TemperatureMin, t),
            TemperatureMax = MathHelper.Lerp(from.TemperatureMax, to.TemperatureMax, t),

            WindSpeedMinKmh  = MathHelper.Lerp(from.WindSpeedMinKmh, to.WindSpeedMinKmh, t),
            WindSpeedMaxKmh  = MathHelper.Lerp(from.WindSpeedMaxKmh, to.WindSpeedMaxKmh, t),
            WindDirection    = Vector2.Lerp(from.WindDirection, to.WindDirection, t),
            WindTurbulence   = MathHelper.Lerp(from.WindTurbulence, to.WindTurbulence, t),

            AmbientColor    = Color.Lerp(from.AmbientColor, to.AmbientColor, t),
            AmbientIntensity = MathHelper.Lerp(from.AmbientIntensity, to.AmbientIntensity, t),
            SkyColor        = Color.Lerp(from.SkyColor, to.SkyColor, t),

            FogColor   = Color.Lerp(from.FogColor, to.FogColor, t),
            FogDensity = MathHelper.Lerp(from.FogDensity, to.FogDensity, t),

            HasPrecipitation  = useTo ? to.HasPrecipitation : from.HasPrecipitation,
            PrecipitationLevel = useTo ? to.PrecipitationLevel : from.PrecipitationLevel,

            HasLightning        = useTo ? to.HasLightning : from.HasLightning,
            LightningMinInterval = MathHelper.Lerp(from.LightningMinInterval, to.LightningMinInterval, t),
            LightningMaxInterval = MathHelper.Lerp(from.LightningMaxInterval, to.LightningMaxInterval, t),

            RainVolume    = MathHelper.Lerp(from.RainVolume, to.RainVolume, t),
            WindVolume    = MathHelper.Lerp(from.WindVolume, to.WindVolume, t),
            ThunderVolume = MathHelper.Lerp(from.ThunderVolume, to.ThunderVolume, t),

            CustomData = to.CustomData
        };
    }

    #endregion
}
