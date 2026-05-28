using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Weather;

namespace Alca.MonoGame.Kernel.UnitTests.Weather;

public sealed class LightningControllerTests
{
    private static (LightningController controller, WeatherWorld weatherWorld, GameWorld gameWorld) Create()
    {
        var weatherWorld = new WeatherWorld();
        var gameWorld    = new GameWorld { WeatherWorld = weatherWorld };
        var controller   = new LightningController(weatherWorld, gameWorld);
        return (controller, weatherWorld, gameWorld);
    }

    private static GameTime MakeGameTime(double dtSeconds)
        => new(TimeSpan.Zero, TimeSpan.FromSeconds(dtSeconds));

    [Fact]
    public void Constructor_IsFlashing_IsFalse()
    {
        var (controller, _, _) = Create();
        Assert.False(controller.IsFlashing);
    }

    [Fact]
    public void TriggerStrikeAt_SetsIsFlashing()
    {
        var (controller, _, _) = Create();
        controller.TriggerStrikeAt(new Vector2(100f, -80f));
        Assert.True(controller.IsFlashing);
    }

    [Fact]
    public void TriggerStrikeAt_RaisesLightningStruckEvent()
    {
        var (controller, weatherWorld, _) = Create();
        int count = 0;
        weatherWorld.LightningStruck += _ => count++;

        controller.TriggerStrikeAt(Vector2.Zero);

        Assert.Equal(1, count);
    }

    [Fact]
    public void TriggerStrikeAt_EventContainsCorrectPosition()
    {
        var (controller, weatherWorld, _) = Create();
        var expected = new Vector2(300f, -80f);
        Vector2 received = Vector2.Zero;
        weatherWorld.LightningStruck += e => received = e.Position;

        controller.TriggerStrikeAt(expected);

        Assert.Equal(expected, received);
    }

    [Fact]
    public void Update_AfterFlashDuration_EndsFlash()
    {
        var (controller, weatherWorld, _) = Create();
        controller.TriggerStrikeAt(Vector2.Zero);
        Assert.True(controller.IsFlashing);

        // Use a profile with HasLightning=false so no new strike fires during Update
        var profile = new WeatherProfile { HasLightning = false };
        controller.Update(MakeGameTime(controller.FlashDuration + 0.1), profile, []);

        Assert.False(controller.IsFlashing);
    }

    [Fact]
    public void Update_BeforeFlashDuration_StillFlashing()
    {
        var (controller, _, _) = Create();
        controller.TriggerStrikeAt(Vector2.Zero);

        var profile = new WeatherProfile { HasLightning = false };
        controller.Update(MakeGameTime(controller.FlashDuration * 0.5), profile, []);

        Assert.True(controller.IsFlashing);
    }

    [Fact]
    public void Update_HasLightningFalse_NeverStrikesAutomatically()
    {
        var (controller, _, _) = Create();
        var profile = new WeatherProfile { HasLightning = false };

        // Advance 60 seconds — no automatic strike should fire
        controller.Update(MakeGameTime(60.0), profile, []);

        Assert.False(controller.IsFlashing);
    }

    [Fact]
    public void Update_HasLightningTrue_StrikesWhenTimerExpires()
    {
        var (controller, _, _) = Create();
        var profile = new WeatherProfile
        {
            HasLightning         = true,
            LightningMinInterval = 0.5f,
            LightningMaxInterval = 0.5f
        };

        // Initial _nextStrikeInterval = 10f, so advance 11 seconds to guarantee a strike
        controller.Update(MakeGameTime(11.0), profile, []);

        Assert.True(controller.IsFlashing);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var (controller, _, _) = Create();
        var ex = Record.Exception(() => controller.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        var (controller, _, _) = Create();
        controller.Dispose();
        var ex = Record.Exception(() => controller.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void FlashIntensity_DefaultIsEight()
    {
        var (controller, _, _) = Create();
        Assert.Equal(8f, controller.FlashIntensity, 3);
    }

    [Fact]
    public void TriggerStrikeAt_WithBehaviours_DoesNotThrow()
    {
        var (controller, _, _) = Create();
        var behaviours = new List<WeatherBehaviour>();
        var profile = new WeatherProfile { HasLightning = false };

        var ex = Record.Exception(() =>
            controller.Update(MakeGameTime(0.016), profile, behaviours));
        Assert.Null(ex);
    }
}
