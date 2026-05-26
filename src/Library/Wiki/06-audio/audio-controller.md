# AudioController

**Namespace:** `Alca.MonoGame.Kernel.Audio`

`AudioController` es el punto de entrada al sistema de audio. Disponible como `Core.Audio`.

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsMuted` | `bool` | Estado global de mute |
| `SongVolume` | `float` | Volumen de la música (0–1); devuelve 0 si muted |
| `SoundEffectVolume` | `float` | Volumen de efectos (0–1); devuelve 0 si muted |
| `ListenerPosition` | `Vector3` | Posición actual del oyente 3D |
| `IsDisposed` | `bool` | Si el controller ya fue liberado |

---

## Métodos

| Método | Descripción |
|---|---|
| `Update()` | Limpia las instancias de SFX que ya terminaron (llamado automáticamente) |
| `PlaySoundEffect(effect, volume, pitch, pan, loop)` | Reproduce un `SoundEffect` y devuelve su instancia |
| `PlaySong(song, isRepeating)` | Reproduce una `Song` (estático) |
| `PauseAudio()` | Pausa todos los sonidos y canciones activos |
| `ResumeAudio()` | Reanuda el audio pausado |
| `MuteAudio()` | Guarda los volúmenes actuales y los pone a cero |
| `UnmuteAudio()` | Restaura los volúmenes previos al mute |
| `ToggleMute()` | Alterna el estado de mute |
| `CreatePool(effect, capacity)` | Crea un `SoundEffectPool` para reproducción zero-alloc |
| `UpdateListener(position, forward)` | Actualiza la posición y dirección del oyente 3D |
| `ApplySpatialAudio(instance, emitter)` | Aplica posicionamiento 3D a una instancia en reproducción |
| `Dispose()` | Libera todos los recursos de audio |

---

## Ejemplo: juego con música y efectos

```csharp
public sealed class GameplayScene : Scene
{
    private Song _bgMusic = null!;
    private SoundEffect _jumpSfx = null!;
    private SoundEffect _coinSfx = null!;

    public override void LoadContent()
    {
        _bgMusic = Content.Load<Song>("Audio/Music/gameplay_theme");
        _jumpSfx = Content.Load<SoundEffect>("Audio/SFX/jump");
        _coinSfx = Content.Load<SoundEffect>("Audio/SFX/coin");
    }

    public override void PostInitialize()
    {
        base.PostInitialize();
        AudioController.PlaySong(_bgMusic, isRepeating: true);
        Core.Audio.SongVolume = 0.6f;
    }

    // Llamado desde PlayerController al saltar:
    public void OnJump()
    {
        Core.Audio.PlaySoundEffect(_jumpSfx, volume: 0.8f, pitch: 0f);
    }

    // Llamado al recoger una moneda:
    public void OnCoinCollected()
    {
        Core.Audio.PlaySoundEffect(_coinSfx, volume: 1.0f, pitch: 0.2f);
    }
}
```

---

## Notas

- `PlaySoundEffect` devuelve el `SoundEffectInstance`; si no necesitas controlar la reproducción, puedes ignorarlo.
- `AudioController.Update()` es llamado automáticamente por el `Core`; no es necesario llamarlo manualmente.
- Para audio espacial, usa `SpatialAudioSource` como componente ECS en lugar de llamar a `PlaySoundEffect` directamente.

---

## Ver también

- [AudioMixer →](audio-mixer.md)
- [Audio espacial →](spatial-audio.md)
