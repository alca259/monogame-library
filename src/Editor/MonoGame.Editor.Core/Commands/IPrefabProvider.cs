namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Abstraction over prefab serialization/deserialization used by
/// <see cref="ApplyPrefabCommand"/> and <see cref="RevertPrefabCommand"/>.
/// Implemented by <c>PrefabManager</c> in Fase 7.
/// </summary>
public interface IPrefabProvider
{
    /// <summary>Loads the prefab at <paramref name="prefabPath"/> and returns a snapshot, or <c>null</c> if not found.</summary>
    EditorGameObject? LoadPrefab(string prefabPath);

    /// <summary>Serializes <paramref name="obj"/> to <paramref name="prefabPath"/>, overwriting any existing prefab.</summary>
    void SavePrefab(EditorGameObject obj, string prefabPath);
}
