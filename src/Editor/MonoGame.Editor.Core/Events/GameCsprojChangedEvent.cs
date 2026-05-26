namespace MonoGame.Editor.Core.Events;

/// <summary>Published when the game .csproj path changes (new project opened or path updated in settings).</summary>
public sealed record GameCsprojChangedEvent(EditorProject Project) : IEditorEvent;
