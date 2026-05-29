namespace MonoGame.Editor.Core.Models;

/// <summary>Representa un componente de comportamiento adjunto a un <see cref="EditorGameObject"/>.</summary>
public sealed class EditorBehaviour
{
    /// <summary>Nombre de tipo calificado con ensamblado de la subclase <c>GameBehaviour</c>.</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Valores de propiedades serializados indexados por nombre de propiedad.</summary>
    public Dictionary<string, JsonElement> Properties { get; } = [];

    /// <summary>Indica si este comportamiento está habilitado.</summary>
    public bool Enabled { get; set; } = true;
}
