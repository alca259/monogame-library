# UIElement, UIContainer y UIRoot

**Namespace:** `Alca.MonoGame.Kernel.UI`

Las tres clases base forman la columna vertebral del árbol de UI.

---

## UIElement

Nodo base abstracto. Todo control hereda de esta clase.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` | Identificador único (asignado al crear) |
| `Bounds` | `Rectangle` | Rectángulo absoluto en pantalla (fijado por Arrange) |
| `DesiredSize` | `Vector2` | Tamaño deseado calculado por Measure |
| `IsLayoutDirty` | `bool` | `true` si el layout necesita recalcularse |
| `IsVisible` | `bool` | Si `false`, no se dibuja ni se hace hit test |
| `IsEnabled` | `bool` | Si `false`, no recibe Update ni input |
| `Opacity` | `float` | Opacidad local 0–1 |
| `EffectiveOpacity` | `float` | Opacidad real (producto de toda la cadena de padres) |
| `Parent` | `UIElement?` | Nodo padre (asignado por `UIContainer`) |

### Métodos

| Método | Descripción |
|---|---|
| `Invalidate()` | Marca este elemento y ancestros como dirty |
| `Measure(availableSize)` | Calcula `DesiredSize`; llamado bottom-up |
| `Arrange(finalBounds)` | Establece `Bounds`; llamado top-down |
| `Update(GameTime)` | Lógica por frame (sólo si `IsEnabled`) |
| `Draw(SpriteBatch)` | Renderizado por frame (sólo si `IsVisible`) |

---

## UIContainer

Extiende `UIElement`. Agrega una colección de hijos y propaga las operaciones.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `ChildrenReadOnly` | `IReadOnlyList<UIElement>` | Vista de sólo lectura usada por `UIInteractionManager` |

### Métodos

| Método | Descripción |
|---|---|
| `Add(child)` | Añade un hijo, establece su `Parent` e invalida el layout |
| `Remove(child)` | Elimina un hijo, limpia `Parent` e invalida el layout |
| `OnChildAdded(child)` | Hook virtual llamado al añadir un hijo |
| `OnChildRemoved(child)` | Hook virtual llamado al eliminar un hijo |

---

## UIRoot

Hereda de `UIContainer`. Es el nodo raíz del árbol. Gestiona el ciclo completo de dibujado y los overlays.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `OverlayManager` | `UIOverlayManager?` | Manager de overlays modales (Tooltip, Dropdown) dibujados siempre encima |

### Métodos

| Método | Descripción |
|---|---|
| `DrawAll(SpriteBatch)` | Abre el SpriteBatch, dibuja el árbol y luego todos los overlays activos |

---

## Ejemplo: árbol mínimo

```csharp
public sealed class HudScene : Scene
{
    private UIRoot _ui = null!;

    protected override void InitializeUI()
    {
        _ui = UIRoot;    // proporcionado por Scene tras llamar EnableUI()

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8f
        };

        stack.Add(new Label { Text = "Puntuación: 0", Font = _font, Color = Color.White });
        stack.Add(new ProgressBar { Value = 1f, FillColor = Color.Green, Pixel = _pixel });

        _ui.Add(stack);

        // Forzar layout inicial
        _ui.Measure(new Vector2(Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height));
        _ui.Arrange(Core.GraphicsDevice.Viewport.Bounds);
    }
}
```

---

## Notas

- `UIElement.Arrange` solo debe ser llamado por el nodo padre o por `UIRoot`; nunca desde código externo en mitad de un frame.
- Modificar `IsVisible` o `Opacity` en `Update` es seguro y no invalida el layout.
- `EffectiveOpacity` es calculado en tiempo real (no cacheado) — evitar llamarlo en hot paths; en su lugar, cachearlo en una variable local.

---

## Ver también

- [Controles →](controls.md)
- [Layouts →](layout.md)
- [Interacción →](interaction.md)
