# Tipos de Luz

**Namespace:** `Alca.MonoGame.Kernel.Lighting`

Todos los tipos de luz heredan de `LightBehaviour` y son `GameBehaviour`, por lo que se adjuntan a entidades del ECS.

---

## LightBehaviour (clase base abstracta)

### Propiedades comunes

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Color` | `Color` | Color de la luz (default: `Color.White`) |
| `Intensity` | `float` | Intensidad de la contribución (default: 1f) |
| `Range` | `float` | Radio de influencia en unidades de mundo (0 = infinito) |
| `LightingLayer` | `LightingLayer` | Capa a la que contribuye esta luz |
| `IsContributing` | `bool` | Si la luz está activa en el `LightingWorld` |

### Ciclo de vida

`Awake` registra la luz en el `LightingWorld` y `OnDestroy` la desregistra automáticamente.

---

## AmbientLight

Luz omnidireccional sin posición ni dirección. Contribuye igual en todo el mundo.

```csharp
var ambientEntity = World!.CreateEntity("Ambient", Vector2.Zero);
var ambient = ambientEntity.AddComponent<AmbientLight>();
ambient.Color     = new Color(40, 40, 80);
ambient.Intensity = 0.4f;
```

---

## DirectionalLight2D

Luz con dirección fija (como el sol). No tiene posición; ilumina todo desde la dirección indicada.

```csharp
public sealed class DirectionalLight2D : LightBehaviour
{
    public Vector2 Direction { get; set; }  // default: Vector2.UnitX
}
```

```csharp
var sunEntity = World!.CreateEntity("Sun", Vector2.Zero);
var sun = sunEntity.AddComponent<DirectionalLight2D>();
sun.Direction = Vector2.Normalize(new Vector2(1, -0.5f));
sun.Color     = Color.LightYellow;
sun.Intensity = 0.8f;
```

---

## PointLight2D

Luz puntual con atenuación por distancia (falloff de potencia configurable).

```csharp
public sealed class PointLight2D : LightBehaviour
{
    public float FalloffExponent { get; set; }  // default: 2f
}
```

- `Range` — radio máximo de influencia.
- `FalloffExponent` — velocidad de la atenuación: 1 = lineal, 2 = cuadrático, 3+ = más pronunciado.

```csharp
var torchEntity = World!.CreateEntity("Torch", new Vector2(300, 200));
var torch = torchEntity.AddComponent<PointLight2D>();
torch.Color           = new Color(255, 160, 80);
torch.Intensity       = 1.2f;
torch.Range           = 150f;
torch.FalloffExponent = 2f;
torch.LightingLayer   = LightingLayer.World;
```

---

## SpotLight2D

Luz en forma de cono con ángulos interior y exterior.

```csharp
public sealed class SpotLight2D : LightBehaviour
{
    public float   InnerAngle { get; set; }  // grados, penumbra interna (default: 15f)
    public float   OuterAngle { get; set; }  // grados, penumbra externa (default: 30f)
    public Vector2? Direction { get; set; }  // null = usa la rotación de la entidad
}
```

```csharp
var spotEntity = World!.CreateEntity("Spotlight", new Vector2(400, 100));
var spot = spotEntity.AddComponent<SpotLight2D>();
spot.Color      = Color.White;
spot.Intensity  = 1.5f;
spot.Range      = 200f;
spot.InnerAngle = 20f;
spot.OuterAngle = 40f;
spot.Direction  = Vector2.UnitY;  // apunta hacia abajo
```

---

## Ejemplo: escena nocturna con antorcha

```csharp
protected override void InitializeWorld()
{
    // Luz ambiental muy tenue para simular la noche
    var ambEntity = World!.CreateEntity("NightAmbient", Vector2.Zero);
    var amb = ambEntity.AddComponent<AmbientLight>();
    amb.Color     = new Color(10, 10, 30);
    amb.Intensity = 0.15f;

    // Antorcha que sigue al jugador
    var playerEntity = World!.FindByName("Player")!;
    var torchChild = World!.CreateEntity("PlayerTorch", Vector2.Zero);
    torchChild.Transform.SetParent(playerEntity.Transform);
    torchChild.Transform.LocalPosition2d = new Vector2(0, -20);

    var flame = torchChild.AddComponent<PointLight2D>();
    flame.Color           = new Color(255, 140, 60);
    flame.Intensity       = 1.0f;
    flame.Range           = 120f;
    flame.FalloffExponent = 2f;

    // Efecto de parpadeo en Update del GameBehaviour de la antorcha
}
```

---

## Notas

- `LightBehaviour.Awake` registra la luz automáticamente; no llames a `LightingWorld.Register` manualmente.
- Para parpadeo, modifica `Intensity` o `Range` en `Update` del `GameBehaviour` que gestiona la antorcha.
- El método CPU `LightingWorld.Resolve` hace un bucle sobre todas las luces activas — en escenas con muchas luces usa el pipeline GPU.

---

## Ver también

- [Pipeline GPU →](gpu-pipeline.md)
- [LightingWorld overview →](overview.md)
