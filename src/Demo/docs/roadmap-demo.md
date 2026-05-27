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
  - `UIScene_Menu` — menú principal de navegación; punto de entrada de la aplicación
  - 12 escenas de demostración (UI + ECS) accesibles desde el menú
  - Cada escena UI expone un botón `← Menú` que vuelve a `UIScene_Menu`
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
│   ├── UIScene_Menu.cs               [OK]
│   ├── UIScene_BasicControls.cs      [OK]
│   ├── UIScene_InputText.cs          [OK]
│   ├── UIScene_TextArea.cs           [OK]
│   ├── UIScene_Sliders.cs            [OK]
│   ├── UIScene_Selection.cs          [OK]
│   ├── UIScene_ColorPicker.cs        [OK]
│   ├── UIScene_Layout.cs             [OK]
│   ├── UIScene_ScrollView.cs         [OK]
│   ├── UIScene_Tooltip.cs            [OK]
│   ├── UIScene_Focus.cs              [OK]
│   ├── UIScene_Transitions.cs        [OK]
│   ├── EcsDemoScene.cs               [OK]
│   ├── Camera2DScene.cs              [OK]
│   ├── Physics2DScene.cs             [OK]
│   └── NavigationScene.cs            [OK]
├── DemoGame.cs
├── Globals.cs
├── Program.cs
└── Alca.MonoGame.Demo.csproj
```

**`DemoGame.cs`:**
- Registrar todas las escenas en `ConfigureServices` como `AddTransient`:
  `UIScene_Menu`, `UIScene_BasicControls`, `UIScene_InputText`, `UIScene_TextArea`,
  `UIScene_Sliders`, `UIScene_Selection`, `UIScene_ColorPicker`, `UIScene_Layout`,
  `UIScene_ScrollView`, `UIScene_Tooltip`, `UIScene_Focus`,
  `UIScene_Transitions`, `EcsDemoScene`, `Camera2DScene`, `Physics2DScene`, `NavigationScene`.
- `PostInitialize` arranca en `UIScene_Menu` (hub principal de navegación).
- Cada escena UI contiene un botón `← Menú` que llama
  `Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>())`.
- `UIScene_Menu` presenta la lista completa de escenas como botones clickables.

---

## UI Demo Scenes — Cobertura Completa

> **Objetivo:** Cada control del sistema UI debe poder probarse visualmente e interactivamente desde el proyecto Demo.

**Navegación entre escenas:**
- `UIScene_Menu` es el hub central; se accede a cada escena haciendo click en su botón.
- Cada escena UI tiene un botón `← Menú` en la cabecera que vuelve a `UIScene_Menu`.
- No existe navegación secuencial por teclado; toda la navegación es con ratón/click.
- Cada escena muestra en la cabecera: título de la escena y número de orden.

**Infraestructura común en cada demo UI:**

Cada escena UI declara como mínimo:
```csharp
private readonly UIRoot _uiRoot = new();
private readonly UIInteractionManager _interactionManager = new();
private Texture2D _pixel = null!;   // solo si la escena usa controles que requieren pixel
private SpriteFont _font = null!;
```

Opcional — solo en escenas con navegación por teclado/gamepad:
```csharp
private readonly UIFocusManager _focusManager = new();
```

Opcional — solo en escenas con Dropdown o Tooltip:
```csharp
private readonly UIOverlayManager _overlayManager = new();
```

Ciclo de vida:
```csharp
// LoadContent
_pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
_pixel.SetData(new[] { Color.White });
_font = Content.Load<SpriteFont>("DefaultFont");
// construir árbol UI aquí (BuildUI)...

// Update
_uiRoot.Update(gameTime);
Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
_uiRoot.Measure(new Vector2(screen.Width, screen.Height));
_uiRoot.Arrange(screen);
_interactionManager.Update(_uiRoot, Core.Input.Mouse);
// con foco: _focusManager.Update(Core.Input.Keyboard, Core.Input.GamePads[0]);

