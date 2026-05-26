# Crossfade de Audio

**Namespace:** `Alca.MonoGame.Kernel.Audio`

`AudioCrossfader` gestiona transiciones suaves entre dos pistas de audio, mezclando gradualmente los volúmenes durante la duración indicada.

---

## AudioCrossfader

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsCrossfading` | `bool` | `true` mientras la transición está en curso |
| `CurrentVolume` | `float` | Volumen actual de la pista dominante (0–1) |

### Métodos

| Método | Descripción |
|---|---|
| `CrossfadeTo(track, duration, targetVolume)` | Inicia la transición a una nueva pista |
| `Stop(fadeOutDuration)` | Hace un fade out y detiene la reproducción |
| `Update(GameTime)` | Avanza el temporizador de crossfade; debe llamarse cada frame |
| `Dispose()` | Detiene y libera todas las pistas |

---

## Ejemplo: cambio de música al entrar en zona

```csharp
public sealed class MusicZone : GameBehaviour
{
    private readonly AudioCrossfader _fader = new();
    private SoundEffect _zoneTrack = null!;
    private bool _playerInside;

    public override void Awake()
    {
        _zoneTrack = Entity.Scene!.Content.Load<SoundEffect>("Audio/Music/dungeon_theme");
    }

    public override void Update(GameTime gameTime)
    {
        _fader.Update(gameTime);
    }

    public void OnPlayerEnter()
    {
        if (!_playerInside)
        {
            _playerInside = true;
            _fader.CrossfadeTo(_zoneTrack, duration: 2f, targetVolume: 0.7f);
        }
    }

    public void OnPlayerExit()
    {
        if (_playerInside)
        {
            _playerInside = false;
            _fader.Stop(fadeOutDuration: 1.5f);
        }
    }

    public override void OnDestroy()
    {
        _fader.Dispose();
    }
}
```

---

## Ejemplo: música de menú al empezar una partida

```csharp
public sealed class MainMenuScene : Scene
{
    private readonly AudioCrossfader _fader = new();
    private SoundEffect _menuTrack = null!;

    public override void LoadContent()
    {
        _menuTrack = Content.Load<SoundEffect>("Audio/Music/menu_theme");
    }

    public override void PostInitialize()
    {
        base.PostInitialize();
        _fader.CrossfadeTo(_menuTrack, duration: 1.5f);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _fader.Update(gameTime);
    }

    public override void UnloadContent()
    {
        base.UnloadContent();
        _fader.Stop(fadeOutDuration: 0.5f);
        // Dar un frame para el fade out antes de liberar
        _fader.Dispose();
    }
}
```

---

## Notas

- `AudioCrossfader` no se registra en el `Core`; créalo como campo privado de la escena o GameBehaviour que lo gestiona.
- Llama siempre a `Dispose()` en `UnloadContent` o `OnDestroy` para liberar las `SoundEffectInstance` internas.
- Si llamas a `CrossfadeTo` mientras ya hay una transición en curso, la pista anterior se detiene inmediatamente y comienza la nueva transición.

---

## Ver también

- [AudioController →](audio-controller.md)
- [Scenes →](../03-scenes/scene.md)
