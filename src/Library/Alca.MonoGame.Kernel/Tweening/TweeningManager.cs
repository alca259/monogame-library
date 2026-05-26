using System.Linq.Expressions;
using MonoGame.Extended;
using MonoGame.Extended.Tweening;

namespace Alca.MonoGame.Kernel.Tweening;

/// <summary>Wrapper around MonoGame.Extended Tweener providing lifecycle-integrated tween management.</summary>
public sealed class TweeningManager
{
    private readonly Tweener _tweener = new();

    /// <summary>Creates a tween that animates a float property from its current value to toValue over duration seconds.</summary>
    /// <param name="target">The object whose property will be animated.</param>
    /// <param name="member">Expression selecting the float property to animate.</param>
    /// <param name="toValue">Target float value at the end of the tween.</param>
    /// <param name="duration">Duration of the tween in seconds.</param>
    /// <param name="easing">Easing function from EasingCatalog.</param>
    /// <param name="delay">Seconds to wait before the tween starts.</param>
    /// <returns>The created Tween, supporting fluent chaining (.Easing, .RepeatForever, .AutoReverse).</returns>
    public Tween TweenTo<T>(T target, Expression<Func<T, float>> member, float toValue, float duration, Func<float, float> easing, float delay = 0f)
        where T : class
    {
        return _tweener.TweenTo(target, member, toValue, duration, delay).Easing(easing);
    }

    /// <summary>Advances all active tweens by the elapsed game time.</summary>
    public void Update(GameTime gameTime)
    {
        _tweener.Update(gameTime.GetElapsedSeconds());
    }

    /// <summary>Cancels and removes all active tweens.</summary>
    public void CancelAll()
    {
        _tweener.CancelAll();
    }

    /// <summary>Cancels a specific tween.</summary>
    public void Cancel(Tween tween)
    {
        tween.Cancel();
    }
}
