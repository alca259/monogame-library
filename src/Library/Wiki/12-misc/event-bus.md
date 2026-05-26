# Event Bus

**Namespace:** `Alca.MonoGame.Kernel.Events`

El sistema de eventos desacopla los publicadores de los suscriptores mediante un bus global (`EventBus`) o canales locales (`EventChannel`) de vida acotada.

---

## EventBus

Bus estático global. No requiere instanciación.

### Métodos

| Método | Descripción |
|---|---|
| `Subscribe<T>(handler)` | Suscribe con prioridad 0 |
| `SubscribeWithPriority<T>(handler, priority)` | Suscribe con prioridad (mayor = primero) |
| `SubscribeOnce<T>(handler)` | Suscribe y se desuscribe automáticamente tras la primera invocación |
| `Unsubscribe<T>(handler)` | Elimina el handler |
| `Publish<T>(evt)` | Despacha el evento a todos los handlers en orden de prioridad |
| `PublishCancellable<T>(evt)` | Despacha; se detiene si `evt.IsCancelled = true` |
| `Clear()` | Elimina todas las suscripciones |

---

## ICancellableEvent

```csharp
public interface ICancellableEvent
{
    bool IsCancelled { get; set; }
}
```

Implementa esta interfaz en eventos que pueden ser interrumpidos por handlers de alta prioridad.

---

## EventChannel

Canal local con ciclo de vida explícito. Ideal para sistemas de objetos con vida limitada (escenas, entidades).

```csharp
public sealed class EventChannel : IDisposable
{
    void Subscribe<T>(Action<T> handler)
    void SubscribeWithPriority<T>(Action<T> handler, int priority)
    void SubscribeOnce<T>(Action<T> handler)
    void Unsubscribe<T>(Action<T> handler)
    void Publish<T>(T evt)
    void PublishCancellable<T>(T evt) where T : ICancellableEvent
    void Clear()
    void Dispose()  // llama a Clear
}
```

---

## Ejemplo: sistema de logros

```csharp
// 1. Definir eventos
public readonly struct EnemyKilledEvent { public string EnemyType { get; init; } }
public readonly struct ItemPickedUpEvent { public string ItemId { get; init; } }

// 2. Sistema de logros suscrito al bus global
public sealed class AchievementSystem
{
    private int _killCount;

    public AchievementSystem()
    {
        EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
    }

    private void OnEnemyKilled(EnemyKilledEvent e)
    {
        _killCount++;
        if (_killCount >= 100)
            UnlockAchievement("Centurion");
    }

    private void OnItemPickedUp(ItemPickedUpEvent e)
    {
        if (e.ItemId == "legendary_sword")
            UnlockAchievement("Chosen One");
    }

    private void UnlockAchievement(string id) { /* ... */ }
}

// 3. Publicar eventos desde cualquier lugar del juego
EventBus.Publish(new EnemyKilledEvent { EnemyType = "Goblin" });
EventBus.Publish(new ItemPickedUpEvent { ItemId = "legendary_sword" });
```

---

## Ejemplo: evento cancelable (ataque interceptado)

```csharp
public struct AttackEvent : ICancellableEvent
{
    public GameEntity Attacker { get; init; }
    public GameEntity Target   { get; init; }
    public float Damage        { get; init; }
    public bool IsCancelled    { get; set; }
}

// El escudo intercepta el ataque con prioridad alta
EventBus.SubscribeWithPriority<AttackEvent>(e =>
{
    if (e.Target.HasTag("shielded"))
        e.IsCancelled = true;  // el atacante no causa daño
}, priority: 10);

// El sistema de daño actúa con prioridad normal
EventBus.Subscribe<AttackEvent>(e =>
{
    e.Target.GetComponent<HealthComponent>()?.TakeDamage(e.Damage);
});
```

---

## Ejemplo: canal local por escena

```csharp
public sealed class GameplayScene : Scene
{
    private readonly EventChannel _channel = new();

    public override void PostInitialize()
    {
        base.PostInitialize();
        _channel.Subscribe<PlayerDeadEvent>(_ => OnPlayerDead());
    }

    public override void UnloadContent()
    {
        base.UnloadContent();
        _channel.Dispose();  // limpia todas las suscripciones
    }

    private void OnPlayerDead() =>
        Core.SceneManager.RequestChange(new GameOverScene());
}
```

---

## Notas

- Desuscribe siempre los handlers del `EventBus` global cuando el suscriptor es destruido; de lo contrario quedan referencias colgantes.
- Usa `EventChannel` en lugar de `EventBus` para sistemas con vida acotada — `Dispose` limpia todo automáticamente.
- `PublishCancellable` despacha en orden de prioridad y para en el primer handler que ponga `IsCancelled = true`.

---

## Ver también

- [ECS GameBehaviour →](../02-ecs/game-behaviour.md)
- [Máquina de estados →](state-machine.md)
