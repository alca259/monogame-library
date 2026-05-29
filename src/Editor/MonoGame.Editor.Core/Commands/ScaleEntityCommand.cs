namespace MonoGame.Editor.Core.Commands;

/// <summary>Cambia la escala de un <see cref="EditorGameObject"/>.</summary>
public sealed class ScaleEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly EditorVector2 _previousScale;
    private readonly EditorVector2 _newScale;

    /// <param name="target">Objeto a escalar.</param>
    /// <param name="newScale">Escala de destino.</param>
    public ScaleEntityCommand(EditorGameObject target, EditorVector2 newScale)
    {
        _target = target;
        _previousScale = target.Scale;
        _newScale = newScale;
    }

    /// <param name="target">Objeto a escalar.</param>
    /// <param name="previousScale">Escala anterior al cambio (explícita, usada por el arrastre del gizmo).</param>
    /// <param name="newScale">Escala de destino.</param>
    public ScaleEntityCommand(EditorGameObject target, EditorVector2 previousScale, EditorVector2 newScale)
    {
        _target = target;
        _previousScale = previousScale;
        _newScale = newScale;
    }

    /// <inheritdoc/>
    public string Description => $"Scale '{_target.Name}'";

    /// <inheritdoc/>
    public void Execute() => _target.Scale = _newScale;

    /// <inheritdoc/>
    public void Undo() => _target.Scale = _previousScale;
}
