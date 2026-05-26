using Alca.MonoGame.Kernel.Tweening;

namespace Alca.MonoGame.Kernel.UnitTests.Tweening;

public sealed class EasingCatalogTests
{
    [Fact]
    public void Linear_AtZero_ReturnsZero() =>
        Assert.Equal(0f, EasingCatalog.Linear(0f), 0.001f);

    [Fact]
    public void Linear_AtOne_ReturnsOne() =>
        Assert.Equal(1f, EasingCatalog.Linear(1f), 0.001f);

    [Fact]
    public void Linear_AtHalf_ReturnsHalf() =>
        Assert.Equal(0.5f, EasingCatalog.Linear(0.5f), 0.001f);

    [Fact]
    public void EaseIn_AtZero_ReturnsZero() =>
        Assert.Equal(0f, EasingCatalog.EaseIn(0f), 0.001f);

    [Fact]
    public void EaseIn_AtOne_ReturnsOne() =>
        Assert.Equal(1f, EasingCatalog.EaseIn(1f), 0.001f);

    [Fact]
    public void EaseOut_AtZero_ReturnsZero() =>
        Assert.Equal(0f, EasingCatalog.EaseOut(0f), 0.001f);

    [Fact]
    public void EaseOut_AtOne_ReturnsOne() =>
        Assert.Equal(1f, EasingCatalog.EaseOut(1f), 0.001f);

    [Fact]
    public void EaseInOut_AtZero_ReturnsZero() =>
        Assert.Equal(0f, EasingCatalog.EaseInOut(0f), 0.001f);

    [Fact]
    public void EaseInOut_AtOne_ReturnsOne() =>
        Assert.Equal(1f, EasingCatalog.EaseInOut(1f), 0.001f);

    [Fact]
    public void BounceOut_AtOne_ReturnsOne() =>
        Assert.Equal(1f, EasingCatalog.BounceOut(1f), 0.001f);

    [Fact]
    public void CubicIn_AtZero_ReturnsZero() =>
        Assert.Equal(0f, EasingCatalog.CubicIn(0f), 0.001f);

    [Fact]
    public void CubicIn_AtOne_ReturnsOne() =>
        Assert.Equal(1f, EasingCatalog.CubicIn(1f), 0.001f);

    [Fact]
    public void BackIn_AtZero_ReturnsZero() =>
        Assert.Equal(0f, EasingCatalog.BackIn(0f), 0.001f);

    [Fact]
    public void BackIn_AtOne_ReturnsOne() =>
        Assert.Equal(1f, EasingCatalog.BackIn(1f), 0.001f);

    [Fact]
    public void AllDelegates_AreNotNull()
    {
        Assert.NotNull(EasingCatalog.Linear);
        Assert.NotNull(EasingCatalog.EaseIn);
        Assert.NotNull(EasingCatalog.EaseOut);
        Assert.NotNull(EasingCatalog.EaseInOut);
        Assert.NotNull(EasingCatalog.QuadIn);
        Assert.NotNull(EasingCatalog.QuadOut);
        Assert.NotNull(EasingCatalog.QuadInOut);
        Assert.NotNull(EasingCatalog.CubicIn);
        Assert.NotNull(EasingCatalog.CubicOut);
        Assert.NotNull(EasingCatalog.CubicInOut);
        Assert.NotNull(EasingCatalog.BounceIn);
        Assert.NotNull(EasingCatalog.BounceOut);
        Assert.NotNull(EasingCatalog.BounceInOut);
        Assert.NotNull(EasingCatalog.ElasticIn);
        Assert.NotNull(EasingCatalog.ElasticOut);
        Assert.NotNull(EasingCatalog.ElasticInOut);
        Assert.NotNull(EasingCatalog.BackIn);
        Assert.NotNull(EasingCatalog.BackOut);
        Assert.NotNull(EasingCatalog.BackInOut);
    }
}
