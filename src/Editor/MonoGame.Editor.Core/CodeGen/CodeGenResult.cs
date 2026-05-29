namespace MonoGame.Editor.Core.CodeGen;

/// <summary>Resultado de una operación de generación de código.</summary>
public sealed record CodeGenResult(
    bool    Success,
    string  OutputPath,
    string? ErrorMessage = null);
