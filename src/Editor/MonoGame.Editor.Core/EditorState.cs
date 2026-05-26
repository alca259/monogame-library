namespace MonoGame.Editor.Core;

/// <summary>Represents the current operational state of the editor.</summary>
public enum EditorState
{
    /// <summary>Editor is active; game loop is stopped, gizmos are visible.</summary>
    Editing,

    /// <summary>Game loop is running with the game's own camera.</summary>
    Playing,

    /// <summary>Game loop is paused; render is active, Update does not execute.</summary>
    Paused,
}
