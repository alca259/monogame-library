namespace MonoGame.Editor.Core.Attributes;

/// <summary>
/// Cuando se aplica a una propiedad <c>float</c> que representa un radio en unidades de mundo,
/// el viewport del editor dibuja un círculo visual centrado en el objeto con ese radio.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EditorRadiusPreviewAttribute : Attribute { }
