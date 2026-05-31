namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>View model for a single .cs file row in the script browser list.</summary>
public sealed class ScriptItem
{
    private readonly Action _onTap;

    /// <summary>File name with extension (e.g. "PlayerController.cs").</summary>
    public string  FileName   { get; }

    /// <summary>Fired when the row is tapped — sets the selection in the parent view.</summary>
    public Command TapCommand { get; }

    public ScriptItem(string fileName, Action onTap)
    {
        FileName   = fileName;
        _onTap     = onTap;
        TapCommand = new Command(_onTap);
    }
}
