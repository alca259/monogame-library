using Alca.MonoGame.Kernel.Tweening;
using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Transitions;
using MonoGame.Extended.Tweening;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Transitions;

public sealed class UITweenExtensionsTests
{
    private static TweeningManager NewManager() => new();

    private static GameTime Delta(double seconds)
        => new(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));

    // ── Concrete minimal UIElement for testing ────────────────────────────────

    private sealed class TestElement : UIElement { }

    // ── FadeIn ────────────────────────────────────────────────────────────────

    [Fact]
    public void FadeIn_SetsOpacityTo1AfterDuration()
    {
        var mgr = NewManager();
        var element = new TestElement { Opacity = 0.5f };

        element.FadeIn(1f, tweening: mgr);
        mgr.Update(Delta(1.0));

        Assert.Equal(1f, element.Opacity, 3);
    }

    [Fact]
    public void FadeIn_SetsOpacityToZeroImmediately()
    {
        var mgr = NewManager();
        var element = new TestElement { Opacity = 0.8f };

        element.FadeIn(1f, tweening: mgr);

        Assert.Equal(0f, element.Opacity, 3);
    }

    // ── FadeOut ───────────────────────────────────────────────────────────────

    [Fact]
    public void FadeOut_SetsOpacityTo0AfterDuration()
    {
        var mgr = NewManager();
        var element = new TestElement { Opacity = 1f };

        element.FadeOut(1f, tweening: mgr);
        mgr.Update(Delta(1.0));

        Assert.Equal(0f, element.Opacity, 3);
    }

    // ── SlideIn ───────────────────────────────────────────────────────────────

    [Fact]
    public void SlideIn_BoundsReturnToOriginalPositionAfterDuration()
    {
        var mgr = NewManager();
        var element = new TestElement();
        element.Arrange(new Rectangle(100, 50, 200, 100));

        element.SlideIn(new Vector2(-200f, 0f), 1f, tweening: mgr);
        mgr.Update(Delta(1.0));

        Assert.Equal(100, element.Bounds.X);
        Assert.Equal(50, element.Bounds.Y);
    }

    // ── UITransitionManager ───────────────────────────────────────────────────

    [Fact]
    public void UITransitionManager_Play_FadeIn_DelegatesToExtension()
    {
        var mgr = NewManager();
        var transitionMgr = new UITransitionManager(mgr);
        var element = new TestElement { Opacity = 0.5f };

        Tween tween = transitionMgr.Play(element, UITransitionType.FadeIn, 1f);
        mgr.Update(Delta(1.0));

        Assert.NotNull(tween);
        Assert.Equal(1f, element.Opacity, 3);
    }
}
