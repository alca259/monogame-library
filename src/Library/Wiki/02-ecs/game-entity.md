# GameEntity

**Namespace:** `Alca.MonoGame.Kernel.ECS`
**Equivalente a:** `GameObject` de Unity

`GameEntity` es el contenedor de comportamientos. No tiene lógica propia: delega toda la funcionalidad en los `GameBehaviour` que se le añaden. Cada entidad tiene siempre un `TransformBehaviour` adjunto.

---

## Identidad

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` | Identificador único generado automáticamente |
| `Name` | `string` | Nombre legible de la entidad (read-only) |
| `Active` | `bool` | Si `false`, la entidad no recibe Update ni Draw |
| `World` | `GameWorld` | Mundo al que pertenece |
| `Transform` | `TransformBehaviour` | Componente espacial; siempre presente |

---

## Añadir comportamientos

### `Add<T>(T behaviour)` — retorna `this` para encadenar

```csharp
var player = world.CreateEntity("Player", new Vector2(100, 200));
player
    .Add(new SpriteRendererBehaviour(texture))
    .Add(new RigidBody2D { IsStatic = false })
    .Add(new PlayerController());
```

`Awake()` se llama en el comportamiento **inmediatamente** al añadirlo.

### `AddComponent<T>()` — crea y añade en un paso

```csharp
var controller = player.AddComponent<PlayerController>();
// controller ya está inicializado y adjunto
```

---

## Acceder a comportamientos

| Método | Descripción |
|---|---|
| `GetComponent<T>()` | Por tipo concreto o interfaz; `null` si no existe |
| `TryGetComponent<T>(out T?)` | Versión con resultado booleano |
| `HasComponent<T>()` | Solo comprueba existencia |
| `GetComponents<T>(List<T>)` | Todos los comportamientos que implementan T en esta entidad |
| `GetAllComponents()` | `IReadOnlyList<GameBehaviour>` de todos los comportamientos |

```csharp
// Acceso por tipo concreto
var rb = entity.GetComponent<RigidBody2D>();

// Acceso por interfaz
var damageable = entity.GetComponent<IDamageable>();

// Comprobación segura
if (entity.TryGetComponent<NavAgent>(out var agent))
    agent.SetDestination(target);
```

---

## Búsqueda en jerarquía

| Método | Descripción |
|---|---|
| `GetComponentInChildren<T>(bool includeInactive)` | Primer T en descendientes (DFS) |
| `GetComponentsInChildren<T>(List<T>, bool includeInactive)` | Todos los T en descendientes |
| `GetComponentInParent<T>(bool includeInactive)` | Primer T subiendo la jerarquía |
| `GetComponentsInParent<T>(List<T>, bool includeInactive)` | Todos los T en ascendientes |

```csharp
// Busca un WeaponBehaviour en cualquier hijo
var weapon = entity.GetComponentInChildren<WeaponBehaviour>();

// Busca el inventario del jugador desde un arma hija
var inventory = weaponEntity.GetComponentInParent<Inventory>();
```

> Estas búsquedas **no son zero-alloc** — no llamarlas dentro de `Update`/`Draw`. Cachea el resultado en `Awake`.

---

## Sistema de tags

```csharp
entity.AddTag("enemy");
entity.AddTag("boss");

if (entity.HasTag("enemy"))
    ShowHealthBar(entity);

// Equivalente más expresivo:
if (entity.CompareTag("player"))
    HandlePlayerCollision();
```

---

## Jerarquía padre-hijo

```csharp
var parent = world.CreateEntity("Vehicle", new Vector2(0, 0));
var wheel = world.CreateEntity("Wheel_FL", new Vector2(20, 10));

// Establece la relación
wheel.SetParent(parent);

// Ahora wheel.Transform.Position es relativa a parent

// Navegar la jerarquía
var root = wheel.Root;                  // nodo raíz
bool isChild = wheel.IsChildOf(parent); // true

// Recorrer descendientes
parent.TraverseDown(e => Console.WriteLine(e.Name));

// Recorrer ascendientes
wheel.TraverseUp(e => Console.WriteLine(e.Name));

// Buscar por nombre en descendientes (BFS, no usar en hot path)
var flWheel = parent.Find("Wheel_FL");

// Orden en la lista de hijos
wheel.SetAsFirstSibling();
wheel.SetAsLastSibling();
int idx = wheel.GetSiblingIndex();

// Desconectar todos los hijos
parent.DetachChildren();
```

---

## Activar / desactivar

```csharp
entity.SetActive(false); // o: entity.Active = false;
// La entidad no recibirá Update ni Draw mientras esté inactiva
entity.SetActive(true);
```

---

## Ejemplo completo: entidad Player con jerarquía

```csharp
// Entidad raíz del jugador
var player = world.CreateEntity("Player", new Vector2(400, 300));
player.AddTag("player");
player
    .Add(new SpriteRendererBehaviour(playerTexture))
    .Add(new RigidBody2D { Mass = 1f })
    .AddComponent<PlayerController>();

// Entidad hija: arma (posición relativa al jugador)
var weapon = world.CreateEntity("Sword");
weapon.SetParent(player);
weapon.Transform.LocalPosition2d = new Vector2(16, 0); // offset a la derecha
weapon
    .Add(new SpriteRendererBehaviour(swordTexture))
    .AddComponent<SwordBehaviour>();

// Desde SwordBehaviour.Awake():
//   var playerController = Entity.GetComponentInParent<PlayerController>();
```

---

## Notas

- `GameEntity` tiene constructor `internal` — siempre crea entidades a través de `GameWorld.CreateEntity(...)`.
- La destrucción es **diferida**: `world.Destroy(entity)` programa la eliminación para el inicio del siguiente frame.
- `Find(string)` usa BFS y no es adecuado para el game loop. Cachea la referencia en `Awake`.

---

## Ver también

- [GameBehaviour →](game-behaviour.md)
- [TransformBehaviour →](transform.md)
- [GameWorld →](game-world.md)
- [ECS Overview →](overview.md)
