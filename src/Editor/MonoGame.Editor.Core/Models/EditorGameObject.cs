namespace MonoGame.Editor.Core.Models;

/// <summary>Represents a game object node in the editor scene hierarchy.</summary>
public sealed class EditorGameObject
{
    /// <summary>Stable unique identifier for this object.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Display name shown in the hierarchy.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this object and its children are active.</summary>
    public bool Active { get; set; } = true;

    /// <summary>World-space position of this object.</summary>
    public EditorVector2 Position { get; set; } = EditorVector2.Zero;

    /// <summary>Rotation in degrees.</summary>
    public float Rotation { get; set; }

    /// <summary>Scale applied to this object and its children.</summary>
    public EditorVector2 Scale { get; set; } = EditorVector2.One;

    /// <summary>Local-space position relative to <see cref="Parent"/>.</summary>
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

    /// <summary>Local-space rotation in degrees relative to <see cref="Parent"/>.</summary>
    [JsonIgnore]
    public float LocalRotation
    {
        get => Parent is null ? Rotation : Rotation - Parent.Rotation;
        set => Rotation = Parent is null ? value : Parent.Rotation + value;
    }

    /// <summary>Local-space scale relative to <see cref="Parent"/>.</summary>
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

    /// <summary>Behaviours attached to this object.</summary>
    public List<EditorBehaviour> Behaviours { get; } = [];

    /// <summary>Child objects in the hierarchy.</summary>
    public List<EditorGameObject> Children { get; } = [];

    /// <summary>Parent object, or <c>null</c> if this is a root object. Excluded from serialization to avoid circular references.</summary>
    [JsonIgnore]
    public EditorGameObject? Parent { get; set; }

    /// <summary>
    /// Path to the source <c>.prefab.json</c> file when this object was instantiated from a prefab,
    /// or <c>null</c> if this is a plain game object.
    /// </summary>
    public string? PrefabPath { get; set; }

    /// <summary>User-defined tags. Serialized with the scene.</summary>
    public List<string> Tags { get; } = [];
}
