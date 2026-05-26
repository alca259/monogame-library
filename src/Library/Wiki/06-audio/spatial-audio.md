# Audio Espacial

**Namespace:** `Alca.MonoGame.Kernel.Audio`

El audio espacial permite posicionar fuentes sonoras en el mundo 3D. La atenuación por distancia es calculada por MonoGame (`SoundEffect.Apply3D`) y los componentes ECS sincronizan automáticamente la posición del emisor y el oyente cada frame.

---

## SpatialAudioSource

`GameBehaviour` que convierte una entidad en una fuente de sonido 3D.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Sound` | `SoundEffect?` | Efecto de sonido a emitir |
| `Volume` | `float` | Volumen base 0–1 |
| `Pitch` | `float` | Ajuste de tono −1 a 1 |
| `Loop` | `bool` | Si la reproducción es en bucle |
| `PlayOnAwake` | `bool` | Inicia la reproducción al despertar la entidad |
| `MixerChannel` | `AudioMixerChannel?` | Canal del mixer para escalar el volumen |
| `State` | `SoundState` | Estado actual de reproducción |

### Métodos

| Método | Descripción |
|---|---|
| `Play()` | Reproduce desde la posición actual de la entidad |
| `Stop()` | Detiene y libera la instancia activa |
| `Pause()` | Pausa la reproducción |
| `Resume()` | Reanuda la reproducción pausada |

---

## SpatialAudioListener

`GameBehaviour` que sincroniza la posición del oyente global con la entidad donde está adjunto.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsMain` | `bool` | Si `true`, alimenta el oyente global de `AudioController` (default: `true`) |

---

## SoundEffectPool

Pool zero-alloc de instancias de `SoundEffect`. Creado a través de `AudioController.CreatePool`.

### Constructor

```csharp
// Usando el factory del AudioController (recomendado):
SoundEffectPool pool = Core.Audio.CreatePool(footstepSfx, capacity: 8);
```

### Métodos

| Método | Descripción |
|---|---|
| `Play(volume, pitch, pan)` | Reproduce una instancia del pool |
| `StopAll()` | Detiene todas las instancias activas |
| `Dispose()` | Libera el pool |

---

## AudioZone

`GameBehaviour` que reproduce un sonido ambiental cuando el oyente entra en su radio de influencia, con fade in/out automático.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `AmbientSound` | `SoundEffect?` | Sonido ambiental de la zona |
| `Radius` | `float` | Radio de influencia en unidades de mundo (default: 50f) |
| `FadeInTime` | `float` | Segundos del fade in al entrar (default: 1f) |
| `FadeOutTime` | `float` | Segundos del fade out al salir (default: 1f) |
| `MixerChannel` | `AudioMixerChannel?` | Canal del mixer |

---

## Ejemplo: pasos con atenuación por distancia

```csharp
// En el GameBehaviour del jugador:
public sealed class PlayerAudio : GameBehaviour
{
    private SoundEffectPool _footsteps = null!;

    public override void Awake()
    {
        var sfx = Entity.GetComponent<ContentLoader>().Load<SoundEffect>("Audio/SFX/footstep");
        _footsteps = Core.Audio.CreatePool(sfx, capacity: 4);
    }

    public void OnStep()
    {
        _footsteps.Play(volume: 0.7f, pitch: Random.Shared.NextSingle() * 0.2f - 0.1f);
    }
}

// En la escena, asignar el listener al jugador:
var listenerBehaviour = playerEntity.AddComponent<SpatialAudioListener>();
listenerBehaviour.IsMain = true;

// En un NPC con sonido propio:
var npc = World!.CreateEntity("Guard", new Vector2(300, 200));
var source = npc.AddComponent<SpatialAudioSource>();
source.Sound = Content.Load<SoundEffect>("Audio/SFX/guard_patrol");
source.Loop = true;
source.PlayOnAwake = true;
source.Volume = 0.8f;
```

---

## Ejemplo: zona ambiental (bosque)

```csharp
var forestZone = World!.CreateEntity("ForestAmbient", new Vector2(0, 0));
var zone = forestZone.AddComponent<AudioZone>();
zone.AmbientSound = Content.Load<SoundEffect>("Audio/Ambient/forest_loop");
zone.Radius = 200f;
zone.FadeInTime  = 2f;
zone.FadeOutTime = 1.5f;
zone.MixerChannel = Core.Audio.Mixer.Ambient;
```

---

## Notas

- `SoundEffectPool.Play` no genera basura en el heap — reutiliza `SoundEffectInstance` previamente creadas.
- El audio 3D de MonoGame usa una unidad de distancia arbitraria; ajusta `Radius` según la escala de tu mundo.
- Si una entidad con `SpatialAudioSource` es destruida, `OnDestroy` libera la instancia activa automáticamente.

---

## Ver también

- [AudioController →](audio-controller.md)
- [AudioMixer →](audio-mixer.md)
- [ECS GameBehaviour →](../02-ecs/game-behaviour.md)
