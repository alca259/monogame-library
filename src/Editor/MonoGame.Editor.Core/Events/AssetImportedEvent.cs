using MonoGame.Editor.Core.Assets;

namespace MonoGame.Editor.Core.Events;

/// <summary>Published when a new or modified asset is detected by the <see cref="ContentWatcher"/>.</summary>
/// <param name="Asset">Descriptor for the imported or changed asset.</param>
public sealed record AssetImportedEvent(AssetInfo Asset) : IEditorEvent;
