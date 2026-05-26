# Spec: Fase 10 — Sistema de Navegación 2D (NavMesh / A*)

## Objetivo

Pathfinding grid-based con A* integrado como `GameBehaviour`, sin allocations en el game loop,
registrado en DI como servicio singleton. Soporta dos modos de vista:

| Modo | Ejes mundo | Uso típico |
|------|-----------|------------|
| `TopDown` | X, Z (Y es altura en 3D) | Vista cenital, isométrica |
| `SideScroll` | X, Y (Y es altura 2D) | Plataformer, run-and-gun lateral |

Los obstáculos tienen una dimensión de **altura configurable**. Un agente con suficiente `JumpHeight`
puede atravesar la celda del obstáculo (como si saltase por encima), con un coste adicional.

---

## `NavigationMode` — `enum NavigationMode`

```csharp
public enum NavigationMode
{
    /// <summary>Vista top-down. La cuadrícula mapea ejes mundo X,Z. Y mundo = altura del objeto.</summary>
    TopDown,
    /// <summary>Vista lateral. La cuadrícula mapea ejes mundo X,Y. Y mundo = altura en pantalla.</summary>
    SideScroll
}
```

---

## `NavCell` — `readonly struct NavCell`

```csharp
public readonly struct NavCell
{
    public int GridX { get; }
    public int GridY { get; }
    public bool IsWalkable { get; }
    public float MovementCost { get; }      // multiplicador de coste de terreno; 1.0 = normal
    /// <summary>Altura del obstáculo en esta celda. 0 = suelo libre.
    /// Un agente cuyo JumpHeight >= ObstacleHeight puede atravesarla.</summary>
    public float ObstacleHeight { get; }
}
```

---

## `NavGrid` — `sealed class NavGrid`

```csharp
public sealed class NavGrid
{
    public int Width { get; }
    public int Height { get; }
    public float CellSize { get; }              // tamaño en unidades de mundo por celda
    public Vector2 Origin { get; }              // posición mundo de la esquina (0,0) de la cuadrícula
    public NavigationMode Mode { get; }

    public NavGrid(int width, int height, float cellSize, Vector2 origin,
                   NavigationMode mode = NavigationMode.TopDown);

    // Escritura
    public void SetWalkable(int x, int y, bool walkable);
    public void SetMovementCost(int x, int y, float cost);
    /// <summary>Define la altura del obstáculo. 0 elimina el obstáculo.</summary>
    public void SetObstacleHeight(int x, int y, float height);
    public void SetAll(bool walkable);

    // Consulta
    public NavCell GetCell(int x, int y);
    public bool IsInBounds(int x, int y);
    public bool IsWalkable(int x, int y);

    // Conversión coordenadas
    /// <summary>
    /// TopDown:    worldPos.X → gridX,  worldPos.Y → gridY  (mapea X,Z ignorando Y mundo)
    /// SideScroll: worldPos.X → gridX,  worldPos.Y → gridY  (mapea X,Y estándar)
    /// La diferencia semántica la gestiona el caller al construir worldPos con Z o Y.
    /// </summary>
    public void WorldToGrid(Vector2 worldPos, out int x, out int y);
    public Vector2 GridToWorld(int x, int y);   // centro de la celda
}
```

**Almacenamiento:** array flat `NavCell[width * height]`, índice = `y * width + x`. Sin LINQ.

---

## `NavAgentProfile` — `readonly struct NavAgentProfile`

Perfil de capacidades del agente, pasado al pathfinder. Sin heap allocation.

```csharp
public readonly struct NavAgentProfile
{
    /// <summary>Altura máxima de obstáculo que el agente puede saltar. 0 = no puede saltar.</summary>
    public float JumpHeight { get; init; }

    /// <summary>Multiplicador de coste aplicado al atravesar una celda con obstáculo saltable.
    /// Representa el esfuerzo extra de saltar. Default 2.0.</summary>
    public float JumpCostMultiplier { get; init; }

    /// <summary>En SideScroll: multiplicador de coste para movimiento ascendente (subir/saltar).
    /// Simula el coste de gravedad en el eje Y. Default 1.5.</summary>
    public float VerticalAscentCostMultiplier { get; init; }

    /// <summary>Permite movimiento diagonal en la cuadrícula. Default true.</summary>
    public bool AllowDiagonal { get; init; }

    /// <summary>Perfil por defecto: sin salto, diagonal activada.</summary>
    public static NavAgentProfile Default => new()
    {
        JumpHeight = 0f,
        JumpCostMultiplier = 2.0f,
        VerticalAscentCostMultiplier = 1.5f,
        AllowDiagonal = true
    };
}
```

---

