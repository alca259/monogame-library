using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Forms.Dialogs;

/// <summary>
/// Diálogo para introducir o seleccionar una ruta relativa (p. ej., la ruta de
/// una textura o shader respecto a la carpeta de contenido del proyecto).
/// </summary>
internal sealed class RelativePathPickerForm : Form
{
    private readonly TextBox _txtPath;
    private readonly Button  _btnOk;

    private readonly string _baseFolder;

    private RelativePathPickerForm(string title, string baseFolder, string initialValue)
    {
        _baseFolder = baseFolder;

        Text            = title;
        Width           = 480;
        Height          = 160;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = EditorColors.PanelBackground;
        ForeColor       = EditorColors.TextPrimary;
        Font            = EditorFonts.Primary;

        // ── Base folder label ─────────────────────────────────────────────────
        Label lblBase = new()
        {
            Dock      = DockStyle.Top,
            Height    = 24,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            Text      = $"Base: {baseFolder}",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0),
        };

        // ── Path row ──────────────────────────────────────────────────────────
        Panel pathRow = new()
        {
            Dock    = DockStyle.Top,
            Height  = 30,
            Padding = new Padding(8, 4, 8, 0),
            BackColor = EditorColors.PanelBackground,
        };

        _txtPath = new TextBox
        {
            Text        = initialValue,
            Font        = EditorFonts.Primary,
            BackColor   = EditorColors.InputBackground,
            ForeColor   = EditorColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Dock        = DockStyle.Fill,
        };

        Button btnBrowse = new()
        {
            Text      = "…",
            Width     = 30,
            Dock      = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextPrimary,
        };
        btnBrowse.FlatAppearance.BorderColor = EditorColors.Border;

        pathRow.Controls.Add(_txtPath);
        pathRow.Controls.Add(btnBrowse);

        // ── Botones ────────────────────────────────────────────────────────────
        Panel btnPanel = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 38,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(8, 6, 8, 6),
        };

        _btnOk = new Button
        {
            Text         = "OK",
            DialogResult = DialogResult.OK,
            Width        = 70,
            Dock         = DockStyle.Right,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.AccentBlue,
            ForeColor    = EditorColors.TextPrimary,
            Font         = EditorFonts.PrimaryBold,
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

        Controls.Add(pathRow);
        Controls.Add(lblBase);
        Controls.Add(btnPanel);

        AcceptButton = _btnOk;
        CancelButton = btnCancel;

        // ── Browse: convierte ruta absoluta a relativa si posible ─────────────
        btnBrowse.Click += (_, _) =>
        {
            using OpenFileDialog ofd = new()
            {
                InitialDirectory = Directory.Exists(baseFolder) ? baseFolder : string.Empty,
                Filter = "All files|*.*",
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            string selected = ofd.FileName;
            if (selected.StartsWith(baseFolder, StringComparison.OrdinalIgnoreCase))
                selected = Path.GetRelativePath(baseFolder, selected);

            _txtPath.Text = selected;
        };
    }

    // ── API pública ────────────────────────────────────────────────────────────

    /// <summary>
    /// Muestra el diálogo para seleccionar una ruta relativa a <paramref name="baseFolder"/>.
    /// </summary>
    /// <param name="owner">Ventana padre.</param>
    /// <param name="baseFolder">Carpeta base para relativizar la ruta.</param>
    /// <param name="title">Título del diálogo.</param>
    /// <param name="initialValue">Valor inicial del campo de ruta.</param>
    /// <returns>Ruta relativa introducida, o <c>null</c> si se cancela.</returns>
    public static string? Show(IWin32Window? owner,
                               string baseFolder,
                               string title = "Select Relative Path",
                               string initialValue = "")
    {
        using RelativePathPickerForm form = new(title, baseFolder, initialValue);
        return form.ShowDialog(owner) == DialogResult.OK
            ? form._txtPath.Text.Trim()
            : null;
    }
}
