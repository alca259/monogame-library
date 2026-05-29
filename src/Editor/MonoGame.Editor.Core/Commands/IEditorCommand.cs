namespace MonoGame.Editor.Core.Commands;

/// <summary>Representa una operación de editor reversible que puede apilarse en un <see cref="CommandStack"/>.</summary>
public interface IEditorCommand
{
    /// <summary>Descripción legible que se muestra en los elementos de menú Editar → Deshacer/Rehacer.</summary>
    string Description { get; }

    /// <summary>Ejecuta la operación.</summary>
    void Execute();

    /// <summary>Revierte la operación, restaurando el estado anterior a la llamada a <see cref="Execute"/>.</summary>
    void Undo();
}
