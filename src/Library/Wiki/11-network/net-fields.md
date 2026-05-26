# NetFields — Campos Sincronizados

**Namespace:** `Alca.MonoGame.Kernel.Network.NetFields`

Los `NetField` son contenedores de valores que se serializan automáticamente cuando están marcados como dirty y se registran en un `NetworkIdentity`.

---

## NetField (clase base)

```csharp
public abstract class NetField
{
    public bool IsDirty { get; }
    public abstract void Serialize(ref NetworkWriter writer)
    public abstract void Deserialize(ref NetworkReader reader)
    public abstract void SetValue(object value)
    public abstract object? GetValue()
    protected void MarkDirty()
}
```

---

## Tipos concretos disponibles

| Clase | Tipo de valor | Evento de cambio |
|---|---|---|
| `NetInt` | `int` | `Action<int, int>` (prev, curr) |
| `NetFloat` | `float` | `Action<float, float>` |
| `NetVector2` | `Vector2` | `Action<Vector2, Vector2>` |

Todos exponen:
- `Value { get; set; }` — asignar marca el campo como dirty.
- Operador implícito de conversión al tipo nativo.
- `OnValueChanged` — disparado al deserializar el valor recibido.

---

## Ejemplo: puntuación compartida

```csharp
public sealed class SharedScore : GameBehaviour
{
    private NetworkIdentity _identity = null!;
    private readonly NetInt _score = new(0);
    private Label _scoreLabel = null!;

    public override void Awake()
    {
        _identity = Entity.GetComponent<NetworkIdentity>();
        _identity.RegisterField(_score);

        // Actualizar UI cuando se recibe un cambio del servidor
        _score.OnValueChanged += (_, newScore) =>
            _scoreLabel.Text = $"Puntos: {newScore}";
    }

    // Llamado sólo en el servidor al puntuar
    public void AddPoints(int points)
    {
        if (!_identity.IsServer) return;
        _score.Value += points;  // se replicará a todos los clientes en el próximo tick
    }
}
```

---

## Ejemplo: salud con retroalimentación visual

```csharp
private readonly NetFloat _health = new(100f);

public override void Awake()
{
    _identity.RegisterField(_health);
    _health.OnValueChanged += (prev, curr) =>
    {
        if (curr < prev)
            PlayHitAnimation();
        if (curr <= 0f)
            Die();
    };
}

// En el servidor, al recibir daño:
public void TakeDamage(float damage)
{
    if (!_identity.IsServer) return;
    _health.Value = Math.Max(0f, _health.Value - damage);
}
```

---

## NetworkWriter y NetworkReader

Para mensajes personalizados (`INetworkMessage`) se usa `NetworkWriter`/`NetworkReader` directamente, que son `ref struct` zero-alloc.

```csharp
public readonly struct SpawnMessage : INetworkMessage
{
    public ushort  MessageId => 10;
    public uint    EntityId  { get; init; }
    public Vector2 Position  { get; init; }
    public string  PrefabKey { get; init; }

    public void Serialize(ref NetworkWriter w)
    {
        w.Write(EntityId);
        w.Write(Position);
        w.WriteString(PrefabKey);
    }

    public void Deserialize(ref NetworkReader r)
    {
        Unsafe.AsRef(in EntityId)  = r.ReadUInt();
        Unsafe.AsRef(in Position)  = r.ReadVector2();
        Unsafe.AsRef(in PrefabKey) = r.ReadString();
    }
}
```

---

## Notas

- Los `NetField` sólo se envían cuando `IsDirty = true` (al cambiar `Value`). Una vez enviados, el flag se limpia.
- No llames a `MarkDirty()` manualmente desde fuera; está disponible sólo para subclases.
- Para tipos personalizados (Color, Rectangle…), implementa tu propio `NetField` heredando de la clase base.

---

## Ver también

- [NetworkIdentity →](network-identity.md)
- [Server y Client →](server-client.md)
