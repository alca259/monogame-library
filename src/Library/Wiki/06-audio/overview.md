# Audio — Visión General

**Namespace:** `Alca.MonoGame.Kernel.Audio`

El sistema de audio envuelve la API de MonoGame y la extiende con un mixer por canales, audio espacial 3D, pools zero-alloc y crossfade de pistas.

---

## Arquitectura del pipeline

```
Core.Audio (AudioController)
    │
    ├── Core.Audio.Master (AudioMixerChannel)
    │       ├── Music  (AudioMixerChannel)
    │       ├── SFX    (AudioMixerChannel)
    │       └── Ambient(AudioMixerChannel)
    │
    ├── SoundEffectPool       ← reproducción zero-alloc de efectos
    ├── SoundEffectInstance[] ← instancias activas (limpiadas en Update)
    │
    └── AudioListener3D       ← posición del oyente global
            └── SpatialAudioSource (GameBehaviour)  ← emisores por entidad
```

---

## Clases principales

| Clase | Descripción |
|---|---|
| `AudioController` | Punto de entrada principal (`Core.Audio`) |
| `AudioMixer` | Gestor de canales (`Core.Audio.Mixer`) |
| `AudioMixerChannel` | Canal con volumen y mute independientes |
| `SoundEffectPool` | Pool de instancias reutilizables zero-alloc |
| `AudioCrossfader` | Transiciones suaves entre pistas de música |
| `SpatialAudioSource` | Emisor de audio 3D posicional (GameBehaviour) |
| `SpatialAudioListener` | Oyente 3D vinculado a una entidad (GameBehaviour) |
| `AudioZone` | Zona ambiental con fade in/out automático (GameBehaviour) |

---

## Ver también

- [AudioController →](audio-controller.md)
- [AudioMixer y canales →](audio-mixer.md)
- [Audio espacial →](spatial-audio.md)
- [Crossfade →](crossfade.md)
