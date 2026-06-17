using CommunityToolkit.Mvvm.ComponentModel;

namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>
/// ViewModel del panel de jerarquía de escena.
/// Gestiona el estado del árbol (expansión, selección, filtro) y los comandos de entidad.
/// La vista (SceneHierarchyPanel) suscribe <see cref="RebuildRequested"/> y
/// <see cref="SelectionSyncRequested"/> para actualizar el TreeView.
/// </summary>
public sealed partial class SceneHierarchyViewModel : ViewModelBase
{
    private readonly HashSet<Guid> _expandedIds = [];
    private bool _syncingSelection;

    protected override EditorFocusContext? FocusContext => EditorFocusContext.Hierarchy;

    /// <summary>Solicita reconstruir el TreeView completo.</summary>
    public event Action? RebuildRequested;

    /// <summary>Solicita sincronizar la selección visual con el objeto dado (null = deseleccionar).</summary>
    public event Action<EditorGameObject?>? SelectionSyncRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private bool _hasScene;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    [NotifyPropertyChangedFor(nameof(CanRename))]
    [NotifyPropertyChangedFor(nameof(CanReparent))]
    private bool _hasSelection;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private string _statusText = "0 objects in scene";

    public bool CanDelete   => HasScene && HasSelection;
    public bool CanRename   => HasSelection;
    public bool CanReparent => HasSelection;

    /// <summary>Tabla de expansión compartida con el panel para restaurar estado entre rebuilds.</summary>
    public HashSet<Guid> ExpandedIds => _expandedIds;

    /// <summary>Bandera usada por la vista para evitar bucles de selección.</summary>
    public bool IsSyncingSelection
    {
        get => _syncingSelection;
        set => _syncingSelection = value;
    }

    // ── Eventos del bus ───────────────────────────────────────────────────────

    protected override void RegisterEvents()
    {
        On<SceneLoadedEvent>(_ => RebuildRequested?.Invoke());
        On<GameObjectSelectedEvent>(e => SelectionSyncRequested?.Invoke(e.GameObject));
        On<UndoPerformedEvent>(_ => RebuildRequested?.Invoke());
        On<RedoPerformedEvent>(_ => RebuildRequested?.Invoke());
    }

    protected override void OnAttached() => RebuildRequested?.Invoke();

    partial void OnSearchFilterChanged(string value) => RebuildRequested?.Invoke();

    // ── Estado ────────────────────────────────────────────────────────────────

    /// <summary>Llamado por la vista tras reconstruir el árbol para actualizar Has* y StatusText.</summary>
    public void UpdateSceneState()
    {
        EditorScene? scene = Context.ActiveScene;
        HasScene     = scene is not null;
        HasSelection = Context.SelectedObject is not null;

        if (scene is null) { StatusText = "0 objects in scene"; return; }
        int n = CountAll(scene.RootGameObjects);
        StatusText = n == 1 ? "1 object in scene" : $"{n} objects in scene";
    }

    private static int CountAll(List<EditorGameObject> list)
    {
        int n = 0;
        foreach (EditorGameObject o in list)
        {
            n++;
            n += CountAll(o.Children);
        }
        return n;
    }

    // ── Selección ─────────────────────────────────────────────────────────────

    /// <summary>Llamado por la vista cuando el usuario selecciona un nodo del TreeView.</summary>
    public void OnNodeSelected(EditorGameObject? obj)
    {
        if (_syncingSelection) return;
        Context.SetSelection(obj);
    }

    // ── Comandos ──────────────────────────────────────────────────────────────

    public void AddEntity()
    {
        EditorScene? scene = Context.ActiveScene;
        if (scene is null) return;

        EditorGameObject? parent = Context.SelectedObject;
        if (parent is not null && !IsObjectInScene(parent, scene)) parent = null;

        int n = scene.RootGameObjects.Count + 1;
        EditorGameObject newObj = new() { Name = $"Entity {n}" };
        Context.Commands.Execute(new CreateEntityCommand(newObj, scene, parent));
        Context.SetSelection(newObj);
        RebuildRequested?.Invoke();
    }

    public void DeleteEntity()
    {
        EditorScene? scene    = Context.ActiveScene;
        EditorGameObject? sel = Context.SelectedObject;
        if (scene is null || sel is null) return;

        Context.Commands.Execute(new DeleteEntityCommand(sel, scene));
        Context.SetSelection(null);
        RebuildRequested?.Invoke();
    }

    public async Task RenameAsync(EditorGameObject obj)
    {
        string? newName = await DialogService.PromptAsync(
            "Rename entity", "Enter new name:", initialValue: obj.Name, maxLength: 128);
        if (string.IsNullOrWhiteSpace(newName) || newName == obj.Name) return;

        Context.Commands.Execute(new RenameEntityCommand(obj, newName));
        RebuildRequested?.Invoke();
    }

    public async Task ReparentAsync()
    {
        EditorScene? scene    = Context.ActiveScene;
        EditorGameObject? sel = Context.SelectedObject;
        if (scene is null || sel is null) return;

        List<EditorGameObject> candidates = CollectReparentCandidates(scene, sel);
        string[] options = new string[candidates.Count + 1];
        options[0] = "(Root — no parent)";
        for (int i = 0; i < candidates.Count; i++) options[i + 1] = candidates[i].Name;

        string? choice = await DialogService.ActionSheetAsync("Reparent to:", "Cancel", null, options);
        if (choice is null or "Cancel") return;

        EditorGameObject? newParent = null;
        if (choice != options[0])
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Name == choice) { newParent = candidates[i]; break; }

        Context.Commands.Execute(new ReparentEntityCommand(sel, scene, newParent));
        RebuildRequested?.Invoke();
    }

    // ── Drag & drop ────────────────────────────────────────────────────────────

    internal EditorGameObject? DraggingObject { get; private set; }

    internal void StartDrag(EditorGameObject obj) => DraggingObject = obj;

    internal void HandleDrop(EditorGameObject target)
    {
        EditorGameObject? source = DraggingObject;
        DraggingObject = null;
        if (source is null || source == target) return;

        EditorScene? scene = Context.ActiveScene;
        if (scene is null) return;

        List<EditorGameObject> valid = CollectReparentCandidates(scene, source);
        if (!valid.Contains(target)) return;

        Context.Commands.Execute(new ReparentEntityCommand(source, scene, target));
        RebuildRequested?.Invoke();
    }

    // ── Helpers estáticos ─────────────────────────────────────────────────────

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
        if (scene.RootGameObjects.Contains(obj)) return true;
        foreach (EditorGameObject r in scene.RootGameObjects)
            if (ContainsChild(r, obj)) return true;
        return false;
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
