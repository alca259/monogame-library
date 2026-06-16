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

    /// <summary>Posición en espacio de mundo de este objeto (X, Y, Z).</summary>
    public EditorVector3 Position { get; set; } = EditorVector3.Zero;

    /// <summary>Rotación Euler en grados (X, Y, Z). Z es el eje de rotación habitual en vistas 2D frontales.</summary>
    public EditorVector3 Rotation { get; set; } = EditorVector3.Zero;

    /// <summary>Escala aplicada a este objeto (X, Y, Z).</summary>
    public EditorVector3 Scale { get; set; } = EditorVector3.One;

    /// <summary>Posición en espacio local relativa a <see cref="Parent"/>.</summary>
    [JsonIgnore]
    public EditorVector3 LocalPosition
    {
        get => Parent is null
            ? Position
            : new EditorVector3(
                Position.X - Parent.Position.X,
                Position.Y - Parent.Position.Y,
                Position.Z - Parent.Position.Z);
        set => Position = Parent is null
            ? value
            : new EditorVector3(
                Parent.Position.X + value.X,
                Parent.Position.Y + value.Y,
                Parent.Position.Z + value.Z);
    }

    /// <summary>Rotación en espacio local en grados relativa a <see cref="Parent"/>.</summary>
    [JsonIgnore]
    public EditorVector3 LocalRotation
    {
        get => Parent is null
            ? Rotation
            : new EditorVector3(
                Rotation.X - Parent.Rotation.X,
                Rotation.Y - Parent.Rotation.Y,
                Rotation.Z - Parent.Rotation.Z);
        set => Rotation = Parent is null
            ? value
            : new EditorVector3(
                Parent.Rotation.X + value.X,
                Parent.Rotation.Y + value.Y,
                Parent.Rotation.Z + value.Z);
    }

    /// <summary>Escala en espacio local relativa a <see cref="Parent"/>.</summary>
    [JsonIgnore]
    public EditorVector3 LocalScale
    {
        get
        {
            if (Parent is null) return Scale;

            float px = MathF.Abs(Parent.Scale.X) < 0.0001f ? 1f : Parent.Scale.X;
            float py = MathF.Abs(Parent.Scale.Y) < 0.0001f ? 1f : Parent.Scale.Y;
            float pz = MathF.Abs(Parent.Scale.Z) < 0.0001f ? 1f : Parent.Scale.Z;
            return new EditorVector3(Scale.X / px, Scale.Y / py, Scale.Z / pz);
        }
        set
        {
            if (Parent is null)
            {
                Scale = value;
                return;
            }

            Scale = new EditorVector3(
                Parent.Scale.X * value.X,
                Parent.Scale.Y * value.Y,
                Parent.Scale.Z * value.Z);
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
