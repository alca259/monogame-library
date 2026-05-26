# Steering

**Namespace:** `Alca.MonoGame.Kernel.Navigation.Steering`

El sistema de steering permite combinar comportamientos de movimiento autónomos (Seek, Flee, Wander…) ponderados para producir una velocidad resultante.

---

## ISteeringBehavior

Interfaz que debe implementar cada comportamiento.

```csharp
public interface ISteeringBehavior
{
    Vector2 CalculateSteering(Vector2 agentPosition,
                              Vector2 agentVelocity,
                              GameTime gameTime)
}
```

---

## SteeringController

`GameBehaviour` que agrega los comportamientos registrados y aplica el resultado a la entidad.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `MaxResultSpeed` | `float` | Velocidad máxima de la velocidad resultante (default: 300f) |
| `ApplyToTransform` | `bool` | Si `true`, mueve la entidad automáticamente (default: true) |
| `ResultVelocity` | `Vector2` | Velocidad calculada este frame (read-only) |

### Métodos

| Método | Descripción |
|---|---|
| `Add(behavior, weight)` | Registra un comportamiento con peso ponderado |
| `Remove(behavior)` | Elimina un comportamiento |

---

## Ejemplo: agente con Seek, Flee y Separation

```csharp
public sealed class FlockingAgent : GameBehaviour
{
    private SteeringController _steering = null!;

    public override void Awake()
    {
        _steering = Entity.AddComponent<SteeringController>();
        _steering.MaxResultSpeed = 150f;

        // Seek al jugador con peso alto
        var seek = new SeekBehavior(() => PlayerPosition());
        _steering.Add(seek, weight: 1.5f);

        // Huir de obstáculos con peso medio
        var flee = new FleeBehavior(() => NearestObstaclePosition(), radius: 60f);
        _steering.Add(flee, weight: 1.0f);

        // Separarse de los agentes vecinos
        var separation = new SeparationBehavior(GetNearbyAgents, minDistance: 30f);
        _steering.Add(separation, weight: 0.8f);
    }

    private Vector2 PlayerPosition() =>
        Entity.World!.FindByName("Player")?.Transform.Position2d ?? Vector2.Zero;

    private Vector2 NearestObstaclePosition() =>
        Vector2.Zero; // implementar según el juego

    private IEnumerable<Vector2> GetNearbyAgents() =>
        Entity.World!.FindByTag("FlockAgent")
              .Select(e => e.Transform.Position2d);
}
```

---

## Comportamiento personalizado

```csharp
public sealed class WanderBehavior : ISteeringBehavior
{
    private float _wanderAngle;
    private readonly float _wanderRadius;
    private readonly float _wanderDistance;

    public WanderBehavior(float radius = 50f, float distance = 100f)
    {
        _wanderRadius   = radius;
        _wanderDistance = distance;
    }

    public Vector2 CalculateSteering(Vector2 agentPosition, Vector2 agentVelocity,
                                     GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _wanderAngle += (Random.Shared.NextSingle() - 0.5f) * 2f * dt;

        var circleCenter = Vector2.Normalize(agentVelocity == Vector2.Zero
            ? Vector2.UnitX
            : agentVelocity) * _wanderDistance;

        var offset = new Vector2(
            MathF.Cos(_wanderAngle) * _wanderRadius,
            MathF.Sin(_wanderAngle) * _wanderRadius);

        return circleCenter + offset;
    }
}
```

Uso:

```csharp
_steering.Add(new WanderBehavior(radius: 40f, distance: 80f), weight: 1f);
```

---

## Notas

- `ApplyToTransform = false` permite al `SteeringController` calcular la velocidad sin moverla, para que otro componente (como `RigidBody2D`) la aplique manualmente vía `_rb.LinearVelocity = _steering.ResultVelocity`.
- La velocidad resultante es la media ponderada de todos los comportamientos registrados, clampeada a `MaxResultSpeed`.
- Evita LINQ en `CalculateSteering` — es llamado cada frame por el `SteeringController.Update`.

---

## Ver también

- [NavAgent →](nav-agent.md)
- [ECS GameBehaviour →](../02-ecs/game-behaviour.md)
