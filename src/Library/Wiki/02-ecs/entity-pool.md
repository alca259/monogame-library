# GameEntityPool

**Namespace:** `Alca.MonoGame.Kernel.ECS`

`GameEntityPool<T>` es un pool de entidades reutilizables para objetos de vida corta y alta frecuencia como proyectiles, partículas, efectos de impacto o monedas. Evita la asignación y destrucción continua de entidades en el heap.

---

## Requisito: interfaz `IPoolable`

El comportamiento de tipo `T` debe implementar `IPoolable`:

```csharp
public interface IPoolable
{
    void Reset();
}
```

`Reset()` se llama automáticamente cada vez que la entidad es recuperada del pool con `Get()`. Aquí debes devolver el componente a su estado inicial.

---

## Creación del pool

```csharp
// Constructor
GameEntityPool<T>(GameWorld world, string name, int prewarm = 0)
```

| Parámetro | Descripción |
|---|---|
| `world` | El `GameWorld` donde se crean las entidades |
| `name` | Nombre de las entidades creadas por el pool |
| `prewarm` | Entidades pre-creadas en el stack (inactivas, listas para usar) |

```csharp
var bulletPool = new GameEntityPool<BulletBehaviour>(
    world: World,
    name: "Bullet",
    prewarm: 20); // crea 20 entidades inactivas al arrancar
```

---

## Usar una entidad del pool

```csharp
public GameEntity Get(Action<GameEntity>? configure = null)
```

1. Extrae una entidad del stack (o crea una nueva si está vacío).
2. Llama a `Reset()` en el comportamiento.
3. Activa la entidad (`Active = true`).
4. Llama al delegate `configure` si se proporcionó.

```csharp
// Disparo simple
var bullet = bulletPool.Get(e =>
{
    e.Transform.Position2d = _barrel.Transform.Position2d;
    e.GetComponent<BulletBehaviour>()!.Direction = _aimDirection;
});
```

---

## Devolver una entidad al pool

```csharp
public void Return(GameEntity entity)
```

Desactiva la entidad (`Active = false`) y la empuja al stack.

```csharp
// Desde BulletBehaviour.Update, cuando el proyectil sale de pantalla:
public override void Update(GameTime gameTime)
{
    if (IsOutOfBounds())
        _pool.Return(Entity); // devolver al pool
}
```

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `AvailableCount` | `int` | Número de entidades disponibles en el stack |

---

## Ejemplo completo: pool de proyectiles

```csharp
// BulletBehaviour.cs
using Alca.MonoGame.Kernel.ECS;
using Microsoft.Xna.Framework;

public sealed class BulletBehaviour : GameBehaviour, IPoolable
{
    private GameEntityPool<BulletBehaviour> _pool = null!;

    public Vector2 Direction { get; set; }
    public float Speed { get; set; } = 600f;
    private float _lifetime;

    public void Init(GameEntityPool<BulletBehaviour> pool) => _pool = pool;

    public void Reset()
    {
        Direction = Vector2.Zero;
        Speed     = 600f;
        _lifetime = 0f;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _lifetime += dt;

        Entity.Transform.Translate(
            new Vector3(Direction * Speed * dt, 0f),
            worldSpace: true);

        // Devolver al pool si excede el tiempo de vida
        if (_lifetime > 3f)
            _pool.Return(Entity);
    }
}
```

```csharp
// WeaponBehaviour.cs
public sealed class WeaponBehaviour : GameBehaviour
{
    private GameEntityPool<BulletBehaviour> _bulletPool = null!;
    private float _cooldown;

    public override void Awake()
    {
        _bulletPool = new GameEntityPool<BulletBehaviour>(
            Entity.World,
            "Bullet",
            prewarm: 30);

        // Inicializar referencias de pool en los comportamientos pre-calentados
        for (int i = 0; i < 30; i++) // solo si necesitas la referencia back al pool
        {
            // Los bullets se inicializan en Get() con el configure delegate
        }
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _cooldown -= dt;

        if (_cooldown <= 0f && Core.Input.IsKeyPressed(Keys.Space))
        {
            _cooldown = 0.2f; // cadencia de disparo

            _bulletPool.Get(e =>
            {
                e.Transform.Position2d = Entity.Transform.Position2d;
                var b = e.GetComponent<BulletBehaviour>()!;
                b.Init(_bulletPool);
                b.Direction = Vector2.UnitX; // apunta a la derecha
            });
        }
    }
}
```

---

## Cuándo usar un pool

| Situación | ¿Usar pool? |
|---|---|
| Proyectiles, balas | Sí |
| Efectos de partículas / explosiones | Sí |
| Monedas o coleccionables repetidos | Sí |
| Enemigos únicos o en pequeña cantidad | No (crea y destruye normalmente) |
| Entidades que cambian de tipo | No |

---

## Notas

- `T` debe ser `public`, tener constructor sin parámetros (`new()`) e implementar `IPoolable`.
- Las entidades del pool **nunca se destruyen** — se desactivan. `GameWorld.Destroy(entity)` no devuelve al pool; debes llamar a `Return` explícitamente.
- `AvailableCount` puede ser 0 si todas las entidades están en uso. El pool crea una nueva automáticamente.

---

## Ver también

- [GameEntity →](game-entity.md)
- [GameBehaviour →](game-behaviour.md)
- [ECS Overview →](overview.md)
