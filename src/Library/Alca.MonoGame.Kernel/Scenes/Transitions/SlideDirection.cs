namespace Alca.MonoGame.Kernel.Scenes.Transitions;

/// <summary>Specifies the direction from which a <see cref="SlideTransition"/> curtain enters the screen.</summary>
public enum SlideDirection
{
    /// <summary>The curtain enters from the left edge moving rightward.</summary>
    Left,

    /// <summary>The curtain enters from the right edge moving leftward.</summary>
    Right,

    /// <summary>The curtain enters from the top edge moving downward.</summary>
    Up,

    /// <summary>The curtain enters from the bottom edge moving upward.</summary>
    Down
}
