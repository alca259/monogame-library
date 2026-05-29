namespace Alca.MonoGame.Kernel.Graphics.Shaders;

/// <summary>Describes the data type of a serializable material property.</summary>
public enum MaterialPropertyType
{
    /// <summary>Single floating-point scalar.</summary>
    Float,

    /// <summary>Two-component float vector.</summary>
    Vector2,

    /// <summary>Three-component float vector.</summary>
    Vector3,

    /// <summary>Four-component float vector.</summary>
    Vector4,

    /// <summary>RGBA color encoded as four floats (R, G, B, A) in the <see cref="MaterialProperty.Data"/> array.</summary>
    Color,

    /// <summary>Reference to a texture asset identified by its content-relative path.</summary>
    Texture2D,
}
