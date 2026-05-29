using MonoGame.Editor.Core.Assets;

namespace MonoGame.Editor.Core.Events;

/// <summary>Published when an asset is selected in the asset browser.</summary>
/// <param name="Asset">The selected asset info, or <c>null</c> when the selection is cleared.</param>
public sealed record AssetSelectedEvent(AssetInfo? Asset) : IEditorEvent;
