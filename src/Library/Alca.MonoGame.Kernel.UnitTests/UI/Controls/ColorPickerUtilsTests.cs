using Alca.MonoGame.Kernel.UI.Controls;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class ColorPickerUtilsTests
{
    #region HsvToRgb

    [Fact]
    public void HsvToRgb_Red_H0_S1_V1()
    {
        Color c = ColorPickerUtils.HsvToRgb(0f, 1f, 1f);
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void HsvToRgb_Green_H120_S1_V1()
    {
        Color c = ColorPickerUtils.HsvToRgb(120f, 1f, 1f);
        Assert.Equal(0, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void HsvToRgb_Blue_H240_S1_V1()
    {
        Color c = ColorPickerUtils.HsvToRgb(240f, 1f, 1f);
        Assert.Equal(0, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(255, c.B);
    }

    [Fact]
    public void HsvToRgb_White_S0_V1()
    {
        Color c = ColorPickerUtils.HsvToRgb(0f, 0f, 1f);
        Assert.Equal(255, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(255, c.B);
    }

    [Fact]
    public void HsvToRgb_Black_S0_V0()
    {
        Color c = ColorPickerUtils.HsvToRgb(0f, 0f, 0f);
        Assert.Equal(0, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
    }

    #endregion

    #region RgbToHsv

    [Fact]
    public void RgbToHsv_Red_ReturnsH0_S1_V1()
    {
        ColorPickerUtils.RgbToHsv(Color.Red, out float h, out float s, out float v);
        Assert.Equal(0f, h, 1);
        Assert.Equal(1f, s, 2);
        Assert.Equal(1f, v, 2);
    }

    [Fact]
    public void RgbToHsv_Black_ReturnsZeroSaturationZeroValue()
    {
        ColorPickerUtils.RgbToHsv(Color.Black, out _, out float s, out float v);
        Assert.Equal(0f, s, 2);
        Assert.Equal(0f, v, 2);
    }

    [Fact]
    public void RgbToHsv_White_ReturnsFullValue_ZeroSaturation()
    {
        ColorPickerUtils.RgbToHsv(Color.White, out _, out float s, out float v);
        Assert.Equal(0f, s, 2);
        Assert.Equal(1f, v, 2);
    }

    [Fact]
    public void HsvToRgb_RoundTrip_Red()
    {
        Color original = new Color(200, 50, 30);
        ColorPickerUtils.RgbToHsv(original, out float h, out float s, out float v);
        Color roundTripped = ColorPickerUtils.HsvToRgb(h, s, v);
        Assert.InRange(Math.Abs(roundTripped.R - original.R), 0, 2);
        Assert.InRange(Math.Abs(roundTripped.G - original.G), 0, 2);
        Assert.InRange(Math.Abs(roundTripped.B - original.B), 0, 2);
    }

    #endregion

    #region HexToColor

    [Fact]
    public void HexToColor_WithHash_ParsesCorrectly()
    {
        Color? c = ColorPickerUtils.HexToColor("#FF8040");
        Assert.NotNull(c);
        Assert.Equal(255, c!.Value.R);
        Assert.Equal(128, c.Value.G);
        Assert.Equal(64, c.Value.B);
    }

    [Fact]
    public void HexToColor_WithoutHash_ParsesCorrectly()
    {
        Color? c = ColorPickerUtils.HexToColor("FF8040");
        Assert.NotNull(c);
        Assert.Equal(255, c!.Value.R);
    }

    [Fact]
    public void HexToColor_LowercaseHex_ParsesCorrectly()
    {
        Color? c = ColorPickerUtils.HexToColor("#ff8040");
        Assert.NotNull(c);
        Assert.Equal(255, c!.Value.R);
    }

    [Fact]
    public void HexToColor_ThreeChar_ParsesExpanded()
    {
        Color? c = ColorPickerUtils.HexToColor("#FFF");
        Assert.NotNull(c);
        Assert.Equal(255, c!.Value.R);
        Assert.Equal(255, c.Value.G);
        Assert.Equal(255, c.Value.B);
    }

    [Fact]
    public void HexToColor_EightChar_ParsesWithAlpha()
    {
        Color? c = ColorPickerUtils.HexToColor("#FF8040AA");
        Assert.NotNull(c);
        Assert.Equal(255, c!.Value.R);
        Assert.Equal(170, c.Value.A);
    }

    [Fact]
    public void HexToColor_InvalidString_ReturnsNull()
    {
        Assert.Null(ColorPickerUtils.HexToColor("ZZZZZZ"));
    }

    [Fact]
    public void HexToColor_EmptyString_ReturnsNull()
    {
        Assert.Null(ColorPickerUtils.HexToColor(string.Empty));
    }

    [Fact]
    public void HexToColor_WrongLength_ReturnsNull()
    {
        Assert.Null(ColorPickerUtils.HexToColor("#FF00"));
    }

    #endregion

    #region ColorToHex

    [Fact]
    public void ColorToHex_Red_ReturnsHashFF0000()
    {
        Assert.Equal("#FF0000", ColorPickerUtils.ColorToHex(Color.Red));
    }

    [Fact]
    public void ColorToHex_Black_ReturnsHash000000()
    {
        Assert.Equal("#000000", ColorPickerUtils.ColorToHex(Color.Black));
    }

    [Fact]
    public void ColorToHex_White_ReturnsHashFFFFFF()
    {
        Assert.Equal("#FFFFFF", ColorPickerUtils.ColorToHex(Color.White));
    }

    [Fact]
    public void ColorToHex_RoundTrip()
    {
        Color original = new Color(100, 150, 200);
        Color? parsed = ColorPickerUtils.HexToColor(ColorPickerUtils.ColorToHex(original));
        Assert.NotNull(parsed);
        Assert.Equal(original.R, parsed!.Value.R);
        Assert.Equal(original.G, parsed.Value.G);
        Assert.Equal(original.B, parsed.Value.B);
    }

    #endregion
}
