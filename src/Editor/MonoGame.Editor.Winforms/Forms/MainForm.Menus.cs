using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Forms;

partial class MainForm
{
    private void BuildMenus()
    {
        BuildFileMenu();
        BuildEditMenu();
        BuildProjectMenu();
        BuildDebugMenu();
        BuildViewMenu();
    }

    // ── File ────────────────────────────────────────────────────────────────

    private void BuildFileMenu()
    {
        ToolStripMenuItem miNewProject  = MenuItem("&New Project…",  () => _vm.NewProjectCommand.Execute(null));
        ToolStripMenuItem miOpenProject = MenuItem("&Open Project…", () => _vm.OpenProjectCommand.Execute(null));
        ToolStripMenuItem miCloseProject = MenuItem("&Close Project", () => _vm.CloseProjectCommand.Execute(null));
        ToolStripMenuItem miNewScene    = MenuItem("&New Scene…",    () => _vm.NewSceneCommand.Execute(null));
        ToolStripMenuItem miSaveScene   = MenuItem("&Save Scene\tCtrl+S",   () => _vm.SaveSceneCommand.Execute(null));
        ToolStripMenuItem miSaveSceneAs = MenuItem("Save Scene &As…",       () => _vm.SaveSceneAsCommand.Execute(null));
        ToolStripMenuItem miExit        = MenuItem("E&xit",          () => _vm.ExitCommand.Execute(null));

        ToolStripMenuItem miRecentHeader = new("Recent Projects") { Enabled = false };

        _mnuFile.DropDownItems.AddRange(new ToolStripItem[]
        {
            miNewProject, miOpenProject, miCloseProject,
            new ToolStripSeparator(),
            miNewScene, miSaveScene, miSaveSceneAs,
            new ToolStripSeparator(),
            miRecentHeader,
            new ToolStripSeparator(),
            miExit,
        });

        _mnuFile.DropDownOpening += (_, _) => RebuildRecentProjects(miRecentHeader);
    }

    private void RebuildRecentProjects(ToolStripMenuItem headerItem)
    {
        // Remove old dynamic items (items after headerItem)
        int headerIdx = _mnuFile.DropDownItems.IndexOf(headerItem);
        while (_mnuFile.DropDownItems.Count > headerIdx + 2
               && _mnuFile.DropDownItems[headerIdx + 1] is not ToolStripSeparator)
        {
            _mnuFile.DropDownItems.RemoveAt(headerIdx + 1);
        }

        IReadOnlyList<string> recents = _vm.Preferences.RecentProjects;
        for (int i = 0; i < recents.Count; i++)
        {
            string path = recents[i];
            string label = $"&{i + 1}  {System.IO.Path.GetFileName(path)}";
            ToolStripMenuItem mi = MenuItem(label, () => _vm.OpenProjectByPathCommand.Execute(path));
            mi.ToolTipText = path;
            _mnuFile.DropDownItems.Insert(headerIdx + 1 + i, mi);
        }
    }

    // ── Edit ────────────────────────────────────────────────────────────────

    private void BuildEditMenu()
    {
        _mnuEdit.DropDownItems.AddRange(new ToolStripItem[]
        {
            MenuItem("&Undo\tCtrl+Z",       () => _vm.UndoCommand.Execute(null)),
            MenuItem("&Redo\tCtrl+Y",       () => _vm.RedoCommand.Execute(null)),
            new ToolStripSeparator(),
            MenuItem("Cu&t\tCtrl+X",        () => _vm.CutCommand.Execute(null)),
            MenuItem("&Copy\tCtrl+C",       () => _vm.CopyCommand.Execute(null)),
            MenuItem("&Paste\tCtrl+V",      () => _vm.PasteCommand.Execute(null)),
            new ToolStripSeparator(),
            MenuItem("&Duplicate",          () => _vm.DuplicateSelectedCommand.Execute(null)),
            MenuItem("&Delete\tDelete",     () => _vm.DeleteSelectedCommand.Execute(null)),
            new ToolStripSeparator(),
            MenuItem("Select &All\tCtrl+A", () => _vm.SelectAllCommand.Execute(null)),
        });
    }

    // ── Project ─────────────────────────────────────────────────────────────

    private void BuildProjectMenu()
    {
        _mnuProject.DropDownItems.AddRange(new ToolStripItem[]
        {
            MenuItem("Project &Settings…",         () => _vm.OpenProjectSettingsCommand.Execute(null)),
            new ToolStripSeparator(),
            MenuItem("&Build Content\tCtrl+B",     () => _vm.BuildContentCommand.Execute(null)),
            MenuItem("Build &Solution",            () => _vm.BuildSolutionCommand.Execute(null)),
            MenuItem("&Generate Code",             () => _vm.GenerateCodeCommand.Execute(null)),
            new ToolStripSeparator(),
            MenuItem("&Run Game\tF5",              () => _vm.RunGameCommand.Execute(null)),
        });
    }

    // ── Debug ────────────────────────────────────────────────────────────────

    private void BuildDebugMenu()
    {
        _mnuDebug.DropDownItems.AddRange(new ToolStripItem[]
        {
            MenuItem("▶  &Play\tF5",   () => _vm.PlayCommand.Execute(null)),
            MenuItem("⏹  &Stop",       () => _vm.StopCommand.Execute(null)),
        });
    }

    // ── View ─────────────────────────────────────────────────────────────────

    private void BuildViewMenu()
    {
        ToolStripMenuItem miHierarchy = CheckMenuItem("&Hierarchy Panel",  () => ToggleHierarchy(), true);
        ToolStripMenuItem miInspector = CheckMenuItem("&Inspector Panel",  () => ToggleInspector(), true);
        ToolStripMenuItem miDock      = CheckMenuItem("&Dock Bar",         () => ToggleDock(),      true);

        _mnuView.DropDownItems.AddRange(new ToolStripItem[]
        {
            miHierarchy,
            miInspector,
            miDock,
        });

        _mnuView.DropDownOpening += (_, _) =>
        {
            miHierarchy.Checked = _hierarchyVisible;
            miInspector.Checked = _inspectorVisible;
            miDock.Checked      = _dockVisible;
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ToolStripMenuItem MenuItem(string text, Action onClick)
    {
        ToolStripMenuItem mi = new(text);
        mi.ForeColor = EditorColors.TextPrimary;
        mi.BackColor = EditorColors.PanelBackground;
        mi.Click    += (_, _) => onClick();
        return mi;
    }

    private static ToolStripMenuItem CheckMenuItem(string text, Action onClick, bool initialChecked)
    {
        ToolStripMenuItem mi = new(text) { CheckOnClick = false, Checked = initialChecked };
        mi.ForeColor = EditorColors.TextPrimary;
        mi.BackColor = EditorColors.PanelBackground;
        mi.Click    += (_, _) => onClick();
        return mi;
    }
}
