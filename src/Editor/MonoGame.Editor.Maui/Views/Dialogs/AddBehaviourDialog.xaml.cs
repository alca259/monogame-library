using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed partial class AddBehaviourDialog : ContentPage
{
    private readonly TaskCompletionSource<string?> _tcs = new();
    private readonly ObservableCollection<BehaviourTreeNode> _filtered = [];
    private readonly HashSet<string> _expandedNamespaces = new(StringComparer.Ordinal);

    private GameObjectRegistry _registry = new();
    private Func<Task>? _rescanCallback;
    private string? _selectedEntry;
    private string _currentFilter = string.Empty;

    private AddBehaviourDialog() => InitializeComponent();

    public static async Task<string?> ShowAsync(INavigation navigation,
                                                 GameObjectRegistry registry,
                                                 Func<Task>? rescanCallback = null)
    {
        var dialog = new AddBehaviourDialog();
        dialog._registry       = registry;
        dialog._rescanCallback = rescanCallback;
        dialog.TypeList.ItemsSource = dialog._filtered;
        dialog.BuildTree();
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    // ── Tree building ─────────────────────────────────────────────────────────

    private void BuildTree()
    {
        _filtered.Clear();
        _selectedEntry      = null;
        AddButton.IsEnabled = false;

        if (!string.IsNullOrEmpty(_currentFilter))
        {
            // Flat filtered list — full type names for clarity when searching
            foreach (string key in _registry.RegisteredTypes.Keys
                         .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                if (key.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase))
                    _filtered.Add(new BehaviourTreeNode(key, key, false, 0));
            }
            foreach (string pending in _registry.PendingTypeNames
                         .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                string display = $"~ {pending}";
                if (display.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase))
                    _filtered.Add(new BehaviourTreeNode(display, pending, true, 0));
            }
            return;
        }

        // Namespace tree
        NamespaceNode root = BuildNamespaceTree(_registry.RegisteredTypes.Keys);

        if (_expandedNamespaces.Count == 0 && root.Children.Count > 0)
        {
            List<string> allPaths = [];
            CollectNamespacePaths(root.Children, allPaths);
            for (int i = 0; i < allPaths.Count - 1; i++)
                _expandedNamespaces.Add(allPaths[i]);
        }

        FlattenNamespaceTree(root.Children, root.Types, _filtered, 0);

        // Pending types as flat entries at the bottom
        foreach (string pending in _registry.PendingTypeNames
                     .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            _filtered.Add(new BehaviourTreeNode($"~ {pending}", pending, true, 0));
        }
    }

    private static void CollectNamespacePaths(
        Dictionary<string, NamespaceNode> nodes,
        List<string> paths)
    {
        foreach (KeyValuePair<string, NamespaceNode> kv in
                     nodes.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            paths.Add(kv.Value.FullPath);
            CollectNamespacePaths(kv.Value.Children, paths);
        }
    }

    private NamespaceNode BuildNamespaceTree(IEnumerable<string> typeNames)
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
        // Direct types at this level first
        foreach ((string shortName, string fullTypeName) in directTypes
                     .OrderBy(t => t.ShortName, StringComparer.Ordinal))
        {
            output.Add(new BehaviourTreeNode(shortName, fullTypeName, false, depth));
        }

        // Namespace children
        foreach (KeyValuePair<string, NamespaceNode> kv in nodes
                     .OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            NamespaceNode node       = kv.Value;
            bool          isExpanded = _expandedNamespaces.Contains(node.FullPath);
            bool          hasChildren = node.Children.Count > 0 || node.Types.Count > 0;

            output.Add(new BehaviourTreeNode(
                kv.Key, node.FullPath, hasChildren, isExpanded, depth));

            if (isExpanded)
                FlattenNamespaceTree(node.Children, node.Types, output, depth + 1);
        }
    }

    // ── Search ────────────────────────────────────────────────────────────────

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        _currentFilter = e.NewTextValue ?? string.Empty;
        BuildTree();
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    private void OnTypeSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not BehaviourTreeNode node)
        {
            _selectedEntry      = null;
            AddButton.IsEnabled = false;
            return;
        }

        if (!node.IsLeaf)
        {
            // Toggle namespace expansion using its full path stored in FullKey
            if (_expandedNamespaces.Contains(node.FullKey))
                _expandedNamespaces.Remove(node.FullKey);
            else
                _expandedNamespaces.Add(node.FullKey);

            TypeList.SelectedItem = null;
            _selectedEntry        = null;
            AddButton.IsEnabled   = false;
            BuildTree();
            return;
        }

        _selectedEntry      = node.FullKey;
        AddButton.IsEnabled = _selectedEntry is not null;
    }

    // ── Rescan ────────────────────────────────────────────────────────────────

    private async void OnRescanClicked(object sender, EventArgs e)
    {
        if (_rescanCallback is null) return;

        RescanButton.IsEnabled = false;
        RescanButton.Text      = "⟳ …";

        try
        {
            await _rescanCallback().ConfigureAwait(true);
        }
        finally
        {
            RescanButton.IsEnabled = true;
            RescanButton.Text      = "⟳ Rescan";
        }

        _expandedNamespaces.Clear();
        _currentFilter = SearchEntry.Text ?? string.Empty;
        BuildTree();
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnCancel(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        _ = Navigation.PopModalAsync();
    }

    private void OnSubmit(object sender, EventArgs e)
    {
        if (_selectedEntry is null) return;
        _tcs.TrySetResult(_selectedEntry);
        _ = Navigation.PopModalAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class NamespaceNode
    {
        public string FullPath { get; }
        public Dictionary<string, NamespaceNode> Children { get; } =
            new(StringComparer.Ordinal);
        public List<(string ShortName, string FullTypeName)> Types { get; } = [];

        public NamespaceNode(string fullPath) => FullPath = fullPath;
    }
}
