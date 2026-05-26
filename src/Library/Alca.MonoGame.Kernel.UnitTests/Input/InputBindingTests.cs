using Microsoft.Xna.Framework.Input;
using Alca.MonoGame.Kernel.Input;

namespace Alca.MonoGame.Kernel.UnitTests.Input;

public sealed class InputBindingTests
{
    [Fact]
    public void ToDisplayString_Keyboard_ReturnsKeyName()
    {
        var binding = new InputBinding { DeviceType = DeviceType.Keyboard, Code = (int)Keys.Space };
        Assert.Equal("Space", binding.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_Keyboard_LetterA_ReturnsA()
    {
        var binding = new InputBinding { DeviceType = DeviceType.Keyboard, Code = (int)Keys.A };
        Assert.Equal("A", binding.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_Mouse_ReturnsButtonName()
    {
        var binding = new InputBinding { DeviceType = DeviceType.Mouse, Code = (int)MouseButton.Left };
        Assert.Equal("Left", binding.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_Mouse_RightButton_ReturnsRight()
    {
        var binding = new InputBinding { DeviceType = DeviceType.Mouse, Code = (int)MouseButton.Right };
        Assert.Equal("Right", binding.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_Gamepad_ReturnsButtonWithSuffix()
    {
        var binding = new InputBinding { DeviceType = DeviceType.Gamepad, Code = (int)Buttons.A };
        Assert.Equal("A (Gamepad)", binding.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_Gamepad_Start_ContainsGamepadSuffix()
    {
        var binding = new InputBinding { DeviceType = DeviceType.Gamepad, Code = (int)Buttons.Start };
        Assert.EndsWith("(Gamepad)", binding.ToDisplayString());
    }

    [Fact]
    public void InputBinding_DefaultValues_AreZero()
    {
        var binding = new InputBinding();
        Assert.Equal(default, binding.DeviceType);
        Assert.Equal(0, binding.Code);
    }
}
