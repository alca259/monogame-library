# Physics2DWorld

**Namespace:** `Alca.MonoGame.Kernel.Physics`

`Physics2DWorld` es el contenedor del mundo físico. Se asigna a `GameWorld.PhysicsWorld` en `CreateWorld()`.

---

## Constructor

```csharp
new Physics2DWorld(Vector2 gravity = default)
```

Valores habituales de gravedad:

| Escenario | Gravedad |
|---|---|
| Plataformas (píxeles/s²) | `new Vector2(0, 600f)` |
| Vista cenital (sin gravedad) | `Vector2.Zero` |
| Espacio | `Vector2.Zero` |

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Gravity` | `Vector2` | Gravedad global del mundo |
| `VelocityIterations` | `int` | Iteraciones de resolución de velocidad (default: 8) |
| `PositionIterations` | `int` | Iteraciones de resolución de posición (default: 3) |
| `Query` | `Physics2DQuery` | Interfaz de consultas espaciales |

---

## Métodos

| Método | Descripción |
|---|---|
| `Step(GameTime)` | Avanza la simulación; llamado automáticamente por `GameWorld` |

---

## Ejemplo: mundo con física básica

```csharp
public sealed class PlatformScene : Scene
{
    protected override GameWorld? CreateWorld()
    {
        return new GameWorld
        {
            PhysicsWorld = new Physics2DWorld(new Vector2(0, 600f))
        };
    }

    protected override void InitializeWorld()
    {
        // Suelo estático
        var ground = World!.CreateEntity("Ground", new Vector2(400, 550));
        ground.Add(new BoxCollider2D { Size = new Vector2(800, 32) });
        ground.Add(new RigidBody2D { IsStatic = true });

        // Jugador dinámico
        var player = World!.CreateEntity("Player", new Vector2(400, 200));
        player.Add(new RigidBody2D { Mass = 1f });
        player.Add(new BoxCollider2D { Size = new Vector2(32, 48) });
        player.AddComponent<PlayerController>();
    }
}
```

---

## Notas

- Aumentar `VelocityIterations` mejora la precisión de las colisiones a costa de rendimiento.
- `Physics2DWorld` debe asignarse antes de que `InitializeWorld` añada componentes físicos.
- No uses el `AetherWorld` interno directamente; la API pública es suficiente para los casos de uso habituales.

---

## Ver también

- [RigidBody2D →](rigid-body.md)
- [Colliders →](colliders.md)
