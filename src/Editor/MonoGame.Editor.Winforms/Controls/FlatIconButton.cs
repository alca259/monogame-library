using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Controls;

/// <summary>
/// Botón plano sin borde con soporte para icono/glifo de texto y estado de toggle.
/// </summary>
internal sealed class FlatIconButton : Button
{
    private bool  _isToggled;
    private Color _normalBack;
    private Color _toggledBack;

    // ── Propiedades ──────────────────────────────────────────────────────────

    /// <summary>Texto/glifo del icono (puede ser un carácter Unicode, p. ej. "⚙").</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Glyph
    {
        get => Text;
        set => Text = value;
    }

    /// <summary>Estado de toggle del botón.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsToggled
    {
        get => _isToggled;
        set
        {
            _isToggled = value;
            BackColor  = value ? _toggledBack : _normalBack;
        }
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public FlatIconButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;

        _normalBack  = EditorColors.BgChrome;
        _toggledBack = EditorColors.AccentBlueDim;

        BackColor = _normalBack;
        ForeColor = EditorColors.TextPrimary;
        Font      = EditorFonts.Primary;

        FlatAppearance.MouseOverBackColor = EditorColors.InputBackgroundHover;
        FlatAppearance.MouseDownBackColor = EditorColors.RowSelected;
    }

    // ── Personalización ───────────────────────────────────────────────────────

    /// <summary>Configura los colores de fondo en estado normal y toggled.</summary>
    public void SetToggleColors(Color normal, Color toggled)
    {
        _normalBack  = normal;
        _toggledBack = toggled;
        BackColor    = _isToggled ? toggled : normal;
    }
}
