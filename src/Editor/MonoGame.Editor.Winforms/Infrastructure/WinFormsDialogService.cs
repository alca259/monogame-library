using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Infrastructure;

/// <summary>Helpers de diálogo sincrónicos para WinForms (prompt, confirm, pick file/folder).</summary>
internal static class WinFormsDialogService
{
    /// <summary>Muestra un diálogo de texto simple. Devuelve la cadena introducida o <c>null</c> si se cancela.</summary>
    public static string? Prompt(IWin32Window? owner, string title, string message,
        string? initialValue = null, int maxLength = 256)
    {
        using Form dlg = BuildPromptForm(title, message, initialValue ?? string.Empty, maxLength);
        if (dlg.ShowDialog(owner) != DialogResult.OK) return null;
        return ((TextBox)dlg.Controls["_tb"]!).Text;
    }

    /// <summary>Muestra un cuadro de confirmación. Devuelve <c>true</c> si el usuario confirma.</summary>
    public static bool Confirm(IWin32Window? owner, string title, string message,
        string yesText = "Yes", string noText = "No")
        => MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
           == DialogResult.Yes;

    /// <summary>Abre un diálogo de selección de archivo. Devuelve la ruta seleccionada o <c>null</c>.</summary>
    public static string? PickFile(IWin32Window? owner, string? initialDir = null, string? filter = null)
    {
        using OpenFileDialog ofd = new()
        {
            InitialDirectory = initialDir ?? string.Empty,
            Filter           = filter ?? "All files (*.*)|*.*",
        };
        return ofd.ShowDialog(owner) == DialogResult.OK ? ofd.FileName : null;
    }

    /// <summary>Abre un diálogo de selección de carpeta. Devuelve la ruta seleccionada o <c>null</c>.</summary>
    public static string? PickFolder(IWin32Window? owner, string? initialDir = null)
    {
        using FolderBrowserDialog fbd = new()
        {
            InitialDirectory      = initialDir ?? string.Empty,
            UseDescriptionForTitle = true,
            Description            = "Select folder",
        };
        return fbd.ShowDialog(owner) == DialogResult.OK ? fbd.SelectedPath : null;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static Form BuildPromptForm(string title, string message, string initialValue, int maxLength)
    {
        Label lbl = new()
        {
            Text      = message,
            Location  = new Point(12, 12),
            Width     = 360,
            Height    = 18,
            Font      = EditorFonts.Primary,
            ForeColor = EditorColors.TextPrimary,
        };

        TextBox tb = new()
        {
            Name        = "_tb",
            Text        = initialValue,
            Location    = new Point(12, 36),
            Width       = 360,
            MaxLength   = maxLength,
            Font        = EditorFonts.Primary,
            BackColor   = EditorColors.PanelBackgroundAlt,
            ForeColor   = EditorColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
        };
        tb.SelectAll();

        Button btnOk = new()
        {
            Text         = "OK",
            DialogResult = DialogResult.OK,
            Location     = new Point(216, 66),
            Width        = 75,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.AccentBlue,
            ForeColor    = EditorColors.TextPrimary,
            Font         = EditorFonts.Primary,
        };
        btnOk.FlatAppearance.BorderSize = 0;

        Button btnCancel = new()
        {
            Text         = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(297, 66),
            Width        = 75,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.PanelBackgroundAlt,
            ForeColor    = EditorColors.TextSecondary,
            Font         = EditorFonts.Primary,
        };
        btnCancel.FlatAppearance.BorderColor = EditorColors.Border;

        Form form = new()
        {
            Text            = title,
            ClientSize      = new Size(384, 100),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition   = FormStartPosition.CenterParent,
            MinimizeBox     = false,
            MaximizeBox     = false,
            AcceptButton    = btnOk,
            CancelButton    = btnCancel,
            BackColor       = EditorColors.PanelBackground,
            ForeColor       = EditorColors.TextPrimary,
            Font            = EditorFonts.Primary,
        };

        form.Controls.Add(lbl);
        form.Controls.Add(tb);
        form.Controls.Add(btnOk);
        form.Controls.Add(btnCancel);

        form.Shown += (_, _) => { tb.Focus(); tb.SelectAll(); };

        return form;
    }
}
