using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>
/// Panel Script Browser: árbol de subcarpetas de GameScriptsPath y lista de
/// ficheros <c>.cs</c>. Permite crear nuevos scripts con nombre de clase y namespace.
/// </summary>
internal sealed class ScriptBrowserPanel : UserControl
{
    private readonly ScriptBrowserViewModel _vm = new();

    private readonly ListBox _lstFolders;
    private readonly ListBox _lstScripts;
    private readonly Button  _btnCreate;

    private bool _rebuilding;

    public ScriptBrowserPanel()
    {
        SuspendLayout();
        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;

        // ── SplitContainer: árbol | lista ────────────────────────────────────
        SplitContainer split = new()
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Vertical,
            BackColor        = EditorColors.Border,
            SplitterWidth    = 2,
            Panel1MinSize    = 100,
            Panel2MinSize    = 120,
            SplitterDistance = 160,
        };

        // Columna izquierda: carpetas
        Label lblFolders = MakeHeader("Folders");
        _lstFolders = MakeListBox();
        _lstFolders.DrawMode  = DrawMode.OwnerDrawFixed;
        _lstFolders.ItemHeight = 18;
        _lstFolders.DrawItem += OnDrawFolder;
        split.Panel1.Controls.Add(_lstFolders);
        split.Panel1.Controls.Add(lblFolders);

        // Columna derecha: scripts
        Label lblScripts = MakeHeader("Scripts");
        _lstScripts = MakeListBox();

        Panel bottomBar = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 30,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(4),
        };

        _btnCreate = new Button
        {
            Text      = "+ New Script",
            Dock      = DockStyle.Left,
            Width     = 90,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
            Enabled   = false,
        };
        _btnCreate.FlatAppearance.BorderColor = EditorColors.Border;
        bottomBar.Controls.Add(_btnCreate);

        split.Panel2.Controls.Add(_lstScripts);
        split.Panel2.Controls.Add(lblScripts);
        split.Panel2.Controls.Add(bottomBar);

        Controls.Add(split);

        // ── Eventos de controles ──────────────────────────────────────────────
        _lstFolders.SelectedIndexChanged += (_, _) =>
        {
            if (_rebuilding || _lstFolders.SelectedIndex < 0) return;
            FolderEntry entry = (FolderEntry)_lstFolders.Items[_lstFolders.SelectedIndex];
            _vm.SelectFolder(entry.Path);
        };

        _btnCreate.Click += async (_, _) =>
        {
            if (!_vm.CanCreateScript) return;
            ScriptCreationResult? result = ScriptCreationForm.Show(FindForm());
            if (result is not null)
                await _vm.CreateScriptAsync(result).ConfigureAwait(true);
        };

        // ── Eventos VM ────────────────────────────────────────────────────────
        _vm.FolderListChanged += OnFolderListChanged;
        _vm.ScriptListChanged += OnScriptListChanged;
        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Rebuild ────────────────────────────────────────────────────────────────

    private void OnFolderListChanged()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnFolderListChanged); return; }

        _rebuilding = true;
        _lstFolders.BeginUpdate();
        _lstFolders.Items.Clear();

        foreach (FolderEntry entry in _vm.FolderItems)
        {
            // Indentation via spaces proportional to depth
            _lstFolders.Items.Add(entry);
        }

        _lstFolders.EndUpdate();
        _rebuilding = false;
    }

    private void OnScriptListChanged()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnScriptListChanged); return; }

        _lstScripts.BeginUpdate();
        _lstScripts.Items.Clear();
        foreach (ScriptEntry entry in _vm.ScriptItems)
            _lstScripts.Items.Add(entry.FileName);
        _lstScripts.EndUpdate();

        _btnCreate.Enabled   = _vm.CanCreateScript;
        _btnCreate.ForeColor = _vm.CanCreateScript ? EditorColors.TextPrimary : EditorColors.TextSecondary;
    }

    // ── DrawItem para mostrar carpetas con sangría ────────────────────────────

    private void OnDrawFolder(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _lstFolders.Items.Count) return;
        FolderEntry entry = (FolderEntry)_lstFolders.Items[e.Index];

        e.DrawBackground();

        bool selected = (e.State & DrawItemState.Selected) != 0;
        Color fg = entry.IsRoot ? EditorColors.AccentBlue : EditorColors.TextPrimary;
        if (selected) fg = EditorColors.TextPrimary;

        string display = (entry.IsRoot ? "Scripts" : Path.GetFileName(entry.Path)) ?? string.Empty;
        int indent = entry.Depth * 12 + 4;

        using SolidBrush brush = new(fg);
        e.Graphics.DrawString(display, EditorFonts.Small, brush,
            new System.Drawing.PointF(indent, e.Bounds.Y + 2));

        e.DrawFocusRectangle();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Label MakeHeader(string text) => new()
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

    private static ListBox MakeListBox() => new()
    {
        Dock        = DockStyle.Fill,
        BackColor   = EditorColors.PanelBackground,
        ForeColor   = EditorColors.TextPrimary,
        Font        = EditorFonts.Small,
        BorderStyle = BorderStyle.None,
    };
}