## `NavPath` — `sealed class NavPath`

Contenedor pre-allocado de waypoints. Reutilizable sin allocations.

```csharp
public sealed class NavPath
{
    public NavPath(int maxCapacity = 512);

    public int Count { get; }
    public bool IsEmpty { get; }

    public Vector2 GetWaypoint(int index);
    public void Clear();

    internal void AddWaypoint(Vector2 point);
    internal void Reverse();
}
```

---

## `Pathfinder` — `sealed class Pathfinder`

Servicio singleton. Implementa A* con movimiento 8-direccional. Zero-alloc en hot path.

```csharp
public sealed class Pathfinder
{
    /// <param name="gridCapacity">Width * Height máximo soportado. Pre-alloca estructuras internas.</param>
    public Pathfinder(int gridCapacity = 65536);

    /// <summary>Calcula la ruta más corta. Devuelve false si no existe ruta.</summary>
    /// <param name="grid">Cuadrícula de navegación.</param>
    /// <param name="startWorld">Posición mundo del origen.</param>
    /// <param name="endWorld">Posición mundo del destino.</param>
    /// <param name="result">NavPath pre-allocado que se llenará con waypoints.</param>
    /// <param name="profile">Capacidades del agente. Default = sin salto, con diagonal.</param>
    public bool FindPath(NavGrid grid, Vector2 startWorld, Vector2 endWorld,
                         NavPath result, NavAgentProfile profile = default);
}
```

### Lógica de traversabilidad por celda

```
Para cada celda vecina durante A*:

si celda.IsWalkable == false && celda.ObstacleHeight == 0:
    → bloqueada (no se puede atravesar)

si celda.IsWalkable == false && celda.ObstacleHeight > 0:
    si profile.JumpHeight >= celda.ObstacleHeight:
        → traversable con coste = baseCost * profile.JumpCostMultiplier
    sino:
        → bloqueada

si celda.IsWalkable == true:
    → traversable con coste = celda.MovementCost
```

### Coste de movimiento vertical (SideScroll)

```
En modo SideScroll, al moverse a una celda con gridY mayor (ascendente):
    coste *= profile.VerticalAscentCostMultiplier

Al moverse a una celda con gridY menor (descendente, gravedad):
    coste *= 0.8f  (descenso bonificado)
```

### Internos (zero-alloc)

- Arrays pre-allocados: `_gCost[capacity]`, `_hCost[capacity]`, `_parent[capacity]`,
  `_inOpen[capacity]`, `_inClosed[capacity]`, `_heap[capacity]`
- Open set: min-heap sobre `_heap` (índices de celda planos)
- Heurística: distancia octal (8-dir); Manhattan si `AllowDiagonal = false`
- Sin corner-cutting: diagonal solo si las dos celdas ortogonales son traversables
- No thread-safe (el caller debe sincronizar si usa multi-thread)

---

## `NavAgent` — `sealed class NavAgent : GameBehaviour`

GameBehaviour que mueve la entidad a lo largo del path calculado.

```csharp
public sealed class NavAgent : GameBehaviour
{
    // Configuración (asignar antes de Awake o en Inspector)
    public float Speed { get; set; }                    // unidades/segundo; default 100
    public float StoppingDistance { get; set; }         // radio de parada; default 5
    public NavAgentProfile Profile { get; set; }        // capacidades del agente
    /// <summary>Si true, rota el Transform hacia el siguiente waypoint.</summary>
    public bool RotateTowardMovement { get; set; }      // default false
    /// <summary>Velocidad de rotación en rad/s. Solo si RotateTowardMovement = true.</summary>
    public float RotationSpeed { get; set; }            // default MathHelper.Pi * 2

    // Estado (read-only)
    public bool HasPath { get; }
    public bool IsMoving { get; }
    public Vector2 Destination { get; }

    // Eventos
    public Action? OnDestinationReached { get; set; }
    public Action? OnPathNotFound { get; set; }

    // API
    /// <summary>Calcula ruta y empieza a moverse. Usa NavGrid y Pathfinder inyectados vía DI.</summary>
    public bool SetDestination(Vector2 worldPosition);
    public void Stop();
    public void RecomputePath();    // recalcula ruta al destino actual (útil si el grid cambia)

    // Ciclo de vida
    protected override void Awake();        // resuelve NavGrid y Pathfinder desde IServiceProvider
    public override void Update(GameTime gameTime);
}
```

**Inyección:** `NavAgent.Awake()` resuelve `NavGrid` y `Pathfinder` desde el `IServiceProvider`
de la entidad. El `NavPath` interno se pre-alloca en `Awake()` con capacidad 512.

