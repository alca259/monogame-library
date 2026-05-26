# Layouts de UI

**Namespace:** `Alca.MonoGame.Kernel.UI.Layout`

El sistema ofrece cinco contenedores de layout, cada uno con una estrategia diferente para posicionar sus hijos.

---

## Canvas

Posicionamiento libre por offset fijo. Cada hijo se coloca en la coordenada absoluta (relativa al Canvas) que se le asigne.

```csharp
var canvas = new Canvas();

canvas.Add(healthBar);
canvas.SetOffset(healthBar, new Vector2(16, 16));

canvas.Add(minimap);
canvas.SetOffset(minimap, new Vector2(screenW - 200, 16));
```

| Método | Descripción |
|---|---|
| `SetOffset(child, offset)` | Establece la posición relativa del hijo |
| `GetOffset(child)` | Devuelve el offset actual del hijo |

**Cuándo usarlo:** HUDs con posiciones fijas (vida en esquina, minimapa, crosshair).

---

## StackPanel

Apila hijos en línea horizontal o vertical con un espaciado uniforme.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Orientation` | `Orientation` | `Horizontal` o `Vertical` |
| `Spacing` | `float` | Píxeles entre hijos |

```csharp
var menu = new StackPanel
{
    Orientation = Orientation.Vertical,
    Spacing = 12f
};
menu.Add(new Button(_font, "Continuar") { TabIndex = 0 });
menu.Add(new Button(_font, "Opciones")  { TabIndex = 1 });
menu.Add(new Button(_font, "Salir")     { TabIndex = 2 });
```

**Cuándo usarlo:** menús, barras de herramientas, listas de opciones.

---

## GridLayout

Divide el espacio en filas y columnas. Cada celda acepta un hijo con spanning.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `ColumnDefinitions` | `IList<GridTrack>` | Definición de cada columna |
| `RowDefinitions` | `IList<GridTrack>` | Definición de cada fila |
| `CellHAlign` | `HAlign` | Alineación horizontal por defecto en celdas |
| `CellVAlign` | `VAlign` | Alineación vertical por defecto en celdas |

### GridTrack — modos de tamaño

| Modo | Factory | Descripción |
|---|---|---|
| `Fixed` | `GridTrack.Fixed(pixels)` | Tamaño fijo en píxeles |
| `Auto` | `GridTrack.Auto()` | Se ajusta al `DesiredSize` del hijo |
| `Star` | `GridTrack.Star(weight)` | Reparte el espacio sobrante proporcionalmente |

### Método de colocación

```csharp
public void SetCell(UIElement child, int row, int col, int rowSpan = 1, int colSpan = 1)
```

### Ejemplo: HUD complejo

```csharp
var grid = new GridLayout();
grid.ColumnDefinitions.Add(GridTrack.Fixed(200));   // col 0: panel lateral
grid.ColumnDefinitions.Add(GridTrack.Star());        // col 1: área de juego
grid.RowDefinitions.Add(GridTrack.Auto());           // fila 0: barra top
grid.RowDefinitions.Add(GridTrack.Star());            // fila 1: centro
grid.RowDefinitions.Add(GridTrack.Fixed(48));        // fila 2: barra inferior

grid.Add(topBar);
grid.SetCell(topBar, row: 0, col: 0, colSpan: 2);

grid.Add(sidePanel);
grid.SetCell(sidePanel, row: 1, col: 0);

grid.Add(gameArea);
grid.SetCell(gameArea, row: 1, col: 1);

grid.Add(statusBar);
grid.SetCell(statusBar, row: 2, col: 0, colSpan: 2);
```

**Cuándo usarlo:** interfaces complejas de tipo editor, pantallas de opciones con etiquetas y controles alineados.

---

## AnchorLayout

Posiciona hijos en función de anclas relativas al rectángulo del contenedor (porcentajes 0–1).

```csharp
var layout = new AnchorLayout();
layout.Add(pauseButton);
// Fijar en la esquina superior derecha
layout.SetAnchor(pauseButton, new Anchor
{
    Right  = 1f,   // borde derecho del contenedor
    Top    = 0f,   // borde superior
    Width  = 80,
    Height = 32,
    MarginRight = 16,
    MarginTop   = 16
});
```

**Cuándo usarlo:** elementos que deben mantenerse anclados a bordes o esquinas independientemente de la resolución.

---

## FlowLayoutPanel

Distribuye hijos en filas (o columnas) con salto automático de línea cuando se agota el espacio.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Spacing` | `float` | Espacio entre hijos |

```csharp
var flow = new FlowLayoutPanel { Spacing = 8f };
foreach (var item in inventoryItems)
    flow.Add(item);
```

**Cuándo usarlo:** inventarios, galerías de imágenes, listas de iconos de longitud variable.

---

## Notas

- Todos los layouts respetan `DesiredSize` calculado en la pasada Measure.
- Para combinar layouts, anida contenedores: `GridLayout` con celdas de tipo `StackPanel`.
- `GridTrack.Star` sólo funciona si el `GridLayout` recibe un `finalBounds` con tamaño definido (no infinito).

---

## Ver también

- [UIElement / UIContainer / UIRoot →](elements.md)
- [Controles →](controls.md)
