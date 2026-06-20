using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Theme;

/// <summary>
/// Helpers de estilización: aplican la paleta de colores y fuentes del editor sobre
/// controles WinForms estándar (port de ControlStyles.xaml).
/// </summary>
internal static class EditorStyles
{
    // ── Dimensiones canónicas ────────────────────────────────────────────────
    public const int ToolbarHeight    = 42;
    public const int MenuHeight       = 28;
    public const int StatusHeight     = 24;
    public const int DockBarHeight    = 266;
    public const int HierarchyWidth   = 268;
    public const int InspectorWidth   = 362;
    public const int ToolButtonSize   = 30;
    public const int PillHeight       = 26;
    public const int PlayButtonWidth  = 36;
    public const int PlayButtonHeight = 28;

    // ── Botones ─────────────────────────────────────────────────────────────

    /// <summary>Aplica estilo de botón plano sin borde.</summary>
    public static void ApplyFlatButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = EditorColors.PanelBackgroundAlt;
        button.ForeColor = EditorColors.TextPrimary;
        button.Font      = EditorFonts.Primary;
        button.FlatAppearance.BorderSize     = 1;
        button.FlatAppearance.BorderColor    = EditorColors.Border;
        button.FlatAppearance.MouseOverBackColor  = EditorColors.InputBackgroundHover;
        button.FlatAppearance.MouseDownBackColor  = EditorColors.RowSelected;
    }

    /// <summary>Aplica estilo de botón de toolbar (30×30 cuadrado, sin borde).</summary>
    public static void ApplyToolButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = EditorColors.BgChrome;
        button.ForeColor = EditorColors.TextPrimary;
        button.Font      = EditorFonts.Primary;
        button.Size      = new Size(ToolButtonSize, ToolButtonSize);
        button.FlatAppearance.BorderSize    = 0;
        button.FlatAppearance.MouseOverBackColor = EditorColors.InputBackgroundHover;
        button.FlatAppearance.MouseDownBackColor = EditorColors.AccentBlueDim;
    }

    /// <summary>Aplica estilo de botón tipo píldora (26px alto, bordes redondeados simulados).</summary>
    public static void ApplyPillButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = EditorColors.InputBackground;
        button.ForeColor = EditorColors.TextPrimary;
        button.Font      = EditorFonts.Small;
        button.Height    = PillHeight;
        button.FlatAppearance.BorderSize  = 1;
        button.FlatAppearance.BorderColor = EditorColors.Border;
        button.FlatAppearance.MouseOverBackColor = EditorColors.InputBackgroundHover;
    }

    /// <summary>Aplica estilo de botón de éxito (verde, para confirmar/crear).</summary>
    public static void ApplySuccessButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = EditorColors.BtnSuccessBg;
        button.ForeColor = EditorColors.TextPrimary;
        button.Font      = EditorFonts.PrimaryBold;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 0x45, 0xAA, 0x4A);
    }

    // ── Labels / títulos ─────────────────────────────────────────────────────

    /// <summary>Aplica estilo de título de sección (mayúsculas, atenuado).</summary>
    public static void ApplySectionTitle(Label label)
    {
        label.Font      = EditorFonts.SmallBold;
        label.ForeColor = EditorColors.TextSecondary;
        label.BackColor = Color.Transparent;
    }

    /// <summary>Aplica estilo de etiqueta monoespaciada (para valores numéricos/código).</summary>
    public static void ApplyMonoLabel(Label label)
    {
        label.Font      = EditorFonts.Mono;
        label.ForeColor = EditorColors.TextPrimary;
        label.BackColor = Color.Transparent;
    }

    /// <summary>Aplica estilo de badge/chip (fondo de acento, texto pequeño).</summary>
    public static void ApplyBadge(Label label)
    {
        label.Font      = EditorFonts.Small;
        label.ForeColor = EditorColors.TextPrimary;
        label.BackColor = EditorColors.AccentBlueDim;
        label.AutoSize  = false;
        label.TextAlign = ContentAlignment.MiddleCenter;
        label.Height    = 18;
    }

    // ── Inputs ───────────────────────────────────────────────────────────────

    /// <summary>Aplica estilo de contenedor de input (fondo oscuro, borde sutil).</summary>
    public static void ApplyInputShell(Control control)
    {
        control.BackColor = EditorColors.InputBackground;
        control.ForeColor = EditorColors.TextPrimary;
        control.Font      = EditorFonts.Primary;
    }

    /// <summary>Aplica estilo de TextBox del editor.</summary>
    public static void ApplyInputTextBox(TextBox textBox)
    {
        textBox.BackColor    = EditorColors.InputBackground;
        textBox.ForeColor    = EditorColors.TextPrimary;
        textBox.BorderStyle  = BorderStyle.FixedSingle;
        textBox.Font         = EditorFonts.Primary;
    }

    // ── Tags de eje ─────────────────────────────────────────────────────────

    /// <summary>Aplica el color de fondo según el eje ("X"/"Y"/"Z").</summary>
    public static Color AxisTagColor(string axis) => axis.ToUpperInvariant() switch
    {
        "X" => EditorColors.AxisRed,
        "Y" => EditorColors.AxisGreen,
        "Z" => EditorColors.AxisBlue,
        _   => EditorColors.TextSecondary,
    };

    // ── Paneles de fondo ─────────────────────────────────────────────────────

    /// <summary>Aplica fondo de panel estándar.</summary>
    public static void ApplyPanel(Panel panel)
    {
        panel.BackColor = EditorColors.PanelBackground;
        panel.ForeColor = EditorColors.TextPrimary;
    }

    /// <summary>Aplica fondo de cromo (toolbar, cabeceras).</summary>
    public static void ApplyChrome(Panel panel)
    {
        panel.BackColor = EditorColors.PanelBackgroundAlt;
        panel.ForeColor = EditorColors.TextPrimary;
    }

    // ── ToolStrip renderer ───────────────────────────────────────────────────

    /// <summary>
    /// Renderer de <see cref="ToolStrip"/> y <see cref="MenuStrip"/> con colores del tema.
    /// </summary>
    public sealed class EditorToolStripRenderer : ToolStripProfessionalRenderer
    {
        public EditorToolStripRenderer() : base(new EditorColorTable()) { }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
    }

    private sealed class EditorColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin        => EditorColors.BgChrome;
        public override Color ToolStripGradientMiddle       => EditorColors.BgChrome;
        public override Color ToolStripGradientEnd          => EditorColors.BgChrome;
        public override Color MenuStripGradientBegin        => EditorColors.BgChrome;
        public override Color MenuStripGradientEnd          => EditorColors.BgChrome;
        public override Color MenuBorder                    => EditorColors.Border;
        public override Color MenuItemBorder                => EditorColors.Border;
        public override Color MenuItemSelected              => EditorColors.RowSelected;
        public override Color MenuItemSelectedGradientBegin => EditorColors.RowSelected;
        public override Color MenuItemSelectedGradientEnd   => EditorColors.RowSelected;
        public override Color MenuItemPressedGradientBegin  => EditorColors.AccentBlueDim;
        public override Color MenuItemPressedGradientEnd    => EditorColors.AccentBlueDim;
        public override Color ImageMarginGradientBegin      => EditorColors.PanelBackground;
        public override Color ImageMarginGradientMiddle     => EditorColors.PanelBackground;
        public override Color ImageMarginGradientEnd        => EditorColors.PanelBackground;
        public override Color ToolStripDropDownBackground   => EditorColors.PanelBackground;
        public override Color ButtonSelectedHighlight       => EditorColors.RowSelected;
        public override Color ButtonSelectedHighlightBorder => EditorColors.Border;
        public override Color ButtonPressedHighlight        => EditorColors.AccentBlueDim;
        public override Color ButtonPressedHighlightBorder  => EditorColors.AccentBlue;
        public override Color ButtonCheckedHighlight        => EditorColors.AccentBlueDim;
        public override Color ButtonCheckedHighlightBorder  => EditorColors.AccentBlue;
        public override Color SeparatorDark                 => EditorColors.Border;
        public override Color SeparatorLight                => EditorColors.PanelBackgroundAlt;
    }
}
