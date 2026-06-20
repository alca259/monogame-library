using System.Drawing;

namespace MonoGame.Editor.Winforms.Theme;

/// <summary>Tokens de color del tema oscuro del editor (port de Colors.xaml).</summary>
internal static class EditorColors
{
    // ── Fondos ──────────────────────────────────────────────────────────────
    public static readonly Color ShellBackground    = Rgb(0x1A, 0x1A, 0x1B);
    public static readonly Color BgWindow           = ShellBackground;
    public static readonly Color PanelBackground    = Rgb(0x1E, 0x1E, 0x20);
    public static readonly Color BgPanel            = PanelBackground;
    public static readonly Color PanelBackgroundAlt = Rgb(0x25, 0x25, 0x28);
    public static readonly Color BgChrome           = PanelBackgroundAlt;
    public static readonly Color ViewportBackground = Rgb(0x14, 0x14, 0x16);
    public static readonly Color InputBackground    = Rgb(0x2A, 0x2A, 0x2E);
    public static readonly Color InputBackgroundHover = Rgb(0x32, 0x32, 0x37);

    // ── Selección y bordes ──────────────────────────────────────────────────
    public static readonly Color RowSelected   = Rgb(0x2D, 0x4A, 0x6B);
    public static readonly Color Border        = Rgb(0x34, 0x34, 0x3A);
    public static readonly Color BorderColor   = Border;
    public static readonly Color BorderFocus   = Rgb(0x4A, 0x9E, 0xFF);

    // ── Acento ─────────────────────────────────────────────────────────────
    public static readonly Color AccentBlue    = Rgb(0x4A, 0x9E, 0xFF);
    public static readonly Color AccentBlueDim = Rgb(0x2F, 0x6F, 0xB0);

    // ── Ejes ────────────────────────────────────────────────────────────────
    public static readonly Color AxisRed   = Rgb(0xE5, 0x48, 0x4D);
    public static readonly Color AxisGreen = Rgb(0x46, 0xC6, 0x6A);
    public static readonly Color AxisBlue  = AccentBlue;

    // ── Transporte / estado ─────────────────────────────────────────────────
    public static readonly Color PlayGreen    = AxisGreen;
    public static readonly Color FpsGreen     = AxisGreen;
    public static readonly Color StopRed      = AxisRed;
    public static readonly Color BuildErrorBg = Rgb(0xC7, 0x3E, 0x3E);
    public static readonly Color BuildErrorFg = Color.White;
    public static readonly Color BuildOkFg    = Rgb(0x9A, 0x9A, 0xA2);

    // ── Texto ───────────────────────────────────────────────────────────────
    public static readonly Color TextPrimary   = Rgb(0xE6, 0xE6, 0xE8);
    public static readonly Color TextSecondary = Rgb(0x9A, 0x9A, 0xA2);
    public static readonly Color TextMuted     = Rgb(0x6A, 0x6A, 0x72);
    public static readonly Color TextDim       = TextMuted;

    // ── Botones ─────────────────────────────────────────────────────────────
    public static readonly Color BtnSuccessBg = Rgb(0x38, 0x8E, 0x3C);
    public static readonly Color MaterialIcon = AxisRed;

    private static Color Rgb(int r, int g, int b) => Color.FromArgb(255, r, g, b);
}
