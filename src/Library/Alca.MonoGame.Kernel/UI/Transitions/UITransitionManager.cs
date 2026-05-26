using Alca.MonoGame.Kernel.Tweening;
using MonoGame.Extended.Tweening;

namespace Alca.MonoGame.Kernel.UI.Transitions;

/// <summary>
/// Maps <see cref="UITransitionType"/> values to the corresponding <see cref="UITweenExtensions"/> calls.
/// </summary>
public sealed class UITransitionManager
{
    private readonly TweeningManager? _tweening;

    /// <summary>Initializes a new instance that uses <see cref="Core.Tweening"/> by default.</summary>
    public UITransitionManager() { }

    /// <summary>Initializes a new instance with an explicit <paramref name="tweening"/> manager (useful in tests).</summary>
    public UITransitionManager(TweeningManager tweening)
    {
        _tweening = tweening;
    }

    /// <summary>Plays the specified <paramref name="transition"/> on <paramref name="element"/>.</summary>
    /// <returns>The created <see cref="Tween"/> for optional chaining.</returns>
    public Tween Play(UIElement element, UITransitionType transition, float duration,
        Func<float, float>? easing = null)
    {
        return transition switch
        {
            UITransitionType.FadeIn
                => element.FadeIn(duration, easing, _tweening),
            UITransitionType.FadeOut
                => element.FadeOut(duration, easing, _tweening),
            UITransitionType.SlideInFromLeft
                => element.SlideIn(new Vector2(-element.Bounds.Width, 0), duration, easing, _tweening),
            UITransitionType.SlideInFromRight
                => element.SlideIn(new Vector2(element.Bounds.Width, 0), duration, easing, _tweening),
            UITransitionType.SlideInFromTop
                => element.SlideIn(new Vector2(0, -element.Bounds.Height), duration, easing, _tweening),
            UITransitionType.SlideInFromBottom
                => element.SlideIn(new Vector2(0, element.Bounds.Height), duration, easing, _tweening),
            UITransitionType.SlideOutToLeft
                => element.SlideOut(new Vector2(-element.Bounds.Width, 0), duration, easing, _tweening),
            UITransitionType.SlideOutToRight
                => element.SlideOut(new Vector2(element.Bounds.Width, 0), duration, easing, _tweening),
            UITransitionType.SlideOutToTop
                => element.SlideOut(new Vector2(0, -element.Bounds.Height), duration, easing, _tweening),
            UITransitionType.SlideOutToBottom
                => element.SlideOut(new Vector2(0, element.Bounds.Height), duration, easing, _tweening),
            _ => throw new ArgumentOutOfRangeException(nameof(transition), transition, null),
        };
    }
}
