using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Weather;

namespace Alca.MonoGame.Kernel.UnitTests.Weather;

public sealed class WeatherWorldTests
{
    private static GameTime MakeGameTime(double dtSeconds = 1.0 / 60.0)
        => new(TimeSpan.Zero, TimeSpan.FromSeconds(dtSeconds));

    [Fact]
    public void Constructor_StartsInSunny()
    {
        var world = new WeatherWorld();
        Assert.Equal(WeatherTypeId.Sunny, world.CurrentWeather);
    }

    [Fact]
    public void Constructor_NotTransitioning()
    {
        var world = new WeatherWorld();
        Assert.False(world.IsTransitioning);
        Assert.Equal(1f, world.TransitionProgress, 3);
    }

    [Fact]
    public void TryGetProfile_Predefined_ReturnsTrue()
    {
        var world = new WeatherWorld();
        bool found = world.TryGetProfile(WeatherTypeId.Storm, out _);
        Assert.True(found);
    }

    [Fact]
    public void TryGetProfile_Unknown_ReturnsFalse()
    {
        var world = new WeatherWorld();
        bool found = world.TryGetProfile(new WeatherTypeId("nonexistent"), out _);
        Assert.False(found);
    }

    [Fact]
    public void SetWeather_BeginnsTransition()
    {
        var world = new WeatherWorld();
        world.SetWeather(WeatherTypeId.Storm, 3f);
        Assert.True(world.IsTransitioning);
        Assert.Equal(0f, world.TransitionProgress, 3);
    }

    [Fact]
    public void SetWeather_UnknownType_Throws()
    {
        var world = new WeatherWorld();
        Assert.Throws<KeyNotFoundException>(
            () => world.SetWeather(new WeatherTypeId("does_not_exist")));
    }

    [Fact]
    public void SetWeatherImmediate_AppliesInstantly()
    {
        var world = new WeatherWorld();
        world.SetWeatherImmediate(WeatherTypeId.Blizzard);
        Assert.Equal(WeatherTypeId.Blizzard, world.CurrentWeather);
        Assert.False(world.IsTransitioning);
    }

    [Fact]
    public void SetWeatherImmediate_UnknownType_Throws()
    {
        var world = new WeatherWorld();
        Assert.Throws<KeyNotFoundException>(
            () => world.SetWeatherImmediate(new WeatherTypeId("ghost")));
    }

    [Fact]
    public void Update_CompletesTransitionAfterDuration()
    {
        var world = new WeatherWorld();
        world.SetWeather(WeatherTypeId.Storm, 1f);

        // Advance 1.1 s — transition should complete
        world.Update(MakeGameTime(1.1));

        Assert.False(world.IsTransitioning);
        Assert.Equal(WeatherTypeId.Storm, world.CurrentWeather);
    }

    [Fact]
    public void CurrentTemperature_AfterUpdate_IsInProfileRange()
    {
        var world = new WeatherWorld();
        world.Update(MakeGameTime(0.1));

        float min = WeatherProfiles.Sunny.TemperatureMin;
        float max = WeatherProfiles.Sunny.TemperatureMax;
        Assert.InRange(world.CurrentTemperature, min - 1f, max + 1f);
    }

    [Fact]
    public void RegisterCustomWeather_ThenTryGetProfile_ReturnsTrue()
    {
        var world = new WeatherWorld();
        var id = new WeatherTypeId("radioactive_rain");
        var profile = new WeatherProfile { TemperatureMin = 25f, TemperatureMax = 30f };

        world.RegisterCustomWeather(id, profile);

        bool found = world.TryGetProfile(id, out WeatherProfile retrieved);
        Assert.True(found);
        Assert.Equal(25f, retrieved.TemperatureMin, 3);
    }

    [Fact]
    public void RegisterCustomWeather_WindAndFog_ForcesZeroFog()
    {
        var world = new WeatherWorld();
        var id = new WeatherTypeId("windy_foggy");
        var profile = new WeatherProfile
        {
            WindSpeedMaxKmh = 10f,
            FogDensity      = 0.5f
        };

        world.RegisterCustomWeather(id, profile);

        world.TryGetProfile(id, out WeatherProfile retrieved);
        Assert.Equal(0f, retrieved.FogDensity, 3);
    }

    [Fact]
    public void ModifyProfile_UpdatesActiveProfile()
    {
        var world = new WeatherWorld();

        world.ModifyProfile(WeatherTypeId.Sunny, new WeatherProfile
        {
            TemperatureMin = 99f,
            TemperatureMax = 99f,
            WindDirection  = Vector2.UnitX
        });

        Assert.Equal(99f, world.ActiveProfile.TemperatureMin, 3);
    }

    [Fact]
    public void ModifyProfile_UnregisteredType_Throws()
    {
        var world = new WeatherWorld();
        Assert.Throws<KeyNotFoundException>(
            () => world.ModifyProfile(new WeatherTypeId("ghost"), default));
    }

