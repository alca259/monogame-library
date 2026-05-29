namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando cambia la ruta del .csproj del juego (nuevo proyecto abierto o ruta actualizada en la configuración).</summary>
public sealed record GameCsprojChangedEvent(EditorProject Project) : IEditorEvent;
