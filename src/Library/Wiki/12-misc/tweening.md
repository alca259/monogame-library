# Tweening

**Namespace:** `Alca.MonoGame.Kernel.Tweening`

El sistema de tweening anima propiedades de objetos a lo largo del tiempo usando funciones de easing. Disponible como `Core.Tweening`.

---

## TweeningManager

### Métodos

| Método | Descripción |
|---|---|
| `TweenTo(target, member, toValue, duration, easing, delay)` | Crea un tween sobre una propiedad `float` |
| `Update(gameTime)` | Avanza todos los tweens activos (llamado automáticamente) |
| `CancelAll()` | Cancela y elimina todos los tweens |
| `Cancel(tween)` | Cancela un tween específico |

### Firma de TweenTo

```csharp
Tween TweenTo<T>(T target,
                 Expression<Func<T, float>> member,
                 float toValue,
                 float duration,
                 Func<float, float> easing,
                 float delay = 0f)
    where T : class
```

Devuelve un `Tween` (de MonoGame.Extended) que puede encadenarse con `.OnComplete(...)`.

---

## EasingCatalog

Catálogo de funciones de easing listas para usar (`Func<float, float>`).

| Función | Descripción |
|---|---|
| `Linear` | Interpolación lineal constante |
| `QuadIn` / `QuadOut` / `QuadInOut` | Cuadrática |
| `CubicIn` / `CubicOut` / `CubicInOut` | Cúbica |
| `BounceIn` / `BounceOut` / `BounceInOut` | Rebote |
| `ElasticIn` / `ElasticOut` / `ElasticInOut` | Resorte elástico |
| `BackIn` / `BackOut` / `BackInOut` | Ligero sobre-recorrido |
| `EaseIn` / `EaseOut` / `EaseInOut` | Alias cuadráticos |

---

## Ejemplo: animación de entrada de UI

```csharp
public override void PostInitialize()
{
    base.PostInitialize();

    // Hacer que el panel aparezca desde la izquierda
    _panel.Bounds = new Rectangle(-300, 100, 300, 400); // posición inicial fuera
    Core.Tweening.TweenTo(
        target:   _panel,
        member:   p => p.Bounds.X,  // anima la coordenada X de Bounds
        toValue:  50f,
        duration: 0.5f,
        easing:   EasingCatalog.BackOut);
}
```

---

## Ejemplo: parpadeo de antorcha

```csharp
private void StartFlicker(PointLight2D light)
{
    // Tween de ida y vuelta en la intensidad
    Core.Tweening
        .TweenTo(light, l => l.Intensity, toValue: 0.7f, duration: 0.12f,
                 easing: EasingCatalog.ElasticOut)
        .OnComplete(() =>
            Core.Tweening.TweenTo(light, l => l.Intensity, toValue: 1.2f, duration: 0.18f,
                                  easing: EasingCatalog.QuadInOut)
                         .OnComplete(() => StartFlicker(light)));
}
```

---

## Ejemplo: cooldown visual de habilidad

```csharp
// Reducir opacidad al activar y restaurarla al terminar el cooldown
Core.Tweening
    .TweenTo(_skillIcon, i => i.Opacity, toValue: 0.4f, duration: 0.1f,
             easing: EasingCatalog.Linear)
    .OnComplete(() =>
        Core.Tweening.TweenTo(_skillIcon, i => i.Opacity, toValue: 1f, duration: 2f,
                               easing: EasingCatalog.Linear));
```

---

## Notas

- `TweenTo` es para propiedades `float`; para otros tipos usa `UITweenExtensions` (UI) o anima manualmente con `Update`.
- Un tween en curso puede cancelarse con `Core.Tweening.Cancel(tween)`.
- `TweeningManager.Update` es llamado automáticamente por el `Core`; no es necesario llamarlo manualmente.

---

## Ver también

- [Transiciones UI →](../05-ui/transitions.md)
- [Timers →](timers.md)
