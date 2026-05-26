namespace MonoGame.Editor.Core.Commands;

/// <summary>Represents a reversible editor operation that can be pushed onto a <see cref="CommandStack"/>.</summary>
public interface IEditorCommand
{
    /// <summary>Human-readable description shown in Edit → Undo/Redo menu items.</summary>
    string Description { get; }

    /// <summary>Performs the operation.</summary>
    void Execute();

    /// <summary>Reverses the operation, restoring the state before <see cref="Execute"/> was called.</summary>
    void Undo();
}
