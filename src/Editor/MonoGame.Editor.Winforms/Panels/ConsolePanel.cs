using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;
using MonoGame.Editor.Winforms.ViewModels.Panels;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>Panel de consola del DockBar inferior: muestra log del editor y salida de build.</summary>
internal sealed class ConsolePanel : UserControl
{
    private readonly ConsolePanelViewModel _vm = new();
    private readonly RichTextBox           _rtb;
    private readonly Button                _btnInfo;
    private readonly Button                _btnWarn;
    private readonly Button                _btnError;
    private readonly Label                 _lblCount;

    private static readonly Color _colorInfo  = Color.FromArgb(180, 180, 180);
    private static readonly Color _colorWarn  = Color.FromArgb(255, 200, 50);
    private static readonly Color _colorError = Color.FromArgb(220, 80, 80);
    private static readonly Color _colorBuild = Color.FromArgb(100, 160, 220);

    public ConsolePanel()
    {
        SuspendLayout();

        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;

        ToolTip tip = new();

        // ── Toolbar ───────────────────────────────────────────────────────────
        Panel toolbar = new()
        {
            Dock      = DockStyle.Top,
            Height    = 26,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(2, 2, 2, 2),
        };

        _btnInfo  = MakeFilterButton("INFO",  tip, "Toggle info messages");
        _btnWarn  = MakeFilterButton("WARN",  tip, "Toggle warnings");
        _btnError = MakeFilterButton("ERROR", tip, "Toggle errors");

        Button btnCopy  = MakeActionButton("Copy",  tip, "Copy visible entries to clipboard");
        Button btnClear = MakeActionButton("Clear", tip, "Clear console");

        _lblCount = new Label
        {
            Dock      = DockStyle.Right,
            Width     = 80,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleRight,
            Padding   = new Padding(0, 0, 4, 0),
        };

        // Dock=Left: primero añadidos = más a la izquierda
        toolbar.Controls.Add(_lblCount);
        toolbar.Controls.Add(btnClear);
        toolbar.Controls.Add(btnCopy);
        // separator visual
        toolbar.Controls.Add(new Panel { Width = 8, Dock = DockStyle.Left, BackColor = EditorColors.PanelBackgroundAlt });
        toolbar.Controls.Add(_btnError);
        toolbar.Controls.Add(_btnWarn);
        toolbar.Controls.Add(_btnInfo);

        // ── RichTextBox ───────────────────────────────────────────────────────
        _rtb = new RichTextBox
        {
            Dock      = DockStyle.Fill,
            ReadOnly  = true,
            WordWrap  = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            BackColor = EditorColors.PanelBackground,
            ForeColor = _colorInfo,
            Font      = EditorFonts.Mono,
            BorderStyle = BorderStyle.None,
        };

        Controls.Add(_rtb);
        Controls.Add(toolbar);

        // ── Eventos de toolbar ─────────────────────────────────────────────────
        _btnInfo.Click  += (_, _) => { _vm.ToggleInfo();  UpdateFilterButtonColors(); };
        _btnWarn.Click  += (_, _) => { _vm.ToggleWarn();  UpdateFilterButtonColors(); };
        _btnError.Click += (_, _) => { _vm.ToggleError(); UpdateFilterButtonColors(); };

        btnCopy.Click  += (_, _) => _vm.CopyAll();
        btnClear.Click += (_, _) => _vm.Clear();

        // ── Eventos VM ────────────────────────────────────────────────────────
        _vm.EntryAppended        += OnEntryAppended;
        _vm.VisibleEntriesRebuilt += OnVisibleEntriesRebuilt;
        _vm.Cleared              += OnCleared;

        UpdateFilterButtonColors();
        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Actualización UI ───────────────────────────────────────────────────────

    private void OnEntryAppended(string line, LogLevel level)
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(() => OnEntryAppended(line, level)); return; }
        AppendLine(line, level);
        UpdateCount();
    }

    private void OnVisibleEntriesRebuilt()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnVisibleEntriesRebuilt); return; }

        _rtb.SuspendLayout();
        _rtb.Clear();

        foreach (string line in _vm.VisibleEntries)
            AppendLine(line, DetectLevel(line));

        _rtb.ResumeLayout();
        UpdateCount();
    }

    private void OnCleared()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnCleared); return; }
        _rtb.Clear();
        UpdateCount();
    }

    private void AppendLine(string line, LogLevel level)
    {
        Color color = level switch
        {
            LogLevel.Warning => _colorWarn,
            LogLevel.Error   => _colorError,
            _                => line.Contains("[BLD]", StringComparison.Ordinal) ? _colorBuild : _colorInfo,
        };

        _rtb.SelectionStart  = _rtb.TextLength;
        _rtb.SelectionLength = 0;
        _rtb.SelectionColor  = color;
        _rtb.AppendText(line + "\n");
        _rtb.ScrollToCaret();
    }

    private void UpdateCount()
        => _lblCount.Text = $"{_vm.VisibleEntries.Count} entries";

    private void UpdateFilterButtonColors()
    {
        _btnInfo.BackColor  = _vm.ShowInfo  ? EditorColors.AccentBlue : EditorColors.PanelBackgroundAlt;
        _btnWarn.BackColor  = _vm.ShowWarn  ? Color.FromArgb(160, 120, 20) : EditorColors.PanelBackgroundAlt;
        _btnError.BackColor = _vm.ShowError ? Color.FromArgb(140, 40, 40) : EditorColors.PanelBackgroundAlt;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static LogLevel DetectLevel(string line)
    {
        if (line.Contains("[WARN]", StringComparison.Ordinal)) return LogLevel.Warning;
        if (line.Contains("[ERR]",  StringComparison.Ordinal)) return LogLevel.Error;
        return LogLevel.Info;
    }

    private static Button MakeFilterButton(string text, ToolTip tip, string tooltip)
    {
        Button btn = new()
        {
            Text      = text,
            Width     = 50,
            Dock      = DockStyle.Left,
            FlatStyle = FlatStyle.Flat,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.Small,
        };
        btn.FlatAppearance.BorderSize = 0;
        tip.SetToolTip(btn, tooltip);
        return btn;
    }

    private static Button MakeActionButton(string text, ToolTip tip, string tooltip)
    {
        Button btn = new()
        {
            Text      = text,
            Width     = 46,
            Dock      = DockStyle.Left,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
        };
        btn.FlatAppearance.BorderColor = EditorColors.Border;
        tip.SetToolTip(btn, tooltip);
        return btn;
    }
}