**Movimiento en Update (sin new):**
- Avanza hacia el waypoint actual usando `Transform.Position2d` (o `Transform.Position` con Z fijo en TopDown).
- Al llegar a `StoppingDistance` del waypoint actual, pasa al siguiente.
- Al llegar al último waypoint, invoca `OnDestinationReached` y limpia el path.

---

## `NavGridDebugRenderer` — `sealed class NavGridDebugRenderer`

Renderizado debug opcional. No usar en builds de producción.

```csharp
public sealed class NavGridDebugRenderer
{
    public Color WalkableColor { get; set; }        // default: Color.Green * 0.3f
    public Color BlockedColor { get; set; }         // default: Color.Red * 0.5f
    public Color ObstacleColor { get; set; }        // default: Color.Orange * 0.5f
    public Color PathColor { get; set; }            // default: Color.Yellow
    public bool ShowGrid { get; set; }              // default: true
    public bool ShowPath { get; set; }              // default: true
    public bool ShowObstacleHeight { get; set; }    // default: false (dibuja texto con altura)

    public NavGridDebugRenderer(Texture2D pixelTexture);

    /// <summary>Dibuja la cuadrícula. Llamar entre SpriteBatch.Begin/End.</summary>
    public void Draw(SpriteBatch spriteBatch, NavGrid grid, NavPath? activePath = null);
}
```

**Sin allocations en `Draw()`.**
Celdas con `ObstacleHeight > 0` se dibujan en `ObstacleColor` con opacidad proporcional a la altura relativa.

---

## Estructura de carpetas

```
src/Library/Alca.MonoGame.Kernel/
└── Navigation/
    ├── NavAgent.cs
    ├── NavAgentProfile.cs
    ├── NavCell.cs
    ├── NavGrid.cs
    ├── NavGridDebugRenderer.cs
    ├── NavigationMode.cs
    ├── NavPath.cs
    └── Pathfinder.cs

src/Library/Alca.MonoGame.Kernel.UnitTests/
└── Navigation/
    ├── NavGridTests.cs
    ├── NavPathTests.cs
    └── PathfinderTests.cs
```

---

## Registro DI

```csharp
// En el bootstrap del juego:
services.AddSingleton<Pathfinder>();
services.AddSingleton(sp => new NavGrid(50, 50, 32f, Vector2.Zero, NavigationMode.TopDown));
```

El `NavAgent` resuelve ambas dependencias desde `IServiceProvider` en `Awake()`.

---

## Tests esperados

### `PathfinderTests.cs`
- `FindPath_DirectLine_NoObstacles_ReturnsShortestPath`
- `FindPath_WithObstacleInMiddle_RoutesAround`
- `FindPath_ImpossiblePath_ReturnsFalse`
- `FindPath_StartEqualsEnd_ReturnsTrueWithEmptyPath`
- `FindPath_DiagonalBlocked_WhenCornerCuttingDisabled`
- `FindPath_ObstacleWithinJumpHeight_AgentCrossesThrough`
- `FindPath_ObstacleExceedsJumpHeight_AgentRoutesAround`
- `FindPath_SideScrollMode_AscentCostHigherThanDescent`
- `FindPath_MultipleObstacles_ChoosesCheapestJumpPath`

### `NavGridTests.cs`
- `WorldToGrid_GridToWorld_RoundTrip_MatchesWithinHalfCell`
- `SetWalkable_BlocksCell_PathfinderAvoidsIt`
- `SetObstacleHeight_CellRemainsCrossableWithSufficientJump`
- `IsInBounds_OutOfRange_ReturnsFalse`
- `GetCell_ValidCoords_ReturnsCorrectData`

### `NavPathTests.cs`
- `AddWaypoint_ExceedsCapacity_DoesNotThrow`
- `Clear_ResetsCount`
- `GetWaypoint_ValidIndex_ReturnsCorrectPosition`

---

## Verificación de fase

```csharp
// Top-down: pared vertical con hueco saltable
var grid = new NavGrid(20, 20, 32f, Vector2.Zero, NavigationMode.TopDown);
grid.SetAll(true);
for (int y = 0; y < 15; y++) grid.SetWalkable(10, y, false);
// Segmento de la pared que puede saltarse (altura 2)
grid.SetObstacleHeight(10, 5, 2f);

var profile = new NavAgentProfile { JumpHeight = 2f, JumpCostMultiplier = 2f,
                                    AllowDiagonal = true };
var pathfinder = new Pathfinder();
var path = new NavPath();

bool found = pathfinder.FindPath(grid, new Vector2(0, 160), new Vector2(640, 160), path, profile);
Assert.True(found);
// El path debe cruzar por la celda (10,5) saltando, no rodear toda la pared
```
