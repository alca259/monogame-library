using Alca.MonoGame.Kernel.Audio.Mixer;

namespace Alca.MonoGame.Kernel.UnitTests.Audio;

public sealed class AudioMixerTests
{
    [Fact]
    public void HasDefaultChannels_AfterConstruction()
    {
        AudioMixer mixer = new();

        Assert.NotNull(mixer.Master);
        Assert.NotNull(mixer.Music);
        Assert.NotNull(mixer.Sfx);
        Assert.NotNull(mixer.Ambient);
    }

    [Fact]
    public void DefaultChannels_HaveExpectedNames()
    {
        AudioMixer mixer = new();

        Assert.Equal(AudioMixer.MasterChannelName, mixer.Master.Name);
        Assert.Equal(AudioMixer.MusicChannelName, mixer.Music.Name);
        Assert.Equal(AudioMixer.SfxChannelName, mixer.Sfx.Name);
        Assert.Equal(AudioMixer.AmbientChannelName, mixer.Ambient.Name);
    }

    [Fact]
    public void DefaultChannels_HaveVolumeOne()
    {
        AudioMixer mixer = new();

        Assert.Equal(1f, mixer.Master.Volume);
        Assert.Equal(1f, mixer.Music.Volume);
        Assert.Equal(1f, mixer.Sfx.Volume);
        Assert.Equal(1f, mixer.Ambient.Volume);
    }

    [Fact]
    public void GetChannel_KnownName_ReturnsChannel()
    {
        AudioMixer mixer = new();

        AudioMixerChannel? channel = mixer.GetChannel(AudioMixer.MusicChannelName);

        Assert.NotNull(channel);
        Assert.Equal(AudioMixer.MusicChannelName, channel!.Name);
    }

    [Fact]
    public void GetChannel_UnknownName_ReturnsNull()
    {
        AudioMixer mixer = new();

        AudioMixerChannel? channel = mixer.GetChannel("DoesNotExist");

        Assert.Null(channel);
    }

    [Fact]
    public void RegisterChannel_NewName_CreatesChannel()
    {
        AudioMixer mixer = new();

        AudioMixerChannel channel = mixer.RegisterChannel("Voice", 0.75f);

        Assert.Equal("Voice", channel.Name);
        Assert.Equal(0.75f, channel.Volume);
    }

    [Fact]
    public void RegisterChannel_DuplicateName_ReturnsExisting()
    {
        AudioMixer mixer = new();
        AudioMixerChannel first = mixer.RegisterChannel("Custom");
        first.Volume = 0.3f;

        AudioMixerChannel second = mixer.RegisterChannel("Custom", 0.9f);

        Assert.Same(first, second);
        Assert.Equal(0.3f, second.Volume);
    }

    [Fact]
    public void HasChannel_ReturnsTrueForRegisteredChannels()
    {
        AudioMixer mixer = new();

        Assert.True(mixer.HasChannel(AudioMixer.MasterChannelName));
        Assert.True(mixer.HasChannel(AudioMixer.MusicChannelName));
        Assert.True(mixer.HasChannel(AudioMixer.SfxChannelName));
        Assert.True(mixer.HasChannel(AudioMixer.AmbientChannelName));
    }

    [Fact]
    public void HasChannel_ReturnsFalseForUnknownChannel()
    {
        AudioMixer mixer = new();

        Assert.False(mixer.HasChannel("Unknown"));
    }

    [Fact]
    public void GetChannel_SameInstanceAs_RegisteredProperty()
    {
        AudioMixer mixer = new();

        AudioMixerChannel? retrieved = mixer.GetChannel(AudioMixer.SfxChannelName);

        Assert.Same(mixer.Sfx, retrieved);
    }
}
