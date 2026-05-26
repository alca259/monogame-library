# Partículas

**Namespace:** `Alca.MonoGame.Kernel.Graphics.Particles`

El sistema de partículas envuelve `MonoGame.Extended.Particles` con una API fluida y una integración directa en el ECS.

---

## Clases principales

| Clase | Descripción |
|---|---|
| `ParticleBuilder` | API fluida para configurar efectos |
| `ParticleEffectWrapper` | Envuelve el efecto y gestiona Update/Draw |
| `ParticleEmitterBehaviour` | `GameBehaviour` que impulsa el efecto desde la entidad |

---

## ParticleEffectWrapper

Envuelve un `ParticleEffect` de MonoGame.Extended. Gestiona el ciclo de vida.

```csharp
var wrapper = new ParticleEffectWrapper();
wrapper.LoadEffect(Content, "Particles/explosion"); // carga desde Content Pipeline
// O configura programáticamente mediante ParticleBuilder
```

### Métodos

| Método | Descripción |
|---|---|
| `Update(GameTime, Vector2 position)` | Avanza la simulación en la posición indicada |
| `Draw(SpriteBatch, BlendState)` | Dibuja las partículas activas |
| `Trigger(Vector2 position)` | Dispara una ráfaga manual en la posición |

---

## ParticleEmitterBehaviour

Componente ECS que integra partículas en una entidad.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Effect` | `ParticleEffectWrapper` | El efecto gestionado (nunca null) |
| `BlendState` | `BlendState` | Estado de mezcla para el dibujado (defecto: `AlphaBlend`) |
| `UseEntityPosition` | `bool` | Si `true`, el efecto sigue la posición de la entidad |
| `Offset` | `Vector2` | Offset adicional sobre la posición de la entidad |

### Métodos

```csharp
// Dispara una ráfaga manual (para efectos de impacto, etc.)
emitter.Trigger();
```

---

## Ejemplo: explosión en impacto de proyectil

```csharp
// Configuración de la entidad "explosión"
public sealed class ExplosionEffect : GameBehaviour, IPoolable
{
    private ParticleEmitterBehaviour _emitter = null!;
    private float _lifetime;

    public override void Awake()
    {
        _emitter = Entity.AddComponent<ParticleEmitterBehaviour>();
        // El efecto se carga/configura aquí o en InitAnimations
    }

    public void Init(string effectPath, ContentManager content)
    {
        _emitter.Effect.LoadEffect(content, effectPath);
        _emitter.UseEntityPosition = true;
    }

    public void Reset()
    {
        _lifetime = 0f;
        _emitter.Trigger();
    }

    public override void Update(GameTime gameTime)
    {
        _lifetime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_lifetime > 2f)
            _pool.Return(Entity); // devolver al pool
    }

    private GameEntityPool<ExplosionEffect> _pool = null!;
    public void SetPool(GameEntityPool<ExplosionEffect> pool) => _pool = pool;
}
```

```csharp
// En la escena, al inicializar:
var explosionPool = new GameEntityPool<ExplosionEffect>(World!, "Explosion", prewarm: 10);

// Cuando el proyectil impacta:
var explosion = explosionPool.Get(e =>
{
    e.Transform.Position2d = impactPosition;
});
```

---

## Ejemplo: lluvia continua de partículas

```csharp
public sealed class RainEmitter : GameBehaviour
{
    private ParticleEmitterBehaviour _emitter = null!;

    public override void Awake()
    {
        _emitter = Entity.AddComponent<ParticleEmitterBehaviour>();
        _emitter.UseEntityPosition = true;
        _emitter.BlendState = BlendState.AlphaBlend;
    }
}
```

---

## Notas de rendimiento

- `ParticleEmitterBehaviour.Update` es llamado automáticamente por el ECS.
- `Effect.Trigger()` crea una ráfaga puntual; para efectos continuos, el emitter ya actualiza la posición cada frame.
- Usa `GameEntityPool<T>` para efectos de impacto que se crean y destruyen frecuentemente.

---

## Ver también

- [GameEntityPool →](../02-ecs/entity-pool.md)
- [ECS GameBehaviour →](../02-ecs/game-behaviour.md)
