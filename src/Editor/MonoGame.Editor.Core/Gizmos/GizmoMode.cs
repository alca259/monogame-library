namespace MonoGame.Editor.Core.Gizmos;

/// <summary>Active transform tool in the editor viewport.</summary>
public enum GizmoMode
{
    /// <summary>Selection only; no transform handles visible.</summary>
    Select,

    /// <summary>Translate the selected object along one or both axes.</summary>
    Move,

    /// <summary>Rotate the selected object around its pivot.</summary>
    Rotate,

    /// <summary>Scale the selected object uniformly or per-axis.</summary>
    Scale,
}
