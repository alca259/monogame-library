# NavAgent

**Namespace:** `Alca.MonoGame.Kernel.Navigation`

`NavAgent` es un `GameBehaviour` que mueve autónomamente una entidad hacia un destino, siguiendo el camino calculado por el `Pathfinder`.

---

## Propiedades

### Configuración

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Speed` | `float` | Velocidad de movimiento en unidades/s (default: 100f) |
| `StoppingDistance` | `float` | Distancia al destino para detenerse (default: 5f) |
| `Profile` | `NavAgentProfile` | Perfil de movimiento del agente |
| `RotateTowardMovement` | `bool` | Rota la entidad en la dirección de movimiento |
| `RotationSpeed` | `float` | Velocidad de giro en rad/s (default: 2π) |

### Estado

| Propiedad | Tipo | Descripción |
|---|---|---|
| `HasPath` | `bool` | Si hay una ruta calculada activa |
| `IsMoving` | `bool` | Si el agente se está moviendo |
| `Destination` | `Vector2` | Destino actual |

### Eventos

| Evento | Descripción |
|---|---|
| `OnDestinationReached` | Disparado cuando el agente llega al destino |
| `OnPathNotFound` | Disparado si no se puede calcular la ruta |

---

## Métodos

| Método | Descripción |
|---|---|
| `SetDestination(worldPosition)` | Calcula la ruta síncronamente y comienza el movimiento |
| `SetDestinationAsync(worldPosition)` | Calcula la ruta en background; `await` seguro desde `Update` |
| `Stop()` | Detiene el movimiento y limpia la ruta |
| `RecomputePath()` | Recalcula la ruta actual al mismo destino |

---

## Ejemplo: enemigo patrullando

```csharp
public sealed class PatrolEnemy : GameBehaviour
{
    private NavAgent _agent = null!;
    private int _currentWaypoint;
    private Vector2[] _waypoints = null!;

    public override void Awake()
    {
        _agent = Entity.GetComponent<NavAgent>();
        _agent.Speed = 80f;
        _agent.StoppingDistance = 8f;
        _agent.OnDestinationReached += GoToNextWaypoint;
        _agent.OnPathNotFound       += OnPathFailed;

        _waypoints =
        [
            new Vector2(100, 300),
            new Vector2(500, 300),
            new Vector2(500, 100),
            new Vector2(100, 100)
        ];

        GoToNextWaypoint();
    }

    private void GoToNextWaypoint()
    {
        _agent.SetDestination(_waypoints[_currentWaypoint]);
        _currentWaypoint = (_currentWaypoint + 1) % _waypoints.Length;
    }

    private void OnPathFailed()
    {
        // El camino está bloqueado; esperar y reintentar
        Core.Timers.Schedule(delay: 1f, callback: () => _agent.RecomputePath());
    }
}
```

---

## Ejemplo: ruta asíncrona con cancelación

```csharp
private CancellationTokenSource _cts = new();

public async void ChasePlayer(Vector2 playerPosition)
{
    _cts.Cancel();
    _cts = new CancellationTokenSource();

    bool found = await _agent.SetDestinationAsync(playerPosition);
    if (!found)
        FallbackBehavior();
}
```

---

## Notas

- `NavAgent.Awake` requiere que `GameWorld.NavGrid` y `GameWorld.Pathfinder` estén asignados.
- `SetDestinationAsync` usa el `AsyncPathfinder` del `GameWorld`; si no está configurado, lanza excepción.
- Recomputar la ruta en cada frame es caro — hazlo sólo cuando el destino cambia o hay obstáculos dinámicos.

---

## Ver también

- [NavGrid y Pathfinder →](nav-grid.md)
- [Steering →](steering.md)
