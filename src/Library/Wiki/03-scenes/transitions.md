# Transiciones de Escena

**Namespace:** `Alca.MonoGame.Kernel.Scenes.Transitions`

El sistema de transiciones permite animar el cambio entre escenas mediante efectos visuales. `SceneManager.RequestChange`, `PushScene` y `PopScene` aceptan un parámetro opcional `ISceneTransition`; si se omite, se aplica `FadeTransition` negro por compatibilidad con versiones anteriores.

---

## Transiciones disponibles

| Nombre | Efecto visual | Requiere GPU / shader |
|---|---|---|
| `FadeTransition` | Fundido a color sólido (negro por defecto) | No |
| `SlideTransition` | Cortina deslizante desde un lado de la pantalla | No |
| `CircleWipeTransition` | Iris radial (apertura/cierre circular) | Opcional (fallback = fade) |
| `DissolveTransition` | Disolución pixelada usando textura de ruido | Opcional (fallback = fade) |

---

## Interfaz `ISceneTransition`

```csharp
public interface ISceneTransition
{
    bool IsTransitionOutComplete { get; }
    bool IsTransitionInComplete  { get; }

    void BeginTransitionOut(float duration);
    void BeginTransitionIn(float duration);
    void Update(float dt);
    void Draw(SpriteBatch spriteBatch, Viewport viewport);
    void Reset();
}
```

`SceneManager` controla el ciclo completo: llama a `BeginTransitionOut`, espera a `IsTransitionOutComplete`, carga la escena nueva, llama a `BeginTransitionIn` y espera a `IsTransitionInComplete`.

---

## Uso básico

```csharp
using Alca.MonoGame.Kernel.Scenes.Transitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// Transición de deslizamiento hacia la izquierda
Texture2D pixel = Content.Load<Texture2D>("Textures/pixel");

Core.SceneManager.RequestChange(
    new GameplayScene(),
    new SlideTransition(pixel, SlideDirection.Left));

// Fade a color personalizado
Core.SceneManager.RequestChange(
    new GameOverScene(),
    new FadeTransition(pixel, fadeColor: Color.DarkRed));

// Iris radial con shader (si está disponible)
Core.SceneManager.RequestChange(
    new NextLevelScene(),
    new CircleWipeTransition());

// Overlay con transición de disolución
Core.SceneManager.PushScene(
    new PauseMenuScene(),
    new DissolveTransition(noiseTexture: Content.Load<Texture2D>("Textures/noise")));
```

### `SlideDirection`

| Valor | Dirección de la cortina |
|---|---|
| `Left` | La cortina entra desde la derecha y sale por la izquierda |
| `Right` | La cortina entra desde la izquierda y sale por la derecha |
| `Up` | La cortina entra desde abajo y sale por arriba |
| `Down` | La cortina entra desde arriba y sale por abajo |

---

## Transición por defecto (compatibilidad)

Pasar `null` como transición (o usar la sobrecarga sin parámetro) aplica `FadeTransition` con color negro y la misma duración que el comportamiento original:

```csharp
// Equivalentes — ambos producen un fade negro de 0.3 s
Core.SceneManager.RequestChange(new MainMenuScene());
Core.SceneManager.RequestChange(new MainMenuScene(), transition: null);
```

El comportamiento es idéntico al de versiones anteriores; no es necesario actualizar código existente.

---

## Implementar una transición personalizada

Implementa `ISceneTransition` para crear efectos propios. El ejemplo siguiente realiza un flash blanco instantáneo:

```csharp
using Alca.MonoGame.Kernel.Scenes.Transitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class FlashTransition : ISceneTransition
{
    private readonly Texture2D _pixel;
    private float _alpha;
    private float _duration;
    private float _elapsed;
    private bool  _outComplete;
    private bool  _inComplete;

    public bool IsTransitionOutComplete => _outComplete;
    public bool IsTransitionInComplete  => _inComplete;

    public FlashTransition(Texture2D pixel) => _pixel = pixel;

    public void BeginTransitionOut(float duration)
    {
        _duration    = duration;
        _elapsed     = 0f;
        _outComplete = false;
    }

    public void BeginTransitionIn(float duration)
    {
        _duration   = duration;
        _elapsed    = 0f;
        _inComplete = false;
    }

    public void Update(float dt)
    {
        _elapsed += dt;
        float t = MathHelper.Clamp(_elapsed / _duration, 0f, 1f);

        if (!_outComplete)
        {
            _alpha = t;
            if (t >= 1f) _outComplete = true;
        }
        else
        {
            _alpha = 1f - t;
            if (t >= 1f) _inComplete = true;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        var color = Color.White * _alpha;
        spriteBatch.Draw(_pixel, viewport.Bounds, color);
    }

    public void Reset()
    {
        _alpha       = 0f;
        _elapsed     = 0f;
        _outComplete = false;
        _inComplete  = false;
    }
}
```

---

## Notas

- Las transiciones son superposiciones 2D dibujadas **encima** de la escena; no capturan ni congelan el frame anterior.
- `CircleWipeTransition` y `DissolveTransition` degradan a `FadeTransition` si el shader o la textura de ruido no están disponibles.
- `SceneManager` llama a `Reset()` automáticamente antes de reutilizar una transición.
- Las transiciones no tienen estado persistente entre cambios de escena; pueden reutilizarse o crearse nuevas instancias.
- El `SpriteBatch` usado en `Draw` está gestionado por `SceneManager`; no es necesario llamar a `Begin`/`End` dentro de la implementación.

---

## Ver también

- [SceneManager →](scene-manager.md)
- [Scene →](scene.md)
