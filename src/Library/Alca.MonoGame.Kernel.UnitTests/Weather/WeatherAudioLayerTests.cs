using Alca.MonoGame.Kernel.Audio;
using Alca.MonoGame.Kernel.Audio.Mixer;
using Alca.MonoGame.Kernel.Weather;

namespace Alca.MonoGame.Kernel.UnitTests.Weather;

/// <summary>
/// Tests for WeatherAudioLayer that do not require OpenAL/audio hardware.
/// Sound-related paths (LoadSounds with real SoundEffect) are marked RequiresAudio.
/// </summary>
public sealed class WeatherAudioLayerTests
{
    private static GameTime MakeGameTime(double dt = 1.0 / 60.0)
        => new(TimeSpan.Zero, TimeSpan.FromSeconds(dt));

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var ex = Record.Exception(() => new WeatherAudioLayer());
        Assert.Null(ex);
    }

    [Fact]
    public void LoadSounds_AllNull_DoesNotThrow()
    {
        var layer = new WeatherAudioLayer();
        var ex = Record.Exception(() => layer.LoadSounds(null, null, null, null));
        Assert.Null(ex);
    }

    [Fact]
    public void Update_WithoutLoadSounds_DoesNotThrow()
    {
        var layer      = new WeatherAudioLayer();
        var profile    = new WeatherProfile();
        var controller = new AudioController(new AudioMixer());
        var ex = Record.Exception(() => layer.Update(MakeGameTime(), profile, controller));
        Assert.Null(ex);
    }

    [Fact]
    public void Update_MultipleFrames_DoesNotThrow()
    {
        var layer      = new WeatherAudioLayer();
        var profile    = new WeatherProfile { RainVolume = 0.8f, WindVolume = 0.5f, ThunderVolume = 0.3f };
        var controller = new AudioController(new AudioMixer());

        for (int i = 0; i < 120; i++)
        {
            var ex = Record.Exception(() => layer.Update(MakeGameTime(), profile, controller));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var layer = new WeatherAudioLayer();
        var ex = Record.Exception(() => layer.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        var layer = new WeatherAudioLayer();
        layer.Dispose();
        var ex = Record.Exception(() => layer.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void PlayThunderStrike_WithoutPool_DoesNotThrow()
    {
        var layer      = new WeatherAudioLayer();
        var ex = Record.Exception(() =>
            layer.PlayThunderStrike(new Vector2(100f, 0f), null!));
        // Pool is null, should return early without touching audioController
        Assert.Null(ex);
    }

    [Fact]
    public void FadeSpeed_DefaultIsOne()
    {
        var layer = new WeatherAudioLayer();
        Assert.Equal(1f, layer.FadeSpeed, 3);
    }

    [Fact]
    public void Channel_DefaultIsNull()
    {
        var layer = new WeatherAudioLayer();
        Assert.Null(layer.Channel);
    }
}
