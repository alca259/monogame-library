# Roadmap Demo: Proyecto Alca.MonoGame.Demo

> Especificación completa del proyecto ejecutable para probar visualmente los sistemas de la librería.
>
> **Referencia cruzada:** Este documento describe únicamente el proyecto Demo. El roadmap de la librería principal está en [`roadmap-v2.md`](roadmap-v2.md).

---

## Proyecto Demo

**Reglas transversales a todos los desarrollos:**
- Al terminar, actualizar este fichero marcando los TODOs completados.

**`src/Demo/Alca.MonoGame.Demo/`** — proyecto ejecutable (`OutputType=WinExe`)
- `DemoGame.cs` — `sealed class DemoGame : Core`
- Escenas ECS:
  - `Scenes/EcsDemoScene.cs` — demo de jerarquía ECS con `TransformBehaviour` padre/hijo
- Escenas UI:
  - 10 escenas que cubren todos los controles del sistema UI
  - `[←] / [→]` navega entre escenas UI; `[Space]` salta a `EcsDemoScene`
- Añadir escenas nuevas conforme se completen nuevas fases

**Para añadir fuentes al demo:**
1. Crear `Content/DefaultFont.spritefont` con un SpriteFont de MonoGame Content Builder
2. Compilar el `.xnb` con MGCB y copiar a `Content/`
3. Ejecutar el proyecto para ver las escenas renderizadas

---

## Estructura de carpetas

```
src/Demo/Alca.MonoGame.Demo/
├── Scenes/
│   ├── EcsDemoScene.cs
│   ├── UIScene_BasicControls.cs
│   ├── UIScene_InputText.cs
│   ├── UIScene_TextArea.cs
│   ├── UIScene_Sliders.cs
│   ├── UIScene_Selection.cs
│   ├── UIScene_ColorPicker.cs
│   ├── UIScene_Layout.cs
│   ├── UIScene_ScrollView.cs
│   ├── UIScene_Tooltip.cs
│   └── UIScene_Focus.cs
├── DemoGame.cs
├── Globals.cs
├── Program.cs
└── Alca.MonoGame.Demo.csproj
```

**`DemoGame.cs`:**
- Registrar las 10 escenas UI + `EcsDemoScene` en `ConfigureServices` como `AddTransient`.
- `PostInitialize` arranca en `UIScene_BasicControls` (primera escena UI).
- Cada escena UI resuelve la escena anterior/siguiente de la lista registrada via `[←]/[→]`.
- `[Space]` navega a `EcsDemoScene` desde cualquier escena UI.

---

## UI Demo Scenes — Cobertura Completa

> **Objetivo:** Cada control del sistema UI debe poder probarse visualmente e interactivamente desde el proyecto Demo.

**Navegación entre escenas:**
- `[←] / [→]` — escena anterior / siguiente en el ciclo de demos de UI
- `[Space]` — saltar a `EcsDemoScene` (demo ECS fuera del ciclo UI)
- Cada escena muestra en la esquina superior izquierda: nombre de la escena y teclas de navegación

**Infraestructura común en cada demo UI:**

Cada escena UI declara y gestiona:
```csharp
private UIRoot _uiRoot = new();
private UIInteractionManager _interactionManager = new();
private UIOverlayManager _overlayManager = new();   // solo si usa Dropdown/Tooltip
private Texture2D _pixel = null!;
private SpriteFont _font = null!;
```

Ciclo de vida:
```csharp
// LoadContent
_pixel = new Texture2D(GraphicsDevice, 1, 1);
_pixel.SetData(new[] { Color.White });
_font = Content.Load<SpriteFont>("DefaultFont");
_uiRoot.OverlayManager = _overlayManager;
// construir árbol UI aquí...

// Update
Rectangle screen = new(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
_uiRoot.Arrange(screen);
_interactionManager.Update(_uiRoot, Core.Input.Mouse);

// Draw
GraphicsDevice.Clear(new Color(20, 20, 30));
_uiRoot.DrawAll(Core.SpriteBatch);
```

---

### Scene 1 — `UIScene_BasicControls.cs`

> Controles cubiertos: **Button**, **Label**, **Checkbox**, **Panel**

**Layout:** `StackPanel` vertical centrado, spacing 16.

| Control | Configuración |
|---------|--------------|
| `Label` | "Basic Controls Demo" — HAlign.Center, Color.Yellow |
| `Button` | "Normal Button" — `Clicked` → incrementa contador en label |
| `Button` | "Disabled Button" — `IsEnabled = false` |
| `Checkbox` | "Toggle me" — `CheckedChanged` → actualiza label de estado |
| `Panel` | Fondo `Color(60,80,60)`, borde `Color.Green` — contiene Label "Soy un Panel" |
| `Label` | Estado reactivo: muestra nº de clicks y estado checked |

