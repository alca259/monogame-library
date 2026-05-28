# WeatherBehaviour

**Namespace:** `Alca.MonoGame.Kernel.Weather`

`WeatherBehaviour` es un componente ECS opt-in que conecta una entidad a la simulación de `WeatherWorld`. Al añadirlo a una entidad, ésta puede recibir fuerzas de viento o impulsos de rayos sobre su `RigidBody2D` hermano.

---

## Propiedades de configuración

| Propiedad | Tipo | Default | Descripción |
|---|---|---|---|
| `ReceivesWind` | `bool` | `false` | Si `true`, `ApplyWindForce` se llama cada frame desde `WeatherWorld` |
| `WindForceMultiplier` | `float` | `1.0` | Multiplicador que escala la fuerza de viento antes de pasarla al `RigidBody2D` |
| `ReceivesLightningImpulse` | `bool` | `false` | Si `true`, el rayo aplica un impulso radial al `RigidBody2D` si la entidad está dentro del radio |

---

## Ciclo de vida

| Método | Comportamiento |
|---|---|
| `Awake()` | Cachea el `RigidBody2D` hermano; llama a `WeatherWorld.Register(this)` |
| `OnDestroy()` | Llama a `WeatherWorld.Unregister(this)` |

El registro/desregistro es automático. No se requiere código adicional en la escena.

---

## Requisitos

- La entidad debe tener un `RigidBody2D` **añadido antes** de `WeatherBehaviour`, ya que `Awake()` lo busca en el momento en que se añade el componente.
- El `GameWorld` al que pertenece la entidad debe tener `WeatherWorld` asignado.

Si falta el `RigidBody2D`, las fuerzas y los impulsos se ignoran silenciosamente.

---

## Uso básico

```csharp
using Alca.MonoGame.Kernel.Physics;
using Alca.MonoGame.Kernel.Weather;

// Configurar el WeatherWorld en el GameWorld
var weatherWorld = new WeatherWorld();
_world.WeatherWorld = weatherWorld;
_world.PhysicsWorld = new Physics2DWorld(new Vector2(0, 500f));

// Crear una entidad que responde al viento
var leaf = _world.CreateEntity("Leaf", new Vector2(400, 200));
leaf.Add(new SpriteRendererBehaviour(leafTexture));
leaf.Add(new RigidBody2D { Mass = 0.3f, LinearDamping = 1f });   // PRIMERO
leaf.Add(new WeatherBehaviour                                      // DESPUÉS
{
    ReceivesWind          = true,
    WindForceMultiplier   = 2.5f,
    ReceivesLightningImpulse = true,
});
```

---

## Fuerzas de viento

El viento se aplica cada frame desde `WeatherWorld.DispatchWind()`:

```
windForce = WindState.ComputeEffectiveForce(totalElapsed, worldUnitsPerKmh)
→ WeatherBehaviour.ApplyWindForce(windForce * WindForceMultiplier)
→ RigidBody2D.ApplyForce(force)
```

La fuerza incluye turbulencia sinusoidal con frecuencias coprimas para un movimiento natural y sin asignaciones de heap.

---

## Impulso de rayo

Cuando un rayo impacta, `LightningController` llama a `ApplyLightningImpulse` en todos los `WeatherBehaviour` registrados:

```
impulseDirection = normalize(entity.Position - strikePosition)
falloff          = 1 - (distance / radius)   // lineal
impulse          = direction * strength * falloff

→ RigidBody2D.ApplyImpulse(impulse)
```

Si la entidad está fuera del radio o la distancia es casi cero, el impulso se ignora.

---

## Combinación con otros comportamientos

```csharp
// Entidad árbol: recibe viento y rayo
tree.Add(new RigidBody2D { IsStatic = false, Mass = 5f, LinearDamping = 3f });
tree.Add(new WeatherBehaviour
{
    ReceivesWind             = true,
    WindForceMultiplier      = 1f,
    ReceivesLightningImpulse = true,
});

// Entidad edificio: ignora viento pero reacciona a rayos
building.Add(new RigidBody2D { IsStatic = true });
building.Add(new WeatherBehaviour
{
    ReceivesWind             = false,
    ReceivesLightningImpulse = true,
});
```

---

## Ver también

- [Visión general →](overview.md)
- [WeatherWorld (API) →](weather-world.md)
- [LightningController →](lightning.md)
- [Physics 2D →](../08-physics/overview.md)
