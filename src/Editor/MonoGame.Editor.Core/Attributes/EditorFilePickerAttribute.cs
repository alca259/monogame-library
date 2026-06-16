namespace MonoGame.Editor.Core.Attributes;

/// <summary>
/// Renderiza una propiedad <c>string</c> como campo de ruta con botón de selección de fichero
/// usando <c>RelativePathPickerDialog</c>. La ruta se almacena relativa a la raíz del proyecto.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EditorFilePickerAttribute : Attribute
{
    /// <summary>Extensiones de fichero permitidas (p. ej. <c>".png"</c>, <c>".xnb"</c>). <c>null</c> o vacío = cualquier fichero.</summary>
    public string[]? Extensions { get; }

    /// <param name="extensions">Extensiones permitidas, incluyendo el punto.</param>
    public EditorFilePickerAttribute(params string[] extensions)
        => Extensions = extensions.Length > 0 ? extensions : null;
}
