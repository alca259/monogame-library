using Alca.MonoGame.Kernel.Audio;

namespace Alca.MonoGame.Kernel.UnitTests.Audio;

/// <summary>
/// Tests for AudioCrossfader. Tests that require SoundEffect instances are marked
/// RequiresAudio since they need OpenAL initialized.
/// </summary>
public sealed class AudioCrossfaderTests
{
    private static GameTime Delta(double seconds)
        => new(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));

    [Fact]
    public void Constructor_DefaultState_IsNotCrossfading()
    {
        using var crossfader = new AudioCrossfader();
        Assert.False(crossfader.IsCrossfading);
    }

    [Fact]
    public void Stop_WithZeroDuration_StopsImmediately()
    {
        using var crossfader = new AudioCrossfader();

        crossfader.Stop(0f);

        Assert.False(crossfader.IsCrossfading);
    }

    [Fact]
    public void Stop_WithPositiveDuration_SetsCrossfadingTrue()
    {
        using var crossfader = new AudioCrossfader();

        crossfader.Stop(1f);

        Assert.True(crossfader.IsCrossfading);
    }

    [Fact]
    public void Stop_WithDuration_FadesOutOverTime()
    {
        using var crossfader = new AudioCrossfader();
        crossfader.Stop(1f);

        // After full duration, crossfade should end.
        crossfader.Update(Delta(1.0));

        Assert.False(crossfader.IsCrossfading);
    }

    [Fact]
    public void Dispose_WhenNotCrossfading_DoesNotThrow()
    {
        var crossfader = new AudioCrossfader();
        Exception? ex = Record.Exception(() => crossfader.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Update_WhenNotCrossfading_DoesNotThrow()
    {
        using var crossfader = new AudioCrossfader();
        Exception? ex = Record.Exception(() => crossfader.Update(Delta(0.016)));
        Assert.Null(ex);
    }
}
