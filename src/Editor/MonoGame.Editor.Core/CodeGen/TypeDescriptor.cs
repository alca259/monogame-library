namespace MonoGame.Editor.Core.CodeGen;

/// <summary>Metadatos sobre una subclase de <c>GameBehaviour</c> descubierta.</summary>
public sealed record TypeDescriptor(
    string  FullName,
    string  ShortName,
    string  Namespace,
    string? SourceFilePath = null);
