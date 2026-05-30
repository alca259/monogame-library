using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Panel izquierdo: árbol de jerarquía de entidades de la escena activa.
/// Usa un <see cref="CollectionView"/> con una lista plana de <see cref="HierarchyItem"/>
/// que se reconstruye cuando cambia la escena, se deshace/rehace o se filtra.
/// La lógica de expansión persiste via <see cref="_expandedIds"/> entre reconstrucciones.
/// </summary>
public sealed partial class SceneHierarchyView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;
    private readonly ObservableCollection<HierarchyItem> _items = [];
    private readonly HashSet<Guid> _expandedIds = [];
    private string _searchFilter = string.Empty;

    private Action<SceneLoadedEvent>?        _onSceneLoaded;
    private Action<GameObjectSelectedEvent>? _onObjectSelected;
    private Action<UndoPerformedEvent>?      _onUndo;
    private Action<RedoPerformedEvent>?      _onRedo;

    public SceneHierarchyView()
    {
        InitializeComponent();
        HierarchyList.ItemsSource = _items;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    // ── EventBus ─────────────────────────────────────────────────────────────

    private void Subscribe()
    {
        _onSceneLoaded    = e => MainThread.BeginInvokeOnMainThread(() => { RebuildList(e.Scene); });
        _onObjectSelected = e => MainThread.BeginInvokeOnMainThread(() => SyncSelection(e.GameObject));
        _onUndo           = _ => MainThread.BeginInvokeOnMainThread(() => RebuildList());
        _onRedo           = _ => MainThread.BeginInvokeOnMainThread(() => RebuildList());

        _bus.Subscribe(_onSceneLoaded);
        _bus.Subscribe(_onObjectSelected);
        _bus.Subscribe(_onUndo);
        _bus.Subscribe(_onRedo);
    }

    private void Unsubscribe()
    {
        if (_onSceneLoaded    is not null) _bus.Unsubscribe(_onSceneLoaded);
        if (_onObjectSelected is not null) _bus.Unsubscribe(_onObjectSelected);
        if (_onUndo           is not null) _bus.Unsubscribe(_onUndo);
        if (_onRedo           is not null) _bus.Unsubscribe(_onRedo);
    }

    // ── List building ─────────────────────────────────────────────────────────

    private void RebuildList(EditorScene? scene = null)
    {
        scene ??= EditorContext.Instance.ActiveScene;

        _items.Clear();

        if (scene is null)
        {
            CountLabel.Text  = "0 entities";
            StatusLabel.Text = "0 objects in scene";
            return;
        }

        bool filtering = !string.IsNullOrEmpty(_searchFilter);
        int total = 0;

        foreach (EditorGameObject root in scene.RootGameObjects)
            FlattenInto(root, depth: 0, filtering, ref total);

        CountLabel.Text  = total == 1 ? "1 entity"        : $"{total} entities";
        StatusLabel.Text = total == 1 ? "1 object in scene" : $"{total} objects in scene";

        SyncSelection(EditorContext.Instance.SelectedObject);
    }

    private void FlattenInto(EditorGameObject obj, int depth, bool filtering, ref int total)
    {
        if (filtering && !MatchesFilter(obj, _searchFilter)) return;

        bool expanded = filtering || _expandedIds.Contains(obj.Id);
        HierarchyItem item = new(obj, depth, expanded,
            onToggleExpand: () =>
            {
                if (item.IsExpanded) _expandedIds.Add(obj.Id);
                else _expandedIds.Remove(obj.Id);
                RebuildList();
            },
            onRename: RenameItemAsync);

        _items.Add(item);
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
            HierarchyList.SelectedItem = null;
            RenameBtn.IsEnabled   = false;
            ReparentBtn.IsEnabled = false;
            return;
        }

        HierarchyItem? match = null;
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].GameObject.Id == selected.Id)
            {
                match = _items[i];
                break;
            }
        }

        HierarchyList.SelectedItem = match;
        RenameBtn.IsEnabled   = match is not null;
        ReparentBtn.IsEnabled = match is not null;
    }

    // ── CollectionView selection ──────────────────────────────────────────────

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not HierarchyItem item) return;
        EditorContext.Instance.SetSelection(item.GameObject);
    }

    // ── Toolbar — add / delete / rename ──────────────────────────────────────

    private void OnAddClicked(object sender, EventArgs e)
    {
        EditorScene? scene = EditorContext.Instance.ActiveScene;
        if (scene is null) return;

        EditorGameObject? parent = EditorContext.Instance.SelectedObject;
        if (parent is not null && !IsObjectInScene(parent, scene)) parent = null;

        int n = scene.RootGameObjects.Count + 1;
        EditorGameObject newObj = new() { Name = $"Entity {n}" };

        EditorContext.Instance.Commands.Execute(new CreateEntityCommand(newObj, scene, parent));
        EditorContext.Instance.SetSelection(newObj);
        RebuildList(scene);
    }

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        EditorScene? scene = EditorContext.Instance.ActiveScene;
        EditorGameObject? selected = EditorContext.Instance.SelectedObject;
        if (scene is null || selected is null) return;

        EditorContext.Instance.Commands.Execute(new DeleteEntityCommand(selected, scene));
        EditorContext.Instance.SetSelection(null);
        RebuildList(scene);
    }

    private async void OnRenameClicked(object sender, EventArgs e)
    {
        EditorGameObject? selected = EditorContext.Instance.SelectedObject;
        if (selected is null) return;

        HierarchyItem? item = null;
        for (int i = 0; i < _items.Count; i++)
            if (_items[i].GameObject.Id == selected.Id) { item = _items[i]; break; }

        if (item is not null)
            await RenameItemAsync(item);
    }

    private async Task RenameItemAsync(HierarchyItem item)
    {
        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? newName = await page.DisplayPromptAsync(
            "Rename entity",
            "Enter new name:",
            initialValue: item.DisplayName,
            maxLength: 128,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(newName) || newName == item.DisplayName) return;

        EditorContext.Instance.Commands.Execute(new RenameEntityCommand(item.GameObject, newName));
        RebuildList();
    }

    // ── Search ────────────────────────────────────────────────────────────────

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchFilter = e.NewTextValue ?? string.Empty;
        RebuildList();
    }

    // ── Reparent ──────────────────────────────────────────────────────────────

    private async void OnReparentClicked(object sender, EventArgs e)
    {
        EditorScene? scene = EditorContext.Instance.ActiveScene;
        EditorGameObject? selected = EditorContext.Instance.SelectedObject;
        if (scene is null || selected is null) return;

        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        // Collect valid parent candidates (all except the selected object and its descendants)
        List<EditorGameObject> candidates = CollectReparentCandidates(scene, selected);
        string[] options = new string[candidates.Count + 1];
        options[0] = "(Root — no parent)";
        for (int i = 0; i < candidates.Count; i++)
            options[i + 1] = candidates[i].Name;

        string? choice = await page.DisplayActionSheetAsync(
            "Reparent to:",
            "Cancel",
            null,
            options);

        if (choice is null or "Cancel") return;

        EditorGameObject? newParent = null;
        if (choice != options[0])
        {
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Name == choice) { newParent = candidates[i]; break; }
        }

        EditorContext.Instance.Commands.Execute(
            new ReparentEntityCommand(selected, scene, newParent));
        RebuildList(scene);
    }

    private static List<EditorGameObject> CollectReparentCandidates(
        EditorScene scene, EditorGameObject excluded)
    {
        List<EditorGameObject> result = [];
        foreach (EditorGameObject root in scene.RootGameObjects)
            CollectDescendants(root, excluded, result);
        return result;
    }

    private static void CollectDescendants(
        EditorGameObject obj, EditorGameObject excluded, List<EditorGameObject> result)
    {
        if (obj == excluded) return;
        result.Add(obj);
        foreach (EditorGameObject child in obj.Children)
            CollectDescendants(child, excluded, result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
