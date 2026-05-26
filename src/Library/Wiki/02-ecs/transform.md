# TransformBehaviour

**Namespace:** `Alca.MonoGame.Kernel.ECS`
**Equivalente a:** `Transform` de Unity

`TransformBehaviour` es el componente espacial adjunto automáticamente a toda entidad creada con `GameWorld.CreateEntity(...)`. Gestiona posición, rotación y escala en espacios local y world, y proporciona matrices de transformación para convertir entre ellos.

---

## Espacios de coordenadas

| Espacio | Descripción |
|---|---|
| **Local** | Relativo al padre (`LocalPosition`, `LocalRotation`, `LocalScale`) |
| **World** | Absoluto en el mundo (`Position`, `Rotation`, `LossyScale`) |

Si la entidad no tiene padre, Local == World.

---

## Constructores

```csharp
// Posición 3D
new TransformBehaviour(new Vector3(10, 5, 0))

// Posición 2D (Z = 0)
new TransformBehaviour(new Vector2(100, 200))

// En el origen
new TransformBehaviour()
```

`GameWorld.CreateEntity` crea el `TransformBehaviour` automáticamente con la posición indicada.

---

## Posición

### Espacio local

```csharp
// 3D
entity.Transform.LocalPosition = new Vector3(10, 5, 0);

// 2D (solo X e Y; Z conservada)
entity.Transform.LocalPosition2d = new Vector2(10, 5);
```

### Espacio world

```csharp
// Posición absoluta en el mundo
entity.Transform.Position = new Vector3(400, 300, 0);

// Solo XY
entity.Transform.Position2d = new Vector2(400, 300);
```

---

## Rotación

Las rotaciones son ángulos de Euler en **radianes** (pitch=X, yaw=Y, roll=Z).

```csharp
// Rotación local
entity.Transform.LocalRotation  = new Vector3(0, 0, MathF.PI / 4f); // 45°
entity.Transform.LocalRotation2d = MathF.PI / 4f;  // solo Z (el eje 2D)

// Rotación world
entity.Transform.Rotation   = new Vector3(0, 0, MathF.PI);
entity.Transform.Rotation2d = MathF.PI;
```

---

## Escala

```csharp
entity.Transform.LocalScale   = new Vector3(2f, 2f, 1f); // doble tamaño
entity.Transform.LocalScale2d = new Vector2(2f, 2f);

// Escala acumulada de toda la jerarquía (solo lectura)
Vector3 globalScale = entity.Transform.LossyScale;
```

---

## Velocidad (campo de conveniencia)

```csharp
entity.Transform.Velocity   = new Vector3(100, 0, 0);
entity.Transform.Velocity2d = new Vector2(100, 0);
```

> No es física real; es un campo de datos para que tus behaviours almacenen velocidad. `RigidBody2D` tiene su propia velocidad de simulación.

---

## Operaciones de transformación

### Mover

```csharp
// Mueve en espacio local (relativo a la rotación)
entity.Transform.Translate(new Vector3(5, 0, 0));

// Mueve en espacio world (absoluto)
entity.Transform.Translate(new Vector3(5, 0, 0), worldSpace: true);
```

### Rotar

```csharp
// Rotación local incremental (en radianes)
entity.Transform.Rotate(new Vector3(0, 0, 0.1f));

// Rotación world incremental
entity.Transform.Rotate(new Vector3(0, 0, 0.1f), worldSpace: true);
```

### Mirar hacia un objetivo (2D)

```csharp
entity.Transform.LookAt(targetEntity.Transform.Position);
// Ajusta LocalRotation2d para que el eje +X apunte al target
```

### Asignar posición y rotación juntas

```csharp
entity.Transform.SetPositionAndRotation(
    new Vector3(400, 300, 0),
    new Vector3(0, 0, MathF.PI / 2f));

entity.Transform.SetLocalPositionAndRotation(
    new Vector3(10, 0, 0),
    new Vector3(0, 0, 0));
```

---

## Convertir puntos entre espacios

```csharp
// Local → World
Vector3 worldPos = entity.Transform.TransformPoint(new Vector3(5, 0, 0));

// World → Local
Vector3 localPos = entity.Transform.InverseTransformPoint(new Vector3(405, 300, 0));

// Dirección Local → World (sin translación)
Vector3 worldDir = entity.Transform.TransformDirection(Vector3.Right);

// Dirección World → Local
Vector3 localDir = entity.Transform.InverseTransformDirection(Vector3.UnitX);
```

---

## Matrices

```csharp
Matrix l2w = entity.Transform.LocalToWorldMatrix; // Local → World
Matrix w2l = entity.Transform.WorldToLocalMatrix; // World → Local (inversa)
```

Útiles para pasar la transformación a un `Effect` de shader 3D.

---

## Jerarquía desde Transform

```csharp
TransformBehaviour? parent = entity.Transform.ParentTransform;
TransformBehaviour root    = entity.Transform.Root;
int childCount             = entity.Transform.ChildCount;
TransformBehaviour? child0 = entity.Transform.GetChild(0);
bool isChild = child.Transform.IsChildOf(parent.Transform);
```

---

## Ejemplo: entidad hija con posición relativa

```csharp
var car   = world.CreateEntity("Car",   new Vector2(200, 400));
var wheel = world.CreateEntity("Wheel", new Vector2(0, 0));

wheel.SetParent(car);
wheel.Transform.LocalPosition2d = new Vector2(40, 15); // offset desde el centro del coche

// Mover el coche mueve la rueda automáticamente
car.Transform.Position2d = new Vector2(300, 400);

// La posición world de la rueda ahora es (340, 415)
Console.WriteLine(wheel.Transform.Position2d); // (340, 415)
```

---

## Ejemplo: apuntar al jugador cada frame

```csharp
public sealed class EnemyAim : GameBehaviour
{
    private GameEntity? _player;

    public override void Start()
    {
        _player = Entity.World.FindByName("Player");
    }

    public override void Update(GameTime gameTime)
    {
        if (_player is null) return;
        Entity.Transform.LookAt(_player.Transform.Position);
    }
}
```

---

## Notas

- `Position` y `Rotation` en espacio world recalculan la matriz en cada acceso. Si los lees varias veces por frame, guarda el valor en una variable local.
- `TransformBehaviour` no acepta ser añadido manualmente más de una vez — solo puede existir un transform por entidad.

---

## Ver también

- [GameEntity →](game-entity.md)
- [GameBehaviour →](game-behaviour.md)
- [ECS Overview →](overview.md)
