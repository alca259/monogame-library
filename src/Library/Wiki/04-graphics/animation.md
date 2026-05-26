# Animación

**Namespace:** `Alca.MonoGame.Kernel.Graphics.Sprites`

El sistema de animación 2D tiene tres capas: `Animation` define la secuencia de frames, `AnimatedSprite` reproduce una animación, y `AnimationStateMachine` gestiona transiciones entre estados animados.

---

## Animation

Define una secuencia de `TextureRegion` con timing y opciones de loop.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Frames` | `List<TextureRegion>` | Frames en orden de reproducción |
| `Delay` | `TimeSpan` | Tiempo entre frames (defecto: 100ms) |
| `Name` | `string` | Identificador del clip |
| `IsLooping` | `bool` | Si vuelve al frame 0 al terminar (defecto: `true`) |
| `SpeedMultiplier` | `float` | Multiplicador de velocidad del clip (defecto: 1.0) |

### Constructores

```csharp
// Con frames y delay
var animation = new Animation(frames, TimeSpan.FromMilliseconds(80));

// Manual
var animation = new Animation
{
    Name       = "idle",
    IsLooping  = true,
    Delay      = TimeSpan.FromMilliseconds(120),
    Frames     = new List<TextureRegion> { frame0, frame1, frame2 }
};
```

---

## AnimatedSprite

Extiende `Sprite` con lógica de reproducción de animación.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Animation` | `Animation?` | Clip activo; asignarlo resetea el frame |
| `IsPlaying` | `bool` | Si está avanzando frames (solo lectura) |
| `IsComplete` | `bool` | `true` si un clip no-looping ha terminado |
| `PlaybackSpeed` | `float` | Multiplicador global de velocidad (defecto: 1.0) |
| `OnComplete` | `Action?` | Callback disparado al completar un clip no-looping |

### Métodos

| Método | Descripción |
|---|---|
| `Play()` | Inicia o reanuda; resetea si `IsComplete` |
| `Pause()` | Congela el frame actual |
| `Resume()` | Alias de `Play()` |
| `Stop()` | Para y resetea al frame 0 |
| `Update(GameTime)` | Avanza la animación; llamar cada frame |

### Velocidad efectiva

La velocidad real = `Animation.SpeedMultiplier × AnimatedSprite.PlaybackSpeed`.

---

## AnimationStateMachine

Gestiona un conjunto de clips nombrados y controla cuál está activo.

### Métodos

| Método | Descripción |
|---|---|
| `Register(string name, Animation)` | Registra un estado; lanza si ya existe |
| `Unregister(string name)` | Elimina un estado |
| `Play(string name)` | Activa el estado; no-op si ya es el activo |
| `Update(GameTime)` | Avanza la animación activa |
| `Draw(SpriteBatch, Vector2)` | Dibuja el frame actual |

### Propiedad

| Propiedad | Tipo | Descripción |
|---|---|---|
| `CurrentState` | `string?` | Nombre del estado activo; `null` si ninguno |

---

## Behaviours ECS

### AnimatedSpriteBehaviour

Componente ECS que controla un `AnimatedSprite`. Llama a `Update` y `Draw` automáticamente.

### AnimationStateMachineBehaviour

Componente ECS que controla una `AnimationStateMachine`.

---

## Ejemplo completo: personaje con idle/run/jump

```csharp
public sealed class CharacterAnimator : GameBehaviour
{
    private AnimationStateMachine _fsm = null!;
    private RigidBody2D _rb = null!;
    private bool _isGrounded;

    public override void Awake()
    {
        _rb = Entity.GetComponent<RigidBody2D>()!;
    }

    // Llamado desde LoadContent de la Scene con la textura ya cargada
    public void InitAnimations(Texture2D sheet)
    {
        _fsm = new AnimationStateMachine();

        // Idle: fila 0, 4 frames de 32×48
        var idleFrames = CreateFrames(sheet, row: 0, count: 4, frameW: 32, frameH: 48);
        _fsm.Register("idle", new Animation(idleFrames, TimeSpan.FromMilliseconds(150)));

        // Run: fila 1, 6 frames
        var runFrames = CreateFrames(sheet, row: 1, count: 6, frameW: 32, frameH: 48);
        _fsm.Register("run", new Animation(runFrames, TimeSpan.FromMilliseconds(80)));

        // Jump: fila 2, 3 frames, sin loop
        var jumpFrames = CreateFrames(sheet, row: 2, count: 3, frameW: 32, frameH: 48);
        _fsm.Register("jump", new Animation(jumpFrames, TimeSpan.FromMilliseconds(100))
        {
            IsLooping = false
        });

        _fsm.Play("idle");
    }

    private static List<TextureRegion> CreateFrames(
        Texture2D sheet, int row, int count, int frameW, int frameH)
    {
        var frames = new List<TextureRegion>(count);
        for (int i = 0; i < count; i++)
            frames.Add(new TextureRegion(sheet, i * frameW, row * frameH, frameW, frameH));
        return frames;
    }

    public override void Update(GameTime gameTime)
    {
        float vy = _rb.LinearVelocity.Y;
        float vx = _rb.LinearVelocity.X;

        // Lógica de transición de estado
        if (!_isGrounded)
        {
            _fsm.Play("jump");
        }
        else if (MathF.Abs(vx) > 10f)
        {
            _fsm.Play("run");
        }
        else
        {
            _fsm.Play("idle");
        }

        _fsm.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        _fsm.Draw(spriteBatch, Entity.Transform.Position2d);
    }
}
```

---

## Animación de una sola vez con callback

```csharp
var deathAnim = new AnimatedSprite(deathAnimation);
deathAnim.OnComplete += () =>
{
    // La animación de muerte terminó
    Entity.World.Destroy(Entity);
};
deathAnim.Play();
```

---

## Ver también

- [Sprites →](sprites.md)
- [ECS GameBehaviour →](../02-ecs/game-behaviour.md)
