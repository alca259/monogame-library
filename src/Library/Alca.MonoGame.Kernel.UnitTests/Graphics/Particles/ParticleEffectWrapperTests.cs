using Alca.MonoGame.Kernel.Graphics.Particles;
using MonoGame.Extended.Particles;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Particles;

public sealed class ParticleEffectWrapperTests
{
    [Fact]
    public void Effect_BeforeLoad_IsNull()
    {
        ParticleEffectWrapper wrapper = new();
        Assert.Null(wrapper.Effect);
    }

    [Fact]
    public void Update_BeforeLoad_DoesNotThrow()
    {
        ParticleEffectWrapper wrapper = new();
        Exception? ex = Record.Exception(() => wrapper.Update(new GameTime(), Vector2.Zero));
        Assert.Null(ex);
    }

    [Fact]
    public void Trigger_BeforeLoad_DoesNotThrow()
    {
        ParticleEffectWrapper wrapper = new();
        Exception? ex = Record.Exception(() => wrapper.Trigger(Vector2.Zero));
        Assert.Null(ex);
    }

    [Fact]
    public void Effect_AfterInternalConstructor_ReturnsInjectedEffect()
    {
        ParticleEffect effect = new("test");
        ParticleEffectWrapper wrapper = new(effect);
        Assert.Same(effect, wrapper.Effect);
    }

    [Fact]
    public void Update_WithEffect_SetsPositionBeforeUpdate()
    {
        ParticleEffect effect = new("test");
        ParticleEffectWrapper wrapper = new(effect);
        Vector2 expectedPosition = new(100f, 200f);

        wrapper.Update(new GameTime(), expectedPosition);

        Assert.Equal(expectedPosition, effect.Position);
    }

    [Fact]
    public void Trigger_WithEffect_DoesNotThrow()
    {
        ParticleEffect effect = new("test") { AutoTrigger = false };
        ParticleEffectWrapper wrapper = new(effect);
        Exception? ex = Record.Exception(() => wrapper.Trigger(new Vector2(50f, 75f)));
        Assert.Null(ex);
    }
}
