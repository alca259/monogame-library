using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>Panel de historial de undo/redo: dos listas y botón Clear.</summary>
internal sealed class UndoHistoryPanel : UserControl
{
    private readonly UndoHistoryViewModel _vm = new();

    private readonly ListBox _lstUndo;
    private readonly ListBox _lstRedo;
    private readonly Label   _lblSummary;

    public UndoHistoryPanel()
    {
        SuspendLayout();
        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;

        // ── Toolbar ───────────────────────────────────────────────────────────
        Panel toolbar = new()
        {
            Dock      = DockStyle.Top,
            Height    = 28,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(4, 4, 4, 4),
        };

        Button btnClear = new()
        {
            Text      = "Clear",
            Dock      = DockStyle.Left,
            Width     = 56,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
        };
        btnClear.FlatAppearance.BorderColor = EditorColors.Border;

        _lblSummary = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            Text      = "0 / 0",
            Padding   = new Padding(0, 0, 6, 0),
        };

        toolbar.Controls.Add(_lblSummary);
        toolbar.Controls.Add(btnClear);

        // ── SplitContainer: Undo | Redo ───────────────────────────────────────
        SplitContainer split = new()
        {
            Dock          = DockStyle.Fill,
            Orientation   = Orientation.Vertical,
            BackColor     = EditorColors.Border,
            SplitterWidth = 2,
            Panel1MinSize = 80,
            Panel2MinSize = 80,
        };

        Label lblUndo = MakeSectionHeader("Undo");
        Label lblRedo = MakeSectionHeader("Redo");

        _lstUndo = MakeListBox();
        _lstRedo = MakeListBox();

        split.Panel1.Controls.Add(_lstUndo);
        split.Panel1.Controls.Add(lblUndo);
        split.Panel2.Controls.Add(_lstRedo);
        split.Panel2.Controls.Add(lblRedo);

        Controls.Add(split);
        Controls.Add(toolbar);

        // ── Eventos de controles ──────────────────────────────────────────────
        btnClear.Click += (_, _) => _vm.Clear();

        // ── Eventos VM ────────────────────────────────────────────────────────
        _vm.RebuildRequested += OnRebuild;
        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Rebuild ────────────────────────────────────────────────────────────────

    private void OnRebuild()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnRebuild); return; }

        _lstUndo.BeginUpdate();
        _lstUndo.Items.Clear();
        foreach (string s in _vm.UndoEntries) _lstUndo.Items.Add(s);
        _lstUndo.EndUpdate();

        _lstRedo.BeginUpdate();
        _lstRedo.Items.Clear();
        foreach (string s in _vm.RedoEntries) _lstRedo.Items.Add(s);
        _lstRedo.EndUpdate();

        _lblSummary.Text = _vm.SummaryText;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static ListBox MakeListBox() => new()
    {
        Dock        = DockStyle.Fill,
        BackColor   = EditorColors.PanelBackground,
        ForeColor   = EditorColors.TextPrimary,
        Font        = EditorFonts.Small,
        BorderStyle = BorderStyle.None,
    };

    private static Label MakeSectionHeader(string text) => new()
    {
        Dock      = DockStyle.Top,
        Height    = 20,
        BackColor = EditorColors.PanelBackgroundAlt,
        ForeColor = EditorColors.TextMuted,
        Font      = EditorFonts.Small,
        Text      = text,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding   = new Padding(6, 0, 0, 0),
    };
}
