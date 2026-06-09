namespace MonoGame.Editor.Maui.Views;

public sealed partial class EditorWindow
{
    #region Dropdown colors

    private static readonly Color DropdownItemFg         = Color.FromArgb("#E6E6E8");
    private static readonly Color DropdownItemBg         = Colors.Transparent;
    private static readonly Color DropdownItemHoverBg    = Color.FromArgb("#2E2E34");
    private static readonly Color DropdownSeparatorColor = Color.FromArgb("#34343A");

    #endregion

    #region Dropdown item model

    private sealed record DropdownItem(
        string Label,
        bool IsSeparator = false,
        Action? Action = null,
        IReadOnlyList<DropdownItem>? Children = null)
    {
        public bool HasChildren => Children is { Count: > 0 };
    }

    #endregion

    #region Dropdown state

    private string? _openMenuTag;

    #endregion

    #region Menu bar — dropdown management

    private void OnFileMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "File") { HideDropdown(); return; }
        ShowDropdown("File", 4, BuildFileMenuItems());
    }

    private void OnEditMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "Edit") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + 4);
        ShowDropdown("Edit", offsetX, BuildEditMenuItems());
    }

    private void OnProjectMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "Project") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + EditMenuBtn.Width + 4);
        ShowDropdown("Project", offsetX, BuildProjectMenuItems());
    }

    private void OnDebugMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "Debug") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + EditMenuBtn.Width + ProjectMenuBtn.Width + 4);
        ShowDropdown("Debug", offsetX, BuildDebugMenuItems());
    }

    private void OnViewMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "View") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + EditMenuBtn.Width + ProjectMenuBtn.Width + DebugMenuBtn.Width + 4);
        ShowDropdown("View", offsetX, BuildViewMenuItems());
    }

    private void OnMenuOverlayTapped(object? sender, TappedEventArgs e) => HideDropdown();

    private const double DropdownRowHeight = 32;
    private const double DropdownSepHeight = 5;   // HeightRequest=1 + Margin top/bottom=2+2
    private const double DropdownTopMargin = 28;

    private void ShowDropdown(string tag, int offsetX, IEnumerable<DropdownItem> items)
    {
        EditorContext.Instance.SetFocus(EditorFocusContext.Global);
        _openMenuTag = tag;
        DropdownStack.Children.Clear();
        HideSubDropdown();

        double cumulativeY = DropdownTopMargin;

        foreach (DropdownItem item in items)
        {
            if (item.IsSeparator)
            {
                cumulativeY += DropdownSepHeight;
                DropdownStack.Children.Add(new BoxView
                {
                    HeightRequest = 1,
                    Color         = DropdownSeparatorColor,
                    Margin        = new Thickness(8, 2),
                });
                continue;
            }

            bool isDisabled = item.Action is null && !item.HasChildren;
            double rowY = cumulativeY;
            cumulativeY += DropdownRowHeight;

            var row = new Grid
            {
                BackgroundColor      = DropdownItemBg,
                Padding              = new Thickness(16, 6),
                MinimumHeightRequest = DropdownRowHeight,
            };

            if (item.HasChildren)
            {
                row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(24, GridUnitType.Absolute)));
            }

            row.Add(new Label
            {
                Text                    = item.Label,
                TextColor               = isDisabled ? Color.FromArgb("#6A6A72") : DropdownItemFg,
                FontSize                = 13,
                VerticalTextAlignment   = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalOptions         = LayoutOptions.Fill,
            }, 0, 0);

            if (item.HasChildren)
            {
                row.Add(new Label
                {
                    Text                    = "›",
                    TextColor               = Color.FromArgb("#9A9AA2"),
                    FontSize                = 14,
                    VerticalTextAlignment   = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions         = LayoutOptions.Fill,
                }, 1, 0);

                var capturedChildren = item.Children!;
                double capturedY = rowY;
                var pointer = new PointerGestureRecognizer();
                pointer.PointerEntered += (_, _) =>
                {
                    row.BackgroundColor = DropdownItemHoverBg;
                    ShowSubDropdown(capturedChildren, offsetX + DropdownPanel.Width, capturedY);
                };
                row.GestureRecognizers.Add(pointer);
            }
            else
            {
                if (!isDisabled)
                {
                    var captured = item.Action!;
                    var tap = new TapGestureRecognizer();
                    tap.Tapped += (_, _) => { HideDropdown(); captured(); };
                    row.GestureRecognizers.Add(tap);
                }

                var pointer = new PointerGestureRecognizer();
                pointer.PointerEntered += (_, _) => { row.BackgroundColor = DropdownItemHoverBg; HideSubDropdown(); };
                pointer.PointerExited  += (_, _) => row.BackgroundColor = DropdownItemBg;
                row.GestureRecognizers.Add(pointer);
            }

            DropdownStack.Children.Add(row);
        }

        DropdownPanel.Margin = new Thickness(offsetX, DropdownTopMargin, 0, 0);
        MenuOverlay.IsVisible = true;
    }

    private void ShowSubDropdown(IReadOnlyList<DropdownItem> items, double x, double y)
    {
        SubDropdownStack.Children.Clear();

        foreach (DropdownItem item in items)
        {
            bool isDisabled = item.Action is null;

            var row = new Grid
            {
                BackgroundColor      = DropdownItemBg,
                Padding              = new Thickness(16, 6),
                MinimumHeightRequest = 32,
            };

            row.Add(new Label
            {
                Text                    = item.Label,
                TextColor               = isDisabled ? Color.FromArgb("#6A6A72") : DropdownItemFg,
                FontSize                = 13,
                VerticalTextAlignment   = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalOptions         = LayoutOptions.Fill,
            });

            if (!isDisabled)
            {
                var captured = item.Action!;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => { HideDropdown(); captured(); };
                row.GestureRecognizers.Add(tap);

                var pointer = new PointerGestureRecognizer();
                pointer.PointerEntered += (_, _) => row.BackgroundColor = DropdownItemHoverBg;
                pointer.PointerExited  += (_, _) => row.BackgroundColor = DropdownItemBg;
                row.GestureRecognizers.Add(pointer);
            }

            SubDropdownStack.Children.Add(row);
        }

        SubDropdownPanel.Margin = new Thickness(x, y, 0, 0);
        SubDropdownPanel.IsVisible = true;
    }

    private void HideSubDropdown()
    {
        SubDropdownStack.Children.Clear();
        SubDropdownPanel.IsVisible = false;
    }

    private void HideDropdown()
    {
        HideSubDropdown();
        MenuOverlay.IsVisible = false;
        _openMenuTag = null;
    }

    #endregion

    #region Menu item builders

    private IEnumerable<DropdownItem> BuildFileMenuItems()
    {
        bool hasProject = EditorContext.Instance.ActiveProject is not null;
        bool hasScene   = EditorContext.Instance.ActiveScene is not null;

        yield return new DropdownItem("New Project…",  Action: () => _vm.NewProjectCommand.Execute(null));
        yield return new DropdownItem("Open Project…", Action: () => _vm.OpenProjectCommand.Execute(null));

        if (_vm.Preferences.RecentProjects.Count > 0)
        {
            List<DropdownItem> recentItems = [];
            foreach (string path in _vm.Preferences.RecentProjects)
            {
                string captured = path;
                string label    = Path.GetFileName(captured.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                recentItems.Add(new DropdownItem(label, Action: () => _ = _vm.OpenProjectByPathAsync(captured)));
            }
            yield return new DropdownItem("Recent Projects", Children: recentItems);
        }

        yield return new DropdownItem("---",            IsSeparator: true);
        yield return new DropdownItem("Close Project",  Action: hasProject ? () => _vm.CloseProjectCommand.Execute(null) : null);
        yield return new DropdownItem("---",            IsSeparator: true);
        yield return new DropdownItem("New Scene",      Action: hasProject ? () => _vm.NewSceneCommand.Execute(null)    : null);
        yield return new DropdownItem("Save Scene",     Action: hasScene   ? () => _ = _vm.SaveSceneAsync()             : null);
        yield return new DropdownItem("Save Scene As…", Action: hasScene   ? () => _ = _vm.SaveSceneAsAsync()           : null);
        yield return new DropdownItem("---",            IsSeparator: true);
        yield return new DropdownItem("Exit",           Action: () => _vm.ExitCommand.Execute(null));
    }

    private IEnumerable<DropdownItem> BuildEditMenuItems()
    {
        bool hasScene     = EditorContext.Instance.ActiveScene is not null;
        bool hasSelection = EditorContext.Instance.SelectedObject is not null;
        bool hasClipboard = EditorContext.Instance.ClipboardEntity is not null;

        yield return new DropdownItem("Undo",       Action: hasScene                 ? () => _vm.UndoCommand.Execute(null)              : null);
        yield return new DropdownItem("Redo",       Action: hasScene                 ? () => _vm.RedoCommand.Execute(null)              : null);
        yield return new DropdownItem("---",        IsSeparator: true);
        yield return new DropdownItem("Cut",        Action: hasSelection             ? () => _vm.CutCommand.Execute(null)               : null);
        yield return new DropdownItem("Copy",       Action: hasSelection             ? () => _vm.CopyCommand.Execute(null)              : null);
        yield return new DropdownItem("Paste",      Action: hasScene && hasClipboard ? () => _vm.PasteCommand.Execute(null)             : null);
        yield return new DropdownItem("Duplicate",  Action: hasSelection             ? () => _vm.DuplicateSelectedCommand.Execute(null) : null);
        yield return new DropdownItem("Delete",     Action: hasSelection             ? () => _vm.DeleteSelectedCommand.Execute(null)    : null);
        yield return new DropdownItem("---",        IsSeparator: true);
        yield return new DropdownItem("Select All", Action: hasScene                 ? () => _vm.SelectAllCommand.Execute(null)         : null);
    }

    private IEnumerable<DropdownItem> BuildProjectMenuItems()
    {
        bool hasProject = EditorContext.Instance.ActiveProject is not null;
        bool hasScene   = EditorContext.Instance.ActiveScene is not null;

        yield return new DropdownItem("Project Settings…", Action: hasProject             ? () => _vm.OpenProjectSettingsCommand.Execute(null) : null);
        yield return new DropdownItem("---",               IsSeparator: true);
        yield return new DropdownItem("Build Content",     Action: hasProject             ? () => _ = _vm.BuildContentAsync()                  : null);
        yield return new DropdownItem("Build Solution",    Action: hasProject             ? () => _ = _vm.BuildSolutionAsync()                 : null);
        yield return new DropdownItem("Generate Code",     Action: hasProject && hasScene ? () => _ = _vm.GenerateCodeAsync()                  : null);
        yield return new DropdownItem("---",               IsSeparator: true);
        yield return new DropdownItem("Run",               Action: hasProject             ? () => _vm.RunGameCommand.Execute(null)             : null);
    }

    private IEnumerable<DropdownItem> BuildDebugMenuItems()
    {
        bool hasScene  = EditorContext.Instance.ActiveScene is not null;
        bool isPlaying = EditorContext.Instance.State is EditorState.Playing;

        yield return new DropdownItem("Play", Action: hasScene && !isPlaying ? _vm.Play : null);
        yield return new DropdownItem("Stop", Action: isPlaying              ? _vm.Stop : null);
    }

    private IEnumerable<DropdownItem> BuildViewMenuItems()
    {
        string hPfx = _hierarchyVisible ? "✓ " : "  ";
        string iPfx = _inspectorVisible ? "✓ " : "  ";
        string dPfx = _dockVisible      ? "✓ " : "  ";

        yield return new DropdownItem($"{hPfx}Hierarchy",   Action: ToggleHierarchy);
        yield return new DropdownItem($"{iPfx}Inspector",   Action: ToggleInspector);
        yield return new DropdownItem($"{dPfx}Bottom Dock", Action: ToggleDock);
        yield return new DropdownItem("---",                IsSeparator: true);
        yield return new DropdownItem("Reset Layout",       Action: ResetLayout);
    }

    #endregion
}
