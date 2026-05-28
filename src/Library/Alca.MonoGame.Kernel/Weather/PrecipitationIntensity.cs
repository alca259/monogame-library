namespace Alca.MonoGame.Kernel.Weather;

/// <summary>Describes the intensity of precipitation particle emission for a weather state.</summary>
public enum PrecipitationIntensity
{
    /// <summary>No precipitation particles.</summary>
    None,

    /// <summary>Sparse precipitation — a few particles visible.</summary>
    Low,

    /// <summary>Moderate precipitation.</summary>
    Medium,

    /// <summary>Heavy precipitation — dense particle coverage.</summary>
    High,

    /// <summary>Extreme precipitation — maximum particle density (e.g. dense fog or blizzard).</summary>
    VeryHigh
}
