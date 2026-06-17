using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Controls;

/// <summary>
/// Control numérico con etiqueta de eje coloreada (X/Y/Z) y botones de incremento/decremento.
/// Port del AxisStepper de MAUI.
/// </summary>
internal sealed class AxisStepper : UserControl
{
    private bool _suppressUpdate;
    private double _value;

    private readonly Panel  _axisTag;
    private readonly Label  _axisLabel;
    private readonly TextBox _entry;
    private readonly Button  _btnUp;
    private readonly Button  _btnDown;

    /// <summary>Se dispara cuando el usuario confirma un nuevo valor.</summary>
    public event EventHandler? ValueCommitted;

    // ── Propiedades ──────────────────────────────────────────────────────────

    /// <summary>Etiqueta de eje ("X", "Y", "Z"). Determina el color del tag.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Axis
    {
        get => _axisLabel.Text;
        set
        {
            _axisLabel.Text    = value;
            _axisTag.BackColor = EditorStyles.AxisTagColor(value);
        }
    }

    /// <summary>Valor numérico actual.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Value
    {
        get => _value;
        set
        {
            _value = value;
            RefreshEntry();
        }
    }

    /// <summary>Cantidad que se suma/resta al pulsar los botones.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Step { get; set; } = 1.0;

    /// <summary>Controla la visibilidad del tag de eje.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowAxisTag
    {
        get => _axisTag.Visible;
        set => _axisTag.Visible = value;
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public AxisStepper()
    {
        SuspendLayout();

        // Tag de eje
        _axisLabel = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.PrimaryBold,
        };

        _axisTag = new Panel { Width = 24 };
        _axisTag.Controls.Add(_axisLabel);

        // Campo de texto
        _entry = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor   = EditorColors.InputBackground,
            ForeColor   = EditorColors.TextPrimary,
            Font        = EditorFonts.Primary,
            TextAlign   = HorizontalAlignment.Right,
            Dock        = DockStyle.Fill,
        };

        // Botones incremento/decremento
        _btnUp = new Button
        {
            Text      = "▲",
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Tiny,
            TabStop   = false,
            Dock      = DockStyle.Fill,
        };
        _btnUp.FlatAppearance.BorderSize = 0;

        _btnDown = new Button
        {
            Text      = "▼",
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Tiny,
            TabStop   = false,
            Dock      = DockStyle.Fill,
        };
        _btnDown.FlatAppearance.BorderSize = 0;

        // Panel de botones (2 filas)
        TableLayoutPanel btnsLayout = new()
        {
            ColumnCount = 1,
            RowCount    = 2,
            Dock        = DockStyle.Fill,
            Margin      = Padding.Empty,
            Padding     = Padding.Empty,
        };
        btnsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        btnsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        btnsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        btnsLayout.Controls.Add(_btnUp,   0, 0);
        btnsLayout.Controls.Add(_btnDown, 0, 1);

        Panel btnsPanel = new() { Width = 18, Dock = DockStyle.Fill };
        btnsPanel.Controls.Add(btnsLayout);

        // Layout principal (3 columnas)
        TableLayoutPanel layout = new()
        {
            ColumnCount = 3,
            RowCount    = 1,
            Dock        = DockStyle.Fill,
            Margin      = Padding.Empty,
            Padding     = Padding.Empty,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_axisTag,  0, 0);
        layout.Controls.Add(_entry,    1, 0);
        layout.Controls.Add(btnsPanel, 2, 0);

        BackColor = EditorColors.InputBackground;
        Height    = 24;
        Controls.Add(layout);

        _entry.TextChanged += OnEntryTextChanged;
        _entry.KeyDown     += OnEntryKeyDown;
        _entry.Leave       += OnEntryLeave;
        _btnUp.Click       += (_, _) => Increment();
        _btnDown.Click     += (_, _) => Decrement();

        Axis  = "X";
        Value = 0.0;

        ResumeLayout(false);
    }

    // ── Lógica interna ────────────────────────────────────────────────────────

    private void RefreshEntry()
    {
        if (_suppressUpdate) return;
        _suppressUpdate = true;
        _entry.Text     = _value.ToString("G", CultureInfo.InvariantCulture);
        _suppressUpdate = false;
    }

    private void OnEntryTextChanged(object? sender, EventArgs e)
    {
        if (_suppressUpdate) return;
        if (double.TryParse(_entry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
        {
            _suppressUpdate = true;
            _value          = parsed;
            _suppressUpdate = false;
        }
    }

    private void OnEntryKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        CommitValue();
        e.Handled          = true;
        e.SuppressKeyPress = true;
    }

    private void OnEntryLeave(object? sender, EventArgs e) => CommitValue();

    private void CommitValue()
    {
        if (double.TryParse(_entry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
            _value = parsed;
        RefreshEntry();
        ValueCommitted?.Invoke(this, EventArgs.Empty);
    }

    private void Increment()
    {
        _value += Step;
        RefreshEntry();
        ValueCommitted?.Invoke(this, EventArgs.Empty);
    }

    private void Decrement()
    {
        _value -= Step;
        RefreshEntry();
        ValueCommitted?.Invoke(this, EventArgs.Empty);
    }
}
