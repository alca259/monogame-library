namespace MonoGame.Editor.Core.Models;

/// <summary>Representa un nodo de objeto de juego en la jerarquía de escena del editor.</summary>
public sealed class EditorGameObject
{
    /// <summary>Identificador único estable para este objeto.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Nombre de visualización que aparece en la jerarquía.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Indica si este objeto y sus hijos están activos.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Posición en espacio de mundo de este objeto.</summary>
    public EditorVector2 Position { get; set; } = EditorVector2.Zero;

    /// <summary>Profundidad en modo 2.5D (orden de paralaje). Se omite del JSON cuando es cero.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float PositionZ { get; set; }

    /// <summary>Rotación en grados.</summary>
    public float Rotation { get; set; }

    /// <summary>Escala aplicada a este objeto y sus hijos.</summary>
    public EditorVector2 Scale { get; set; } = EditorVector2.One;

    /// <summary>Posición en espacio local relativa a <see cref="Parent"/>.</summary>
    [JsonIgnore]
    public EditorVector2 LocalPosition
    {
        get => Parent is null
            ? Position
            : new EditorVector2(Position.X - Parent.Position.X, Position.Y - Parent.Position.Y);
        set => Position = Parent is null
            ? value
            : new EditorVector2(Parent.Position.X + value.X, Parent.Position.Y + value.Y);
    }

    /// <summary>Rotación en espacio local en grados relativa a <see cref="Parent"/>.</summary>
    [JsonIgnore]
    public float LocalRotation
    {
        get => Parent is null ? Rotation : Rotation - Parent.Rotation;
        set => Rotation = Parent is null ? value : Parent.Rotation + value;
    }

    /// <summary>Escala en espacio local relativa a <see cref="Parent"/>.</summary>
    [JsonIgnore]
    public EditorVector2 LocalScale
    {
        get
        {
            if (Parent is null) return Scale;

            float px = MathF.Abs(Parent.Scale.X) < 0.0001f ? 1f : Parent.Scale.X;
            float py = MathF.Abs(Parent.Scale.Y) < 0.0001f ? 1f : Parent.Scale.Y;
            return new EditorVector2(Scale.X / px, Scale.Y / py);
        }
        set
        {
            if (Parent is null)
            {
                Scale = value;
                return;
            }

            Scale = new EditorVector2(Parent.Scale.X * value.X, Parent.Scale.Y * value.Y);
        }
    }

    /// <summary>Comportamientos adjuntos a este objeto.</summary>
    public List<EditorBehaviour> Behaviours { get; } = [];

    /// <summary>Objetos hijos en la jerarquía.</summary>
    public List<EditorGameObject> Children { get; } = [];

    /// <summary>Objeto padre, o <c>null</c> si este es un objeto raíz. Excluido de la serialización para evitar referencias circulares.</summary>
    [JsonIgnore]
    public EditorGameObject? Parent { get; set; }

    /// <summary>
    /// Ruta al archivo fuente <c>.prefab.json</c> cuando este objeto fue instanciado desde un prefab,
    /// o <c>null</c> si es un objeto de juego simple.
    /// </summary>
    public string? PrefabPath { get; set; }

    /// <summary>Etiquetas definidas por el usuario. Se serializan con la escena.</summary>
    public List<string> Tags { get; } = [];
}
