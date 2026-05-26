# SceneManager

**Namespace:** `Alca.MonoGame.Kernel.Scenes`

`SceneManager` gestiona qué escena está activa, las transiciones con efecto de fade y una pila de overlays. Se accede a través de `Core.SceneManager`.

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `CurrentScene` | `Scene?` | Escena activa (no cuenta los overlays) |
| `OverlayCount` | `int` | Número de overlays en la pila |
| `ActiveUIRoot` | `UIRoot?` | `UIRoot` de la escena activa; `null` si no tiene UI |

---

## Cambio de escena con fade

```csharp
public void RequestChange(Scene scene)
```

1. Desapila y destruye todos los overlays actuales.
2. Inicia el fade out (0.3 s).
3. Al llegar al negro: descarga la escena anterior (`UnloadContent`) e inicializa la nueva.
4. Inicia el fade in (0.3 s).

```csharp
// Navegar al menú principal
Core.SceneManager.RequestChange(new MainMenuScene());

// Ir a gameplay
Core.SceneManager.RequestChange(new GameplayScene());
```

> Las escenas cargadas mediante `RequestChange` siempre tienen un fade suave. Si quieres un cambio inmediato, llama `RequestChange` igualmente — el fade dura solo 0.3 s.

---

## Overlays (pila de escenas)

Los overlays se apilan encima de la escena actual **sin destruirla**. Son ideales para menús de pausa, diálogos de confirmación o HUD especiales.

### `PushScene(Scene overlay)`

Inicializa el overlay inmediatamente (sin fade) y lo apila:

```csharp
Core.SceneManager.PushScene(new PauseMenuScene());
// La escena de gameplay sigue corriendo debajo (según IsOverlay)
```

### `PopScene()`

Elimina y destruye el overlay superior:

```csharp
// Desde un botón "Reanudar" del menú de pausa:
Core.SceneManager.PopScene();
```

---

## Comportamiento de actualización y dibujado

| Situación | Update | Draw |
|---|---|---|
| Sin overlays | Escena actual | Escena actual |
| Con overlay en la pila | Solo el overlay superior | Escena base (si `IsOverlay = true`) + todos los overlays de abajo a arriba |

> Solo el **top** de la pila recibe `Update`. La escena base **no** se actualiza mientras hay overlays.

---

## Fade negro

El fade negro se dibuja automáticamente por el sistema. `DrawFadeOverlay` existe para integración manual:

```csharp
// Disponible si quieres controlar cuándo se dibuja:
sceneManager.DrawFadeOverlay(spriteBatch, graphicsDevice, blackTexture);
```

La duración del fade es fija: **0.3 segundos** de fade out + 0.3 de fade in.

---

## Capacidad de la pila

La pila de overlays tiene capacidad para **4 escenas**. Si necesitas más, rediseña la arquitectura de pantallas.

---

## Ejemplo: menú principal con overlay de opciones

```csharp
// MainMenuScene.cs
public sealed class MainMenuScene : Scene
{
    protected override void PreInitialize() => EnableUI();

    protected override void InitializeUI()
    {
        var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 16 };
        UIRoot!.Add(stack);

        var playBtn = new Button(null, "Jugar");
        playBtn.Clicked += () => Core.SceneManager.RequestChange(new GameplayScene());
        stack.Add(playBtn);

        var optionsBtn = new Button(null, "Opciones");
        optionsBtn.Clicked += () => Core.SceneManager.PushScene(new OptionsOverlay());
        stack.Add(optionsBtn);

        var exitBtn = new Button(null, "Salir");
        exitBtn.Clicked += () => Core.Instance.Exit();
        stack.Add(exitBtn);
    }
}
```

```csharp
// OptionsOverlay.cs
public sealed class OptionsOverlay : Scene
{
    public override bool IsOverlay => true; // el menú sigue visible debajo

    protected override void PreInitialize() => EnableUI();

    protected override void InitializeUI()
    {
        var panel = new Panel();
        panel.Bounds = new Rectangle(200, 150, 400, 300);
        UIRoot!.Add(panel);

        var closeBtn = new Button(null, "Cerrar");
        closeBtn.Clicked += () => Core.SceneManager.PopScene();
        panel.Add(closeBtn);
    }
}
```

---

## Ejemplo: flujo completo de escenas

```csharp
// SplashScene → MainMenu → Gameplay → GameOver → MainMenu
public sealed class SplashScene : Scene
{
    private float _timer;

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_timer >= 3f)
            Core.SceneManager.RequestChange(new MainMenuScene());
    }
}
```

---

## Notas

- `SceneManager` se crea automáticamente por `Core` — no instanciar manualmente.
- Llamar `RequestChange` durante el fade (mientras otra transición está en curso) encola la nueva escena; se aplicará al terminar la actual.
- `PopScene` no hace nada si no hay overlays en la pila.

---

## Ver también

- [Scene →](scene.md)
- [Scenes Overview →](overview.md)
- [Core →](../01-core/core.md)
