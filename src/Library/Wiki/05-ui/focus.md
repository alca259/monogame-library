# Foco de UI

**Namespace:** `Alca.MonoGame.Kernel.UI.Focus`

El sistema de foco permite navegar entre controles con teclado o gamepad sin usar el ratón. Implementa dos modos: orden Tab y vecinos direccionales (D-Pad).

---

## IFocusable

Interfaz que deben implementar los controles que aceptan foco.

```csharp
public interface IFocusable
{
    int TabIndex { get; set; }
    int? FocusNeighborUp    { get; set; }
    int? FocusNeighborDown  { get; set; }
    int? FocusNeighborLeft  { get; set; }
    int? FocusNeighborRight { get; set; }
    bool IsFocused { get; }
    void OnFocusGained();
    void OnFocusLost();
}
```

- `TabIndex` — orden de tabulación (valores únicos; duplicados ignorados).
- `FocusNeighborXxx` — `TabIndex` del vecino en esa dirección (null = sin vecino).
- `OnFocusGained` / `OnFocusLost` — callbacks al ganar/perder foco.

---

## UIFocusManager

Gestiona el foco global. Disponible como `Core.UIFocus`.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `FocusedElement` | `IFocusable?` | Elemento con foco actualmente |

### Métodos

| Método | Descripción |
|---|---|
| `Register(element)` | Registra un elemento; se inserta en orden de `TabIndex` |
| `Unregister(element)` | Elimina el elemento; si tenía foco, lo limpia sin disparar `OnFocusLost` |
| `Clear()` | Elimina todos los elementos; dispara `OnFocusLost` en el actual |
| `SetFocus(element)` | Transfiere el foco; dispara `OnFocusLost` → `OnFocusGained` |
| `FocusNext()` | Siguiente en orden `TabIndex`, circular |
| `FocusPrevious()` | Anterior en orden `TabIndex`, circular |
| `FocusUp()` | Mueve al vecino de `FocusNeighborUp` |
| `FocusDown()` | Mueve al vecino de `FocusNeighborDown` |
| `FocusLeft()` | Mueve al vecino de `FocusNeighborLeft` |
| `FocusRight()` | Mueve al vecino de `FocusNeighborRight` |
| `Update(kb, pad)` | Procesa Tab / D-Pad; debe llamarse una vez por frame |

---

## Ejemplo: formulario navegable sin ratón

```csharp
public sealed class SettingsScene : Scene
{
    private Button _btnApply  = null!;
    private Slider _sldVolume = null!;
    private Checkbox _chkFullscreen = null!;

    protected override void InitializeUI()
    {
        _sldVolume    = new Slider(_pixel) { MinValue = 0, MaxValue = 1, Value = 0.8f };
        _chkFullscreen = new Checkbox(_font, "Pantalla completa");
        _btnApply     = new Button(_font, "Aplicar");

        // Orden Tab: 0 → 1 → 2
        _sldVolume.TabIndex    = 0;
        _chkFullscreen.TabIndex = 1;
        _btnApply.TabIndex     = 2;

        // Vecinos direccionales para D-Pad
        _sldVolume.FocusNeighborDown    = 1;   // va a checkbox al bajar
        _chkFullscreen.FocusNeighborUp  = 0;   // sube al slider
        _chkFullscreen.FocusNeighborDown = 2;  // baja al botón
        _btnApply.FocusNeighborUp       = 1;   // sube al checkbox

        Core.UIFocus.Register(_sldVolume);
        Core.UIFocus.Register(_chkFullscreen);
        Core.UIFocus.Register(_btnApply);

        // Dar el foco inicial al primer control
        Core.UIFocus.SetFocus(_sldVolume);

        var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 12f };
        stack.Add(_sldVolume);
        stack.Add(_chkFullscreen);
        stack.Add(_btnApply);
        UIRoot!.Add(stack);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        Core.UIFocus.Update(Core.Input.Keyboard, Core.Input.GamePad);
    }
}
```

---

## Notas

- Llama siempre a `Core.UIFocus.Clear()` o `Unregister` en `UnloadContent` para evitar referencias colgadas entre escenas.
- Si un elemento pierde el foco porque el `UIInteractionManager` hizo clic en otro, el focus manager lo actualiza automáticamente (si se le pasó en `UIInteractionManager.Update`).
- Los controles del framework (`Button`, `Slider`, `Checkbox`, `Dropdown`, `RadioButton`, `TextBox`, `TextArea`, `NumericBox`) ya implementan `IFocusable`.

---

## Ver también

- [Controles →](controls.md)
- [Interacción →](interaction.md)
