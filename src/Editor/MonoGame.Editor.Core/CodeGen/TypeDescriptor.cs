namespace MonoGame.Editor.Core.CodeGen;

/// <summary>Metadata about a discovered <c>GameBehaviour</c> subclass.</summary>
public sealed record TypeDescriptor(
    string  FullName,
    string  ShortName,
    string  Namespace,
    string? SourceFilePath = null);
