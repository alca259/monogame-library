using Alca.MonoGame.Kernel.Weather;

namespace Alca.MonoGame.Kernel.UnitTests.Weather;

public sealed class WeatherProfileTests
{
    private static WeatherProfile MakeProfile(
        float tempMin, float tempMax,
        float windMin, float windMax,
        float fogDensity = 0f,
        bool hasPrecipitation = false,
        PrecipitationIntensity level = PrecipitationIntensity.None,
        bool hasLightning = false,
        float rainVol = 0f, float windVol = 0f, float thunderVol = 0f,
        string? customData = null) => new()
    {
        TemperatureMin    = tempMin,
        TemperatureMax    = tempMax,
        WindSpeedMinKmh   = windMin,
        WindSpeedMaxKmh   = windMax,
        WindDirection     = Vector2.UnitX,
        FogDensity        = fogDensity,
        HasPrecipitation  = hasPrecipitation,
        PrecipitationLevel = level,
        HasLightning      = hasLightning,
        RainVolume        = rainVol,
        WindVolume        = windVol,
        ThunderVolume     = thunderVol,
        CustomData        = customData
    };

    [Fact]
    public void Lerp_AtT0_ReturnsFrom()
    {
        WeatherProfile from = MakeProfile(10f, 20f, 2f, 4f);
        WeatherProfile to   = MakeProfile(30f, 40f, 10f, 15f);

        WeatherProfile result = WeatherProfile.Lerp(from, to, 0f);

        Assert.Equal(10f, result.TemperatureMin, 3);
        Assert.Equal(20f, result.TemperatureMax, 3);
        Assert.Equal(2f,  result.WindSpeedMinKmh, 3);
        Assert.Equal(4f,  result.WindSpeedMaxKmh, 3);
    }

    [Fact]
    public void Lerp_AtT1_ReturnsTo()
    {
        WeatherProfile from = MakeProfile(10f, 20f, 2f, 4f);
        WeatherProfile to   = MakeProfile(30f, 40f, 10f, 15f);

        WeatherProfile result = WeatherProfile.Lerp(from, to, 1f);

        Assert.Equal(30f, result.TemperatureMin, 3);
        Assert.Equal(40f, result.TemperatureMax, 3);
        Assert.Equal(10f, result.WindSpeedMinKmh, 3);
        Assert.Equal(15f, result.WindSpeedMaxKmh, 3);
    }

    [Fact]
    public void Lerp_AtHalf_MidpointValues()
    {
        WeatherProfile from = MakeProfile(0f, 0f, 0f, 0f, rainVol: 0f);
        WeatherProfile to   = MakeProfile(20f, 20f, 10f, 10f, rainVol: 1f);

        WeatherProfile result = WeatherProfile.Lerp(from, to, 0.5f);

        Assert.Equal(10f,  result.TemperatureMin, 3);
        Assert.Equal(5f,   result.WindSpeedMinKmh, 3);
        Assert.Equal(0.5f, result.RainVolume, 3);
    }

    [Fact]
    public void Lerp_BoolFields_SwitchAtHalf()
    {
        WeatherProfile from = MakeProfile(0f, 0f, 0f, 0f,
            hasPrecipitation: false, hasLightning: false);
        WeatherProfile to   = MakeProfile(0f, 0f, 0f, 0f,
            hasPrecipitation: true,  hasLightning: true);

        WeatherProfile atPre  = WeatherProfile.Lerp(from, to, 0.49f);
        WeatherProfile atPost = WeatherProfile.Lerp(from, to, 0.5f);

        Assert.False(atPre.HasPrecipitation);
        Assert.False(atPre.HasLightning);
        Assert.True(atPost.HasPrecipitation);
        Assert.True(atPost.HasLightning);
    }

    [Fact]
    public void Lerp_PrecipitationLevel_SwitchesAtHalf()
    {
        WeatherProfile from = MakeProfile(0f, 0f, 0f, 0f,
            level: PrecipitationIntensity.None);
        WeatherProfile to   = MakeProfile(0f, 0f, 0f, 0f,
            level: PrecipitationIntensity.High);

        WeatherProfile atPre  = WeatherProfile.Lerp(from, to, 0.49f);
        WeatherProfile atPost = WeatherProfile.Lerp(from, to, 0.5f);

        Assert.Equal(PrecipitationIntensity.None, atPre.PrecipitationLevel);
        Assert.Equal(PrecipitationIntensity.High, atPost.PrecipitationLevel);
    }

    [Fact]
    public void Lerp_CustomData_InheritedFromTo()
    {
        WeatherProfile from = MakeProfile(0f, 0f, 0f, 0f, customData: "{\"from\":true}");
        WeatherProfile to   = MakeProfile(0f, 0f, 0f, 0f, customData: "{\"to\":true}");

        WeatherProfile result = WeatherProfile.Lerp(from, to, 0f);
        Assert.Equal("{\"to\":true}", result.CustomData);
    }

    [Fact]
    public void Lerp_CustomData_Null_WhenToIsNull()
    {
        WeatherProfile from = MakeProfile(0f, 0f, 0f, 0f, customData: "{\"from\":true}");
        WeatherProfile to   = MakeProfile(0f, 0f, 0f, 0f, customData: null);

        WeatherProfile result = WeatherProfile.Lerp(from, to, 0f);
        Assert.Null(result.CustomData);
    }

    [Fact]
    public void Profile_CanHaveBothWindAndFog_NoConstraintInStruct()
    {
        // The struct itself does NOT enforce the wind/fog constraint; WeatherWorld does.
        var profile = new WeatherProfile
        {
            WindSpeedMaxKmh = 10f,
            FogDensity      = 0.5f
        };
        Assert.Equal(10f,  profile.WindSpeedMaxKmh);
        Assert.Equal(0.5f, profile.FogDensity);
    }

    [Fact]
    public void Lerp_AudioVolumes_Interpolate()
    {
        WeatherProfile from = MakeProfile(0f, 0f, 0f, 0f,
            rainVol: 0f, windVol: 0f, thunderVol: 0f);
        WeatherProfile to   = MakeProfile(0f, 0f, 0f, 0f,
            rainVol: 1f, windVol: 1f, thunderVol: 1f);

        WeatherProfile result = WeatherProfile.Lerp(from, to, 0.25f);

        Assert.Equal(0.25f, result.RainVolume,    3);
        Assert.Equal(0.25f, result.WindVolume,    3);
        Assert.Equal(0.25f, result.ThunderVolume, 3);
    }
}