**Notas:**
- `Panel` necesita `Pixel` para renderizar fondo y borde.
- `Button` sin `Texture` → renderiza como rectángulo de color con texto centrado.

---

### Scene 2 — `UIScene_InputText.cs`

> Controles cubiertos: **TextBox**, **NumericBox**, **PasswordBox**

**Layout:** `GridLayout` 2 columnas — izquierda: etiqueta descriptora, derecha: control.

| Fila | Col 0 (Label) | Col 1 (Control) |
|------|---------------|-----------------|
| 0 | "TextBox:" | `TextBox` (Placeholder: "Escribe aquí...") |
| 1 | "NumericBox:" | `NumericBox` (Min=0, Max=100, Step=1, Value=50) |
| 2 | "Password:" | `PasswordBox` (Placeholder: "Contraseña") |
| 3 | "Valores:" | Label reactivo con los 3 valores en tiempo real |

**Notas:**
- `TextChanged` / `ValueChanged` actualizan el label de la fila 3.
- `Tab` navega entre los tres campos; configurar `UIFocusManager` con `TabIndex` 0–2.

---

### Scene 3 — `UIScene_TextArea.cs`

> Controles cubiertos: **TextArea**

**Layout:** full-screen menos márgenes de 20px.

- `Label` header: "TextArea Demo — escribe texto largo"
- `TextArea` (ancho completo, alto 200 px, `WrapText=true`)
- `Label` contador de caracteres: "X / 500 chars"
- `Button` "Limpiar" → `textArea.Text = string.Empty`

---

### Scene 4 — `UIScene_Sliders.cs`

> Controles cubiertos: **Slider** (horizontal y vertical), **ProgressBar** (horizontal y vertical, gradiente)

**Layout:** `StackPanel` vertical, con un `StackPanel` horizontal anidado para las barras.

| Control | Configuración |
|---------|--------------|
| `Label` | "Sliders & Progress Bars" — header |
| `Slider` H | MinValue=0, MaxValue=100, Step=1, ancho 300 px |
| `Label` | Reactivo: "Slider H: {valor}" |
| `Slider` V | MinValue=0, MaxValue=1, Orientation=Vertical, alto 120 px |
| `Label` | Reactivo: "Slider V: {valor:F2}" |
| `ProgressBar` H | Animada +0.05/s, 300×20 px |
| `ProgressBar` V+gradient | Animada, `ColorGradient=true`, 20×120 px |
| `Button` "Reset" | Resetea sliders y barras a 0 |

**Notas:**
- El slider H controla el valor de la `ProgressBar` H (vinculados por `ValueChanged`).
- La `ProgressBar` V se auto-incrementa sola en `Update`; al llegar a 1 reinicia desde 0.
- `Slider` necesita `Pixel` para renderizar track y thumb.

---

### Scene 5 — `UIScene_Selection.cs`

> Controles cubiertos: **Dropdown**, **RadioGroup** / **RadioButton**

**Layout:** `StackPanel` vertical, spacing 12.

| Control | Configuración |
|---------|--------------|
| `Label` | "Dropdown & Radio Demo" — header |
| `Dropdown` | Opciones: "Opción A", "Opción B", "Opción C", "Opción D" |
| `Label` | Reactivo: "Dropdown: {selección}" |
| `RadioGroup` | 3 RadioButtons: "Radio 1", "Radio 2", "Radio 3" |
| `Label` | Reactivo: "Radio: {selección}" |

**Notas:**
- `Dropdown` necesita `UIOverlayManager`, `Font` y `Pixel`.
- `RadioGroup.SelectionChanged` → actualiza el label reactivo.
- `Dropdown.SelectedIndexChanged` → actualiza su label reactivo.

---

### Scene 6 — `UIScene_ColorPicker.cs`

> Controles cubiertos: **ColorPickerRGB**, **ColorPickerHSV**

**Layout:** `StackPanel` horizontal con dos columnas, swatch debajo.

- Columna izquierda (`StackPanel` V): `Label` "RGB" + `ColorPickerRGB` (ancho 260 px)
- Columna derecha (`StackPanel` V): `Label` "HSV" + `ColorPickerHSV` (ancho 260 px)
- Fila inferior: `Panel` swatch 60×60 px + `Label` hex "#RRGGBB"

