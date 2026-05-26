using Alca.MonoGame.Kernel.Graphics.Particles;
using MonoGame.Extended.Particles;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Particles;

public sealed class ParticleBuilderTests
{
    [Fact]
    public void WithSprayProfile_ReturnsSameBuilder()
    {
        ParticleBuilder builder = new();
        Assert.Same(builder, builder.WithSprayProfile(0.5f, 100f));
    }

    [Fact]
    public void WithCircleProfile_ReturnsSameBuilder()
    {
        ParticleBuilder builder = new();
        Assert.Same(builder, builder.WithCircleProfile(50f));
    }

    [Fact]
    public void WithGravity_ReturnsSameBuilder()
    {
        ParticleBuilder builder = new();
        Assert.Same(builder, builder.WithGravity(9.8f));
    }

    [Fact]
    public void WithLifetime_ReturnsSameBuilder()
    {
        ParticleBuilder builder = new();
        Assert.Same(builder, builder.WithLifetime(1f, 3f));
    }

    [Fact]
    public void WithColorRange_ReturnsSameBuilder()
    {
        ParticleBuilder builder = new();
        Assert.Same(builder, builder.WithColorRange(Color.Yellow, Color.Red));
    }

    [Fact]
    public void WithCapacity_ReturnsSameBuilder()
    {
        ParticleBuilder builder = new();
        Assert.Same(builder, builder.WithCapacity(200));
    }

    [Fact]
    public void Build_Default_ReturnsNonNullEffect()
    {
        ParticleEffect effect = new ParticleBuilder().Build();
        Assert.NotNull(effect);
    }

    [Fact]
    public void Build_HasSingleEmitter()
    {
        ParticleEffect effect = new ParticleBuilder().Build();
        Assert.Single(effect.Emitters);
    }

    [Fact]
    public void Build_WithCapacity_EmitterHasCorrectCapacity()
    {
        const int capacity = 250;
        ParticleEffect effect = new ParticleBuilder().WithCapacity(capacity).Build();
        Assert.Equal(capacity, effect.Emitters[0].Capacity);
    }

    [Fact]
    public void Build_WithLifetime_EmitterHasAverageLifespan()
    {
        const float min = 1f;
        const float max = 3f;
        float expected = (min + max) * 0.5f;
        ParticleEffect effect = new ParticleBuilder().WithLifetime(min, max).Build();
        Assert.Equal(expected, effect.Emitters[0].LifeSpan, 0.001f);
    }

    [Fact]
    public void Build_WithGravity_EmitterHasGravityModifier()
    {
        ParticleEffect effect = new ParticleBuilder().WithGravity(50f).Build();
        Assert.NotEmpty(effect.Emitters[0].Modifiers);
    }

    [Fact]
    public void Build_WithColorRange_EmitterHasColorModifier()
    {
        ParticleEffect effect = new ParticleBuilder()
            .WithColorRange(Color.White, Color.Black)
            .Build();
        Assert.NotEmpty(effect.Emitters[0].Modifiers);
    }

    [Fact]
    public void Build_FluentChain_ReturnsNonNullEffect()
    {
        ParticleEffect effect = new ParticleBuilder()
            .WithCapacity(100)
            .WithSprayProfile(0.3f, 80f)
            .WithLifetime(0.5f, 1.5f)
            .WithGravity(30f)
            .WithColorRange(Color.Orange, Color.Transparent)
            .Build();

        Assert.NotNull(effect);
        Assert.Single(effect.Emitters);
    }
}
