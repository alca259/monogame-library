# Timers

**Namespace:** `Alca.MonoGame.Kernel.Timers`

`TimerManager` gestiona timers de un solo disparo y repetitivos con un pool zero-alloc. Disponible como `Core.Timers`.

---

## TimerManager

### Métodos

| Método | Descripción |
|---|---|
| `Schedule(seconds, callback)` | Ejecuta `callback` una vez después de `seconds` |
| `ScheduleRepeating(seconds, callback, maxFires)` | Ejecuta `callback` cada `seconds`; opcional `maxFires` |
| `CancelAll()` | Cancela todos los timers activos |
| `Update(gameTime)` | Avanza todos los timers (llamado automáticamente por el Core) |

Ambos métodos devuelven un `GameTimer` que puede controlarse individualmente.

---

## GameTimer

| Propiedad / Método | Descripción |
|---|---|
| `IsDone` | `true` cuando el timer ha terminado o fue cancelado |
| `Pause()` | Pausa el conteo de tiempo |
| `Resume()` | Reanuda desde donde se pausó |
| `Cancel()` | Cancela el timer sin ejecutar el callback |

---

## Ejemplo: respawn timer

```csharp
public sealed class PlayerRespawnSystem : GameBehaviour
{
    private GameTimer? _respawnTimer;
    private const float RespawnDelay = 3f;

    public void OnPlayerDied()
    {
        _respawnTimer = Core.Timers.Schedule(RespawnDelay, SpawnPlayer);
        ShowRespawnMessage();
    }

    private void SpawnPlayer()
    {
        var player = World!.CreateEntity("Player", spawnPoint);
        player.AddComponent<PlayerController>();
        HideRespawnMessage();
    }

    public override void OnDestroy()
    {
        _respawnTimer?.Cancel();
    }
}
```

---

## Ejemplo: cooldown de habilidad

```csharp
public sealed class AbilitySystem : GameBehaviour
{
    private bool _canUseAbility = true;
    private const float CooldownTime = 5f;

    public void UseAbility()
    {
        if (!_canUseAbility) return;

        _canUseAbility = false;
        TriggerAbilityEffect();

        Core.Timers.Schedule(CooldownTime, () =>
        {
            _canUseAbility = true;
            ShowAbilityReady();
        });
    }
}
```

---

## Ejemplo: contador con límite de disparos

```csharp
private int _waveCount;

private void StartWaveTimer()
{
    Core.Timers.ScheduleRepeating(
        seconds:  30f,
        callback: SpawnNextWave,
        maxFires: 5);  // 5 oleadas, luego el timer expira
}

private void SpawnNextWave()
{
    _waveCount++;
    // ... lanzar enemigos ...
}
```

---

## Notas

- `TimerManager.Update` es llamado automáticamente por el `Core`; no es necesario llamarlo manualmente.
- `GameTimer` es una clase pooled — no la almacenes más allá de su ciclo de vida; comprueba `IsDone` antes de usarla.
- Para animaciones con tiempo, prefiere `TweeningManager` sobre timers manuales.

---

## Ver también

- [Tweening →](tweening.md)
- [Event Bus →](event-bus.md)
