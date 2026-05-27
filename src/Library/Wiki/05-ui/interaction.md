# Interacción con el Puntero

**Namespace:** `Alca.MonoGame.Kernel.UI.Interaction`

El sistema de interacción gestiona el hit testing del árbol UI contra el puntero y despacha eventos de entrada (hover, clic, suelta) a los controles.

---

## IUIInteractable

Interfaz que implementan los controles que responden al puntero.

```csharp
public interface IUIInteractable
{
    bool IsHovered { get; }
    void OnPointerEnter();
    void OnPointerLeave();
    void OnPointerDown(ref UIPointerEventArgs args);
    void OnPointerUp(ref UIPointerEventArgs args);
}
```

- `IsHovered` — `true` cuando el puntero está sobre el control.
- `OnPointerEnter` / `OnPointerLeave` — el puntero entró o salió del `Bounds`.
- `OnPointerDown` / `OnPointerUp` — botón del ratón presionado o soltado.

`UIPointerEventArgs` es un `ref struct` con la posición del puntero y un flag `Handled` para detener el bubbling.

---

## UIInteractionManager

Ejecuta el hit testing y despacha eventos cada frame. Disponible como `Core.UIInteraction`.

### Método principal

```csharp
public void Update(UIRoot root, MouseInfo mouse, UIFocusManager? focusManager = null)
```

- Realiza un recorrido DFS sobre el árbol; los hijos se prueban **de último a primero** (el más dibujado encima es el primero en recibir el evento).
- Si se hace clic sobre un elemento `IFocusable`, el focus manager transfiere el foco automáticamente.

### IsPointerOverUI

```csharp
public bool IsPointerOverUI { get; }
```

`true` si el puntero está sobre un control interactivo (`IUIInteractable`): Button, Slider, Checkbox, TextBox, etc. Los contenedores de layout puros (AnchorLayout, StackPanel, GridLayout…) son **transparentes** a esta comprobación — no bloquean el input del juego aunque el cursor esté sobre su área.

Se actualiza en cada llamada a `Update`.

---

## Bloqueo de input: UI vs. juego

Cuando hay una capa de UI sobre el mundo del juego (botones, paneles, etc.), un clic sobre un botón UI no debe propagarse al juego subyacente.

### Orden correcto en `Update`

El `UIInteractionManager.Update` debe llamarse **antes** de procesar el input del juego, para que `IsPointerOverUI` esté disponible en esa misma frame:

```csharp
public override void Update(GameTime gameTime)
{
    // 1. Actualizar y hacer layout de la UI
    _uiRoot.Update(gameTime);
    Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
    _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
    _uiRoot.Arrange(screen);
    _interactionManager.Update(_uiRoot, Core.Input.Mouse);  // ← primero la UI

    // 2. Solo procesar input de juego si el puntero NO está sobre la UI
    if (!_interactionManager.IsPointerOverUI)
    {
        if (Core.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
        {
            SpawnObject(Core.Input.Mouse.Position.ToVector2());
        }
    }

    _world.Update(gameTime);
}
```

### Anti-patrón a evitar

```csharp
// MAL: el input del juego se procesa antes de que la UI lo consuma
if (Core.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
    SpawnObject(mousePos);  // ← se ejecuta aunque se haya pulsado un botón UI

_interactionManager.Update(_uiRoot, Core.Input.Mouse);  // demasiado tarde
```

```csharp
internal static UIElement? HitTest(UIElement element, Point point)
```

Devuelve el elemento más profundo cuyo `Bounds` contiene el punto, o `null`. Solo considera elementos con `IsVisible = true` e `IsEnabled = true`.

---

## Ejemplo: botón personalizado

```csharp
public sealed class IconButton : UIElement, IUIInteractable
{
    private readonly Texture2D _icon;
    private Color _tint = Color.White;

    public event Action? Clicked;
    public bool IsHovered { get; private set; }

    public IconButton(Texture2D icon) => _icon = icon;

    public void OnPointerEnter()
    {
        IsHovered = true;
        _tint = Color.Yellow;
    }

    public void OnPointerLeave()
    {
        IsHovered = false;
        _tint = Color.White;
    }

    public void OnPointerDown(ref UIPointerEventArgs args) { }

    public void OnPointerUp(ref UIPointerEventArgs args)
    {
        if (!args.Handled)
        {
            args.Handled = true;
            Clicked?.Invoke();
        }
    }

    public override void Measure(Vector2 availableSize) =>
        DesiredSize = new Vector2(_icon.Width, _icon.Height);

    public override void Draw(SpriteBatch spriteBatch) =>
        spriteBatch.Draw(_icon, Bounds, _tint);
}
```

Uso en la escena:

```csharp
// En Update, una vez por frame:
Core.UIInteraction.Update(UIRoot!, Core.Input.Mouse, Core.UIFocus);
```

---

## Notas

- El `UIInteractionManager` no genera garbage; usa `ref struct` internamente.
- El DFS garantiza que los overlays (dibujados al final) se prueban antes que el árbol principal.
- Si `args.Handled = true` en `OnPointerDown`, el evento no se propaga hacia los padres.
- `UIInteractionManager.Update` debe llamarse **después** del Arrange para que `Bounds` estén actualizados.

---

## Ver también

- [Foco →](focus.md)
- [Controles →](controls.md)
