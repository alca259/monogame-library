namespace MonoGame.Editor.Core.Attributes;

/// <summary>
/// Marca una clase que hereda de <c>BehaviourEditor</c> como editor personalizado
/// para el tipo de Behaviour indicado. El <c>BehaviourEditorRegistry</c> escanea
/// los ensamblados cargados en busca de este atributo al iniciar el editor.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomBehaviourEditorAttribute : Attribute
{
    /// <summary>
    /// Tipo concreto del Behaviour (cuando el ensamblado está referenciado en compilación).
    /// Usa <see cref="TargetTypeName"/> cuando el tipo no es accesible en tiempo de compilación.
    /// </summary>
    public Type? TargetType { get; }

    /// <summary>Nombre completo del tipo del Behaviour (p. ej. <c>"Alca.MonoGame.Kernel.Audio.Ambient.AudioZoneBehaviour"</c>).
    /// Permite registrar editores para tipos de paquetes que no están referenciados directamente.</summary>
    public string? TargetTypeName { get; }

    /// <param name="targetType">Tipo del Behaviour (requiere referencia en compilación).</param>
    public CustomBehaviourEditorAttribute(Type targetType) => TargetType = targetType;

    /// <param name="targetTypeName">Nombre completo del tipo del Behaviour (sin necesidad de referencia en compilación).</param>
    public CustomBehaviourEditorAttribute(string targetTypeName) => TargetTypeName = targetTypeName;
}
