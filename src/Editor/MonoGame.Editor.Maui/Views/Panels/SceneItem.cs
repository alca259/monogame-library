namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>Modelo de fila para la lista de escenas del dock Scenes.</summary>
public sealed class SceneItem
{
    private readonly Func<SceneItem, Task> _onLoad;

    public string Name { get; }
    public string FilePath { get; }
    public bool IsDirty { get; init; }

    public string DisplayName => IsDirty ? $"{Name} ●" : Name;

    public Command LoadCommand { get; }

    public SceneItem(string filePath, Func<SceneItem, Task> onLoad, bool isDirty = false)
    {
        FilePath = filePath;
        IsDirty = isDirty;
        _onLoad = onLoad;

        string fn = Path.GetFileName(filePath);
        Name = fn.EndsWith(".scene.json", StringComparison.OrdinalIgnoreCase)
            ? fn[..^".scene.json".Length]
            : Path.GetFileNameWithoutExtension(fn);

        LoadCommand = new Command(async () => await _onLoad(this));
    }
}
