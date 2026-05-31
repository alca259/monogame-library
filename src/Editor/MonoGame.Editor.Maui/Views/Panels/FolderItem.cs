namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Modelo de fila para el árbol de carpetas del asset browser (CollectionView plana).
/// Mirrors the <see cref="HierarchyItem"/> expansion pattern with depth-based left padding.
/// </summary>
public sealed class FolderItem
{
    private readonly Action _onToggle;

    public string    Name        { get; }
    public string    FullPath    { get; }
    public int       Depth       { get; }
    public bool      HasChildren { get; }
    public bool      IsExpanded  { get; set; }
    public bool      IsRoot      { get; }
    public bool      IsNotRoot   => !IsRoot;

    public Thickness LeftPadding => new(Depth * 14 + 4, 2, 4, 2);
    public string    ExpandIcon  => HasChildren ? (IsExpanded ? "▾" : "▶") : string.Empty;

    public Command ToggleExpandCommand { get; }
    public Command RenameCommand       { get; }
    public Command DeleteCommand       { get; }

    public FolderItem(string fullPath, int depth, bool hasChildren, bool isExpanded,
                      bool isRoot, Action onToggle, Action onRename, Action onDelete)
    {
        FullPath    = fullPath;
        Name        = Path.GetFileName(fullPath) is { Length: > 0 } n ? n : fullPath;
        Depth       = depth;
        HasChildren = hasChildren;
        IsExpanded  = isExpanded;
        IsRoot      = isRoot;
        _onToggle   = onToggle;

        ToggleExpandCommand = new Command(() =>
        {
            IsExpanded = !IsExpanded;
            _onToggle();
        });
        RenameCommand = new Command(onRename, () => !isRoot);
        DeleteCommand = new Command(onDelete, () => !isRoot);
    }
}
