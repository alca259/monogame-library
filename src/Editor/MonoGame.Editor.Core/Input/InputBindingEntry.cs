using Alca.MonoGame.Kernel.Input;
using Microsoft.Xna.Framework.Input;

namespace MonoGame.Editor.Core.Input;

/// <summary>Representación inmutable en el editor de un único enlace de entrada (dispositivo + código).</summary>
public readonly record struct InputBindingEntry(DeviceType DeviceType, int Code)
{
    /// <summary>Devuelve una cadena de visualización legible para este enlace.</summary>
    public string ToDisplayString() => DeviceType switch
    {
        DeviceType.Keyboard => ((Keys)Code).ToString(),
        DeviceType.Mouse => ((Alca.MonoGame.Kernel.Input.MouseButton)Code).ToString(),
        DeviceType.Gamepad => $"{(Buttons)Code} (Gamepad)",
        _ => Code.ToString()
    };
}
