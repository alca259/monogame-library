namespace MonoGame.Editor.Core.Models;

/// <summary>Represents a behaviour component attached to an <see cref="EditorGameObject"/>.</summary>
public sealed class EditorBehaviour
{
    /// <summary>Assembly-qualified type name of the <c>GameBehaviour</c> subclass.</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Serialized property values keyed by property name.</summary>
    public Dictionary<string, JsonElement> Properties { get; } = [];

    /// <summary>Whether this behaviour is enabled.</summary>
    public bool Enabled { get; set; } = true;
}
