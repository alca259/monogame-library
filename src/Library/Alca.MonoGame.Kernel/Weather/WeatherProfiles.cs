namespace Alca.MonoGame.Kernel.Weather;

/// <summary>
/// Default <see cref="WeatherProfile"/> catalog for all predefined <see cref="WeatherTypeId"/> values.
/// Profiles follow the climatological constraints defined in the specification:
/// states with fog always have zero wind; states with wind have zero fog density.
/// </summary>
public static class WeatherProfiles
{
    #region Light color presets
    private static readonly Color _warmAmbient    = new(255, 220, 160);
    private static readonly Color _neutralAmbient = new(200, 200, 200);
    private static readonly Color _coldAmbient    = new(160, 185, 220);

    private static readonly Color _sunnySky  = new(100, 160, 230);
    private static readonly Color _cloudySky = new(130, 140, 150);
    private static readonly Color _stormSky  = new(80, 85, 90);
    private static readonly Color _fogSky    = new(200, 205, 210);
    private static readonly Color _snowSky   = new(190, 195, 210);

    private static readonly Color _fogColor  = new(210, 215, 220, 200);
    #endregion

    #region Catalog
    /// <summary>Clear sunny day. Warm light, light breeze, no precipitation.</summary>
    public static readonly WeatherProfile Sunny = new()
    {
        TemperatureMin   = 24f,
        TemperatureMax   = 24f,
        WindSpeedMinKmh  = 2f,
        WindSpeedMaxKmh  = 4f,
        WindDirection    = new Vector2(1f, 0f),
        WindTurbulence   = 0.1f,
        AmbientColor     = _warmAmbient,
        AmbientIntensity = 1f,
        SkyColor         = _sunnySky,
        FogColor         = Color.Transparent,
        FogDensity       = 0f,
        HasPrecipitation = false,
        PrecipitationLevel = PrecipitationIntensity.None,
        HasLightning     = false,
        RainVolume       = 0f,
        WindVolume       = 0.05f,
        ThunderVolume    = 0f
    };

    /// <summary>Extreme heat wave. Very warm light, light breeze, no precipitation.</summary>
    public static readonly WeatherProfile HeatWave = new()
    {
        TemperatureMin   = 40f,
        TemperatureMax   = 40f,
        WindSpeedMinKmh  = 2f,
        WindSpeedMaxKmh  = 4f,
        WindDirection    = new Vector2(1f, 0f),
        WindTurbulence   = 0.05f,
        AmbientColor     = new Color(255, 200, 100),
        AmbientIntensity = 1f,
        SkyColor         = new Color(180, 150, 80),
        FogColor         = Color.Transparent,
        FogDensity       = 0f,
        HasPrecipitation = false,
        PrecipitationLevel = PrecipitationIntensity.None,
        HasLightning     = false,
        RainVolume       = 0f,
        WindVolume       = 0.05f,
        ThunderVolume    = 0f
    };

    /// <summary>Overcast cloudy sky. Cool light, light breeze, no precipitation.</summary>
    public static readonly WeatherProfile Cloudy = new()
    {
        TemperatureMin   = 15f,
        TemperatureMax   = 15f,
        WindSpeedMinKmh  = 2f,
        WindSpeedMaxKmh  = 4f,
        WindDirection    = new Vector2(1f, 0f),
        WindTurbulence   = 0.2f,
        AmbientColor     = _coldAmbient,
        AmbientIntensity = 0.75f,
        SkyColor         = _cloudySky,
        FogColor         = Color.Transparent,
        FogDensity       = 0f,
        HasPrecipitation = false,
        PrecipitationLevel = PrecipitationIntensity.None,
        HasLightning     = false,
        RainVolume       = 0f,
        WindVolume       = 0.1f,
        ThunderVolume    = 0f
    };

    /// <summary>
    /// Dense fog. Cool light, zero wind (wind and fog are mutually exclusive), very high fog particle coverage.
    /// </summary>
    public static readonly WeatherProfile Fog = new()
    {
        TemperatureMin   = 15f,
        TemperatureMax   = 15f,
        WindSpeedMinKmh  = 0f,
        WindSpeedMaxKmh  = 0f,                       // no wind — enforced constraint
        WindDirection    = Vector2.Zero,
        WindTurbulence   = 0f,
        AmbientColor     = _coldAmbient,
        AmbientIntensity = 0.55f,
        SkyColor         = _fogSky,
        FogColor         = _fogColor,
        FogDensity       = 0.75f,
        HasPrecipitation = true,
        PrecipitationLevel = PrecipitationIntensity.VeryHigh,
        HasLightning     = false,
        RainVolume       = 0f,
        WindVolume       = 0f,
        ThunderVolume    = 0f
    };

    /// <summary>Rain storm. Cold light, light wind, heavy rain particles.</summary>
    public static readonly WeatherProfile Storm = new()
    {
        TemperatureMin   = 15f,
        TemperatureMax   = 15f,
        WindSpeedMinKmh  = 2f,
        WindSpeedMaxKmh  = 4f,
        WindDirection    = new Vector2(1f, 0f),
        WindTurbulence   = 0.3f,
        AmbientColor     = _coldAmbient,
        AmbientIntensity = 0.5f,
        SkyColor         = _stormSky,
        FogColor         = Color.Transparent,
        FogDensity       = 0f,
        HasPrecipitation = true,
        PrecipitationLevel = PrecipitationIntensity.High,
        HasLightning     = false,
        RainVolume       = 0.9f,
        WindVolume       = 0.2f,
        ThunderVolume    = 0.1f
    };

