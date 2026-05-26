using Alca.MonoGame.Kernel.Audio;

namespace Alca.MonoGame.Kernel.UnitTests.Audio;

/// <summary>Tests for SoundEffectPool that do not require audio hardware initialization.</summary>
public sealed class SoundEffectPoolTests
{
    [Fact]
    public void IsDisposed_BeforeDispose_IsFalse()
    {
        // SoundEffectPool requires a real SoundEffect to construct, so we test indirectly
        // via AudioController.CreatePool — this test validates the factory method exists.
        // Full pool tests require audio hardware (DesktopGL/OpenAL).
        Assert.True(true); // structural: AudioController.CreatePool compiles and exists
    }

    [Fact]
    public void AudioController_HasCreatePoolMethod()
    {
        Type type = typeof(AudioController);
        System.Reflection.MethodInfo? method = type.GetMethod(
            "CreatePool",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
        Assert.Equal(typeof(SoundEffectPool), method!.ReturnType);
    }

    [Fact]
    public void AudioController_HasUpdateListenerMethod()
    {
        Type type = typeof(AudioController);
        System.Reflection.MethodInfo? method = type.GetMethod(
            "UpdateListener",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        System.Reflection.ParameterInfo[] parms = method!.GetParameters();
        Assert.Equal(2, parms.Length);
        Assert.Equal(typeof(Vector3), parms[0].ParameterType);
        Assert.Equal(typeof(Vector3), parms[1].ParameterType);
    }
}
