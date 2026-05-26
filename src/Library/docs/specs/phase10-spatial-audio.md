# Spec: Fase 10.x — Spatial Audio 2.5D

## Objetivo

Mejorar el sistema de audio para dar soporte pleno a audio espacial 2.5D, considerando los tres ejes (X, Y, Z).  
Añadir un mezclador de canales (`AudioMixer`), fuentes/oyentes como `GameBehaviour`, y zonas de audio ambiental.

---

## Milestone 1 — AudioMixerChannel

**`Audio/AudioMixerChannel.cs`** — `sealed class AudioMixerChannel`

- `Name` (string, read-only) — nombre del canal
- `Volume` (float, 0–1) — volumen del canal; el setter clampea automáticamente
- `Muted` (bool) — silencia el canal
- `EffectiveVolume` (float, read-only) — 0 cuando muted, Volume en caso contrario
- Constructor `public AudioMixerChannel(string name, float volume = 1f)`

---

## Milestone 2 — AudioMixer

**`Audio/AudioMixer.cs`** — `sealed class AudioMixer`

Canales predefinidos (instanciados en el constructor):
- `Master` — canal raíz
- `Music` — música de fondo
- `Sfx` — efectos de sonido
- `Ambient` — sonido ambiental

API pública:
- `RegisterChannel(string name, float volume = 1f)` → `AudioMixerChannel` — idempotente (si ya existe, lo devuelve)
- `GetChannel(string name)` → `AudioMixerChannel?`
- `HasChannel(string name)` → bool

Constantes de nombre: `MasterChannelName`, `MusicChannelName`, `SfxChannelName`, `AmbientChannelName`

---

## Milestone 3 — AudioController (extensión)

**`Audio/AudioController.cs`** — **MODIFICAR**

- `ListenerPosition` (Vector3, read-only) — expone la posición actual del `AudioListener3D`
- `ApplySpatialAudio(SoundEffectInstance, AudioEmitter3D)` — delega en `emitter.Apply3D(instance, _listener)`

---

## Milestone 4 — GameWorld (extensión)

**`ECS/GameWorld.cs`** — **MODIFICAR**

- `AudioController?` — expone el controlador de audio para que los `GameBehaviour` de audio lo usen vía `Entity.World.AudioController`
- `AudioMixer?` — expone el mezclador

---

## Milestone 5 — SpatialAudioSource

**`Audio/SpatialAudioSource.cs`** — `sealed class SpatialAudioSource : GameBehaviour`

Emite audio 3D desde la posición world-space de la entidad. Los tres ejes (X, Y, Z) se propagan al `AudioEmitter3D` cada frame.

Propiedades:
- `Sound` (SoundEffect?) — efecto de sonido a emitir
- `Volume` (float, 0–1, default 1)
- `Pitch` (float, -1–1, default 0)
- `Loop` (bool)
- `PlayOnAwake` (bool)
- `MixerChannel` (AudioMixerChannel?) — enrutamiento de volumen
- `State` (SoundState, read-only) — `Stopped` antes de Play

Ciclo de vida:
- `Awake()` — obtiene `AudioController` de `Entity.World.AudioController`; llama `Play()` si `PlayOnAwake`
- `Update(GameTime)` — sync `_emitter.Position = Transform.Position` (Vector3); aplica volumen de canal; llama `ApplySpatialAudio`
- `OnDestroy()` — llama `Stop()`

Métodos: `Play()`, `Stop()`, `Pause()`, `Resume()`

> Sin `AudioController` en el world, los métodos de reproducción son no-op (degradación silenciosa).

---

## Milestone 6 — SpatialAudioListener

**`Audio/SpatialAudioListener.cs`** — `sealed class SpatialAudioListener : GameBehaviour`

Actualiza la posición del oyente 3D del `AudioController` desde la posición world de la entidad.

Propiedades:
- `IsMain` (bool, default true) — solo el listener principal actualiza el `AudioController`

Ciclo de vida:
- `Awake()` — obtiene `AudioController` de `Entity.World.AudioController`
- `Update(GameTime)` — si `IsMain && controller != null`: `controller.UpdateListener(Transform.Position, forwardFromMatrix)` donde forward se extrae de `LocalToWorldMatrix`

---

## Milestone 7 — AudioZone

**`Audio/AudioZone.cs`** — `sealed class AudioZone : GameBehaviour`

Zona esférica de audio ambiental. Considera los tres ejes (X, Y, Z) para el cálculo de distancia.  
Cuando el oyente entra en la zona, el sonido se fade-in; cuando sale, fade-out.

Propiedades:
- `AmbientSound` (SoundEffect?) — sonido ambiental en loop
- `Radius` (float, default 50) — radio en unidades mundo
- `FadeInTime` (float, segundos, default 1)
- `FadeOutTime` (float, segundos, default 1)
- `MixerChannel` (AudioMixerChannel?)

Ciclo de vida:
- `Awake()` — obtiene `AudioController` de `Entity.World.AudioController`
- `Update(GameTime)` — calcula `dist = Vector3.Distance(Transform.Position, controller.ListenerPosition)`; fade lineal `volume = 1 - dist/Radius`; para/inicia la instancia según estado
- `OnDestroy()` — para y libera la instancia

Atenuación: lineal `1 - (dist / radius)` — a distancia 0 volumen máximo, en el borde volumen 0.

---

## Tests esperados

`UnitTests/Audio/AudioMixerChannelTests.cs`:
- `DefaultVolume_IsOne`
- `Volume_IsClamped_ToRange`
- `Muted_WhenTrue_EffectiveVolumeIsZero`
- `Muted_WhenFalse_EffectiveVolumeEqualsVolume`
- `Name_IsPreserved`

`UnitTests/Audio/AudioMixerTests.cs`:
- `HasDefaultChannels_AfterConstruction`
- `GetChannel_KnownName_ReturnsChannel`
- `GetChannel_UnknownName_ReturnsNull`
- `RegisterChannel_NewName_CreatesChannel`
- `RegisterChannel_DuplicateName_ReturnsExisting`
- `HasChannel_ReturnsTrueForKnownChannels`

`UnitTests/Audio/SpatialAudioSourceTests.cs`:
- `DefaultVolume_IsOne`
- `DefaultPitch_IsZero`
- `DefaultLoop_IsFalse`
- `DefaultPlayOnAwake_IsFalse`
- `State_BeforePlay_IsStopped`
- `MixerChannel_CanBeAssigned`

`UnitTests/Audio/SpatialAudioListenerTests.cs`:
- `DefaultIsMain_IsTrue`
- `Awake_WithNoController_DoesNotThrow`
- `Update_SyncsListenerPosition_AllThreeAxes`

`UnitTests/Audio/AudioZoneTests.cs`:
- `DefaultRadius_Is50`
- `DefaultFadeInTime_Is1`
- `DefaultFadeOutTime_Is1`
- `Awake_WithNoController_DoesNotThrow`
- `Update_WithNoAmbientSound_DoesNotThrow`

---

## Estructura de carpetas

```
Audio/
├── AudioController.cs        (modificado)
├── AudioEmitter3D.cs
├── AudioListener3D.cs
├── AudioMixer.cs             (nuevo)
├── AudioMixerChannel.cs      (nuevo)
├── AudioZone.cs              (nuevo)
├── SoundEffectPool.cs
├── SpatialAudioListener.cs   (nuevo)
└── SpatialAudioSource.cs     (nuevo)
```