    /// <summary>Thunderstorm with lightning. Cold light, moderate wind, lightning strikes.</summary>
    public static readonly WeatherProfile Thunderstorm = new()
    {
        TemperatureMin    = 20f,
        TemperatureMax    = 20f,
        WindSpeedMinKmh   = 10f,
        WindSpeedMaxKmh   = 15f,
        WindDirection     = new Vector2(1f, 0f),
        WindTurbulence    = 0.5f,
        AmbientColor      = _coldAmbient,
        AmbientIntensity  = 0.4f,
        SkyColor          = _stormSky,
        FogColor          = Color.Transparent,
        FogDensity        = 0f,
        HasPrecipitation  = true,
        PrecipitationLevel = PrecipitationIntensity.Medium,
        HasLightning      = true,
        LightningMinInterval = 5f,
        LightningMaxInterval = 15f,
        RainVolume        = 0.7f,
        WindVolume        = 0.4f,
        ThunderVolume     = 0.6f
    };

    /// <summary>Hail storm. Cold light, moderate wind, medium-intensity hail particles.</summary>
    public static readonly WeatherProfile HailStorm = new()
    {
        TemperatureMin   = 30f,
        TemperatureMax   = 30f,
        WindSpeedMinKmh  = 4f,
        WindSpeedMaxKmh  = 8f,
        WindDirection    = new Vector2(1f, 0f),
        WindTurbulence   = 0.4f,
        AmbientColor     = _coldAmbient,
        AmbientIntensity = 0.55f,
        SkyColor         = _stormSky,
        FogColor         = Color.Transparent,
        FogDensity       = 0f,
        HasPrecipitation = true,
        PrecipitationLevel = PrecipitationIntensity.Medium,
        HasLightning     = false,
        RainVolume       = 0.5f,
        WindVolume       = 0.3f,
        ThunderVolume    = 0f
    };

    /// <summary>Blizzard snowstorm. Cold light, light wind, heavy snow particles.</summary>
    public static readonly WeatherProfile Blizzard = new()
    {
        TemperatureMin   = -5f,
        TemperatureMax   = 0f,
        WindSpeedMinKmh  = 2f,
        WindSpeedMaxKmh  = 4f,
        WindDirection    = new Vector2(1f, 0f),
        WindTurbulence   = 0.3f,
        AmbientColor     = new Color(180, 195, 230),
        AmbientIntensity = 0.6f,
        SkyColor         = _snowSky,
        FogColor         = new Color(220, 225, 240, 80),
        FogDensity       = 0.2f,
        HasPrecipitation = true,
        PrecipitationLevel = PrecipitationIntensity.High,
        HasLightning     = false,
        RainVolume       = 0f,
        WindVolume       = 0.2f,
        ThunderVolume    = 0f
    };

    /// <summary>Extreme cold snap. Cold light, very strong wind, no precipitation.</summary>
    public static readonly WeatherProfile ColdSnap = new()
    {
        TemperatureMin   = -10f,
        TemperatureMax   = -10f,
        WindSpeedMinKmh  = 30f,
        WindSpeedMaxKmh  = 33f,
        WindDirection    = new Vector2(1f, 0f),
        WindTurbulence   = 0.6f,
        AmbientColor     = new Color(150, 175, 225),
        AmbientIntensity = 0.65f,
        SkyColor         = _snowSky,
        FogColor         = Color.Transparent,
        FogDensity       = 0f,
        HasPrecipitation = false,
        PrecipitationLevel = PrecipitationIntensity.None,
        HasLightning     = false,
        RainVolume       = 0f,
        WindVolume       = 0.8f,
        ThunderVolume    = 0f
    };

    /// <summary>Orange-alert wind event. Neutral light, very strong wind, wind sprite particles at high density.</summary>
    public static readonly WeatherProfile OrangeWind = new()
    {
        TemperatureMin   = 10f,
        TemperatureMax   = 10f,
        WindSpeedMinKmh  = 60f,
        WindSpeedMaxKmh  = 75f,
        WindDirection    = new Vector2(1f, 0f),
        WindTurbulence   = 0.7f,
        AmbientColor     = _neutralAmbient,
        AmbientIntensity = 0.8f,
        SkyColor         = _cloudySky,
        FogColor         = Color.Transparent,
        FogDensity       = 0f,
        HasPrecipitation = true,
        PrecipitationLevel = PrecipitationIntensity.High,
        HasLightning     = false,
        RainVolume       = 0f,
        WindVolume       = 1f,
        ThunderVolume    = 0f
    };
    #endregion

    #region Lookup helper
    /// <summary>
    /// Returns the default profile for a predefined <see cref="WeatherTypeId"/>, or <see langword="null"/> if the id is not in the built-in catalog.
    /// </summary>
    public static WeatherProfile? Get(WeatherTypeId id)
    {
        if (id == WeatherTypeId.Sunny)        return Sunny;
        if (id == WeatherTypeId.HeatWave)     return HeatWave;
        if (id == WeatherTypeId.Cloudy)       return Cloudy;
        if (id == WeatherTypeId.Fog)          return Fog;
        if (id == WeatherTypeId.Storm)        return Storm;
        if (id == WeatherTypeId.Thunderstorm) return Thunderstorm;
        if (id == WeatherTypeId.HailStorm)    return HailStorm;
        if (id == WeatherTypeId.Blizzard)     return Blizzard;
        if (id == WeatherTypeId.ColdSnap)     return ColdSnap;
        if (id == WeatherTypeId.OrangeWind)   return OrangeWind;
        return null;
    }
    #endregion
}
