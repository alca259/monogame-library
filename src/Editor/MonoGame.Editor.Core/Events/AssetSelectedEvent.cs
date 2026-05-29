using MonoGame.Editor.Core.Assets;

namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando se selecciona un asset en el explorador de assets.</summary>
/// <param name="Asset">La información del asset seleccionado, o <c>null</c> cuando se limpia la selección.</param>
public sealed record AssetSelectedEvent(AssetInfo? Asset) : IEditorEvent;
