using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Forms.Dialogs;

/// <summary>
/// Diálogo de creación de proyecto nuevo.
/// Devuelve <see cref="NewProjectResult"/> con nombre, carpeta padre y ruta del .csproj del juego,
/// o <c>null</c> si el usuario cancela.
/// </summary>
internal sealed class NewProjectForm : Form
{
    private readonly TextBox _txtName;
    private readonly TextBox _txtParent;
    private readonly TextBox _txtCsproj;
    private readonly Button  _btnOk;
    private readonly Label   _lblError;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Muestra el diálogo modal y devuelve el resultado, o <c>null</c> si se cancela.</summary>
    public static NewProjectResult? Show(IWin32Window? owner)
    {
        using NewProjectForm dlg = new();
        if (dlg.ShowDialog(owner) != DialogResult.OK) return null;
        return new NewProjectResult(
            dlg._txtName.Text.Trim(),
            dlg._txtParent.Text.Trim(),
            dlg._txtCsproj.Text.Trim());
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    private NewProjectForm()
    {
        Text            = "New Project";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(520, 248);
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
            ColumnCount = 3,
            RowCount    = 5,
            BackColor   = EditorColors.PanelBackground,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent,  100));

        _txtName   = MakeTextBox();
        _txtParent = MakeTextBox();
        _txtCsproj = MakeTextBox();
        _txtCsproj.PlaceholderText = "(optional)";

        Button btnPickParent = MakeBrowse();
        Button btnPickCsproj = MakeBrowse();

        _lblError = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = Color.FromArgb(220, 80, 80),
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        grid.Controls.Add(MakeLabel("Project name:"), 0, 0);
        grid.Controls.Add(_txtName,                   1, 0);
        grid.SetColumnSpan(_txtName, 2);

        grid.Controls.Add(MakeLabel("Parent folder:"), 0, 1);
        grid.Controls.Add(_txtParent,                  1, 1);
        grid.Controls.Add(btnPickParent,               2, 1);

        grid.Controls.Add(MakeLabel("Game .csproj:"), 0, 2);
        grid.Controls.Add(_txtCsproj,                 1, 2);
        grid.Controls.Add(btnPickCsproj,              2, 2);

        grid.Controls.Add(_lblError, 1, 3);
        grid.SetColumnSpan(_lblError, 2);

        // ── Botonera inferior ─────────────────────────────────────────────────
        (Panel footer, _btnOk, Button btnCancel) = MakeFooter("Create");
        _btnOk.Enabled = false;
        AcceptButton   = _btnOk;
        CancelButton   = btnCancel;

        Controls.Add(grid);
        Controls.Add(footer);

        // ── Eventos ───────────────────────────────────────────────────────────
        btnPickParent.Click += (_, _) =>
        {
            string? path = WinFormsDialogService.PickFolder(this,
                _txtParent.Text.Length > 0 ? _txtParent.Text : null);
            if (path is not null) _txtParent.Text = path;
        };

        btnPickCsproj.Click += (_, _) =>
        {
            string? dir = _txtCsproj.Text.Length > 0
                ? Path.GetDirectoryName(_txtCsproj.Text)
                : _txtParent.Text.Length > 0 ? _txtParent.Text : null;
            string? path = WinFormsDialogService.PickFile(this, dir,
                "C# Project (*.csproj)|*.csproj|All files (*.*)|*.*");
            if (path is not null) _txtCsproj.Text = path;
        };

        _txtName.TextChanged   += (_, _) => Revalidate();
        _txtParent.TextChanged += (_, _) => Revalidate();
    }

    // ── Validación ────────────────────────────────────────────────────────────

    private void Revalidate()
    {
        string name   = _txtName.Text.Trim();
        string parent = _txtParent.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            _lblError.Text = "Project name is required.";
            _btnOk.Enabled = false;
            return;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            _lblError.Text = "Project name contains invalid characters.";
            _btnOk.Enabled = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(parent))
        {
            _lblError.Text = "Parent folder is required.";
            _btnOk.Enabled = false;
            return;
        }

        _lblError.Text = string.Empty;
        _btnOk.Enabled = true;
    }

    // ── Helpers estáticos ─────────────────────────────────────────────────────

    private static Label MakeLabel(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = EditorColors.TextSecondary,
        Font      = EditorFonts.Primary,
    };

    private static TextBox MakeTextBox() => new()
    {
        Dock        = DockStyle.Fill,
        BackColor   = EditorColors.InputBackground,
        ForeColor   = EditorColors.TextPrimary,
        BorderStyle = BorderStyle.FixedSingle,
        Font        = EditorFonts.Primary,
    };

    private static Button MakeBrowse()
    {
        Button btn = new()
        {
            Text      = "…",
            Dock      = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Primary,
        };
        btn.FlatAppearance.BorderColor = EditorColors.Border;
        return btn;
    }

    private static (Panel panel, Button ok, Button cancel) MakeFooter(string okText)
    {
        Panel panel = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 46,
            Padding   = new Padding(10, 8, 10, 8),
            BackColor = EditorColors.PanelBackgroundAlt,
        };

        Button ok = new()
        {
            Text         = okText,
            Dock         = DockStyle.Right,
            Width        = 80,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.AccentBlue,
            ForeColor    = EditorColors.TextPrimary,
            Font         = EditorFonts.PrimaryBold,
            DialogResult = DialogResult.OK,
        };
        ok.FlatAppearance.BorderSize = 0;

        Button cancel = new()
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
        cancel.FlatAppearance.BorderColor = EditorColors.Border;

        panel.Controls.Add(ok);
        panel.Controls.Add(cancel);
        return (panel, ok, cancel);
    }
}
