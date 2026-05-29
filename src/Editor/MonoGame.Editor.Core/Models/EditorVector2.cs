namespace MonoGame.Editor.Core.Models;

/// <summary>Vector 2D utilizado en los modelos del editor. Se corresponde con <c>Vector2</c> de MonoGame en la capa de WinForms.</summary>
public record struct EditorVector2(float X, float Y)
{
    /// <summary>Vector cero (0, 0).</summary>
    public static readonly EditorVector2 Zero = new(0f, 0f);

    /// <summary>Vector unitario (1, 1).</summary>
    public static readonly EditorVector2 One = new(1f, 1f);
}
