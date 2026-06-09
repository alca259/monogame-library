namespace MonoGame.Editor.Core.Commands;

/// <summary>Reemplaza la lista de etiquetas de un objeto de juego (totalmente deshacible).</summary>
public sealed class SetTagsCommand : IEditorCommand
{
    private readonly EditorGameObject _obj;
    private readonly List<string> _oldTags;
    private readonly List<string> _newTags;

    /// <summary>Crea un comando para reemplazar las etiquetas de <paramref name="obj"/> con <paramref name="newTags"/>.</summary>
    public SetTagsCommand(EditorGameObject obj, IEnumerable<string> newTags)
    {
        _obj = obj;
        _oldTags = [.. obj.Tags];
        _newTags = [.. newTags];
    }

    /// <inheritdoc/>
    public string Description => "Set Tags";

    /// <inheritdoc/>
    public void Execute() => ApplyTags(_newTags);

    /// <inheritdoc/>
    public void Undo() => ApplyTags(_oldTags);

    private void ApplyTags(List<string> tags)
    {
        _obj.Tags.Clear();
        _obj.Tags.AddRange(tags);
    }
}
