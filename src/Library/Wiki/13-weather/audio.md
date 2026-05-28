# WeatherAudioLayer

**Namespace:** `Alca.MonoGame.Kernel.Weather`

`WeatherAudioLayer` gestiona el audio atmosférico: tres canales de loop ambiente (lluvia, viento, trueno) y un pool pre-asignado para truenos espaciales de impacto. Los volúmenes se interpolan suavemente hacia los targets del perfil activo — cero asignaciones en `Update`.

---

## Ciclo de vida

```csharp
// 1. Crear
var audio = new WeatherAudioLayer();

// 2. Cargar sonidos (cualquier argumento puede ser null)
audio.LoadSounds(
    rainSfx:         Content.Load<SoundEffect>("weather/rain_loop"),
    windSfx:         Content.Load<SoundEffect>("weather/wind_loop"),
    thunderAmbSfx:   Content.Load<SoundEffect>("weather/thunder_ambient"),
    thunderStrikeSfx: Content.Load<SoundEffect>("weather/thunder_strike")
);

// 3. (Opcional) Configurar
audio.FadeSpeed = 2f;                      // velocidad de interpolación de volumen (u/s)
audio.Channel   = myMixer.WeatherChannel;  // routing de AudioMixer

// 4. Registrar en WeatherWorld
weatherWorld.EnableAudio(audio);
weatherWorld.AudioController = Core.Audio;  // necesario para audio espacial

// 5. Limpieza
audio.Dispose();
```

---

## Propiedades de configuración

| Propiedad | Tipo | Default | Descripción |
|---|---|---|---|
| `FadeSpeed` | `float` | `1.0` | Velocidad de interpolación de volumen en unidades/segundo. Valores altos = transiciones más bruscas. |
| `Channel` | `AudioMixerChannel?` | `null` | Canal de `AudioMixer` para routing centralizado de volumen |

---

## LoadSounds

```csharp
void LoadSounds(
    SoundEffect? rainSfx,
    SoundEffect? windSfx,
    SoundEffect? thunderAmbSfx,
    SoundEffect? thunderStrikeSfx,
    int thunderPoolSize = 4)
```

- Los tres primeros parámetros crean `SoundEffectInstance` en loop.
- `thunderStrikeSfx` rellena un pool pre-asignado de `thunderPoolSize` instancias para truenos espaciales.
- Pasar `null` para cualquier sonido deshabilita ese canal permanentemente.

---

## Volúmenes por perfil

`WeatherAudioLayer` lee los campos de volumen del `WeatherProfile` activo y los interpola usando `FadeSpeed`:

| Campo del perfil | Canal |
|---|---|
| `RainVolume` | Loop de lluvia |
| `WindVolume` | Loop de viento |
| `ThunderVolume` | Loop de trueno ambiente |

Los truenos de impacto no tienen volumen objetivo — se reproducen con `ThunderVolume` del perfil al dispararse.

---

## Audio espacial de truenos

Cuando `LightningController` dispara un rayo, llama a `WeatherAudioLayer.PlayThunderStrike`:

```csharp
// Llamado internamente por LightningController
audioLayer.PlayThunderStrike(strikePosition, audioController);
```

El pool garantiza hasta `thunderPoolSize` truenos simultáneos sin asignaciones. Si todas las instancias del pool están ocupadas, el trueno adicional se descarta.

---

## Integración con AudioMixer

```csharp
var mixer = new AudioMixer();
var weatherChannel = mixer.CreateChannel("Weather", volume: 0.8f);

var audio = new WeatherAudioLayer { Channel = weatherChannel };
audio.LoadSounds(rain, wind, thunderAmb, thunderStrike);
weatherWorld.EnableAudio(audio);

// Silenciar toda la lluvia con fade
weatherChannel.FadeTo(0f, duration: 2f);
```

---

## Limpieza

```csharp
audio.Dispose();
// Detiene y libera todos los SoundEffectInstance y el pool de truenos
```

---

## Ejemplo completo

```csharp
protected override void PostInitialize()
{
    var weatherWorld = new WeatherWorld();
    _world.WeatherWorld = weatherWorld;
    _world.AudioController = Core.Audio;

    var audio = new WeatherAudioLayer { FadeSpeed = 1.5f };
    audio.LoadSounds(
        rainSfx:          Content.Load<SoundEffect>("sfx/rain"),
        windSfx:          Content.Load<SoundEffect>("sfx/wind"),
        thunderAmbSfx:    Content.Load<SoundEffect>("sfx/thunder_amb"),
        thunderStrikeSfx: Content.Load<SoundEffect>("sfx/thunder_strike"),
        thunderPoolSize:  6
    );
    weatherWorld.EnableAudio(audio);

    // Lightning necesita audio para truenos espaciales
    var lightning = new LightningController(weatherWorld, _world)
    {
        AudioController = Core.Audio,
        AudioLayer      = audio,
    };
    weatherWorld.EnableLightning(lightning);

    weatherWorld.SetWeather(WeatherTypeId.Thunderstorm);
}
```

---

## Ver también

- [Visión general →](overview.md)
- [WeatherWorld (API) →](weather-world.md)
- [LightningController →](lightning.md)
- [Audio →](../06-audio/overview.md)
