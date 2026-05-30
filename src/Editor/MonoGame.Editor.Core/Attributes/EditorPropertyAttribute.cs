namespace MonoGame.Editor.Core.Attributes;

/// <summary>
/// Marca una propiedad pública de una subclase de <c>GameBehaviour</c> como visible y editable
/// en el panel Inspector del editor.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EditorPropertyAttribute : Attribute
{
    /// <summary>Etiqueta personalizada que se muestra junto al control. Por defecto usa el nombre de la propiedad.</summary>
    public string? Label { get; init; }

    /// <summary>Valor mínimo permitido (para tipos numéricos).</summary>
    public float Min { get; init; } = float.MinValue;

    /// <summary>Valor máximo permitido (para tipos numéricos).</summary>
    public float Max { get; init; } = float.MaxValue;

    /// <summary>Texto de información emergente que se muestra al pasar el cursor.</summary>
    public string? Tooltip { get; init; }

    /// <summary>
    /// Agrupa propiedades en una fila de dos columnas. Las propiedades con el mismo valor no nulo
    /// se renderizan una junto a la otra. Valor 0 (por defecto) significa apilado normal.
    /// </summary>
    public int SideBySideGroup { get; init; } = 0;
}
