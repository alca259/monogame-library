namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Revierte una instancia de <see cref="EditorGameObject"/> a la definición almacenada en su archivo de prefab,
/// descartando cualquier sobreescritura local.
/// </summary>
public sealed class RevertPrefabCommand : IEditorCommand
{
    private readonly EditorGameObject _instance;
    private readonly string _prefabPath;
    private readonly IPrefabProvider _provider;
    private EditorGameObject? _snapshotBeforeRevert;

    /// <param name="instance">Instancia a revertir.</param>
    /// <param name="prefabPath">Ruta al archivo <c>.prefab.json</c> fuente.</param>
    /// <param name="provider">Proveedor utilizado para cargar datos de prefab.</param>
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
        // Tomar instantánea del estado de la instancia ANTES de aplicar los datos del prefab para que Deshacer pueda restaurarlos.
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
