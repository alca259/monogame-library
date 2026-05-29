# Trigger Volumes 2D

**Namespace:** `Alca.MonoGame.Kernel.Physics.Triggers`

`TriggerZone2D` es un componente ligero de detección de solapamientos que funciona de forma completamente independiente del motor de física Aether. Se asigna a `GameWorld.TriggerWorld` mediante una instancia de `TriggerWorld` y no requiere `RigidBody2D` ni `Physics2DWorld`.

---

## Diferencia con `Collider2D.IsTrigger`

| Característica | `Collider2D.IsTrigger` | `TriggerZone2D` |
|---|---|---|
| Requiere `Physics2DWorld` | Sí | No |
| Requiere `RigidBody2D` | Sí | No |
| Resolución de colisiones | Aether.Physics2D | Sistema propio (O(n²)) |
| Formas soportadas | Box, Circle, Polygon | AABB, Circle |
| Filtrado | `CollisionCategory` (flags) | `LayerMask` (int bitmask) |
| Eventos OnStay | No | Sí |
| Coste por zona | Fixture de Aether | Struct de stack |

Usa `TriggerZone2D` cuando no necesites simulación física y quieras un sistema de zonas de activación sin dependencias adicionales.

---

## Integración con GameWorld

Para habilitar el sistema de triggers, asigna `TriggerWorld` en `CreateWorld()`:

```csharp
protected override GameWorld? CreateWorld()
{
    return new GameWorld
    {
        TriggerWorld = new TriggerWorld()
        // PhysicsWorld no es necesario
    };
}
```

`GameWorld` llama automáticamente a `TriggerWorld.Update()` en cada frame. No es necesario invocarlo manualmente.

---

## Crear una zona de activación

`TriggerZone2D` es un `GameBehaviour` que se añade a cualquier `GameEntity`. Se registra en `TriggerWorld` durante `Awake` y se elimina durante `OnDestroy`.

```csharp
protected override void InitializeWorld()
{
    // Zona de recogida de ítem (AABB)
    var pickup = World!.CreateEntity("Pickup", new Vector2(300, 200));
    var pickupTrigger = pickup.AddComponent<TriggerZone2D>();
    pickupTrigger.Shape      = TriggerShapeType.AABB;
    pickupTrigger.Width      = 48f;
    pickupTrigger.Height     = 48f;
    pickupTrigger.LayerMask  = TriggerLayers.Collectible;
    pickupTrigger.OnEnter    = info => CollectItem(info);

    // Zona de daño radial (Circle)
    var hazard = World!.CreateEntity("Hazard", new Vector2(500, 400));
    var hazardTrigger = hazard.AddComponent<TriggerZone2D>();
    hazardTrigger.Shape     = TriggerShapeType.Circle;
    hazardTrigger.Radius    = 64f;
    hazardTrigger.Offset    = new Vector2(0, -8f); // centrado ligeramente arriba
    hazardTrigger.LayerMask = TriggerLayers.Damage;
}
```

### Propiedades de `TriggerZone2D`

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Shape` | `TriggerShapeType` | `AABB` o `Circle` |
| `Width` | `float` | Ancho (solo AABB) |
| `Height` | `float` | Alto (solo AABB) |
| `Radius` | `float` | Radio (solo Circle) |
| `Offset` | `Vector2` | Desplazamiento del centro respecto a la entidad |
| `LayerMask` | `int` | Bitmask de grupos que puede detectar |
| `OnEnter` | `Action<TriggerOverlapInfo>?` | Callback al comenzar el solapamiento |
| `OnStay` | `Action<TriggerOverlapInfo>?` | Callback mientras dura el solapamiento |
| `OnExit` | `Action<TriggerOverlapInfo>?` | Callback al terminar el solapamiento |

---

## Eventos Enter / Stay / Exit

`TriggerOverlapInfo` es un `readonly struct` que se pasa sin asignaciones de heap.

```csharp
public readonly struct TriggerOverlapInfo
{
    public TriggerZone2D Self  { get; }
    public TriggerZone2D Other { get; }
}
```

Ejemplo de uso de los tres eventos:

```csharp
var zone = entity.AddComponent<TriggerZone2D>();
zone.Shape     = TriggerShapeType.AABB;
zone.Width     = 80f;
zone.Height    = 80f;
zone.LayerMask = TriggerLayers.Player;

zone.OnEnter = info =>
{
    // El jugador entra: activar puerta
    _door.Open();
};

zone.OnStay = info =>
{
    // El jugador permanece: acumular daño por segundo
    float dt = (float)_lastGameTime.ElapsedGameTime.TotalSeconds;
    info.Other.Entity.GetComponent<HealthComponent>()?.TakeDamage(10f * dt);
};

zone.OnExit = info =>
{
    // El jugador sale: cerrar puerta
    _door.Close();
};
```

> `OnStay` se dispara cada frame mientras el solapamiento esté activo. Usa delta time para cálculos continuos.

---

## LayerMask — filtrado de grupos

Dos zonas se detectan entre sí únicamente si comparten al menos un bit en sus `LayerMask`. Esto permite separar grupos de zonas sin coste adicional.

```csharp
// Definición de capas (en GameConstants.cs o similar)
public static class TriggerLayers
{
    public const int Player      = 1 << 0;  // 1
    public const int Enemy       = 1 << 1;  // 2
    public const int Collectible = 1 << 2;  // 4
    public const int Damage      = 1 << 3;  // 8
    public const int Dialogue    = 1 << 4;  // 16
}

// Zona del jugador detecta coleccionables y zonas de daño
playerTrigger.LayerMask = TriggerLayers.Player | TriggerLayers.Collectible | TriggerLayers.Damage;

// Zona coleccionable solo responde al jugador
pickupTrigger.LayerMask = TriggerLayers.Collectible | TriggerLayers.Player;

// Zona de enemigo: no detecta ni al jugador ni a otros enemigos
enemyTrigger.LayerMask = TriggerLayers.Enemy;
// → No comparte bits con Player ni con Damage → no hay eventos entre ellos
```

---

## Solapamiento mixto (AABB vs Circle)

El sistema resuelve solapamientos entre zonas de distinta forma sin configuración adicional:

| Combinación | Algoritmo |
|---|---|
| AABB vs AABB | Intersección de rectángulos |
| Circle vs Circle | Distancia al cuadrado vs suma de radios |
| AABB vs Circle | Test punto-rectángulo con distancia |

Todas las pruebas operan en coordenadas mundo (posición de entidad + `Offset`).

---

## Notas de rendimiento

- El sistema evalúa todos los pares posibles en cada frame: complejidad **O(n²)**.
- Para un rendimiento óptimo, mantén el número total de zonas activas **por debajo de 200**.
- Las zonas se comparan por `LayerMask` antes del test geométrico: un bitmask sin bits comunes descarta el par de inmediato.
- `TriggerOverlapInfo` es un `readonly struct`; los callbacks no generan basura en el heap.
- Destruye entidades portadoras de `TriggerZone2D` en lugar de desactivarlas cuando ya no sean necesarias — `OnDestroy` limpia el registro automáticamente.

---

## Ver también

- [Physics2DWorld →](physics-world.md)
- [Colliders 2D →](colliders.md)
