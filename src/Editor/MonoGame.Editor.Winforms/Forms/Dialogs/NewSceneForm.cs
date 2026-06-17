using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Forms.Dialogs;

/// <summary>
/// Diálogo de creación de escena nueva.
/// Devuelve <see cref="NewSceneResult"/> con nombre y dimensiones del mundo,
/// o <c>null</c> si el usuario cancela.
/// </summary>
internal sealed class NewSceneForm : Form
{
    private readonly TextBox       _txtName;
    private readonly NumericUpDown _numWidth;
    private readonly NumericUpDown _numHeight;
    private readonly Button        _btnOk;
    private readonly Label         _lblError;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Muestra el diálogo modal y devuelve el resultado, o <c>null</c> si se cancela.</summary>
    public static NewSceneResult? Show(IWin32Window? owner)
    {
        using NewSceneForm dlg = new();
        if (dlg.ShowDialog(owner) != DialogResult.OK) return null;
        return new NewSceneResult(
            dlg._txtName.Text.Trim(),
            (float)dlg._numWidth.Value,
            (float)dlg._numHeight.Value);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    private NewSceneForm()
    {
        Text            = "New Scene";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(400, 210);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor       = EditorColors.PanelBackground;
        ForeColor       = EditorColors.TextPrimary;
        Font            = EditorFonts.Primary;
        ShowIcon        = false;
        ShowInTaskbar   = false;
        MaximizeBox     = false;
        MinimizeBox     = false;

        // ── Grid de campos ────────────────────────────────────────────────────
        TableLayoutPanel grid = new()
        {
            Dock        = DockStyle.Fill,
            Padding     = new Padding(14, 14, 14, 4),
            ColumnCount = 2,
            RowCount    = 5,
            BackColor   = EditorColors.PanelBackground,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _txtName = new TextBox
        {
            Dock        = DockStyle.Fill,
            BackColor   = EditorColors.InputBackground,
            ForeColor   = EditorColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font        = EditorFonts.Primary,
        };

        _numWidth = MakeSpinner(1, 32768, 1920);
        _numHeight = MakeSpinner(1, 32768, 1080);

        _lblError = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = Color.FromArgb(220, 80, 80),
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        grid.Controls.Add(MakeLabel("Scene name:"),    0, 0);
        grid.Controls.Add(_txtName,                    1, 0);
        grid.Controls.Add(MakeLabel("World width:"),   0, 1);
        grid.Controls.Add(_numWidth,                   1, 1);
        grid.Controls.Add(MakeLabel("World height:"),  0, 2);
        grid.Controls.Add(_numHeight,                  1, 2);
        grid.Controls.Add(_lblError, 1, 3);

        // ── Botonera inferior ─────────────────────────────────────────────────
        Panel footer = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 46,
            Padding   = new Padding(10, 8, 10, 8),
            BackColor = EditorColors.PanelBackgroundAlt,
        };

        _btnOk = new Button
        {
            Text         = "Create",
            Dock         = DockStyle.Right,
            Width        = 80,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.AccentBlue,
            ForeColor    = EditorColors.TextPrimary,
            Font         = EditorFonts.PrimaryBold,
            DialogResult = DialogResult.OK,
            Enabled      = false,
        };
        _btnOk.FlatAppearance.BorderSize = 0;

        Button btnCancel = new()
        {
            Text         = "Cancel",
            Dock         = DockStyle.Right,
            Width        = 76,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.PanelBackground,
            ForeColor    = EditorColors.TextSecondary,
            Font         = EditorFonts.Primary,
            DialogResult = DialogResult.Cancel,
        };
        btnCancel.FlatAppearance.BorderColor = EditorColors.Border;

        footer.Controls.Add(_btnOk);
        footer.Controls.Add(btnCancel);

        AcceptButton = _btnOk;
        CancelButton = btnCancel;

        Controls.Add(grid);
        Controls.Add(footer);

        // ── Eventos ───────────────────────────────────────────────────────────
        _txtName.TextChanged += (_, _) => Revalidate();
    }

    // ── Validación ────────────────────────────────────────────────────────────

    private void Revalidate()
    {
        string name = _txtName.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            _lblError.Text = "Scene name is required.";
            _btnOk.Enabled = false;
            return;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            _lblError.Text = "Scene name contains invalid characters.";
            _btnOk.Enabled = false;
            return;
        }

        _lblError.Text = string.Empty;
        _btnOk.Enabled = true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Label MakeLabel(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = EditorColors.TextSecondary,
        Font      = EditorFonts.Primary,
    };

    private static NumericUpDown MakeSpinner(int min, int max, int value) => new()
    {
        Dock         = DockStyle.Fill,
        Minimum      = min,
        Maximum      = max,
        Value        = value,
        BackColor    = EditorColors.InputBackground,
        ForeColor    = EditorColors.TextPrimary,
        Font         = EditorFonts.Primary,
        BorderStyle  = BorderStyle.FixedSingle,
        TextAlign    = HorizontalAlignment.Right,
        DecimalPlaces = 0,
    };
}
