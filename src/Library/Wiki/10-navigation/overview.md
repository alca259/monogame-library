# Navegación — Visión General

**Namespace:** `Alca.MonoGame.Kernel.Navigation`

El sistema de navegación implementa A* sobre un grid 2D con soporte para pathfinding síncrono y asíncrono, movimiento autónomo de agentes y comportamientos de steering.

---

## Arquitectura

```
GameWorld.NavGrid (NavGrid)
    │
    ├── GameWorld.Pathfinder (Pathfinder)       ← A* síncrono
    ├── GameWorld.AsyncPathfinder               ← A* en hilo de fondo
    └── GameWorld.NavPhysicsSync                ← sincroniza obstacles desde física
    
[por entidad]:
    └── NavAgent (GameBehaviour)
            └── SteeringController (GameBehaviour) ← comportamientos opcionales
```

---

## Integración con GameWorld

```csharp
protected override GameWorld? CreateWorld()
{
    var grid = new NavGrid(
        width:    50,
        height:   50,
        cellSize: 32f,
        origin:   Vector2.Zero,
        mode:     NavigationMode.TopDown);

    return new GameWorld
    {
        NavGrid          = grid,
        Pathfinder       = new Pathfinder(),
        AsyncPathfinder  = new AsyncPathfinder()
    };
}
```

---

## Modos de navegación

| Modo | Descripción |
|---|---|
| `TopDown` | Vista cenital; X e Y son el plano horizontal |
| `SideScroll` | Plataformas; X es horizontal, Y es altura |

---

## Ver también

- [NavGrid →](nav-grid.md)
- [NavAgent →](nav-agent.md)
- [Steering →](steering.md)