**Notas:**
- `ColorPickerRGB.ColorChanged` y `ColorPickerHSV.ColorChanged` → actualizan `Panel.BackgroundColor` y el label hex.
- Los dos pickers comparten la misma referencia a `Pixel` y `Font`.

---

### Scene 7 — `UIScene_Layout.cs`

> Controles cubiertos: **StackPanel**, **FlowLayoutPanel**, **GridLayout**, **AnchorLayout**, **Canvas**

**Diseño:** 5 zonas visuales enmarcadas por `Panel` con borde, distribuidas en pantalla.

| Zona | Layout interno | Contenido |
|------|----------------|-----------|
| Top-left 300×150 | `StackPanel` vertical, spacing 6 | 4 Labels: "Ítem 1" – "Ítem 4" |
| Top-right 300×150 | `StackPanel` horizontal, spacing 6 | 4 Labels con colores distintos |
| Center 300×150 | `FlowLayoutPanel` (wrap automático) | 8 Buttons pequeños "Btn 1" – "Btn 8" |
| Bottom-left 300×150 | `GridLayout` 3 cols × 2 filas | 6 Labels "R0C0" – "R1C2" |
| Bottom-right 300×150 | `AnchorLayout` | Labels en top-left, top-right, bottom-left, bottom-right y center |

**Notas:**
- El `Canvas` actúa como contenedor raíz posicionando las 5 zonas manualmente por sus `Bounds`.
- Cada zona tiene un `Panel` de fondo con borde para delimitar visualmente el área de layout.

---

### Scene 8 — `UIScene_ScrollView.cs`

> Controles cubiertos: **ScrollView**

**Layout:** Dos `ScrollView` lado a lado.

- **Vertical** (300×300 px): 25 Labels "Ítem 01" – "Ítem 25"; scroll con rueda de ratón.
- **Horizontal** (400×60 px): 10 Labels anchos "Categoría Muy Larga X"; scroll arrastrando (o rueda horizontal).

**Notas:**
- `ScrollView` requiere `GraphicsDevice` en el constructor.
- Scroll wheel se procesa con `Core.Input.Mouse.ScrollWheelDelta` en `Update`.

---

### Scene 9 — `UIScene_Tooltip.cs`

> Controles cubiertos: **Tooltip**, **UISprite**, **UIOverlayManager**

**Layout:** `StackPanel` vertical con 4 targets (Buttons + un UISprite).

| Elemento | Tooltip al hover |
|----------|-----------------|
| Button "Hover 1" | "Este es el botón principal" |
| Button "Hover 2" | "Haz click para continuar" |
| Button "Hover 3" | "Texto largo que se clampea al borde de pantalla" |
| `UISprite` (pixel 64×64 blanco) | "Un sprite con tooltip" |

**Notas:**
- Se crea un único `Tooltip` registrado en `UIOverlayManager`.
- En `Update`: si el button/sprite está hovered (`IUIInteractable.IsHovered`), llamar `tooltip.Show(anchorPos, screenBounds)`.
- Al salir del hover, llamar `tooltip.Hide()`.

---

### Scene 10 — `UIScene_Focus.cs`

> Controles cubiertos: **UIFocusManager**, navegación por teclado con Tab y flechas

**Layout:** Grid 3×3 de Buttons.

- 9 Buttons con etiquetas "F1" – "F9" y `TabIndex` 0–8.
- `FocusNeighborUp/Down/Left/Right` configurados para navegación cardinal (ej. F5 tiene vecino Up=F2, Down=F8, Left=F4, Right=F6).
- `Label` de estado: "Foco actual: F{n}"
- `Tab` / `Shift+Tab` ciclan el foco en orden de `TabIndex`.
- Arrow keys navegan por vecinos configurados.
- `Space` / `Enter` simulan `Clicked` en el botón enfocado.

**Notas:**
- `UIFocusManager.Update(Keyboard.GetState())` debe llamarse en `Update` después del `Arrange`.
- Los Buttons resaltan visualmente al recibir foco (ya gestionado por `Button.OnFocusGained`).

---

## ECS Demo Scene

### `EcsDemoScene.cs`

> Controles cubiertos: **GameEntity**, **TransformBehaviour**, **GameWorld**

- Entidad padre en el centro de pantalla.
- Entidad hijo que orbita alrededor del padre (modifica `LocalPosition` con ángulo creciente).
- Labels en pantalla mostrando posición world y local de cada entidad.
- `[Space]` vuelve al ciclo de escenas UI.
