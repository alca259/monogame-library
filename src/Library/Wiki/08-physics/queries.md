# Queries de Física 2D

**Namespace:** `Alca.MonoGame.Kernel.Physics`

`Physics2DQuery` permite consultar el mundo físico sin crear colisiones reales: raycasts, overlap tests y detección de suelo.

---

## Physics2DQuery

Accesible como `World.PhysicsWorld.Query`.

### Raycast

```csharp
// Primer impacto
bool hit = query.Raycast(origin, direction, maxDistance, mask, out RaycastHit2D hit);

// Todos los impactos (sin allocation si la lista se pre-asigna)
query.RaycastAll(origin, direction, maxDistance, mask, results);
```

### Overlap

```csharp
// Punto
bool found = query.OverlapPoint(point, mask, out Collider2D? collider);

// Círculo
query.OverlapCircle(center, radius, mask, results);

// Caja
query.OverlapBox(center, halfSize, angle, mask, results);
```

---

## RaycastHit2D

Struct con los datos del impacto.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Point` | `Vector2` | Punto de impacto en el mundo |
| `Normal` | `Vector2` | Normal de la superficie en el impacto |
| `Distance` | `float` | Distancia desde el origen al impacto |
| `Collider` | `Collider2D?` | Collider golpeado |
| `IsHit` | `bool` | `true` si el raycast impactó algo |

---

## Ejemplo: detección de suelo

```csharp
public sealed class GroundChecker : GameBehaviour
{
    private readonly List<RaycastHit2D> _hits = [];
    private bool _isGrounded;

    public bool IsGrounded => _isGrounded;

    public override void Update(GameTime gameTime)
    {
        var pos = Entity.Transform.Position2d;
        var query = Entity.World!.PhysicsWorld!.Query;

        // Raycast hacia abajo desde los pies
        _isGrounded = query.Raycast(
            origin:      pos,
            direction:   Vector2.UnitY,
            maxDistance: 26f,
            mask:        CollisionCategory.Terrain,
            hit:         out _);
    }
}
```

---

## Ejemplo: campo de visión de enemigo

```csharp
public sealed class EnemyVision : GameBehaviour
{
    private readonly List<RaycastHit2D> _hits = [];
    public bool CanSeePlayer { get; private set; }

    public override void Update(GameTime gameTime)
    {
        var query  = Entity.World!.PhysicsWorld!.Query;
        var origin = Entity.Transform.Position2d;

        var playerEntity = Entity.World.FindByName("Player");
        if (playerEntity is null) return;

        var target = playerEntity.Transform.Position2d;
        var dir    = Vector2.Normalize(target - origin);
        float dist = Vector2.Distance(origin, target);

        if (!query.Raycast(origin, dir, dist,
            CollisionCategory.Terrain | CollisionCategory.Player,
            out var hit)) return;

        // Si el primer impacto es el jugador, hay línea de visión
        CanSeePlayer = hit.Collider?.Entity == playerEntity;
    }
}
```

---

## Ejemplo: zona de explosión (overlap circular)

```csharp
private readonly List<Collider2D> _inRadius = [];

public void Explode(Vector2 position, float radius)
{
    _inRadius.Clear();
    World!.PhysicsWorld!.Query.OverlapCircle(
        center: position,
        radius: radius,
        mask:   CollisionCategory.Enemy | CollisionCategory.Player,
        results: _inRadius);

    foreach (var col in _inRadius)
    {
        var dir   = col.Entity.Transform.Position2d - position;
        float dist = dir.Length();
        float force = (1f - dist / radius) * 800f;
        col.Entity.TryGetComponent<RigidBody2D>()?.ApplyImpulse(Vector2.Normalize(dir) * force);
    }
}
```

---

## Notas

- Pre-asigna las listas de resultados como campos para evitar allocations en `Update`.
- `RaycastAll` y los métodos `OverlapXxx` añaden a la lista existente; limpia con `Clear()` antes de llamarlos.
- El parámetro `mask` es la `CollisionCategory.Mask` de las fixtures con las que debe interactuar el raycast.

---

## Ver también

- [Colliders →](colliders.md)
- [RigidBody2D →](rigid-body.md)
