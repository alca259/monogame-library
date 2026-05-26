# Transiciones de UI

**Namespace:** `Alca.MonoGame.Kernel.UI.Transitions`

El sistema de transiciones permite animar la entrada y salida de paneles, menús y overlays usando el motor de tweening integrado.

---

## UITransitionType

Enumeración con los tipos de transición disponibles.

| Valor | Descripción |
|---|---|
| `FadeIn` | Opacidad 0 → 1 |
| `FadeOut` | Opacidad 1 → 0 |
| `SlideInFromLeft` | Desliza desde fuera del borde izquierdo |
| `SlideInFromRight` | Desliza desde fuera del borde derecho |
| `SlideInFromTop` | Desliza desde fuera del borde superior |
| `SlideInFromBottom` | Desliza desde fuera del borde inferior |
| `SlideOutToLeft` | Sale hacia el borde izquierdo |
| `SlideOutToRight` | Sale hacia el borde derecho |
| `SlideOutToTop` | Sale hacia el borde superior |
| `SlideOutToBottom` | Sale hacia el borde inferior |

---

## UITransitionManager

Mapea `UITransitionType` a la llamada de tween correspondiente sobre un `UIElement`.

### Constructores

```csharp
// Usa Core.Tweening (caso habitual)
new UITransitionManager()

// Inyección explícita del manager (tests)
new UITransitionManager(TweeningManager tweening)
```

### Métodos

```csharp
public Tween Play(UIElement element, UITransitionType transition, float duration,
                  Func<float, float>? easing = null)
```

Devuelve el `Tween` creado para encadenar callbacks.

---

## UITweenExtensions

Extensiones estáticas sobre `UIElement` que generan los tweens concretos. Son llamadas internamente por `UITransitionManager` pero pueden usarse directamente.

```csharp
element.FadeIn(tweening, duration, easing);
element.FadeOut(tweening, duration, easing);
element.SlideIn(tweening, duration, direction, easing);
element.SlideOut(tweening, duration, direction, easing);
```

---

## Ejemplo: menú con animación de entrada

```csharp
public sealed class MainMenuScene : Scene
{
    private Panel _menuPanel  = null!;
    private UITransitionManager _transitions = null!;

    protected override void InitializeUI()
    {
        _transitions = new UITransitionManager();

        _menuPanel = new Panel
        {
            BackgroundColor = new Color(0, 0, 0, 180)
        };
        _menuPanel.Add(new Button(_font, "Jugar") { TabIndex = 0 });
        _menuPanel.Add(new Button(_font, "Opciones") { TabIndex = 1 });
        _menuPanel.Add(new Button(_font, "Salir") { TabIndex = 2 });

        UIRoot!.Add(_menuPanel);
    }

    public override void PostInitialize()
    {
        base.PostInitialize();
        // Animar la entrada del menú desde la izquierda
        _transitions.Play(_menuPanel, UITransitionType.SlideInFromLeft, duration: 0.4f,
            easing: EasingCatalog.QuadOut);
    }
}
```

### Encadenar transición de salida

```csharp
_transitions
    .Play(_menuPanel, UITransitionType.FadeOut, duration: 0.3f)
    .OnComplete(() => Core.SceneManager.RequestChange(new GameplayScene()));
```

---

## Notas

- `UITransitionManager` usa `Core.Tweening` por defecto — asegúrate de que el `TweeningManager` esté inicializado (lo hace el `Core` automáticamente).
- Las transiciones de slide asumen que el elemento ya está colocado en su posición final; la animación comienza fuera de pantalla y termina en `Bounds`.
- Si necesitas una transición personalizada, usa directamente `Core.Tweening.TweenTo` sobre `Opacity` u otras propiedades del elemento.

---

## Ver también

- [Tweening →](../12-misc/tweening.md)
- [SceneManager →](../03-scenes/scene-manager.md)
