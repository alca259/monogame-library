# GameBehaviour

**Namespace:** `Alca.MonoGame.Kernel.ECS`
**Hereda de:** (ninguna clase, es `abstract`)

`GameBehaviour` es la clase base para toda la lógica de componentes. Solo se invoca `Update` y `Draw` si la subclase los sobreescribe — la detección se hace por reflexión **una sola vez** al añadir el comportamiento, sin coste por frame.

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Entity` | `GameEntity` | Entidad a la que está adjunto. Garantizado no-null desde `Awake`. |
| `EntityOrNull` | `GameEntity?` | Versión nullable; disponible solo en subclases (`protected`). |
| `Enabled` | `bool` | Si `false`, se omiten `Update` y `Draw` (defecto: `true`). |

---

## Ciclo de vida

### `Awake()`

Se llama **inmediatamente** al añadir el comportamiento a la entidad con `Add<T>()` o `AddComponent<T>()`. Úsalo para cachear referencias a componentes hermanos.

```csharp
public override void Awake()
{
    // Entity ya está disponible aquí
    _rb = Entity.GetComponent<RigidBody2D>()!;
    _sprite = Entity.GetComponent<SpriteRendererBehaviour>()!;
}
```

### `Start()`

Se llama **antes del primer `Update`** del frame en que la entidad entra al mundo. A diferencia de `Awake`, todas las entidades del mundo ya existen en este punto.

```csharp
public override void Start()
{
    // Busca el objetivo más cercano (todas las entidades ya existen)
    _world = Entity.World;
    _world.FindComponents<EnemyBehaviour>(_enemyList);
}
```

### `Update(GameTime gameTime)`

Se llama **cada frame** si está sobreescrito y `Enabled == true`.

> **REGLA DE RENDIMIENTO:** No crear objetos de tipo referencia dentro de `Update`. No usar `new List<T>()`, `new string`, concatenación de strings, ni LINQ. Pre-asigna en `Awake`/`Start`.

```csharp
private Vector2 _movement; // pre-asignado como campo

public override void Update(GameTime gameTime)
{
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

    _movement = Vector2.Zero; // struct — safe
    if (Core.Input.IsKeyHeld(Keys.D)) _movement.X += 1f;
    if (Core.Input.IsKeyHeld(Keys.A)) _movement.X -= 1f;

    Entity.Transform.Translate(new Vector3(_movement * Speed * dt, 0f));
}
```

### `Draw(GameTime gameTime, SpriteBatch spriteBatch)`

Se llama **cada frame de render** si está sobreescrito. Solo debe contener llamadas de renderizado — no lógica.

```csharp
public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
{
    spriteBatch.Draw(
        _debugTexture,
        Entity.Transform.Position2d,
        Color.Red * 0.5f);
}
```

### `OnDestroy()`

Se llama cuando la entidad es destruida con `GameWorld.Destroy(entity)`. Úsalo para liberar recursos o cancelar subscripciones.

```csharp
public override void OnDestroy()
{
    EventBus.Unsubscribe<DamageEvent>(OnDamage);
    _soundSource?.Stop();
}
```

---

## Ejemplo completo: PlayerController

```csharp
using Alca.MonoGame.Kernel.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public sealed class PlayerController : GameBehaviour
{
    private const float Speed = 200f;
    private const float JumpForce = 400f;

    private RigidBody2D _rb = null!;
    private bool _isGrounded;

    // Campos pre-asignados para Update (no heap allocations)
    private readonly List<RaycastHit2D> _groundHits = new(4);

    public override void Awake()
    {
        _rb = Entity.GetComponent<RigidBody2D>()!;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var input = Core.Input;

        // Movimiento horizontal
        float horizontal = 0f;
        if (input.IsKeyHeld(Keys.D)) horizontal += 1f;
        if (input.IsKeyHeld(Keys.A)) horizontal -= 1f;

        _rb.LinearVelocity = new Vector2(horizontal * Speed, _rb.LinearVelocity.Y);

        // Salto
        if (_isGrounded && input.IsKeyPressed(Keys.Space))
            _rb.LinearVelocity = new Vector2(_rb.LinearVelocity.X, -JumpForce);
    }

    public override void OnDestroy()
    {
        // Limpieza si fuera necesaria
    }
}
```

---

## Acceder a `Entity` de forma segura

`Entity` lanza `InvalidOperationException` si se accede antes de `Awake`. Si necesitas acceder en el constructor, usa `EntityOrNull` (solo disponible en subclases):

```csharp
public override void Awake()
{
    // Correcto: Entity disponible desde aquí
    var transform = Entity.Transform;
}

// En el constructor — NO acceder a Entity aquí
public PlayerController() { }
```

---

## Deshabilitar un comportamiento

```csharp
// Desde otro comportamiento:
var controller = Entity.GetComponent<PlayerController>()!;
controller.Enabled = false; // pausa Update y Draw del controller
```

---

## Notas

- Los métodos vacíos (`Awake`, `Start`, `Update`, `Draw`, `OnDestroy`) tienen costo cero si no se sobreescriben.
- Un mismo tipo solo puede añadirse una vez por entidad. `Add<T>` sobreescribe si ya existe.
- `GameBehaviour` es `abstract` — no se puede instanciar directamente.

---

## Ver también

- [GameEntity →](game-entity.md)
- [TransformBehaviour →](transform.md)
- [ECS Overview →](overview.md)