    [Fact]
    public void ModifyProfile_TargetDuringTransition_UpdatesDestination()
    {
        var world = new WeatherWorld();
        world.SetWeather(WeatherTypeId.Storm, 10f);

        world.ModifyProfile(WeatherTypeId.Storm, new WeatherProfile
        {
            TemperatureMin = 77f,
            TemperatureMax = 77f
        });

        world.TryGetProfile(WeatherTypeId.Storm, out WeatherProfile stored);
        Assert.Equal(77f, stored.TemperatureMin, 3);
    }

    [Fact]
    public void LightningStruck_EventRaisedWhenTriggered()
    {
        var gameWorld = new GameWorld();
        var weather   = new WeatherWorld();
        gameWorld.WeatherWorld = weather;

        int count = 0;
        weather.LightningStruck += _ => count++;

        weather.RaiseLightningStruck(new LightningStrikeEvent
        {
            Position        = Vector2.Zero,
            Intensity       = 1f,
            ImpulseRadius   = 100f,
            ImpulseStrength = 200f
        });

        Assert.Equal(1, count);
    }

    [Fact]
    public void EnableParticles_SetsParticlesProperty()
    {
        var world   = new WeatherWorld();
        var layer   = new WeatherParticleLayer();
        world.EnableParticles(layer);
        Assert.Same(layer, world.Particles);
    }

    [Fact]
    public void EnableAudio_SetsAudioProperty()
    {
        var world = new WeatherWorld();
        var audio = new WeatherAudioLayer();
        world.EnableAudio(audio);
        Assert.Same(audio, world.Audio);
    }

    [Fact]
    public void SetWeather_ZeroDuration_AppliesImmediate()
    {
        var world = new WeatherWorld();
        world.SetWeather(WeatherTypeId.ColdSnap, 0f);
        Assert.False(world.IsTransitioning);
        Assert.Equal(WeatherTypeId.ColdSnap, world.CurrentWeather);
    }

    [Fact]
    public void IsInterior_Default_IsFalse()
    {
        var world = new WeatherWorld();
        Assert.False(world.IsInterior);
    }

    [Fact]
    public void IsInterior_True_TemperatureConvergesToIndoorTemperature()
    {
        var world = new WeatherWorld();
        world.SetWeatherImmediate(WeatherTypeId.Blizzard); // cold outdoors
        world.IsInterior = true;
        world.IndoorTemperature = 22f;

        // Simulate ~10 seconds indoors
        for (int i = 0; i < 600; i++)
            world.Update(MakeGameTime(1.0 / 60.0));

        Assert.InRange(world.CurrentTemperature, 20f, 24f);
    }

    [Fact]
    public void IsInterior_True_ParticleLayerIsInteriorSetToTrue()
    {
        var world  = new WeatherWorld();
        var layer  = new WeatherParticleLayer();
        world.EnableParticles(layer);
        world.IsInterior = true;

        world.Update(MakeGameTime());

        Assert.True(layer.IsInterior);
    }

    [Fact]
    public void IsInterior_False_ParticleLayerIsInteriorSetToFalse()
    {
        var world  = new WeatherWorld();
        var layer  = new WeatherParticleLayer();
        world.EnableParticles(layer);
        layer.IsInterior = true; // pre-set to true
        world.IsInterior = false;

        world.Update(MakeGameTime());

        Assert.False(layer.IsInterior);
    }

    [Fact]
    public void IndoorTemperature_Default_Is22()
    {
        var world = new WeatherWorld();
        Assert.Equal(22f, world.IndoorTemperature, 3);
    }

    [Fact]
    public void IndoorAudioMultiplier_Default_Is02()
    {
        var world = new WeatherWorld();
        Assert.Equal(0.2f, world.IndoorAudioMultiplier, 3);
    }

    [Fact]
    public void SetWeatherImmediate_ResetsGustAndTempWalkState()
    {
        var world = new WeatherWorld();
        // Run several frames to let gust machine activate
        for (int i = 0; i < 10; i++)
            world.Update(MakeGameTime(1.0));

        // Switching immediately resets internal gust state without throwing
        world.SetWeatherImmediate(WeatherTypeId.Storm);
        world.Update(MakeGameTime());

        // Wind speed should be within the new profile's range
        world.TryGetProfile(WeatherTypeId.Storm, out WeatherProfile storm);
        Assert.InRange(world.CurrentWind.SpeedKmh, storm.WindSpeedMinKmh, storm.WindSpeedMaxKmh + 1f);
    }

    [Fact]
    public void Update_WindSpeedStaysWithinProfileRange()
    {
        var world = new WeatherWorld();
        world.SetWeatherImmediate(WeatherTypeId.OrangeWind); // 60–75 km/h

        for (int i = 0; i < 60; i++)
            world.Update(MakeGameTime(1.0 / 60.0));

        world.TryGetProfile(WeatherTypeId.OrangeWind, out WeatherProfile p);
        Assert.InRange(world.CurrentWind.SpeedKmh, p.WindSpeedMinKmh, p.WindSpeedMaxKmh);
    }
}
