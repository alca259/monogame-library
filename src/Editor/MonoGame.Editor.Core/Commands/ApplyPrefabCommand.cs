namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Guarda el estado actual de una instancia de <see cref="EditorGameObject"/> en su archivo de prefab,
/// sobrescribiendo la definición de prefab anterior.
/// </summary>
public sealed class ApplyPrefabCommand : IEditorCommand
{
    private readonly EditorGameObject _instance;
    private readonly string _prefabPath;
    private readonly IPrefabProvider _provider;
    private EditorGameObject? _snapshotBeforeApply;

    /// <param name="instance">Instancia cuyo estado se convertirá en la nueva definición de prefab.</param>
    /// <param name="prefabPath">Ruta al archivo <c>.prefab.json</c> de destino.</param>
    /// <param name="provider">Proveedor utilizado para cargar y guardar datos de prefab.</param>
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
