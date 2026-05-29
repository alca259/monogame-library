namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Abstracción sobre la serialización/deserialización de prefabs utilizada por
/// <see cref="ApplyPrefabCommand"/> y <see cref="RevertPrefabCommand"/>.
/// Implementada por <c>PrefabManager</c> en la Fase 7.
/// </summary>
public interface IPrefabProvider
{
    /// <summary>Carga el prefab en <paramref name="prefabPath"/> y devuelve una instantánea, o <c>null</c> si no se encuentra.</summary>
    EditorGameObject? LoadPrefab(string prefabPath);

    /// <summary>Serializa <paramref name="obj"/> en <paramref name="prefabPath"/>, sobrescribiendo cualquier prefab existente.</summary>
    void SavePrefab(EditorGameObject obj, string prefabPath);
}
