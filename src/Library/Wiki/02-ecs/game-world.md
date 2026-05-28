# GameWorld

**Namespace:** `Alca.MonoGame.Kernel.ECS`
**Equivalente a:** Scene de Unity (gestión de entidades)

`GameWorld` es el propietario de todas las entidades y el motor del ciclo ECS. También integra los subsistemas opcionales (física, iluminación, navegación, audio, networking).

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsEnabled` | `bool` | Si `false`, omite `Update` (pero Draw sigue ejecutándose) |
| `EntityCount` | `int` | Número de entidades activas en el mundo |
| `PhysicsWorld` | `Physics2DWorld?` | Simulación 2D; `Step` automático en cada `Update` |
| `LightingWorld` | `LightingWorld?` | Registro automático de `LightBehaviour` |
| `NavGrid` | `NavGrid?` | Rejilla de navegación para `NavAgent` |
| `Pathfinder` | `Pathfinder?` | A* síncrono |
| `AsyncPathfinder` | `AsyncPathfinder?` | A* en hilo de fondo |
| `NavPhysicsSync` | `NavGridPhysicsSync?` | Sincroniza NavGrid con Physics tras cada paso |
| `AudioController` | `AudioController?` | Necesario para `SpatialAudioSource` / `SpatialAudioListener` |
| `AudioMixer` | `AudioMixer?` | Routing de volumen por canal |
| `NetworkServer` | `NetworkServer?` | Servidor UDP; set por `NetworkManagerBehaviour` |
| `NetworkClient` | `NetworkClient?` | Cliente UDP; set por `NetworkManagerBehaviour` |
| `WeatherWorld` | `WeatherWorld?` | Simulación meteorológica; `Update` automático en cada frame |

---

## Creación de entidades

```csharp
// 2D — TransformBehaviour en posición XY (Z = 0)
GameEntity player = world.CreateEntity("Player", new Vector2(400, 300));

// 3D — TransformBehaviour en posición XYZ
GameEntity cube = world.CreateEntity("Cube", new Vector3(0, 1, 5));

// Sin posición (origen)
GameEntity manager = world.CreateEntity("GameManager");
```

La entidad **no es accesible** hasta el inicio del siguiente `Update` (creación diferida).

---

## Destrucción de entidades

```csharp
// Diferida: OnDestroy() se llama al inicio del siguiente Update
world.Destroy(enemyEntity);

// Inmediata: destruye todas las entidades ahora mismo
world.Destroy();
```

---

## Consultas (zero-alloc)

Usa siempre las sobrecargas que reciben un `List<T>` para evitar asignaciones:

```csharp
// Pre-asigna la lista en Awake/Start
private readonly List<GameEntity> _enemies = new(32);
private readonly List<HealthComponent> _healthComponents = new(32);

// Cada frame — zero-alloc
_enemies.Clear();
world.FindEntities<EnemyBehaviour>(_enemies);

_healthComponents.Clear();
world.FindComponents<HealthComponent>(_healthComponents);

// Por nombre (primera coincidencia)
GameEntity? boss = world.FindByName("FinalBoss");

// Por tag
private readonly List<GameEntity> _collectibles = new(16);
_collectibles.Clear();
world.GetEntitiesByTag("collectible", _collectibles);

// Por interfaz en todos los behaviours
private readonly List<IUpdatable> _updatables = new(64);
world.GetBehavioursWithInterface<IUpdatable>(_updatables);
```

> Las versiones que devuelven `IEnumerable<T>` están marcadas con `[Obsolete]` porque asignan un enumerador. Evítalas en el game loop.

---

## Subsistemas opcionales

Los subsistemas se asignan como propiedades del mundo antes de cargar las entidades:

```csharp
var world = new GameWorld
{
    PhysicsWorld = new Physics2DWorld(gravity: new Vector2(0, 500f)),
    LightingWorld = new LightingWorld(),
    NavGrid       = new NavGrid(mapWidth, mapHeight, cellSize),
    Pathfinder    = new Pathfinder(),
    AudioController = Core.Audio,
    AudioMixer    = new AudioMixer()
};
```

Una vez asignados, los componentes de las entidades los detectan automáticamente en su `Awake`.

---

## Ejemplo completo

```csharp
using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Physics;
using Alca.MonoGame.Kernel.Audio;
using Microsoft.Xna.Framework;

public sealed class GameplayScene : Scene
{
    protected override GameWorld? CreateWorld()
    {
        return new GameWorld
        {
            PhysicsWorld    = new Physics2DWorld(new Vector2(0, 600f)),
            LightingWorld   = new LightingWorld(),
            AudioController = Core.Audio,
            AudioMixer      = new AudioMixer()
        };
    }

    protected override void InitializeWorld()
    {
        // Suelo estático
        var ground = World!.CreateEntity("Ground", new Vector2(400, 560));
        ground.Add(new BoxCollider2D { Width = 800, Height = 32 });
        ground.Add(new RigidBody2D { IsStatic = true });

        // Jugador dinámico
        var player = World.CreateEntity("Player", new Vector2(400, 300));
        player
            .Add(new SpriteRendererBehaviour(Content.Load<Texture2D>("player")))
            .Add(new RigidBody2D { Mass = 1f })
            .Add(new BoxCollider2D { Width = 32, Height = 48 })
            .AddComponent<PlayerController>();

        // Luz de punto sobre el jugador
        var light = World.CreateEntity("PlayerLight");
        light.SetParent(player);
        light.Transform.LocalPosition2d = new Vector2(0, -16);
        light.Add(new PointLight2D { Radius = 200f, Color = Color.Orange });
    }
}
```

---

## Notas de rendimiento

- `FindEntities<T>(List<T>)` y `FindComponents<T>(List<T>)` son **O(n)** donde n es el número de entidades. No usar para búsquedas frecuentes — cachea los resultados.
- La destrucción masiva (`world.Destroy()`) es síncrona e inmediata — segura para llamarla desde `UnloadContent`.
- El orden de actualización de entidades es el orden en que fueron creadas (FIFO).

---

## Ver también

- [GameEntity →](game-entity.md)
- [ECS Overview →](overview.md)
- [Física 2D →](../08-physics/overview.md)
- [Iluminación →](../09-lighting/overview.md)
- [Navegación →](../10-navigation/overview.md)
