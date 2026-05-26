# AudioMixer y Canales

**Namespace:** `Alca.MonoGame.Kernel.Audio`

El `AudioMixer` divide el audio en canales independientes con volumen y mute por canal. Disponible como `Core.Audio.Mixer`.

---

## AudioMixer

### Canales predefinidos

| Canal | Constante | Descripción |
|---|---|---|
| `Master` | `"Master"` | Canal raíz; su volumen multiplica a todos los demás |
| `Music` | `"Music"` | Música de fondo |
| `Sfx` | `"SFX"` | Efectos de sonido de juego |
| `Ambient` | `"Ambient"` | Sonidos ambientales |

### Métodos

| Método | Descripción |
|---|---|
| `RegisterChannel(name, volume)` | Registra un canal nuevo o devuelve el existente con ese nombre |
| `GetChannel(name)` | Devuelve el canal o `null` si no existe |
| `HasChannel(name)` | Comprueba si el canal existe |

---

## AudioMixerChannel

Canal individual con volumen y mute independientes.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Name` | `string` | Nombre del canal |
| `Volume` | `float` | Volumen 0–1 (clamped) |
| `Muted` | `bool` | Si `true`, el volumen efectivo es 0 |
| `EffectiveVolume` | `float` | Volumen real: `0` si `Muted`, `Volume` si no |

---

## Ejemplo: pantalla de opciones de audio

```csharp
public sealed class AudioSettingsPanel : UIContainer
{
    private readonly AudioMixer _mixer;
    private readonly Slider _masterSlider;
    private readonly Slider _musicSlider;
    private readonly Slider _sfxSlider;
    private readonly Slider _ambientSlider;
    private readonly Checkbox _muteAll;

    public AudioSettingsPanel(SpriteFont font, Texture2D pixel)
    {
        _mixer = Core.Audio.Mixer;

        _masterSlider = CreateSlider(pixel, _mixer.Master.Volume);
        _musicSlider  = CreateSlider(pixel, _mixer.Music.Volume);
        _sfxSlider    = CreateSlider(pixel, _mixer.Sfx.Volume);
        _ambientSlider = CreateSlider(pixel, _mixer.Ambient.Volume);

        _masterSlider.ValueChanged  += v => _mixer.Master.Volume  = v;
        _musicSlider.ValueChanged   += v => _mixer.Music.Volume   = v;
        _sfxSlider.ValueChanged     += v => _mixer.Sfx.Volume     = v;
        _ambientSlider.ValueChanged += v => _mixer.Ambient.Volume = v;

        _muteAll = new Checkbox(font, "Silenciar todo") { IsChecked = _mixer.Master.Muted };
        _muteAll.CheckedChanged += muted => _mixer.Master.Muted = muted;

        var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8f };
        stack.Add(new Label { Text = "Master",  Font = font, Color = Color.White });
        stack.Add(_masterSlider);
        stack.Add(new Label { Text = "Música",  Font = font, Color = Color.White });
        stack.Add(_musicSlider);
        stack.Add(new Label { Text = "Efectos", Font = font, Color = Color.White });
        stack.Add(_sfxSlider);
        stack.Add(new Label { Text = "Ambiente",Font = font, Color = Color.White });
        stack.Add(_ambientSlider);
        stack.Add(_muteAll);
        Add(stack);
    }

    private static Slider CreateSlider(Texture2D pixel, float initialValue) =>
        new Slider(pixel) { MinValue = 0, MaxValue = 1, Value = initialValue };
}
```

---

## Canal personalizado

```csharp
// Crear un canal extra para voces
var voiceChannel = Core.Audio.Mixer.RegisterChannel("Voice", volume: 1f);

// Reproducir un efecto de sonido rutado por ese canal
var instance = Core.Audio.PlaySoundEffect(_voiceLine);
instance.Volume = voiceChannel.EffectiveVolume;

// Silenciar sólo las voces
voiceChannel.Muted = true;
```

---

## Notas

- El `AudioMixer` no aplica el volumen del canal automáticamente a los `SoundEffectInstance` existentes; es responsabilidad del código de reproducción multiplicar `channel.EffectiveVolume` al crear la instancia.
- `AudioMixerChannel` es `sealed`; para lógica extra (p. ej. ducking), extiende la integración en tu propia clase.

---

## Ver también

- [AudioController →](audio-controller.md)
- [Audio espacial →](spatial-audio.md)
