using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGame.Editor.Maui.Views.Panels;
using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel del panel de jerarquía: mantiene una lista plana de
/// <see cref="HierarchyItem"/> que se reconstruye al cambiar la escena, deshacer/rehacer
/// o filtrar. La expansión persiste en <see cref="_expandedIds"/> entre reconstrucciones.
/// </summary>
public sealed partial class SceneHierarchyViewModel : ViewModelBase
{
    private readonly HashSet<Guid> _expandedIds = [];
    private bool _syncingSelection;

    protected override EditorFocusContext? FocusContext => EditorFocusContext.Hierarchy;

    public ObservableCollection<HierarchyItem> Items { get; } = [];

    [ObservableProperty]
    private HierarchyItem? _selectedItem;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddEntityCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteEntityCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReparentCommand))]
    private bool _hasScene;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteEntityCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReparentCommand))]
    private bool _hasSelection;

    [ObservableProperty]
    private string _countText = "0 entities";

    [ObservableProperty]
    private string _statusText = "0 objects in scene";

    protected override void RegisterEvents()
    {
        On<SceneLoadedEvent>(e => RebuildList(e.Scene));
        On<GameObjectSelectedEvent>(e => SyncSelection(e.GameObject));
        On<UndoPerformedEvent>(_ => RebuildList());
        On<RedoPerformedEvent>(_ => RebuildList());
    }

    protected override void OnAttached() => RebuildList();

    partial void OnSearchFilterChanged(string value) => RebuildList();

    partial void OnSelectedItemChanged(HierarchyItem? value)
    {
        if (_syncingSelection) return;
        if (value is not null) Context.SetSelection(value.GameObject);
    }

    // ── List building ─────────────────────────────────────────────────────────

    private void RebuildList(EditorScene? scene = null)
    {
        scene ??= Context.ActiveScene;

        _syncingSelection = true;
        Items.Clear();
        _syncingSelection = false;

        HasScene = scene is not null;

        if (scene is null)
        {
            CountText = "0 entities";
            StatusText = "0 objects in scene";
            HasSelection = false;
            return;
        }

        bool filtering = !string.IsNullOrEmpty(SearchFilter);
        int total = 0;

        foreach (EditorGameObject root in scene.RootGameObjects)
            FlattenInto(root, depth: 0, filtering, ref total);

        CountText = total == 1 ? "1 entity" : $"{total} entities";
        StatusText = total == 1 ? "1 object in scene" : $"{total} objects in scene";

        SyncSelection(Context.SelectedObject);
    }

    private void FlattenInto(EditorGameObject obj, int depth, bool filtering, ref int total)
    {
        if (filtering && !MatchesFilter(obj, SearchFilter)) return;

        bool expanded = filtering || _expandedIds.Contains(obj.Id);
        HierarchyItem? item = null;
        item = new(obj, depth, expanded,
            onToggleExpand: () =>
            {
                if (item!.IsExpanded) _expandedIds.Add(obj.Id);
                else _expandedIds.Remove(obj.Id);
                RebuildList();
            },
            onRename: RenameItemAsync);

        Items.Add(item);
        total++;

        if ((expanded || filtering) && obj.Children.Count > 0)
        {
            foreach (EditorGameObject child in obj.Children)
                FlattenInto(child, depth + 1, filtering, ref total);
        }
    }

    private static bool MatchesFilter(EditorGameObject obj, string filter)
    {
        if (obj.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) return true;
        foreach (EditorGameObject child in obj.Children)
            if (MatchesFilter(child, filter)) return true;
        return false;
    }

    private void SyncSelection(EditorGameObject? selected)
    {
        if (selected is null)
        {
            _syncingSelection = true;
            SelectedItem = null;
            _syncingSelection = false;
            HasSelection = false;
            return;
        }

        HierarchyItem? match = null;
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].GameObject.Id == selected.Id)
            {
                match = Items[i];
                break;
            }
        }

        _syncingSelection = true;
        SelectedItem = match;
        _syncingSelection = false;
        HasSelection = match is not null;
    }

    // ── Commands ────────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(HasScene))]
    private void AddEntity()
    {
        EditorScene? scene = Context.ActiveScene;
        if (scene is null) return;

        EditorGameObject? parent = Context.SelectedObject;
        if (parent is not null && !IsObjectInScene(parent, scene)) parent = null;

        int n = scene.RootGameObjects.Count + 1;
        EditorGameObject newObj = new() { Name = $"Entity {n}" };

        Context.Commands.Execute(new CreateEntityCommand(newObj, scene, parent));
        Context.SetSelection(newObj);
        RebuildList(scene);
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void DeleteEntity()
    {
        EditorScene? scene = Context.ActiveScene;
        EditorGameObject? selected = Context.SelectedObject;
        if (scene is null || selected is null) return;

        Context.Commands.Execute(new DeleteEntityCommand(selected, scene));
        Context.SetSelection(null);
        RebuildList(scene);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task RenameAsync()
    {
        if (SelectedItem is { } item)
            await RenameItemAsync(item);
    }

    private async Task RenameItemAsync(HierarchyItem item)
    {
        string? newName = await DialogService.PromptAsync(
            "Rename entity", "Enter new name:", initialValue: item.DisplayName, maxLength: 128);

        if (string.IsNullOrWhiteSpace(newName) || newName == item.DisplayName) return;

        Context.Commands.Execute(new RenameEntityCommand(item.GameObject, newName));
        RebuildList();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ReparentAsync()
    {
        EditorScene? scene = Context.ActiveScene;
        EditorGameObject? selected = Context.SelectedObject;
        if (scene is null || selected is null) return;

        List<EditorGameObject> candidates = CollectReparentCandidates(scene, selected);
        string[] options = new string[candidates.Count + 1];
        options[0] = "(Root — no parent)";
        for (int i = 0; i < candidates.Count; i++)
            options[i + 1] = candidates[i].Name;

        string? choice = await DialogService.ActionSheetAsync("Reparent to:", "Cancel", null, options);
        if (choice is null or "Cancel") return;

        EditorGameObject? newParent = null;
        if (choice != options[0])
        {
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Name == choice) { newParent = candidates[i]; break; }
        }

        Context.Commands.Execute(new ReparentEntityCommand(selected, scene, newParent));
        RebuildList(scene);
    }

    private bool CanDelete() => HasSelection && HasScene;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<EditorGameObject> CollectReparentCandidates(EditorScene scene, EditorGameObject excluded)
    {
        List<EditorGameObject> result = [];
        foreach (EditorGameObject root in scene.RootGameObjects)
            CollectDescendants(root, excluded, result);
        return result;
    }

    private static void CollectDescendants(EditorGameObject obj, EditorGameObject excluded, List<EditorGameObject> result)
    {
        if (obj == excluded) return;
        result.Add(obj);
        foreach (EditorGameObject child in obj.Children)
            CollectDescendants(child, excluded, result);
    }

    private static bool IsObjectInScene(EditorGameObject obj, EditorScene scene)
    {
        return scene.RootGameObjects.Contains(obj)
            || scene.RootGameObjects.Any(r => ContainsChild(r, obj));
    }

    private static bool ContainsChild(EditorGameObject parent, EditorGameObject target)
    {
        foreach (EditorGameObject child in parent.Children)
        {
            if (child == target) return true;
            if (ContainsChild(child, target)) return true;
        }
        return false;
    }
}
