namespace MonoGame.Editor.Core.Models;

/// <summary>A 2D vector used in editor models. Maps to MonoGame's <c>Vector2</c> at the WinForms layer.</summary>
public record struct EditorVector2(float X, float Y)
{
    /// <summary>Zero vector (0, 0).</summary>
    public static readonly EditorVector2 Zero = new(0f, 0f);

    /// <summary>One vector (1, 1).</summary>
    public static readonly EditorVector2 One = new(1f, 1f);
}
