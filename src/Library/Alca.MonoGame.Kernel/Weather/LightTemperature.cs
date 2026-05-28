namespace Alca.MonoGame.Kernel.Weather;

/// <summary>Describes the color temperature of the ambient light for a weather state.</summary>
public enum LightTemperature
{
    /// <summary>Warm yellowish-orange tones (clear sun, heat wave).</summary>
    Warm,

    /// <summary>Neutral white light (overcast but not dark).</summary>
    Neutral,

    /// <summary>Cool blue-grey tones (rain, fog, snow, overcast).</summary>
    Cold
}
