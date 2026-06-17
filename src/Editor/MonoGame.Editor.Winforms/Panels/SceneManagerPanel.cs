using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Infrastructure;
using MonoGame.Editor.Winforms.Theme;
using MonoGame.Editor.Winforms.ViewModels.Panels;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>Panel que lista las escenas del proyecto en la pestaña Scenes del DockBar.</summary>
internal sealed class SceneManagerPanel : UserControl
{
    private readonly SceneManagerViewModel _vm = new();
    private readonly ListView              _listView;
    private readonly Label                 _lblActive;
    private readonly Label                 _lblStatus;

    public SceneManagerPanel()
    {
        SuspendLayout();

        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;

        ToolTip tip = new();

        // ── Toolbar ───────────────────────────────────────────────────────────
        Panel toolbar = new()
        {
            Dock      = DockStyle.Top,
            Height    = 28,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(2, 2, 2, 2),
        };

        Button btnNew    = MakeToolButton("+ New",  tip, "New scene (N)");
        Button btnRename = MakeToolButton("Rename", tip, "Rename selected scene");
        Button btnDelete = MakeToolButton("Delete", tip, "Delete selected scene");

        // WinForms Dock=Left apila de izquierda a derecha
        toolbar.Controls.Add(btnDelete);
        toolbar.Controls.Add(btnRename);
        toolbar.Controls.Add(btnNew);

        // ── Lista de escenas ──────────────────────────────────────────────────
        _listView = new ListView
        {
            Dock          = DockStyle.Fill,
            View          = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            HeaderStyle   = ColumnHeaderStyle.None,
            BackColor     = EditorColors.PanelBackground,
            ForeColor     = EditorColors.TextPrimary,
            Font          = EditorFonts.Primary,
            BorderStyle   = BorderStyle.None,
            GridLines     = false,
            MultiSelect   = false,
        };
        _listView.Columns.Add("Name", -2, HorizontalAlignment.Left);

        // ── Barra de estado ───────────────────────────────────────────────────
        Panel statusBar = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 20,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(4, 0, 4, 0),
        };

        _lblActive = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "No active scene",
        };

        _lblStatus = new Label
        {
            Dock      = DockStyle.Right,
            Width     = 80,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleRight,
            Text      = "0 scenes",
        };

        statusBar.Controls.Add(_lblActive);
        statusBar.Controls.Add(_lblStatus);

        Controls.Add(_listView);
        Controls.Add(toolbar);
        Controls.Add(statusBar);

        // ── Eventos de toolbar ─────────────────────────────────────────────────
        btnNew.Click += async (_, _) =>
        {
            if (!_vm.HasProject) return;
            string? name = WinFormsDialogService.Prompt(FindForm(), "New Scene", "Scene name:");
            if (string.IsNullOrWhiteSpace(name)) return;
            await _vm.NewSceneAsync(name).ConfigureAwait(true);
        };

        btnRename.Click += (_, _) =>
        {
            if (_listView.SelectedItems.Count == 0) return;
            SceneData data = (SceneData)_listView.SelectedItems[0].Tag!;
            string? newName = WinFormsDialogService.Prompt(FindForm(), "Rename Scene",
                "Enter new name:", initialValue: data.Name);
            if (string.IsNullOrWhiteSpace(newName) || newName == data.Name) return;
            _vm.RenameScene(data, newName);
        };

        btnDelete.Click += (_, _) =>
        {
            if (_listView.SelectedItems.Count == 0) return;
            SceneData data = (SceneData)_listView.SelectedItems[0].Tag!;
            bool ok = WinFormsDialogService.Confirm(FindForm(), "Delete Scene",
                $"Delete '{data.Name}'? This cannot be undone.", "Delete", "Cancel");
            if (ok) _vm.DeleteScene(data);
        };

        // ── Doble clic → cargar escena ─────────────────────────────────────────
        _listView.MouseDoubleClick += async (_, _) =>
        {
            if (_listView.SelectedItems.Count == 0) return;
            SceneData data = (SceneData)_listView.SelectedItems[0].Tag!;
            await _vm.LoadSceneAsync(data).ConfigureAwait(true);
        };

        // ── VM ────────────────────────────────────────────────────────────────
        _vm.RebuildRequested          += OnRebuild;
        _vm.PropertyChanged           += (_, e) =>
        {
            if (e.PropertyName is nameof(SceneManagerViewModel.ActiveSceneText))
                UpdateStatusLabels();
        };

        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Actualización UI ───────────────────────────────────────────────────────

    private void OnRebuild()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnRebuild); return; }

        _listView.BeginUpdate();
        _listView.Items.Clear();

        foreach (SceneData data in _vm.Scenes)
        {
            ListViewItem item = new(data.DisplayName) { Tag = data };
            if (data.IsDirty) item.ForeColor = Color.FromArgb(255, 200, 80);
            _listView.Items.Add(item);
        }

        _listView.EndUpdate();
        UpdateStatusLabels();
    }

    private void UpdateStatusLabels()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(UpdateStatusLabels); return; }
        _lblStatus.Text = _vm.SceneCountText;
        _lblActive.Text = _vm.ActiveSceneText;
    }

    // ── Layout helpers ─────────────────────────────────────────────────────────

    private static Button MakeToolButton(string text, ToolTip tip, string tooltip)
    {
        Button btn = new()
        {
            Text      = text,
            Width     = 60,
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
