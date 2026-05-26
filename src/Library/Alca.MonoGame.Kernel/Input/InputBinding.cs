namespace Alca.MonoGame.Kernel.Input;

/// <summary>Represents a serializable mapping from a device input source to a logical action.</summary>
public readonly struct InputBinding
{
    /// <summary>Gets the type of device this binding targets.</summary>
    public DeviceType DeviceType { get; init; }

    /// <summary>Gets the device-specific code for this binding, castable to <see cref="Keys"/>, <see cref="Buttons"/>, or <see cref="MouseButton"/>.</summary>
    public int Code { get; init; }

    /// <summary>Returns a human-readable display string for this binding (e.g., "Space", "A (Gamepad)", "Left").</summary>
    public string ToDisplayString() => DeviceType switch
    {
        DeviceType.Keyboard => ((Keys)Code).ToString(),
        DeviceType.Mouse    => ((MouseButton)Code).ToString(),
        DeviceType.Gamepad  => $"{(Buttons)Code} (Gamepad)",
        _                   => Code.ToString()
    };
}
