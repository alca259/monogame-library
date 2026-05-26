namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Reverts a <see cref="EditorGameObject"/> instance to the definition stored in its prefab file,
/// discarding any local overrides.
/// </summary>
public sealed class RevertPrefabCommand : IEditorCommand
{
    private readonly EditorGameObject _instance;
    private readonly string _prefabPath;
    private readonly IPrefabProvider _provider;
    private EditorGameObject? _snapshotBeforeRevert;

    /// <param name="instance">Instance to revert.</param>
    /// <param name="prefabPath">Path to the source <c>.prefab.json</c> file.</param>
    /// <param name="provider">Provider used to load prefab data.</param>
    public RevertPrefabCommand(EditorGameObject instance, string prefabPath, IPrefabProvider provider)
    {
        _instance = instance;
        _prefabPath = prefabPath;
        _provider = provider;
    }

    /// <inheritdoc/>
    public string Description => $"Revert from Prefab '{System.IO.Path.GetFileName(_prefabPath)}'";

    /// <inheritdoc/>
    public void Execute()
    {
        // Snapshot the instance state BEFORE applying prefab data so Undo can restore it.
        _snapshotBeforeRevert = new EditorGameObject
        {
            Name = _instance.Name,
            Active = _instance.Active,
            Position = _instance.Position,
            Rotation = _instance.Rotation,
            Scale = _instance.Scale,
        };
        foreach (EditorBehaviour b in _instance.Behaviours)
            _snapshotBeforeRevert.Behaviours.Add(b);

        EditorGameObject prefab = _provider.LoadPrefab(_prefabPath)
            ?? throw new InvalidOperationException($"Prefab not found: {_prefabPath}");

        CopyProperties(prefab, _instance);
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_snapshotBeforeRevert is not null)
            CopyProperties(_snapshotBeforeRevert, _instance);
    }

    private static void CopyProperties(EditorGameObject source, EditorGameObject target)
    {
        target.Name = source.Name;
        target.Active = source.Active;
        target.Position = source.Position;
        target.Rotation = source.Rotation;
        target.Scale = source.Scale;

        target.Behaviours.Clear();
        foreach (EditorBehaviour b in source.Behaviours)
            target.Behaviours.Add(b);
    }
}