// Draw
Core.GraphicsDevice.Clear(new Color(20, 20, 30));
_uiRoot.DrawAll(Core.SpriteBatch);
```

Patrón del botón "← Menú" en `BuildUI`:
```csharp
var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
root.Add(backBtn);
```

---

### Menu — `UIScene_Menu.cs` [OK]

> Tipo: hub de navegación. No demuestra ningún control en sí — es el punto de entrada de la aplicación.

**Layout:** `AnchorLayout` centrado; `ScrollView` (700×340 px) con un `StackPanel` vertical de botones.

| Elemento | Configuración |
|----------|--------------|
| `Label` título | "MonoGame UI Demo — Selecciona una escena" — `Color.Yellow`, `HAlign.Center` |
| 12 `Button` | Uno por escena, texto numerado, `HAlign.Left` |
| `ScrollView` | 700×340 px — permite añadir escenas futuras sin cambiar el layout |

**Comportamiento:**
- Cada botón llama `Core.SceneManager.RequestChange(Core.GetService<TScene>())`.
- Fondo `new Color(15, 15, 25)` (más oscuro que el resto de escenas).
- No usa `_focusManager` ni `_overlayManager`.
- Botones usan `HoveredColor = Color.LightGray`; no necesitan `_pixel` de borde (color plano).

---

### Scene 1 — `UIScene_BasicControls.cs` [OK]

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

### Scene 2 — `UIScene_InputText.cs` [OK]

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

### Scene 3 — `UIScene_TextArea.cs` [OK]

> Controles cubiertos: **TextArea**

**Layout:** full-screen menos márgenes de 20px.

- `Label` header: "TextArea Demo — escribe texto largo"
- `TextArea` (ancho completo, alto 200 px, `WrapText=true`)
- `Label` contador de caracteres: "X / 500 chars"
- `Button` "Limpiar" → `textArea.Text = string.Empty`

---

### Scene 4 — `UIScene_Sliders.cs` [OK]

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

### Scene 5 — `UIScene_Selection.cs` [OK]

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

### Scene 6 — `UIScene_ColorPicker.cs` [OK]

> Controles cubiertos: **ColorPickerRGB**, **ColorPickerHSV**

**Layout:** `StackPanel` horizontal con dos columnas, swatch debajo.

- Columna izquierda (`StackPanel` V): `Label` "RGB" + `ColorPickerRGB` (ancho 260 px)
- Columna derecha (`StackPanel` V): `Label` "HSV" + `ColorPickerHSV` (ancho 260 px)
- Fila inferior: `Panel` swatch 60×60 px + `Label` hex "#RRGGBB"

**Notas:**
- `ColorPickerRGB.ColorChanged` y `ColorPickerHSV.ColorChanged` → actualizan `Panel.BackgroundColor` y el label hex.
- Los dos pickers comparten la misma referencia a `Pixel` y `Font`.

---

### Scene 7 — `UIScene_Layout.cs` [OK]

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

### Scene 8 — `UIScene_ScrollView.cs` [OK]

> Controles cubiertos: **ScrollView**

**Layout:** Dos `ScrollView` lado a lado.

- **Vertical** (300×300 px): 25 Labels "Ítem 01" – "Ítem 25"; scroll con rueda de ratón.
- **Horizontal** (400×60 px): 10 Labels anchos "Categoría Muy Larga X"; scroll arrastrando (o rueda horizontal).

**Notas:**
- `ScrollView` requiere `GraphicsDevice` en el constructor.
- Scroll wheel se procesa con `Core.Input.Mouse.ScrollWheelDelta` en `Update`.

---

### Scene 9 — `UIScene_Tooltip.cs` [OK]

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

### Scene 10 — `UIScene_Focus.cs` [OK]

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

### Scene 11 — `UIScene_Transitions.cs` [OK]

> Controles cubiertos: **UITransitionManager**, **UITweenExtensions**
> (FadeIn, FadeOut, SlideInFromLeft/Right/Top/Bottom, SlideOutToLeft/Right/Top/Bottom)

**Layout:** Dos columnas lado a lado.

**Columna izquierda — controles (≈400 px):**

| Control | Configuración |
|---------|--------------|
| `Label` | "Transitions Demo" — header |
| `Dropdown` | "Transition In:" — 5 opciones de entrada (FadeIn, SlideInFrom×4) |
| `Dropdown` | "Transition Out:" — 5 opciones de salida (FadeOut, SlideOutTo×4) |
| `Label` + `Slider` | "Duración: X.Xs" — rango 0.2–2.0 s, step 0.1 |
| `Dropdown` | "Easing:" — Linear, EaseOutQuad, EaseInQuad, EaseInOutQuad, EaseOutBounce |
| `Button` | "▶ Play In" — ejecuta la transición de entrada seleccionada |
| `Button` | "▶ Play Out" — ejecuta la transición de salida seleccionada |
| `Button` | "Reset" — restaura `Opacity=1` y posición original del target |
| `Label` | Reactivo: "Estado: Idle / Playing" |

**Columna derecha — target (≈400 px):**
- `Panel` de 200×120 px con `Label` "Target" centrado — es el elemento sobre el que se ejecutan las transiciones.
- `Label` debajo: "Última transición: {nombre}" en `Color.DimGray`.

**Notas:**
- `private UITransitionManager _transitions = new();`
- Guardar `_targetPanel.Bounds` original en `LoadContent` para el botón Reset.
- El estado Playing/Idle se gestiona con un `bool _isPlaying` local.
- Registrar `UIScene_Transitions` en `DemoGame.ConfigureServices`.
- Añadir botón `"11. UI Transitions (UITransitionManager)"` en `UIScene_Menu.BuildUI`.
- Requiere `using Alca.MonoGame.Kernel.UI.Transitions;` (o añadir al `Globals.cs`).

---

## ECS Demo Scene

### `EcsDemoScene.cs` [OK]

> Controles cubiertos: **GameEntity**, **TransformBehaviour**, **GameWorld**

- Entidad padre en el centro de pantalla.
- Entidad hijo que orbita alrededor del padre (modifica `LocalPosition` con ángulo creciente).
- Labels en pantalla mostrando posición world y local de cada entidad.
- Botón `← Menú` para volver al hub de navegación.

---

## Escenas Futuras / Pendientes

> No hay escenas pendientes — todas las escenas del roadmap han sido implementadas.

### Camera2D Demo — `Camera2DScene.cs` [OK]

> Sistemas: **Camera2D**, **CameraEffects** (Shake, ZoomTo, Follow)

- Sprite/rectángulo orbitando en mundo 2D renderizado con `SpriteBatch.Begin(transformMatrix: camera.GetTransformMatrix(...))`.
- Panel UI superpuesto sin transformación: botones Shake, Zoom In/Out/Reset, Toggle Follow.
- `Label` reactivo con posición y zoom actuales.
- `_cameraEffects.Update(gameTime)` en `Update`.

**Notas:**
- El mundo se dibuja con `SpriteBatch.Begin(transformMatrix: camera.GetTransformMatrix(viewport))`.
- La UI se dibuja en una segunda pasada sin transformación de cámara.

---

### Physics2D Demo — `Physics2DScene.cs` [OK]

> Sistemas: **Physics2DWorld**, **RigidBody2D**, **BoxCollider2D**, **CircleCollider2D**, **Physics2DQuery**

- `Physics2DWorld` con gravedad `(0, 9.8f)`.
- Suelo estático: entidad con `BoxCollider2D`, `IsStatic = true`.
- Click para spawnear bolas dinámicas (`CircleCollider2D` + `RigidBody2D`).
- Panel de controles UI: botones Spawn Ball, Apply Impulse, Raycast.
- `DebugDraw` opcional para visualizar AABB de colliders.

**Notas:**
- `GameWorld.PhysicsWorld = new Physics2DWorld(gravity)` antes de crear entidades.
- La conversión posición ratón → mundo requiere la inversa de la matrix de cámara.

---

### Navigation Demo — `NavigationScene.cs` [OK]

> Sistemas: **NavGrid**, **Pathfinder**, **NavAgent**, **SteeringController**

- `NavGrid` 20×15 celdas con obstáculos configurables en runtime.
- Click derecho → `navAgent.SetDestination(worldPos)` con ruta A* visualizada.
- Panel de controles UI: Toggle Obstacle, Recompute Path, Show/Hide Grid.
- `Label` reactivo: estado del agente (`Idle`/`Moving`) y nº de waypoints.

**Notas:**
- `Pathfinder` se instancia con `new Pathfinder(gridCapacity: 20 * 15)`.
- La conversión posición ratón → celda: `navGrid.WorldToCell(mouseWorldPos)`.
- Ruta visualizada con `DebugDraw.DrawLine` entre waypoints consecutivos.
