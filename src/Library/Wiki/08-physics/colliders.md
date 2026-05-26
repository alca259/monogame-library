# Colliders 2D

**Namespace:** `Alca.MonoGame.Kernel.Physics`

Los colliders definen la forma física de una entidad. Todos heredan de `Collider2D`.

---

## Propiedades comunes (Collider2D)

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsTrigger` | `bool` | Si `true`, detecta solapamientos sin resolver física |
| `Layer` | `CollisionCategory` | Capa de colisión de esta fixture |
| `Mask` | `CollisionCategory` | Capas con las que puede colisionar |
| `Friction` | `float` | Coeficiente de fricción (default: 0.5f) |
| `Restitution` | `float` | Rebote 0–1 (default: 0) |
| `Density` | `float` | Densidad para cálculo de masa (default: 1f) |

### Eventos

| Evento | Tipo | Descripción |
|---|---|---|
| `OnCollisionEnter` | `Action<Collider2D>?` | Primera colisión sólida con otro collider |
| `OnCollisionExit` | `Action<Collider2D>?` | La colisión sólida termina |
| `OnTriggerEnter` | `Action<Collider2D>?` | Solapamiento con un trigger |
| `OnTriggerExit` | `Action<Collider2D>?` | El solapamiento con el trigger termina |

---

## BoxCollider2D

Caja alineada con los ejes.

```csharp
public sealed class BoxCollider2D : Collider2D
{
    public Vector2 Size   { get; set; }   // default: Vector2.One
    public Vector2 Offset { get; set; }   // default: Vector2.Zero
}
```

---

## CircleCollider2D

Círculo perfecto — el más eficiente para objetos redondos.

```csharp
public sealed class CircleCollider2D : Collider2D
{
    public float   Radius { get; set; }   // default: 0.5f
    public Vector2 Offset { get; set; }   // default: Vector2.Zero
}
```

---

## PolygonCollider2D

Polígono convexo con vértices arbitrarios.

```csharp
public sealed class PolygonCollider2D : Collider2D
{
    public void SetPath(ReadOnlySpan<Vector2> vertices)
}
```

---

## CollisionCategory

Enum de flags para filtrar colisiones.

| Valor | Descripción |
|---|---|
| `None` | Sin colisión |
| `Default` | Capa genérica |
| `Player` | Jugador |
| `Enemy` | Enemigos |
| `Projectile` | Proyectiles |
| `Trigger` | Zonas trigger |
| `Terrain` | Terreno |
| `All` | Todas las capas |

---

## Ejemplo: plataformas con filtrado de capas

```csharp
protected override void InitializeWorld()
{
    // Plataforma sólida
    var platform = World!.CreateEntity("Platform", new Vector2(300, 400));
    platform.Add(new RigidBody2D { IsStatic = true });
    platform.Add(new BoxCollider2D
    {
        Size  = new Vector2(200, 20),
        Layer = CollisionCategory.Terrain,
        Mask  = CollisionCategory.Player | CollisionCategory.Enemy
    });

    // Zona de daño (trigger)
    var lavaZone = World!.CreateEntity("Lava", new Vector2(300, 500));
    lavaZone.Add(new RigidBody2D { IsStatic = true });
    var lavaTrigger = lavaZone.Add(new BoxCollider2D
    {
        Size      = new Vector2(200, 20),
        IsTrigger = true,
        Layer     = CollisionCategory.Trigger,
        Mask      = CollisionCategory.Player
    });
    lavaTrigger.OnTriggerEnter += other =>
    {
        other.Entity.GetComponent<HealthComponent>()?.TakeDamage(999);
    };

    // Proyectil — no colisiona con otros proyectiles
    var bullet = World!.CreateEntity("Bullet", bulletPosition);
    bullet.Add(new RigidBody2D { Mass = 0.1f });
    bullet.Add(new CircleCollider2D
    {
        Radius = 5f,
        Layer  = CollisionCategory.Projectile,
        Mask   = CollisionCategory.Terrain | CollisionCategory.Enemy
    });
}
```

---

## Notas

- Los colliders deben añadirse **después** del `RigidBody2D` para adjuntarse al cuerpo correcto.
- `PolygonCollider2D.SetPath` requiere un polígono convexo; Aether rechazará formas cóncavas.
- Para triggers, sigue siendo necesario un `RigidBody2D` (estático para zonas fijas).

---

## Ver también

- [RigidBody2D →](rigid-body.md)
- [Queries →](queries.md)
