namespace Alca.MonoGame.Kernel.UI.Transitions;

/// <summary>Predefined transition types that can be applied to UI elements.</summary>
public enum UITransitionType
{
    /// <summary>Fade element opacity from 0 to 1.</summary>
    FadeIn,
    /// <summary>Fade element opacity from current value to 0.</summary>
    FadeOut,
    /// <summary>Slide element into position from the left edge.</summary>
    SlideInFromLeft,
    /// <summary>Slide element into position from the right edge.</summary>
    SlideInFromRight,
    /// <summary>Slide element into position from the top edge.</summary>
    SlideInFromTop,
    /// <summary>Slide element into position from the bottom edge.</summary>
    SlideInFromBottom,
    /// <summary>Slide element out to the left edge.</summary>
    SlideOutToLeft,
    /// <summary>Slide element out to the right edge.</summary>
    SlideOutToRight,
    /// <summary>Slide element out to the top edge.</summary>
    SlideOutToTop,
    /// <summary>Slide element out to the bottom edge.</summary>
    SlideOutToBottom,
}
