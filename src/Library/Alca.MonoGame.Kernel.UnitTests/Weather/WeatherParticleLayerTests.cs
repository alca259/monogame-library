using Alca.MonoGame.Kernel.Weather;

namespace Alca.MonoGame.Kernel.UnitTests.Weather;

/// <summary>
/// Tests for WeatherParticleLayer that do not require a GPU (all textures are null).
/// Particle effect creation with real textures requires the GraphicsDevice collection.
/// </summary>
public sealed class WeatherParticleLayerTests
{
    private static GameTime MakeGameTime(double dt = 1.0 / 60.0)
        => new(TimeSpan.Zero, TimeSpan.FromSeconds(dt));

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var ex = Record.Exception(() => new WeatherParticleLayer());
        Assert.Null(ex);
    }

    [Fact]
    public void LoadContent_AllNull_DoesNotThrow()
    {
        var layer = new WeatherParticleLayer();
        var ex = Record.Exception(() => layer.LoadContent(null, null, null, null, null));
        Assert.Null(ex);
    }

    [Fact]
    public void Update_WithoutLoadContent_DoesNotThrow()
    {
        var layer   = new WeatherParticleLayer();
        var profile = new WeatherProfile { HasPrecipitation = true, PrecipitationLevel = PrecipitationIntensity.High };
        var wind    = new WindState { Direction = Vector2.UnitX, SpeedKmh = 10f, Turbulence = 0.2f };

        var ex = Record.Exception(() => layer.Update(MakeGameTime(), profile, wind));
        Assert.Null(ex);
    }

    [Fact]
    public void Update_MultipleFramesAllWeathers_DoesNotThrow()
    {
        var layer = new WeatherParticleLayer();
        WeatherTypeId[] types =
        [
            WeatherTypeId.Sunny, WeatherTypeId.Storm, WeatherTypeId.Blizzard,
            WeatherTypeId.Fog,   WeatherTypeId.ColdSnap, WeatherTypeId.OrangeWind
        ];

        foreach (WeatherTypeId id in types)
        {
            WeatherProfile profile = WeatherProfiles.Get(id) ?? default;
            var wind = new WindState
            {
                Direction  = Vector2.UnitX,
                SpeedKmh   = profile.WindSpeedMaxKmh,
                Turbulence = profile.WindTurbulence
            };
            var ex = Record.Exception(() => layer.Update(MakeGameTime(), profile, wind));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void Draw_WithoutLoadContent_DoesNotThrow()
    {
        // Draw without a SpriteBatch (null effects guard check)
        var layer = new WeatherParticleLayer();
        var ex = Record.Exception(() => layer.Draw(null!));
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var layer = new WeatherParticleLayer();
        var ex = Record.Exception(() => layer.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        var layer = new WeatherParticleLayer();
        layer.Dispose();
        var ex = Record.Exception(() => layer.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void EmitterPosition_SetAndGet()
    {
        var layer = new WeatherParticleLayer();
        var pos = new Vector2(400f, -100f);
        layer.EmitterPosition = pos;
        Assert.Equal(pos, layer.EmitterPosition);
    }

    [Fact]
    public void EmitterWidth_DefaultIs1600()
    {
        var layer = new WeatherParticleLayer();
        Assert.Equal(1600f, layer.EmitterWidth, 3);
    }
}
