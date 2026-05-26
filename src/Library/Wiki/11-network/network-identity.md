# NetworkIdentity y Replicación

**Namespace:** `Alca.MonoGame.Kernel.Network`

`NetworkIdentity` es el componente ECS que marca una entidad como replicada. Gestiona el envío de campos dirty y la aplicación de actualizaciones recibidas.

---

## NetworkIdentity

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `NetworkId` | `uint` | ID único de red asignado por el servidor |
| `IsOwner` | `bool` | `true` si este peer es el dueño de la entidad |
| `IsServer` | `bool` | `true` si se ejecuta en el servidor |
| `SyncInterval` | `float` | Segundos entre envíos de sincronización (default: 0.05f = 20 Hz) |

### Eventos

| Evento | Descripción |
|---|---|
| `OnFieldsApplied` | Disparado después de deserializar y aplicar campos recibidos |

### Métodos

| Método | Descripción |
|---|---|
| `RegisterField(field)` | Registra un `NetField` para sincronización automática |

---

## NetworkTransformSync

`GameBehaviour` que sincroniza automáticamente `Transform.Position2d` y `Transform.Rotation2d`.

```csharp
var sync = entity.AddComponent<NetworkTransformSync>();
// El NetworkIdentity lo registra automáticamente al hacer Awake
```

---

## NetworkPhysicsSync

`GameBehaviour` que sincroniza `RigidBody2D.LinearVelocity` y la posición física.

```csharp
var physSync = entity.AddComponent<NetworkPhysicsSync>();
```

---

## Ejemplo: jugador replicado

```csharp
public sealed class NetworkedPlayer : GameBehaviour
{
    private NetworkIdentity _identity = null!;
    private readonly NetVector2 _position = new(Vector2.Zero);
    private readonly NetFloat   _health   = new(100f);

    public override void Awake()
    {
        _identity = Entity.GetComponent<NetworkIdentity>();
        _identity.RegisterField(_position);
        _identity.RegisterField(_health);
        _identity.SyncInterval = 0.033f;  // ~30 Hz

        _health.OnValueChanged += (prev, curr) =>
            UpdateHealthBar(curr);
    }

    public override void Update(GameTime gameTime)
    {
        if (!_identity.IsOwner) return;

        // Mover con input local
        var pos = Entity.Transform.Position2d;
        if (Core.Input.IsKeyHeld(Keys.Right)) pos.X += 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (Core.Input.IsKeyHeld(Keys.Left))  pos.X -= 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Asignar al NetField — se marca dirty automáticamente
        _position.Value = pos;
        Entity.Transform.Position2d = pos;
    }

    private void UpdateHealthBar(float hp) { /* actualizar UI */ }
}
```

---

## Notas

- `NetworkIdentity.IsOwner` es `false` en peers remotos — usa este flag para bloquear el input en entidades que no controla el jugador local.
- Los campos sólo se envían cuando están dirty (`IsDirty = true`); si no cambian, no generan tráfico.
- `SyncInterval = 0.05f` (20 Hz) es un buen equilibrio; reduce a 10 Hz para entidades que se mueven poco.

---

## Ver también

- [NetFields →](net-fields.md)
- [Server y Client →](server-client.md)
