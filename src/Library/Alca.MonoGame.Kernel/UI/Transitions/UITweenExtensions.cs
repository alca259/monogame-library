using Alca.MonoGame.Kernel.Tweening;
using MonoGame.Extended.Tweening;

namespace Alca.MonoGame.Kernel.UI.Transitions;

/// <summary>
/// Extension methods for animating common UI transitions (fade, slide) on any <see cref="UIElement"/>.
/// All methods accept an explicit <see cref="TweeningManager"/> for testability; omit it to use <see cref="Core.Tweening"/>.
/// </summary>
public static class UITweenExtensions
{
    #region Fade
    /// <summary>Tweens <paramref name="element"/> opacity from 0 to 1 over <paramref name="duration"/> seconds.</summary>
    public static Tween FadeIn(this UIElement element, float duration,
        Func<float, float>? easing = null, TweeningManager? tweening = null)
    {
        element.Opacity = 0f;
        var mgr = tweening ?? Core.Tweening;
        return mgr.TweenTo(element, e => e.Opacity, 1f, duration, easing ?? EasingCatalog.Linear);
    }

    /// <summary>Tweens <paramref name="element"/> opacity from its current value to 0 over <paramref name="duration"/> seconds.</summary>
    public static Tween FadeOut(this UIElement element, float duration,
        Func<float, float>? easing = null, TweeningManager? tweening = null)
    {
        var mgr = tweening ?? Core.Tweening;
        return mgr.TweenTo(element, e => e.Opacity, 0f, duration, easing ?? EasingCatalog.Linear);
    }
    #endregion

    #region Slide
    /// <summary>
    /// Offsets <paramref name="element"/>'s bounds by <paramref name="fromOffset"/> then tweens back to the original position.
    /// </summary>
    public static Tween SlideIn(this UIElement element, Vector2 fromOffset, float duration,
        Func<float, float>? easing = null, TweeningManager? tweening = null)
    {
        int toX = element.Bounds.X;
        int toY = element.Bounds.Y;

        element.Bounds = new Rectangle(toX + (int)fromOffset.X, toY + (int)fromOffset.Y,
            element.Bounds.Width, element.Bounds.Height);

        var proxy = new SlideProxy(element, toX, toY);
        var mgr = tweening ?? Core.Tweening;

        mgr.TweenTo(proxy, p => p.OffsetX, 0f, duration, easing ?? EasingCatalog.Linear);
        return mgr.TweenTo(proxy, p => p.OffsetY, 0f, duration, easing ?? EasingCatalog.Linear);
    }

    /// <summary>
    /// Tweens <paramref name="element"/>'s bounds by <paramref name="toOffset"/> away from its current position.
    /// </summary>
    public static Tween SlideOut(this UIElement element, Vector2 toOffset, float duration,
        Func<float, float>? easing = null, TweeningManager? tweening = null)
    {
        int fromX = element.Bounds.X;
        int fromY = element.Bounds.Y;
        int destX = fromX + (int)toOffset.X;
        int destY = fromY + (int)toOffset.Y;

        var proxy = new SlideProxy(element, fromX, fromY);
        var mgr = tweening ?? Core.Tweening;

        mgr.TweenTo(proxy, p => p.OffsetX, (float)(destX - fromX), duration, easing ?? EasingCatalog.Linear);
        return mgr.TweenTo(proxy, p => p.OffsetY, (float)(destY - fromY), duration, easing ?? EasingCatalog.Linear);
    }
    #endregion

    #region Internal proxy
    private sealed class SlideProxy
    {
        private readonly UIElement _element;
        private readonly int _baseX;
        private readonly int _baseY;
        private float _offsetX;
        private float _offsetY;

        internal SlideProxy(UIElement element, int baseX, int baseY)
        {
            _element = element;
            _baseX = baseX;
            _baseY = baseY;
        }

        public float OffsetX
        {
            get => _offsetX;
            set
            {
                _offsetX = value;
                var b = _element.Bounds;
                _element.Bounds = new Rectangle(_baseX + (int)_offsetX, _baseY + (int)_offsetY, b.Width, b.Height);
            }
        }

        public float OffsetY
        {
            get => _offsetY;
            set
            {
                _offsetY = value;
                var b = _element.Bounds;
                _element.Bounds = new Rectangle(_baseX + (int)_offsetX, _baseY + (int)_offsetY, b.Width, b.Height);
            }
        }
    }
    #endregion
}
