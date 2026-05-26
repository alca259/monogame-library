using Alca.MonoGame.Kernel.Input;
using Microsoft.Xna.Framework.Input;

namespace MonoGame.Editor.Core.Input;

/// <summary>Editor-side immutable representation of a single input binding (device + code).</summary>
public readonly record struct InputBindingEntry(DeviceType DeviceType, int Code)
{
    /// <summary>Returns a human-readable display string for this binding.</summary>
    public string ToDisplayString() => DeviceType switch
    {
        DeviceType.Keyboard => ((Keys)Code).ToString(),
        DeviceType.Mouse    => ((Alca.MonoGame.Kernel.Input.MouseButton)Code).ToString(),
        DeviceType.Gamepad  => $"{(Buttons)Code} (Gamepad)",
        _                   => Code.ToString()
    };
}
