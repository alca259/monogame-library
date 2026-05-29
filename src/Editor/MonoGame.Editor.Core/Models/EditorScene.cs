namespace MonoGame.Editor.Core.Models;

/// <summary>Representa una escena abierta en el editor.</summary>
public sealed class EditorScene
{
    /// <summary>Nombre de visualización de la escena.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Ruta absoluta al archivo <c>.json</c> de la escena.</summary>
    public string ScenePath { get; set; } = string.Empty;

    /// <summary>Límites opcionales del mundo 2D en píxeles. Cero = sin límites.</summary>
    public EditorVector2 WorldSize { get; set; } = EditorVector2.Zero;

    /// <summary>Configuración opcional de subsistemas para el GameWorld. Null = nuevo GameWorld() simple sin subsistemas.</summary>
    public EditorWorldConfig? WorldConfig { get; set; }

    /// <summary>Objetos de juego de nivel raíz en esta escena (sin padre).</summary>
    public List<EditorGameObject> RootGameObjects { get; } = [];
}
