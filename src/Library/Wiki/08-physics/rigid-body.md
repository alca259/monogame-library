# RigidBody2D

**Namespace:** `Alca.MonoGame.Kernel.Physics`

`RigidBody2D` es el cuerpo físico de una entidad. Sincroniza bidireccional-mente la posición y rotación con `TransformBehaviour`.

---

## Propiedades

### Configuración (antes de `Awake`)

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsStatic` | `bool` | Si `true`, el cuerpo no se mueve por fuerzas ni colisiones |
| `Mass` | `float` | Masa en kg |

### En tiempo de ejecución

| Propiedad | Tipo | Descripción |
|---|---|---|
| `LinearVelocity` | `Vector2` | Velocidad lineal actual (unidades/s) |
| `AngularVelocity` | `float` | Velocidad angular actual (radianes/s) |
| `LinearDamping` | `float` | Fricción con el aire para velocidad lineal |
| `AngularDamping` | `float` | Fricción con el aire para velocidad angular |
| `UseGravity` | `bool` | Si `false`, la gravedad no afecta a este cuerpo |

---

## Métodos

| Método | Descripción |
|---|---|
| `ApplyForce(force)` | Aplica una fuerza continua (acumulativa en el frame) |
| `ApplyImpulse(impulse)` | Aplica un impulso instantáneo |
| `ApplyTorque(torque)` | Aplica torsión angular |

---

## Ejemplo: personaje con física

```csharp
public sealed class PlayerController : GameBehaviour
{
    private RigidBody2D _rb = null!;
    private bool _onGround;
    private const float MoveSpeed = 200f;
    private const float JumpForce = 400f;

    public override void Awake()
    {
        _rb = Entity.GetComponent<RigidBody2D>();

        // Evitar que el personaje rote al chocar
        _rb.AngularDamping = float.MaxValue;

        // Detectar si está en el suelo
        var box = Entity.GetComponent<BoxCollider2D>();
        box.OnCollisionEnter += other =>
        {
            if (other.Entity.Name == "Ground")
                _onGround = true;
        };
        box.OnCollisionExit += other =>
        {
            if (other.Entity.Name == "Ground")
                _onGround = false;
        };
    }

    public override void Update(GameTime gameTime)
    {
        float dir = 0;
        if (Core.Input.IsKeyHeld(Keys.Left))  dir -= 1f;
        if (Core.Input.IsKeyHeld(Keys.Right)) dir += 1f;

        _rb.LinearVelocity = new Vector2(dir * MoveSpeed, _rb.LinearVelocity.Y);

        if (_onGround && Core.Input.IsKeyPressed(Keys.Space))
            _rb.ApplyImpulse(new Vector2(0, -JumpForce));
    }
}
```

Configuración de la entidad en la escena:

```csharp
var player = World!.CreateEntity("Player", new Vector2(400, 100));
player.Add(new RigidBody2D { Mass = 1f, UseGravity = true });
player.Add(new BoxCollider2D
{
    Size   = new Vector2(32, 48),
    Layer  = CollisionCategory.Player,
    Mask   = CollisionCategory.Terrain | CollisionCategory.Enemy
});
player.AddComponent<PlayerController>();
```

---

## Notas

- Configura `IsStatic` y `Mass` **antes** de que el cuerpo despierte (`Awake`).
- `ApplyForce` es frame-rate-dependent; prefiere `ApplyImpulse` para acciones puntuales como saltar.
- Modificar `LinearVelocity` directamente es válido para movimiento de plataformas; no acumula fuerzas.

---

## Ver también

- [Colliders →](colliders.md)
- [Physics2DWorld →](physics-world.md)
