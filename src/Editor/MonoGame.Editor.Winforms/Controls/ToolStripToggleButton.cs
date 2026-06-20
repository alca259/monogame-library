using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Controls;

/// <summary>
/// <see cref="ToolStripButton"/> con estado de toggle: al estar activado se resalta
/// con el color de acento del editor.
/// </summary>
internal sealed class ToolStripToggleButton : ToolStripButton
{
    private bool _isToggled;

    /// <summary>Estado activo/inactivo del botón.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsToggled
    {
        get => _isToggled;
        set
        {
            _isToggled = value;
            Checked    = value;
            BackColor  = value ? EditorColors.AccentBlueDim : EditorColors.BgChrome;
        }
    }

    public ToolStripToggleButton() => Init();
    public ToolStripToggleButton(string text) : base(text) => Init();
    public ToolStripToggleButton(Image image) : base(image) => Init();
    public ToolStripToggleButton(string text, Image? image) : base(text, image) => Init();

    private void Init()
    {
        CheckOnClick = false;
        BackColor    = EditorColors.BgChrome;
        ForeColor    = EditorColors.TextPrimary;
    }
}
