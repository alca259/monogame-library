namespace Alca.MonoGame.Kernel.UI.Layout;

/// <summary>Determines how a grid track (column or row) is sized.</summary>
public enum GridSizeMode
{
    /// <summary>Track has a fixed pixel size determined by <see cref="GridTrack.Value"/>.</summary>
    Fixed,

    /// <summary>Track sizes itself to its largest child's desired size.</summary>
    Auto,

    /// <summary>Track takes a proportional share of the remaining space; <see cref="GridTrack.Value"/> is the weight.</summary>
    Star
}
