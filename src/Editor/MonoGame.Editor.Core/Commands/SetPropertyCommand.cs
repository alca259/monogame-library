namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Comando genérico que establece cualquier propiedad de <typeparamref name="T"/> a un nuevo valor mediante un delegado.
/// Adecuado para ediciones de propiedades dirigidas por el inspector y actualizaciones de diccionarios de <see cref="EditorBehaviour"/>.
/// </summary>
public sealed class SetPropertyCommand<T> : IEditorCommand
{
    private readonly T _previousValue;
    private readonly T _newValue;
    private readonly Action<T> _setter;
    private readonly string _description;

    /// <param name="description">Descripción legible (p.ej. "Set Speed").</param>
    /// <param name="previousValue">Valor a restaurar al deshacer.</param>
    /// <param name="newValue">Valor a aplicar al ejecutar.</param>
    /// <param name="setter">Delegado que escribe el valor de vuelta en la propiedad de destino.</param>
    public SetPropertyCommand(string description, T previousValue, T newValue, Action<T> setter)
    {
        _description = description;
        _previousValue = previousValue;
        _newValue = newValue;
        _setter = setter;
    }

    /// <inheritdoc/>
    public string Description => _description;

    /// <inheritdoc/>
    public void Execute() => _setter(_newValue);

    /// <inheritdoc/>
    public void Undo() => _setter(_previousValue);
}
