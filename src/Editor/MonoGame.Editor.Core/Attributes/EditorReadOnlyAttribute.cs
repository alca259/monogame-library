namespace MonoGame.Editor.Core.Attributes;

/// <summary>Muestra la propiedad en el Inspector pero sin permitir edición.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EditorReadOnlyAttribute : Attribute { }
