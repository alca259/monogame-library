namespace MonoGame.Editor.Core.Models;

/// <summary>Represents a scene open in the editor.</summary>
public sealed class EditorScene
{
    /// <summary>Display name of the scene.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the scene's <c>.json</c> file.</summary>
    public string ScenePath { get; set; } = string.Empty;

    /// <summary>Optional 2D world bounds in pixels. Zero = unbounded.</summary>
    public EditorVector2 WorldSize { get; set; } = EditorVector2.Zero;

    /// <summary>Top-level game objects in this scene (no parent).</summary>
    public List<EditorGameObject> RootGameObjects { get; } = [];
}
