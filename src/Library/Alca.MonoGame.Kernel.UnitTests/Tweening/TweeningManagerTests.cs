using Alca.MonoGame.Kernel.Tweening;
using MonoGame.Extended.Tweening;

namespace Alca.MonoGame.Kernel.UnitTests.Tweening;

public sealed class TweeningManagerTests
{
    private sealed class TestTarget
    {
        public float Value { get; set; }
    }

    [Fact]
    public void TweenTo_ReturnsNonNullTween()
    {
        TweeningManager manager = new();
        TestTarget target = new() { Value = 0f };

        Tween tween = manager.TweenTo(target, t => t.Value, 100f, 1f, EasingCatalog.Linear);

        Assert.NotNull(tween);
    }

    [Fact]
    public void Update_WithNoTweens_DoesNotThrow()
    {
        TweeningManager manager = new();
        GameTime gameTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

        Exception? ex = Record.Exception(() => manager.Update(gameTime));
        Assert.Null(ex);
    }

    [Fact]
    public void CancelAll_WithNoTweens_DoesNotThrow()
    {
        TweeningManager manager = new();

        Exception? ex = Record.Exception(() => manager.CancelAll());
        Assert.Null(ex);
    }

    [Fact]
    public void CancelAll_WithActiveTweens_DoesNotThrow()
    {
        TweeningManager manager = new();
        TestTarget target = new() { Value = 0f };
        manager.TweenTo(target, t => t.Value, 100f, 2f, EasingCatalog.Linear);

        Exception? ex = Record.Exception(() => manager.CancelAll());
        Assert.Null(ex);
    }

    [Fact]
    public void Cancel_OnTween_DoesNotThrow()
    {
        TweeningManager manager = new();
        TestTarget target = new() { Value = 0f };
        Tween tween = manager.TweenTo(target, t => t.Value, 100f, 2f, EasingCatalog.Linear);

        Exception? ex = Record.Exception(() => manager.Cancel(tween));
        Assert.Null(ex);
    }

    [Fact]
    public void TweenTo_LinearEasing_UpdatesValueProportionally()
    {
        TweeningManager manager = new();
        TestTarget target = new() { Value = 0f };
        manager.TweenTo(target, t => t.Value, 100f, 1f, EasingCatalog.Linear);

        manager.Update(new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5)));

        Assert.Equal(50f, target.Value, 1f);
    }

    [Fact]
    public void TweenTo_AfterFullDuration_ReachesTargetValue()
    {
        TweeningManager manager = new();
        TestTarget target = new() { Value = 0f };
        manager.TweenTo(target, t => t.Value, 100f, 1f, EasingCatalog.Linear);

        manager.Update(new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0)));

        Assert.Equal(100f, target.Value, 1f);
    }
}
