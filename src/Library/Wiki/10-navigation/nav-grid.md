# NavGrid y Pathfinder

**Namespace:** `Alca.MonoGame.Kernel.Navigation`

`NavGrid` es el mapa de navegación. `Pathfinder` y `AsyncPathfinder` calculan rutas A* sobre él.

---

## NavGrid

### Constructor

```csharp
new NavGrid(int width, int height, float cellSize, Vector2 origin,
            NavigationMode mode = NavigationMode.TopDown)
```

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Width` | `int` | Columnas del grid |
| `Height` | `int` | Filas del grid |
| `CellSize` | `float` | Tamaño de cada celda en píxeles |
| `Origin` | `Vector2` | Posición del mundo del grid |
| `Mode` | `NavigationMode` | `TopDown` o `SideScroll` |

### Métodos de escritura

| Método | Descripción |
|---|---|
| `SetWalkable(x, y, walkable)` | Marca una celda como transitable o bloqueada |
| `SetMovementCost(x, y, cost)` | Coste adicional de atravesar la celda (1 = normal) |
| `SetObstacleHeight(x, y, height)` | Altura del obstáculo (para perfil de salto) |
| `SetAll(walkable)` | Establece todas las celdas al mismo estado |

### Métodos de consulta

| Método | Descripción |
|---|---|
| `GetCell(x, y)` | Devuelve la `NavCell` en esa posición |
| `IsInBounds(x, y)` | Comprueba si las coordenadas son válidas |
| `IsWalkable(x, y)` | Si la celda es transitable |
| `WorldToGrid(worldPos, out x, out y)` | Convierte posición de mundo a coordenadas de grid |
| `GridToWorld(x, y)` | Centro de la celda en coordenadas de mundo |

---

## NavCell

Struct de sólo lectura que describe una celda.

```csharp
public readonly record struct NavCell
{
    public int   GridX          { get; init; }
    public int   GridY          { get; init; }
    public bool  IsWalkable     { get; init; }
    public float MovementCost   { get; init; }
    public float ObstacleHeight { get; init; }
}
```

---

## NavAgentProfile

Define las capacidades de movimiento de un tipo de agente.

```csharp
public readonly struct NavAgentProfile
{
    public float JumpHeight                    { get; init; }
    public float JumpCostMultiplier            { get; init; }
    public float VerticalAscentCostMultiplier  { get; init; }
    public bool  AllowDiagonal                 { get; init; }
    public static NavAgentProfile Default { get; }
}
```

---

## Pathfinder (síncrono)

```csharp
public sealed class Pathfinder
{
    public Pathfinder(int gridCapacity = 65536)
    public bool FindPath(NavGrid grid, Vector2 startWorld, Vector2 endWorld,
                         NavPath result, NavAgentProfile profile = default)
}
```

Devuelve `true` si se encontró ruta. Escribe los waypoints en `result`.

---

## AsyncPathfinder

```csharp
public sealed class AsyncPathfinder : IDisposable
{
    public Task<NavPath?> FindPathAsync(NavGrid grid, Vector2 from, Vector2 to,
                                        NavAgentProfile profile,
                                        CancellationToken ct = default)
    public void Dispose()
}
```

Calcula la ruta en un hilo de fondo. Devuelve `null` si no hay ruta.

---

## Ejemplo: grid para un nivel de plataformas

```csharp
protected override void InitializeWorld()
{
    var grid = World!.NavGrid!;

    // Bloquear toda la cuadrícula por defecto
    grid.SetAll(false);

    // Definir las plataformas transitables
    for (int x = 0; x < 30; x++)
        grid.SetWalkable(x, 14, true);  // suelo principal

    for (int x = 5; x < 12; x++)
        grid.SetWalkable(x, 10, true);  // plataforma superior izquierda

    for (int x = 18; x < 26; x++)
        grid.SetWalkable(x, 8, true);   // plataforma superior derecha

    // Perfil de agente volador (sin restricción de salto)
    var flyerProfile = new NavAgentProfile { AllowDiagonal = true };

    // Buscar ruta desde el spawner al objetivo
    var path = new NavPath();
    bool found = World.Pathfinder!.FindPath(
        grid,
        startWorld: new Vector2(50, 450),
        endWorld:   new Vector2(600, 300),
        result:     path,
        profile:    NavAgentProfile.Default);
}
```

---

## Notas

- `Pathfinder` es thread-safe — puede usarse desde `AsyncPathfinder` sin sincronización adicional.
- Pre-asigna el `NavPath` como campo para evitar allocations al recomputar rutas.
- `SetMovementCost` permite simular terreno con coste (barro = 2x, agua = 5x).

---

## Ver también

- [NavAgent →](nav-agent.md)
- [Steering →](steering.md)
