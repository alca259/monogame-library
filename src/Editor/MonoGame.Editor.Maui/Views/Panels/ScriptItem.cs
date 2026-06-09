namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>View model for a single .cs file row in the script browser list.</summary>
public sealed class ScriptItem
{
    /// <summary>File name with extension (e.g. "PlayerController.cs").</summary>
    public string FileName { get; }

    public Command TapCommand { get; }
    public Command RenameCommand { get; }
    public Command DeleteCommand { get; }

    public ScriptItem(string fileName, Action onTap, Action onRename, Action onDelete)
    {
        FileName = fileName;
        TapCommand = new Command(onTap);
        RenameCommand = new Command(onRename);
        DeleteCommand = new Command(onDelete);
    }
}
