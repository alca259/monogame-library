using Alca.MonoGame.Kernel.Audio;

namespace Alca.MonoGame.Kernel.UnitTests.Audio;

public sealed class AudioMixerChannelTests
{
    [Fact]
    public void Name_IsPreserved()
    {
        AudioMixerChannel channel = new("Music");
        Assert.Equal("Music", channel.Name);
    }

    [Fact]
    public void DefaultVolume_IsOne()
    {
        AudioMixerChannel channel = new("SFX");
        Assert.Equal(1f, channel.Volume);
    }

    [Fact]
    public void Volume_IsClamped_BelowZero()
    {
        AudioMixerChannel channel = new("SFX");
        channel.Volume = -5f;
        Assert.Equal(0f, channel.Volume);
    }

    [Fact]
    public void Volume_IsClamped_AboveOne()
    {
        AudioMixerChannel channel = new("SFX");
        channel.Volume = 999f;
        Assert.Equal(1f, channel.Volume);
    }

    [Fact]
    public void Volume_CanBeSetInRange()
    {
        AudioMixerChannel channel = new("SFX");
        channel.Volume = 0.5f;
        Assert.Equal(0.5f, channel.Volume);
    }

    [Fact]
    public void Muted_DefaultIsFalse()
    {
        AudioMixerChannel channel = new("SFX");
        Assert.False(channel.Muted);
    }

    [Fact]
    public void Muted_WhenTrue_EffectiveVolumeIsZero()
    {
        AudioMixerChannel channel = new("SFX") { Volume = 0.8f, Muted = true };
        Assert.Equal(0f, channel.EffectiveVolume);
    }

    [Fact]
    public void Muted_WhenFalse_EffectiveVolumeEqualsVolume()
    {
        AudioMixerChannel channel = new("SFX") { Volume = 0.6f, Muted = false };
        Assert.Equal(0.6f, channel.EffectiveVolume);
    }

    [Fact]
    public void Constructor_WithInitialVolume_ClampsToRange()
    {
        AudioMixerChannel channel = new("SFX", 1.5f);
        Assert.Equal(1f, channel.Volume);
    }
}
