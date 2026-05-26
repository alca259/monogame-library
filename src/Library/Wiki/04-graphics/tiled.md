# Mapas Tiled

**Namespace:** `Alca.MonoGame.Kernel.Graphics.Tiled`

El framework incluye un wrapper sobre `MonoGame.Extended.Tiled` para cargar y renderizar mapas creados con el editor [Tiled](https://www.mapeditor.org/).

---

## TiledMapRenderer

Renderiza un mapa `.tmx` de Tiled usando las capas de tiles configuradas.

### Uso básico

```csharp
// En LoadContent:
var tiledMap = Content.Load<TiledMap>("Maps/level1");
_mapRenderer = new TiledMapRenderer(Core.GraphicsDevice, tiledMap);

// En Update:
_mapRenderer.Update(gameTime);

// En Draw:
_mapRenderer.Draw(camera.GetTransformMatrix(viewport));
```

---

## TiledObjectLayer

Permite acceder a los objetos de una capa de objetos del mapa para generar colisiones, spawns, etc.

```csharp
var objectLayer = new TiledObjectLayer(tiledMap, "Collisions");

foreach (var obj in objectLayer.Objects)
{
    var collider = world.CreateEntity(obj.Name, obj.Position);
    collider.Add(new BoxCollider2D { Width = obj.Width, Height = obj.Height });
    collider.Add(new RigidBody2D { IsStatic = true });
}
```

---

## Ejemplo completo: nivel desde Tiled

```csharp
public sealed class LevelScene : Scene
{
    private TiledMapRenderer _mapRenderer = null!;
    private TiledMap _tiledMap = null!;

    protected override GameWorld? CreateWorld()
    {
        return new GameWorld
        {
            PhysicsWorld = new Physics2DWorld(new Vector2(0, 600f))
        };
    }

    public override void LoadContent()
    {
        _tiledMap    = Content.Load<TiledMap>("Maps/level1");
        _mapRenderer = new TiledMapRenderer(Core.GraphicsDevice, _tiledMap);
    }

    protected override void InitializeWorld()
    {
        // Leer capa de colisiones del mapa
        var collisionLayer = new TiledObjectLayer(_tiledMap, "Collisions");
        foreach (var obj in collisionLayer.Objects)
        {
            var e = World!.CreateEntity(obj.Name, obj.Position);
            e.Add(new BoxCollider2D { Width = obj.Width, Height = obj.Height });
            e.Add(new RigidBody2D { IsStatic = true });
        }

        // Leer spawns
        var spawnLayer = new TiledObjectLayer(_tiledMap, "Spawns");
        foreach (var spawn in spawnLayer.Objects)
        {
            if (spawn.Type == "player")
            {
                var player = World!.CreateEntity("Player", spawn.Position);
                player.AddComponent<PlayerController>();
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _mapRenderer.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        var transform = _camera.GetTransformMatrix(Core.GraphicsDevice.Viewport);
        _mapRenderer.Draw(transform);
        base.Draw(gameTime);
    }
}
```

---

## Notas

- Los mapas Tiled deben estar compilados con el Content Pipeline de MonoGame (extensión `.mgcb`).
- Las capas de tiles animados se actualizan en `_mapRenderer.Update(gameTime)`.
- Las coordenadas de los objetos de Tiled están en el espacio del mapa (píxeles desde el origen).

---

## Ver también

- [Física 2D →](../08-physics/overview.md)
- [Navegación →](../10-navigation/nav-grid.md)
