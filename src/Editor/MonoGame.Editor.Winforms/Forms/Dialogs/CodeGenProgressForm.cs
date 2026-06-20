using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Forms.Dialogs;

/// <summary>
/// Ventana de progreso para la generación de código.
/// Se muestra como ventana no modal; usa <see cref="AddFileResult"/> y
/// <see cref="MarkComplete"/> para actualizar el estado desde el hilo de UI.
/// </summary>
internal sealed class CodeGenProgressForm : Form
{
    private readonly ListView _listView;
    private readonly Label    _lblSummary;
    private readonly Button   _btnClose;

    private int _successCount;
    private int _failedCount;

    // ── Constructor ───────────────────────────────────────────────────────────

    public CodeGenProgressForm()
    {
        Text            = "Code Generation";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(560, 360);
        MinimumSize     = new Size(400, 240);
        BackColor       = EditorColors.PanelBackground;
        ForeColor       = EditorColors.TextPrimary;
        Font            = EditorFonts.Primary;
        ShowIcon        = false;
        ShowInTaskbar   = false;

        // ── Header ────────────────────────────────────────────────────────────
        Panel header = new()
        {
            Dock      = DockStyle.Top,
            Height    = 34,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(12, 8, 12, 0),
        };
        Label lblTitle = new()
        {
            Text      = "Generating scene code…",
            Dock      = DockStyle.Fill,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.PrimaryBold,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        header.Controls.Add(lblTitle);

        // ── ListView de resultados ─────────────────────────────────────────────
        _listView = new ListView
        {
            Dock          = DockStyle.Fill,
            View          = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            HeaderStyle   = ColumnHeaderStyle.Nonclickable,
            BackColor     = EditorColors.PanelBackground,
            ForeColor     = EditorColors.TextPrimary,
            Font          = EditorFonts.Mono,
            BorderStyle   = BorderStyle.None,
            GridLines     = false,
            MultiSelect   = false,
        };
        _listView.Columns.Add("St", 24,  HorizontalAlignment.Center);
        _listView.Columns.Add("File", 450, HorizontalAlignment.Left);

        // ── Footer ────────────────────────────────────────────────────────────
        Panel footer = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 46,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(10, 8, 10, 8),
        };

        _lblSummary = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "Working…",
        };

        _btnClose = new Button
        {
            Text      = "Close",
            Dock      = DockStyle.Right,
            Width     = 80,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackground,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Primary,
            Enabled   = false,
        };
        _btnClose.FlatAppearance.BorderColor = EditorColors.Border;
        _btnClose.Click += (_, _) => Close();

        footer.Controls.Add(_lblSummary);
        footer.Controls.Add(_btnClose);

        Controls.Add(_listView);
        Controls.Add(header);
        Controls.Add(footer);
    }

    // ── API pública ────────────────────────────────────────────────────────────

    /// <summary>Añade un resultado de archivo al listado.</summary>
    public void AddFileResult(string filePath, bool success)
    {
        if (!IsHandleCreated || IsDisposed) return;
        if (InvokeRequired) { Invoke(() => AddFileResult(filePath, success)); return; }

        if (success) _successCount++; else _failedCount++;

        Color rowColor = success ? Color.FromArgb(100, 200, 100) : Color.FromArgb(220, 80, 80);
        string icon    = success ? "✓" : "✗";

        ListViewItem item = new(icon) { ForeColor = rowColor };
        item.SubItems.Add(filePath);
        _listView.Items.Add(item);
        _listView.EnsureVisible(_listView.Items.Count - 1);
    }

    /// <summary>Marca la generación como completada y habilita el botón de cierre.</summary>
    public void MarkComplete(int successCount, int failedCount)
    {
        if (!IsHandleCreated || IsDisposed) return;
        if (InvokeRequired) { Invoke(() => MarkComplete(successCount, failedCount)); return; }

        _successCount = successCount;
        _failedCount  = failedCount;

        bool allOk = failedCount == 0;
        _lblSummary.Text      = allOk
            ? $"Done — {successCount} file(s) generated."
            : $"Done — {successCount} ok, {failedCount} failed.";
        _lblSummary.ForeColor = allOk ? Color.FromArgb(100, 200, 100) : Color.FromArgb(220, 80, 80);

        _btnClose.Enabled   = true;
        _btnClose.BackColor = EditorColors.AccentBlue;
        _btnClose.ForeColor = EditorColors.TextPrimary;
        _btnClose.Font      = EditorFonts.PrimaryBold;
        _btnClose.FlatAppearance.BorderSize = 0;
    }
}
