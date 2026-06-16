namespace MonoGame.Editor.Core.Attributes;

/// <summary>Renderiza una propiedad numérica como slider con el rango indicado.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EditorRangeAttribute : Attribute
{
    /// <summary>Valor mínimo del slider.</summary>
    public float Min { get; }

    /// <summary>Valor máximo del slider.</summary>
    public float Max { get; }

    /// <param name="min">Límite inferior del rango.</param>
    /// <param name="max">Límite superior del rango.</param>
    public EditorRangeAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}
