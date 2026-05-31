using System.Reflection;
using Alca.MonoGame.Kernel.Audio;
using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Lighting;
using Alca.MonoGame.Kernel.Navigation;
using Alca.MonoGame.Kernel.Physics;
using Microsoft.Xna.Framework;

namespace MonoGame.Editor.Core.PlayMode;

/// <summary>Convierte un <see cref="EditorScene"/> en un <see cref="GameWorld"/> activo mediante reflexión.</summary>
public static class SceneToWorldConverter
{
    /// <summary>Crea un <see cref="GameWorld"/> poblado con entidades que reflejan la jerarquía de la escena del editor.</summary>
    public static GameWorld Convert(EditorScene scene, GameObjectRegistry registry)
    {
        var world = new GameWorld();
        ApplyWorldConfig(world, scene.WorldConfig);
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
        entity.Active = obj.Active;

        if (parent is not null)
            entity.SetParent(parent);

        foreach (var b in obj.Behaviours)
        {
            if (b.Enabled)
                TryAddBehaviour(entity, b, registry);
        }

        for (int i = 0; i < obj.Tags.Count; i++)
            entity.AddTag(obj.Tags[i]);

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
        // Resolver el tipo por nombre completo primero, luego por nombre corto/simple
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

        // SpriteRendererBehaviour requiere un argumento Texture2D en el constructor — no es instanciable en tiempo de edición.
        if (type.Name == "SpriteRendererBehaviour") return;

        try
        {
            var instance = (GameBehaviour)Activator.CreateInstance(type)!;
            ApplyProperties(instance, type, b.Properties);

            // Invocar entity.Add<TipoConcreto>(instance) con el tipo en tiempo de ejecución
            typeof(GameEntity)
                .GetMethod("Add")!
                .MakeGenericMethod(type)
                .Invoke(entity, [instance]);
        }
        catch
        {
            // El tipo carece de constructor sin parámetros o Add lanzó una excepción — omitir este GameBehaviour.
        }
    }

    private static void ApplyWorldConfig(GameWorld world, EditorWorldConfig? cfg)
    {
        if (cfg is null) return;

        if (cfg.UsePhysics2D)
            world.PhysicsWorld = new Physics2DWorld(new Vector2(cfg.GravityX, cfg.GravityY));

        if (cfg.UseLighting)
        {
            int[] c = cfg.AmbientColorRgba;
            world.LightingWorld = new LightingWorld
            {
                AmbientColor = new Microsoft.Xna.Framework.Color(c[0], c[1], c[2], c[3])
            };
        }

        if (cfg.UseNavigation)
        {
            int cap = cfg.NavGridWidth * cfg.NavGridHeight;
            world.NavGrid = new NavGrid(cfg.NavGridWidth, cfg.NavGridHeight, cfg.NavGridCellSize,
                new Vector2(cfg.NavGridOriginX, cfg.NavGridOriginY));
            world.Pathfinder = new Pathfinder(cap);
        }

        if (cfg.UseAudio)
            world.AudioController = new AudioController();
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
