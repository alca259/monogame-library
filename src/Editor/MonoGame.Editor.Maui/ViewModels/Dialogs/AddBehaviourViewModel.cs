using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGame.Editor.Maui.Views.Dialogs;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>
/// ViewModel del diálogo "Add Behaviour": árbol de namespaces de tipos de behaviour
/// registrados (con búsqueda y rescan). Devuelve el nombre completo del tipo elegido.
/// </summary>
public sealed partial class AddBehaviourViewModel : DialogViewModel<string>
{
    private readonly HashSet<string> _expandedNamespaces = new(StringComparer.Ordinal);

    private GameObjectRegistry _registry = new();
    private Func<Task>? _rescanCallback;
    private string? _selectedEntry;
    private bool _syncing;

    public ObservableCollection<BehaviourTreeNode> Filtered { get; } = [];

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private BehaviourTreeNode? _selectedNode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private bool _canAdd;

    [ObservableProperty]
    private string _rescanText = "⟳ Rescan";

    [ObservableProperty]
    private bool _rescanEnabled = true;

    /// <summary>Inicializa el diálogo con el registro de tipos y un callback opcional de rescan.</summary>
    public void Initialize(GameObjectRegistry registry, Func<Task>? rescanCallback)
    {
        _registry       = registry;
        _rescanCallback = rescanCallback;
        BuildTree();
    }

    partial void OnSearchFilterChanged(string value) => BuildTree();

    partial void OnSelectedNodeChanged(BehaviourTreeNode? value)
    {
        if (_syncing) return;

        if (value is null)
        {
            _selectedEntry = null;
            CanAdd = false;
            return;
        }

        if (!value.IsLeaf)
        {
            if (_expandedNamespaces.Contains(value.FullKey)) _expandedNamespaces.Remove(value.FullKey);
            else _expandedNamespaces.Add(value.FullKey);

            _selectedEntry = null;
            CanAdd = false;
            BuildTree();
            return;
        }

        _selectedEntry = value.FullKey;
        CanAdd = true;
    }

    [RelayCommand]
    private async Task RescanAsync()
    {
        if (_rescanCallback is null) return;

        RescanEnabled = false;
        RescanText    = "⟳ …";
        try
        {
            await _rescanCallback().ConfigureAwait(true);
        }
        finally
        {
            RescanEnabled = true;
            RescanText    = "⟳ Rescan";
        }

        _expandedNamespaces.Clear();
        BuildTree();
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void Submit()
    {
        if (_selectedEntry is null) return;
        Close(_selectedEntry);
    }

    // ── Tree building ─────────────────────────────────────────────────────────

    private void BuildTree()
    {
        _syncing = true;
        Filtered.Clear();
        SelectedNode = null;
        _syncing = false;

        _selectedEntry = null;
        CanAdd = false;

        if (!string.IsNullOrEmpty(SearchFilter))
        {
            foreach (string key in _registry.RegisteredTypes.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                if (key.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase))
                    Filtered.Add(new BehaviourTreeNode(key, key, false, 0));
            }
            foreach (string pending in _registry.PendingTypeNames.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                string display = $"~ {pending}";
                if (display.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase))
                    Filtered.Add(new BehaviourTreeNode(display, pending, true, 0));
            }
            return;
        }

        NamespaceNode root = BuildNamespaceTree(_registry.RegisteredTypes.Keys);

        if (_expandedNamespaces.Count == 0 && root.Children.Count > 0)
        {
            List<string> allPaths = [];
            CollectNamespacePaths(root.Children, allPaths);
            for (int i = 0; i < allPaths.Count - 1; i++)
                _expandedNamespaces.Add(allPaths[i]);
        }

        FlattenNamespaceTree(root.Children, root.Types, Filtered, 0);

        foreach (string pending in _registry.PendingTypeNames.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            Filtered.Add(new BehaviourTreeNode($"~ {pending}", pending, true, 0));
    }

    private static void CollectNamespacePaths(Dictionary<string, NamespaceNode> nodes, List<string> paths)
    {
        foreach (KeyValuePair<string, NamespaceNode> kv in nodes.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            paths.Add(kv.Value.FullPath);
            CollectNamespacePaths(kv.Value.Children, paths);
        }
    }

    private static NamespaceNode BuildNamespaceTree(IEnumerable<string> typeNames)
    {
        var root = new NamespaceNode(string.Empty);

        foreach (string typeName in typeNames.OrderBy(n => n, StringComparer.Ordinal))
        {
            string[] parts = typeName.Split('.');
            NamespaceNode current = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                string nsPath = string.Join('.', parts, 0, i + 1);
                if (!current.Children.TryGetValue(parts[i], out NamespaceNode? child))
                {
                    child = new NamespaceNode(nsPath);
                    current.Children[parts[i]] = child;
                }
                current = child;
            }

            current.Types.Add((parts[^1], typeName));
        }

        return root;
    }

    private void FlattenNamespaceTree(
        Dictionary<string, NamespaceNode> nodes,
        List<(string ShortName, string FullTypeName)> directTypes,
        ObservableCollection<BehaviourTreeNode> output,
        int depth)
    {
        foreach ((string shortName, string fullTypeName) in directTypes.OrderBy(t => t.ShortName, StringComparer.Ordinal))
            output.Add(new BehaviourTreeNode(shortName, fullTypeName, false, depth));

        foreach (KeyValuePair<string, NamespaceNode> kv in nodes.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            NamespaceNode node        = kv.Value;
            bool          isExpanded  = _expandedNamespaces.Contains(node.FullPath);
            bool          hasChildren = node.Children.Count > 0 || node.Types.Count > 0;

            output.Add(new BehaviourTreeNode(kv.Key, node.FullPath, hasChildren, isExpanded, depth));

            if (isExpanded)
                FlattenNamespaceTree(node.Children, node.Types, output, depth + 1);
        }
    }

    private sealed class NamespaceNode(string fullPath)
    {
        public string FullPath { get; } = fullPath;
        public Dictionary<string, NamespaceNode> Children { get; } = new(StringComparer.Ordinal);
        public List<(string ShortName, string FullTypeName)> Types { get; } = [];
    }
}
