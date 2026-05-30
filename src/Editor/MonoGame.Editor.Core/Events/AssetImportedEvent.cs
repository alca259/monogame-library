namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando el <see cref="ContentWatcher"/> detecta un asset nuevo o modificado.</summary>
/// <param name="Asset">Descriptor del asset importado o modificado.</param>
public sealed record AssetImportedEvent(AssetInfo Asset) : IEditorEvent;
