# ECS — Visión General

**Namespace:** `Alca.MonoGame.Kernel.ECS`

El sistema ECS (*Entity-Component-System*) de la librería sigue el patrón de composición sobre herencia. En lugar de crear jerarquías de clases para distintos tipos de objetos del juego, cada objeto es una **entidad** (`GameEntity`) que contiene **comportamientos** (`GameBehaviour`). Un **mundo** (`GameWorld`) posee todas las entidades y conduce el ciclo de actualización.

---

## Diagrama de relaciones

```
GameWorld
  ├── GameEntity "Player"
  │     ├── TransformBehaviour   ← siempre presente; posición/rotación/escala
  │     ├── SpriteRendererBehaviour
  │     ├── PlayerController     ← tu lógica de juego
  │     └── RigidBody2D
  │
  ├── GameEntity "Enemy"
  │     ├── TransformBehaviour
  │     ├── AnimatedSpriteBehaviour
  │     └── NavAgent
  │
  └── GameEntity "Camera"
        ├── TransformBehaviour
        └── CameraController

Subsistemas opcionales en GameWorld:
  Physics2DWorld ─── RigidBody2D / Colliders
  LightingWorld  ─── LightBehaviour (PointLight2D, etc.)
  NavGrid        ─── NavAgent / Pathfinder
  AudioController ── SpatialAudioSource
  NetworkServer  ─── NetworkIdentity
```

---

## Ciclo de vida de un GameBehaviour

```
GameEntity.Add(behaviour)
  └─> Awake()       ← sincrónico, inmediato al añadir

Primer frame donde la entidad es activa:
  └─> Start()       ← antes del primer Update

Cada frame (si el método está sobreescrito):
  ├─> Update(GameTime)
  └─> Draw(GameTime, SpriteBatch)

Al destruir la entidad:
  └─> OnDestroy()
```

> `Update` y `Draw` **solo se invocan** si la subclase los sobreescribe. Esto se detecta por reflexión una única vez al añadir el comportamiento — no hay coste por frame.

---

## Flujo de actualización del mundo

Cada frame, `GameWorld.Update(gameTime)` realiza:

1. **FlushPending** — incorpora entidades recién creadas, destruye las marcadas
2. **PhysicsWorld.Step** — si hay mundo físico asignado
3. **NavPhysicsSync.SyncAll** — si hay sync physics↔navgrid
4. **Entity.Update** — para cada entidad activa, en orden de creación

Y `GameWorld.Draw(gameTime, spriteBatch)` llama a `Entity.Draw` para todas las entidades activas.

---

## Operaciones deferred (diferidas)

La creación y destrucción de entidades son **diferidas** hasta el inicio del siguiente `Update`. Esto evita modificar la lista de entidades mientras se itera sobre ella:

```csharp
// Se añade a _toAdd, no a _entities todavía:
var bullet = world.CreateEntity("Bullet", shootPos);

// Se marca en _toDestroy; OnDestroy() se llama en el siguiente Update:
world.Destroy(enemy);
```

---

## Rendimiento

- `GameBehaviour.Update` y `Draw` solo se incluyen en las listas de ejecutables si el tipo los sobreescribe. Sin comportamiento sobreescrito = sin coste.
- `GameWorld.FindComponents<T>(List<T>)` y `FindEntities<T>(List<GameEntity>)` son zero-alloc — rellenan una lista existente.
- No usar LINQ sobre entidades en `Update`/`Draw`.
- `GameEntityPool<T>` para objetos de vida corta (proyectiles, efectos).

---

## Ver también

- [GameEntity →](game-entity.md)
- [GameBehaviour →](game-behaviour.md)
- [GameWorld →](game-world.md)
- [TransformBehaviour →](transform.md)
- [GameEntityPool →](entity-pool.md)
