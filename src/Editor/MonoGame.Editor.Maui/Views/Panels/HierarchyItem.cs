namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Modelo de fila para el árbol de jerarquía (CollectionView plana).
/// Cada instancia representa un <see cref="EditorGameObject"/> aplanado en la
/// profundidad <see cref="Depth"/> con estado de expansión gestionado por la vista.
/// </summary>
public sealed class HierarchyItem
{
    private readonly Action _onToggleExpand;
    private readonly Func<HierarchyItem, Task> _onRename;
    private readonly Action<HierarchyItem> _onDragStart;
    private readonly Action<HierarchyItem> _onDrop;

    private static readonly Color PrefabColor = Color.FromArgb("#7EB8F7");
    private static readonly Color NormalColor = Color.FromArgb("#E6E6E8");
    private static readonly Color InactiveColor = Color.FromArgb("#5A5A62");

    public EditorGameObject GameObject { get; }
    public int Depth { get; }
    public bool IsExpanded { get; set; }
    public bool HasChildren => GameObject.Children.Count > 0;
    public bool IsLeaf => !HasChildren;

    public Thickness LeftPadding => new(Depth * 16 + 4, 2, 4, 2);
    public string ExpandIcon => HasChildren ? (IsExpanded ? "▾" : "▶") : string.Empty;
    public string DisplayName => GameObject.Name;

    public Color NameColor => !GameObject.Active
        ? InactiveColor
        : GameObject.PrefabPath is not null
            ? PrefabColor
            : NormalColor;

    public Command ToggleExpandCommand { get; }
    public Command RenameCommand { get; }
    public Command DragStartingCommand { get; }
    public Command DropCommand { get; }
    public Command PointerEnteredCommand { get; }

    private readonly Action<HierarchyItem> _onPointerEntered;

    public HierarchyItem(
        EditorGameObject obj,
        int depth,
        bool isExpanded,
        Action onToggleExpand,
        Func<HierarchyItem, Task> onRename,
        Action<HierarchyItem> onDragStart,
        Action<HierarchyItem> onDrop,
        Action<HierarchyItem> onPointerEntered)
    {
        GameObject = obj;
        Depth = depth;
        IsExpanded = isExpanded;
        _onToggleExpand = onToggleExpand;
        _onRename = onRename;
        _onDragStart = onDragStart;
        _onDrop = onDrop;
        _onPointerEntered = onPointerEntered;

        ToggleExpandCommand = new Command(() =>
        {
            IsExpanded = !IsExpanded;
            _onToggleExpand();
        });

        RenameCommand = new Command(async () => await _onRename(this));
        DragStartingCommand = new Command(() => _onDragStart(this));
        DropCommand = new Command(() => _onDrop(this));
        PointerEnteredCommand = new Command(() => _onPointerEntered(this));
    }
}
