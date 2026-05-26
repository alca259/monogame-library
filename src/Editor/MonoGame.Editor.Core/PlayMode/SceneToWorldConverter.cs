using System.Reflection;
using Alca.MonoGame.Kernel.ECS;
using Microsoft.Xna.Framework;
using MonoGame.Editor.Core.Registry;

namespace MonoGame.Editor.Core.PlayMode;

/// <summary>Converts an <see cref="EditorScene"/> into a live <see cref="GameWorld"/> via reflection.</summary>
public static class SceneToWorldConverter
{
    /// <summary>Creates a <see cref="GameWorld"/> populated with entities mirroring the editor scene hierarchy.</summary>
    public static GameWorld Convert(EditorScene scene, GameObjectRegistry registry)
    {
        var world = new GameWorld();
        foreach (var root in scene.RootGameObjects)
            CreateRecursive(world, root, null, registry);
        return world;
    }

    private static void CreateRecursive(
        GameWorld world,
        EditorGameObject obj,
        GameEntity? parent,
        GameObjectRegistry registry)
    {
        var entity = world.CreateEntity(obj.Name, new Vector2(obj.Position.X, obj.Position.Y));

        ApplyTransform(entity, obj);

        if (parent is not null)
            entity.SetParent(parent);

        foreach (var b in obj.Behaviours)
        {
            if (b.Enabled)
                TryAddBehaviour(entity, b, registry);
        }

        foreach (var child in obj.Children)
            CreateRecursive(world, child, entity, registry);
    }

    private static void ApplyTransform(GameEntity entity, EditorGameObject obj)
    {
        TransformBehaviour? transform = entity.Transform;
        if (transform is null)
            return;

        transform.Position2d = new Vector2(obj.Position.X, obj.Position.Y);
        transform.Rotation2d = obj.Rotation;
        transform.LocalScale2d = new Vector2(obj.Scale.X, obj.Scale.Y);
    }

    private static void TryAddBehaviour(GameEntity entity, EditorBehaviour b, GameObjectRegistry registry)
    {
        // Resolve type by full name first, then by short/simple name
        Type? type = null;
        registry.RegisteredTypes.TryGetValue(b.TypeName, out type);

        if (type is null)
        {
            foreach (var kvp in registry.RegisteredTypes)
            {
                if (kvp.Value.Name == b.TypeName || kvp.Key.EndsWith('.' + b.TypeName))
                {
                    type = kvp.Value;
                    break;
                }
            }
        }

        if (type is null) return;

        try
        {
            var instance = (GameBehaviour)Activator.CreateInstance(type)!;
            ApplyProperties(instance, type, b.Properties);

            // Invoke entity.Add<ConcreteType>(instance) with the runtime type
            typeof(GameEntity)
                .GetMethod("Add")!
                .MakeGenericMethod(type)
                .Invoke(entity, [instance]);
        }
        catch
        {
            // Type lacks a parameterless constructor or Add threw — skip this behaviour.
        }
    }

    private static void ApplyProperties(
        GameBehaviour instance,
        Type type,
        Dictionary<string, JsonElement> props)
    {
        foreach (var (name, elem) in props)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanWrite) continue;
            try
            {
                object? value = DeserializeValue(elem, prop.PropertyType);
                if (value is not null)
                    prop.SetValue(instance, value);
            }
            catch { }
        }
    }

    private static object? DeserializeValue(JsonElement e, Type t)
    {
        if (t == typeof(float))   return e.GetSingle();
        if (t == typeof(double))  return e.GetDouble();
        if (t == typeof(int))     return e.GetInt32();
        if (t == typeof(bool))    return e.GetBoolean();
        if (t == typeof(string))  return e.GetString();

        if (t == typeof(Vector2))
            return new Vector2(e.GetProperty("X").GetSingle(), e.GetProperty("Y").GetSingle());

        if (t == typeof(Vector3))
            return new Vector3(
                e.GetProperty("X").GetSingle(),
                e.GetProperty("Y").GetSingle(),
                e.GetProperty("Z").GetSingle());

        if (t == typeof(Microsoft.Xna.Framework.Color))
            return new Microsoft.Xna.Framework.Color(
                e.GetProperty("R").GetInt32(),
                e.GetProperty("G").GetInt32(),
                e.GetProperty("B").GetInt32(),
                e.GetProperty("A").GetInt32());

        if (t.IsEnum && e.ValueKind == JsonValueKind.String)
            return Enum.Parse(t, e.GetString()!);

        return null;
    }
}
