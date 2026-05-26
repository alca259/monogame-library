namespace Alca.MonoGame.Kernel.UI.Layout;

/// <summary>Defines the sizing behavior of a single column or row in a <see cref="GridLayout"/>.</summary>
public readonly struct GridTrack
{
    /// <summary>How the track size is determined.</summary>
    public GridSizeMode SizeMode { get; init; }

    /// <summary>
    /// Pixel size for <see cref="GridSizeMode.Fixed"/>; ignored for <see cref="GridSizeMode.Auto"/>;
    /// proportional weight for <see cref="GridSizeMode.Star"/>.
    /// </summary>
    public float Value { get; init; }

    /// <summary>Creates a new <see cref="GridTrack"/> with the given mode and value.</summary>
    /// <param name="mode">Sizing mode.</param>
    /// <param name="value">Pixel size (Fixed), or star weight (Star). Default 1.</param>
    public GridTrack(GridSizeMode mode, float value = 1f)
    {
        SizeMode = mode;
        Value = value;
    }

    /// <summary>Shorthand for a fixed-size track.</summary>
    public static GridTrack Fixed(float pixels) => new(GridSizeMode.Fixed, pixels);

    /// <summary>Shorthand for an auto-size track.</summary>
    public static GridTrack Auto() => new(GridSizeMode.Auto, 0f);

    /// <summary>Shorthand for a star-weight track.</summary>
    public static GridTrack Star(float weight = 1f) => new(GridSizeMode.Star, weight);
}
