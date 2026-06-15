namespace MonoGame.Editor.Core.Models;

/// <summary>Vector 3D utilizado en los modelos del editor. Se corresponde con <c>Vector3</c> de MonoGame en la capa de runtime.</summary>
public record struct EditorVector3(float X, float Y, float Z)
{
    /// <summary>Vector cero (0, 0, 0).</summary>
    public static readonly EditorVector3 Zero = new(0f, 0f, 0f);

    /// <summary>Vector unitario (1, 1, 1).</summary>
    public static readonly EditorVector3 One = new(1f, 1f, 1f);
}
