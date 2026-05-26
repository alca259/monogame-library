using MonoGame.Editor.Core.Localization;

namespace MonoGame.Editor.Core.Events;

/// <summary>Published when a localization model has been loaded from disk.</summary>
public sealed record LocalizationLoadedEvent(LocalizationEditorModel Model) : IEditorEvent;
