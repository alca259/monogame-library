namespace MonoGame.Editor.Core.Attributes;

/// <summary>Inserta un separador de sección con título encima de la propiedad en el Inspector.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EditorHeaderAttribute : Attribute
{
    /// <summary>Texto del encabezado de sección.</summary>
    public string Title { get; }

    /// <param name="title">Texto que se mostrará como separador antes de esta propiedad.</param>
    public EditorHeaderAttribute(string title) => Title = title;
}
