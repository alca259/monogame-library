namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Saves the current state of a <see cref="EditorGameObject"/> instance to its prefab file,
/// overwriting the previous prefab definition.
/// </summary>
public sealed class ApplyPrefabCommand : IEditorCommand
{
    private readonly EditorGameObject _instance;
    private readonly string _prefabPath;
    private readonly IPrefabProvider _provider;
    private EditorGameObject? _snapshotBeforeApply;

    /// <param name="instance">Instance whose state will become the new prefab definition.</param>
    /// <param name="prefabPath">Path to the target <c>.prefab.json</c> file.</param>
    /// <param name="provider">Provider used to load and save prefab data.</param>
    public ApplyPrefabCommand(EditorGameObject instance, string prefabPath, IPrefabProvider provider)
    {
        _instance = instance;
        _prefabPath = prefabPath;
        _provider = provider;
    }

    /// <inheritdoc/>
    public string Description => $"Apply Prefab '{System.IO.Path.GetFileName(_prefabPath)}'";

    /// <inheritdoc/>
    public void Execute()
    {
        _snapshotBeforeApply = _provider.LoadPrefab(_prefabPath);
        _provider.SavePrefab(_instance, _prefabPath);
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_snapshotBeforeApply is not null)
            _provider.SavePrefab(_snapshotBeforeApply, _prefabPath);
    }
}
