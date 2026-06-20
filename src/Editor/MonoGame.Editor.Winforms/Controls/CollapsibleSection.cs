using System.ComponentModel;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Controls;

/// <summary>
/// Panel colapsable con cabecera de flecha + título. Los controles hijos se añaden a
/// <see cref="ContentPanel"/>.
/// </summary>
internal sealed class CollapsibleSection : UserControl
{
    private bool _expanded = true;

    private readonly Panel  _header;
    private readonly Button _toggleBtn;
    private readonly Label  _titleLabel;
    private readonly Panel  _contentPanel;

    // ── Propiedades ──────────────────────────────────────────────────────────

    /// <summary>Texto del título de la sección.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    /// <summary>Estado expandido/colapsado.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Expanded
    {
        get => _expanded;
        set
        {
            _expanded              = value;
            _contentPanel.Visible  = value;
            _toggleBtn.Text        = value ? "▼" : "▶";
            RecalcHeight();
        }
    }

    /// <summary>Panel en el que se añaden los controles de contenido.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Panel ContentPanel => _contentPanel;

    // ── Constructor ───────────────────────────────────────────────────────────

    public CollapsibleSection()
    {
        SuspendLayout();

        _toggleBtn = new Button
        {
            Text      = "▼",
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Tiny,
            Size      = new System.Drawing.Size(20, 24),
            Location  = new System.Drawing.Point(0, 0),
            TabStop   = false,
        };
        _toggleBtn.FlatAppearance.BorderSize = 0;
        _toggleBtn.Click += (_, _) => Expanded = !_expanded;

        _titleLabel = new Label
        {
            Text      = "Sección",
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.PrimaryBold,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Location  = new System.Drawing.Point(22, 0),
            Dock      = DockStyle.None,
        };

        _header = new Panel
        {
            Height    = 24,
            Dock      = DockStyle.Top,
            BackColor = EditorColors.PanelBackgroundAlt,
        };
        _header.Controls.Add(_titleLabel);
        _header.Controls.Add(_toggleBtn);
        _header.Resize += (_, _) => _titleLabel.Size = new System.Drawing.Size(_header.Width - 24, 24);

        _contentPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = EditorColors.PanelBackground,
            Padding   = new Padding(4),
        };

        Dock      = DockStyle.Top;
        BackColor = EditorColors.PanelBackground;
        Controls.Add(_contentPanel);
        Controls.Add(_header);

        _contentPanel.SizeChanged += (_, _) => RecalcHeight();
        ResumeLayout(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RecalcHeight() =>
        Height = _header.Height + (_expanded ? _contentPanel.Height : 0);
}
