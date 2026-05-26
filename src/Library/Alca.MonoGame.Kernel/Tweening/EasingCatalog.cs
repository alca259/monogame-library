using MonoGame.Extended.Tweening;

namespace Alca.MonoGame.Kernel.Tweening;

/// <summary>Re-exports MonoGame.Extended easing functions as Func&lt;float, float&gt; delegates with ergonomic names.</summary>
public static class EasingCatalog
{
    /// <summary>Constant-rate linear interpolation.</summary>
    public static readonly Func<float, float> Linear = EasingFunctions.Linear;

    /// <summary>Quadratic ease-in (slow start).</summary>
    public static readonly Func<float, float> EaseIn = EasingFunctions.QuadraticIn;

    /// <summary>Quadratic ease-out (slow end).</summary>
    public static readonly Func<float, float> EaseOut = EasingFunctions.QuadraticOut;

    /// <summary>Quadratic ease-in-out (slow start and end).</summary>
    public static readonly Func<float, float> EaseInOut = EasingFunctions.QuadraticInOut;

    /// <summary>Quadratic ease-in.</summary>
    public static readonly Func<float, float> QuadIn = EasingFunctions.QuadraticIn;

    /// <summary>Quadratic ease-out.</summary>
    public static readonly Func<float, float> QuadOut = EasingFunctions.QuadraticOut;

    /// <summary>Quadratic ease-in-out.</summary>
    public static readonly Func<float, float> QuadInOut = EasingFunctions.QuadraticInOut;

    /// <summary>Cubic ease-in.</summary>
    public static readonly Func<float, float> CubicIn = EasingFunctions.CubicIn;

    /// <summary>Cubic ease-out.</summary>
    public static readonly Func<float, float> CubicOut = EasingFunctions.CubicOut;

    /// <summary>Cubic ease-in-out.</summary>
    public static readonly Func<float, float> CubicInOut = EasingFunctions.CubicInOut;

    /// <summary>Bounce ease-in.</summary>
    public static readonly Func<float, float> BounceIn = EasingFunctions.BounceIn;

    /// <summary>Bounce ease-out.</summary>
    public static readonly Func<float, float> BounceOut = EasingFunctions.BounceOut;

    /// <summary>Bounce ease-in-out.</summary>
    public static readonly Func<float, float> BounceInOut = EasingFunctions.BounceInOut;

    /// <summary>Elastic ease-in (overshoot spring).</summary>
    public static readonly Func<float, float> ElasticIn = EasingFunctions.ElasticIn;

    /// <summary>Elastic ease-out.</summary>
    public static readonly Func<float, float> ElasticOut = EasingFunctions.ElasticOut;

    /// <summary>Elastic ease-in-out.</summary>
    public static readonly Func<float, float> ElasticInOut = EasingFunctions.ElasticInOut;

    /// <summary>Back ease-in (slight overshoot at start).</summary>
    public static readonly Func<float, float> BackIn = EasingFunctions.BackIn;

    /// <summary>Back ease-out (slight overshoot at end).</summary>
    public static readonly Func<float, float> BackOut = EasingFunctions.BackOut;

    /// <summary>Back ease-in-out.</summary>
    public static readonly Func<float, float> BackInOut = EasingFunctions.BackInOut;
}
