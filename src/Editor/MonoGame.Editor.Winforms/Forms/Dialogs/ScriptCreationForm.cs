using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Forms.Dialogs;

/// <summary>
/// Diálogo de creación de script: nombre de clase, namespace y subcarpeta relativa.
/// Devuelve <see cref="ScriptCreationResult"/> si el usuario confirma, o <c>null</c>
/// si cancela.
/// </summary>
internal sealed class ScriptCreationForm : Form
{
    private readonly TextBox _txtClass;
    private readonly TextBox _txtNamespace;
    private readonly TextBox _txtRelFolder;
    private readonly Button  _btnOk;

    private ScriptCreationForm()
    {
        Text            = "New Script";
        Width           = 400;
        Height          = 220;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = EditorColors.PanelBackground;
        ForeColor       = EditorColors.TextPrimary;
        Font            = EditorFonts.Primary;

        // ── Campos ────────────────────────────────────────────────────────────
        TableLayoutPanel grid = new()
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 3,
            Padding     = new Padding(12, 10, 12, 4),
            BackColor   = EditorColors.PanelBackground,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        _txtClass     = MakeTextBox();
        _txtNamespace = MakeTextBox();
        _txtRelFolder = MakeTextBox();
        _txtRelFolder.PlaceholderText = "(leave empty to use selected folder)";

        grid.Controls.Add(MakeLabel("Class name *"),   0, 0);
        grid.Controls.Add(_txtClass,                   1, 0);
        grid.Controls.Add(MakeLabel("Namespace *"),    0, 1);
        grid.Controls.Add(_txtNamespace,               1, 1);
        grid.Controls.Add(MakeLabel("Subfolder"),      0, 2);
        grid.Controls.Add(_txtRelFolder,               1, 2);

        // ── Botones ────────────────────────────────────────────────────────────
        Panel btnPanel = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 40,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(8, 6, 8, 6),
        };

        _btnOk = new Button
        {
            Text         = "Create",
            DialogResult = DialogResult.OK,
            Width        = 80,
            Dock         = DockStyle.Right,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.AccentBlue,
            ForeColor    = EditorColors.TextPrimary,
            Font         = EditorFonts.PrimaryBold,
            Enabled      = false,
        };
        _btnOk.FlatAppearance.BorderSize = 0;

        Button btnCancel = new()
        {
            Text         = "Cancel",
            DialogResult = DialogResult.Cancel,
            Width        = 70,
            Dock         = DockStyle.Right,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.PanelBackgroundAlt,
            ForeColor    = EditorColors.TextSecondary,
        };
        btnCancel.FlatAppearance.BorderColor = EditorColors.Border;

        btnPanel.Controls.Add(_btnOk);
        btnPanel.Controls.Add(btnCancel);

        Controls.Add(grid);
        Controls.Add(btnPanel);

        AcceptButton = _btnOk;
        CancelButton = btnCancel;

        // ── Validación en tiempo real ──────────────────────────────────────────
        _txtClass.TextChanged     += (_, _) => ValidateForm();
        _txtNamespace.TextChanged += (_, _) => ValidateForm();
    }

    // ── API pública ────────────────────────────────────────────────────────────

    /// <summary>Muestra el diálogo y devuelve el resultado o <c>null</c> si se cancela.</summary>
    public static ScriptCreationResult? Show(IWin32Window? owner)
    {
        using ScriptCreationForm form = new();
        return form.ShowDialog(owner) == DialogResult.OK
            ? new ScriptCreationResult(
                form._txtClass.Text.Trim(),
                form._txtNamespace.Text.Trim(),
                form._txtRelFolder.Text.Trim())
            : null;
    }

    // ── Validación ─────────────────────────────────────────────────────────────

    private void ValidateForm()
    {
        bool classOk     = IsValidIdentifier(_txtClass.Text.Trim());
        bool namespaceOk = IsValidNamespace(_txtNamespace.Text.Trim());

        _btnOk.Enabled    = classOk && namespaceOk;
        _txtClass.ForeColor     = classOk     ? EditorColors.TextPrimary : EditorColors.AxisRed;
        _txtNamespace.ForeColor = namespaceOk ? EditorColors.TextPrimary : EditorColors.AxisRed;
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        foreach (char c in name)
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        return true;
    }

    private static bool IsValidNamespace(string ns)
    {
        if (string.IsNullOrEmpty(ns)) return false;
        foreach (string part in ns.Split('.'))
            if (!IsValidIdentifier(part)) return false;
        return true;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static TextBox MakeTextBox() => new()
    {
        Dock        = DockStyle.Fill,
        Font        = EditorFonts.Primary,
        BackColor   = EditorColors.InputBackground,
        ForeColor   = EditorColors.TextPrimary,
        BorderStyle = BorderStyle.FixedSingle,
    };

    private static Label MakeLabel(string text) => new()
    {
        Text      = text,
        ForeColor = EditorColors.TextSecondary,
        Font      = EditorFonts.Small,
        TextAlign = ContentAlignment.MiddleLeft,
        Dock      = DockStyle.Fill,
    };
}
