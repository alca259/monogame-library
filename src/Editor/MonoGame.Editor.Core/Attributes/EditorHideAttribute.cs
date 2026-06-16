namespace MonoGame.Editor.Core.Attributes;

/// <summary>Oculta la propiedad en el Inspector. El valor sigue serializándose en la escena con su valor por defecto.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EditorHideAttribute : Attribute { }
