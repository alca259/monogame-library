namespace MonoGame.Editor.Core.CodeGen;

/// <summary>Result of a code generation operation.</summary>
public sealed record CodeGenResult(
    bool    Success,
    string  OutputPath,
    string? ErrorMessage = null);
