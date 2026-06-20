namespace Alca.MonoGame.Kernel.UI.Controls.Utilities;

/// <summary>Pure math helpers for color format conversions used by the color picker controls.</summary>
public static class ColorPickerUtils
{
    #region HSV ↔ RGB

    /// <summary>Converts HSV to a MonoGame <see cref="Color"/>.</summary>
    /// <param name="h">Hue in degrees [0, 360).</param>
    /// <param name="s">Saturation [0, 1].</param>
    /// <param name="v">Value/brightness [0, 1].</param>
    public static Color HsvToRgb(float h, float s, float v)
    {
        if (s == 0f)
        {
            byte gray = (byte)(v * 255f);
            return new Color(gray, gray, gray);
        }

        h /= 60f;
        int sector = (int)h;
        float f = h - sector;
        float p = v * (1f - s);
        float q = v * (1f - s * f);
        float t = v * (1f - s * (1f - f));

        float r, g, b;
        switch (sector % 6)
        {
            case 0:  r = v; g = t; b = p; break;
            case 1:  r = q; g = v; b = p; break;
            case 2:  r = p; g = v; b = t; break;
            case 3:  r = p; g = q; b = v; break;
            case 4:  r = t; g = p; b = v; break;
            default: r = v; g = p; b = q; break;
        }

        return new Color((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f));
    }

    /// <summary>Decomposes a <see cref="Color"/> into HSV components.</summary>
    /// <param name="color">Input color (alpha is ignored).</param>
    /// <param name="h">Hue in degrees [0, 360).</param>
    /// <param name="s">Saturation [0, 1].</param>
    /// <param name="v">Value/brightness [0, 1].</param>
    public static void RgbToHsv(Color color, out float h, out float s, out float v)
    {
        float r = color.R / 255f;
        float g = color.G / 255f;
        float b = color.B / 255f;

        float max = MathF.Max(r, MathF.Max(g, b));
        float min = MathF.Min(r, MathF.Min(g, b));
        float delta = max - min;

        v = max;
        s = (max == 0f) ? 0f : delta / max;

        if (delta == 0f)
        {
            h = 0f;
            return;
        }

        if (max == r)
            h = 60f * ((g - b) / delta % 6f);
        else if (max == g)
            h = 60f * ((b - r) / delta + 2f);
        else
            h = 60f * ((r - g) / delta + 4f);

        if (h < 0f) h += 360f;
    }

    #endregion

    #region Hex ↔ Color

    /// <summary>Parses a hex color string to a <see cref="Color"/>. Returns null if parsing fails.</summary>
    /// <param name="hex">Hex string with or without '#', e.g. "#FF8040" or "FF8040". Supports RGB (3-char) and RRGGBB (6-char).</param>
    public static Color? HexToColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;

        ReadOnlySpan<char> span = hex.AsSpan();
        if (span.Length > 0 && span[0] == '#')
            span = span[1..];

        if (span.Length == 3)
        {
            if (!TryParseHexByte(span[0], span[0], out byte r)) return null;
            if (!TryParseHexByte(span[1], span[1], out byte g)) return null;
            if (!TryParseHexByte(span[2], span[2], out byte b)) return null;
            return new Color(r, g, b);
        }

        if (span.Length == 6)
        {
            if (!TryParseHexByte(span[0], span[1], out byte r)) return null;
            if (!TryParseHexByte(span[2], span[3], out byte g)) return null;
            if (!TryParseHexByte(span[4], span[5], out byte b)) return null;
            return new Color(r, g, b);
        }

        if (span.Length == 8)
        {
            if (!TryParseHexByte(span[0], span[1], out byte r)) return null;
            if (!TryParseHexByte(span[2], span[3], out byte g)) return null;
            if (!TryParseHexByte(span[4], span[5], out byte b)) return null;
            if (!TryParseHexByte(span[6], span[7], out byte a)) return null;
            return new Color(r, g, b, a);
        }

        return null;
    }

    /// <summary>Converts a <see cref="Color"/> to a "#RRGGBB" hex string.</summary>
    public static string ColorToHex(Color color)
        => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>Converts a <see cref="Color"/> to a "#RRGGBBAA" hex string including alpha.</summary>
    public static string ColorToHexAlpha(Color color)
        => $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

    private static bool TryParseHexByte(char hi, char lo, out byte result)
    {
        int h = HexCharToInt(hi);
        int l = HexCharToInt(lo);
        if (h < 0 || l < 0) { result = 0; return false; }
        result = (byte)((h << 4) | l);
        return true;
    }

    private static int HexCharToInt(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1
    };

    #endregion
}
