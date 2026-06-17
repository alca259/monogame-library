# Roadmap de migración: MonoGame.Editor.Maui → MonoGame.Editor.Winforms

## Contexto

El editor de escenas vive hoy en `src/Editor/` como app .NET MAUI (WinUI unpackaged) sobre
una solución propia `MonoGame.Editor.slnx`. La rama actual es `feature/winforms_rollback`:
la intención es **abandonar MAUI** y reconstruir la capa de presentación en **WinForms**,
manteniendo intacta toda la lógica.

La arquitectura ya está limpiamente separada en tres proyectos:

- **`MonoGame.Editor.Core`** (net10.0) — estado, modelos, comandos, eventos, gizmos,
  serialización, codegen, assets. **100 % agnóstico de UI.** Referencia
  `Alca.MonoGame.Kernel`, `MonoGame.Framework.DesktopGL 3.8.*`, `MonoGame.Extended 6.0.0`.
- **`MonoGame.Editor.SourceGenerator`** (netstandard2.0) — genera `*_Scene.g.cs`. No toca UI.
- **`MonoGame.Editor.Maui`** (net10.0-windows) — front-end MVVM (CommunityToolkit.Mvvm)
  que se va a **sustituir**, no portar fichero a fichero de XAML.

**Resultado esperado:** un nuevo proyecto `MonoGame.Editor.Winforms` (WinForms, net10.0-windows)
dentro de la misma solución que reproduce toda la funcionalidad, estilos, diálogos, paneles,
viewport, gizmos, behaviours y propiedades del editor MAUI. La migración es **incremental**:
ambos front-ends conviven mientras dura el traspaso; al final se retira el proyecto MAUI.

Este documento es deliberadamente exhaustivo para poder **iterarlo en el tiempo**: cada fase
es autocontenida, verificable y deja el editor funcionando.
Por cada fase, se hará un git commit local **sin push**, detallando lo que se ha hecho.

---

## Decisiones arquitectónicas (CERRADAS)

1. **Viewport = MonoGame real embebido.** Se hospeda un `GraphicsDevice` real de MonoGame en
   un control WinForms y se dibuja con `SpriteBatch` real (sprites, shaders, texturas), no solo
   gizmos GDI+.
   - **Restricción dura:** Core referencia `MonoGame.Framework.DesktopGL`. Un proceso solo puede
     cargar **un** flavor de `MonoGame.Framework.dll`. Como la app WinForms referenciará a Core,
     queda **bloqueada en DesktopGL** → **no** se puede usar el `SwapChainRenderTarget` de WindowsDX
     (que es el camino HWND clásico). Por tanto, embedding = **off-screen `RenderTarget2D` +
     readback (`GetData<Color>`) + blit a `Bitmap`** dibujado en `OnPaint` del control.
     (Alternativa de mayor rendimiento documentada en §Riesgos, fuera de alcance inicial.)
2. **Se mantienen los ViewModels (CommunityToolkit.Mvvm).** Son agnósticos de UI
   (`INotifyPropertyChanged` + `ICommand`). Se reutilizan casi verbatim; solo cambia el
   marshalling de hilo (`MainThread.BeginInvokeOnMainThread` → `SynchronizationContext` /
   `Control.BeginInvoke`) y el enlace pasa a **data binding de WinForms**.
3. **Layout = SplitContainer anidados** nativos de WinForms (sin librería de docking). Mapea 1:1
   al layout actual de filas/columnas fijas con separadores arrastrables.

---

## Estructura objetivo de la solución

```
src/Editor/
├── MonoGame.Editor.slnx                 (+ nuevo proyecto añadido)
├── MonoGame.Editor.Core/                 ← SIN CAMBIOS (reutilizado tal cual)
├── MonoGame.Editor.SourceGenerator/      ← SIN CAMBIOS
├── MonoGame.Editor.Maui/                 ← se retira al final
└── MonoGame.Editor.Winforms/             ← NUEVO
    ├── MonoGame.Editor.Winforms.csproj   (net10.0-windows, UseWindowsForms=true,
    │                                       ref → Core; PackageRef CommunityToolkit.Mvvm,
    │                                       Serilog, Serilog.Sinks.File)
    ├── Program.cs                         (Main: STAThread, Serilog, ApplicationConfiguration,
    │                                       arranca el host MonoGame y Application.Run(MainForm))
    ├── Theme/
    │   ├── EditorColors.cs                (tokens de Colors.xaml → System.Drawing.Color const)
    │   ├── EditorFonts.cs                 (Segoe UI / Consolas, tamaños)
    │   └── EditorStyles.cs                (helpers: estilizar Button/Label/TextBox/Panel)
    ├── Infrastructure/
    │   ├── UiDispatcher.cs                (marshalling a hilo UI vía SynchronizationContext)
    │   ├── WinFormsBindingExtensions.cs   (helpers Bind() sobre INotifyPropertyChanged)
    │   ├── DialogService.cs               (alertas/prompts/file·folder pickers WinForms)
    │   └── EditorBootstrapper.cs          (inicializa EditorContext, logger, registries)
    ├── Rendering/
    │   ├── MonoGameViewportHost.cs        (Control: hospeda GraphicsDevice off-screen + blit)
    │   ├── MonoGameRenderLoop.cs          (bucle de render/Update con Application.Idle o timer)
    │   ├── ViewportSceneRenderer.cs       (dibuja escena/grid/handles con SpriteBatch real)
    │   ├── GizmoRenderer.cs               (grid, ejes, selection box, gizmos — primitivas MG)
    │   ├── OrientationGizmoRenderer.cs    (gizmo orientación + hit-test, port de la lógica)
    │   ├── MaterialPreviewHost.cs         (preview de material con MonoGame off-screen)
    │   └── PrimitiveBatch.cs              (líneas/círculos/paths con VertexPositionColor)
    ├── Controls/
    │   ├── AxisStepper.cs                 (UserControl: tag color + entry + ▲▼)
    │   ├── CollapsibleSection.cs          (cabecera con flecha + panel de contenido)
    │   ├── ToolStripToggleButton.cs       (botón toggle estilo toolbar)
    │   ├── FlatIconButton.cs              (botón plano con icono/glyph)
    │   └── ThemedListView.cs              (ListView/TreeView temados oscuros reutilizables)
    ├── Forms/
    │   ├── MainForm.cs / .Designer.cs     (ventana principal: menú, toolbar, splits, statusbar)
    │   └── Dialogs/                       (un Form por diálogo — ver §Diálogos)
    ├── Panels/                            (un UserControl por panel — ver §Paneles)
    ├── Drawers/                           (editores de behaviour — port de Drawers/ MAUI)
    └── Resources/
        ├── Shaders/                       (copiados de Maui/Resources/Shaders)
        └── Icons/                         (glyphs/iconos para toolbar y árboles)
```

**Regla de oro:** `MonoGame.Editor.Core` **no se toca**. Toda comunicación entre paneles sigue
fluyendo por `EditorContext.Instance` + `IEditorEventBus`. Si surge la tentación de modificar
Core, parar y replantear: casi siempre es síntoma de meter UI donde no toca.

---

## Mapa de reutilización (qué se copia, qué se reescribe)

| Origen (Maui) | Destino (Winforms) | Acción |
|---|---|---|
| `MonoGame.Editor.Core/*` | — | **Reutilizar tal cual** (referencia de proyecto) |
| `ViewModels/*ViewModel.cs` | `ViewModels/*` (copiados) | **Reutilizar**; cambiar namespace y marshalling |
| `ViewModels/ViewModelBase.cs` | `Infrastructure` | Adaptar `On<TEvent>` → `UiDispatcher` en vez de `MainThread` |
| `ViewModels/Dialogs/DialogViewModel.cs` | igual | Reutilizar (evento `CloseRequested`) |
| `Rendering/EditorCamera2D.cs` | `Rendering/EditorCamera2D.cs` | **Reutilizar** (matemática pura, sin MAUI) |
| `Rendering/ViewportRenderer.cs` (IDrawable) | `Rendering/GizmoRenderer.cs` + `ViewportSceneRenderer.cs` | **Reescribir** sobre SpriteBatch/PrimitiveBatch; portar toda la lógica de gizmos/hit-test |
| `Rendering/MaterialPreviewRenderer.cs` | `Rendering/MaterialPreviewHost.cs` | Reescribir sobre MonoGame |
| `Services/DialogService.cs` | `Infrastructure/DialogService.cs` | **Reescribir** con `OpenFileDialog`/`SaveFileDialog`/`FolderBrowserDialog`/`MessageBox` |
| `Controls/AxisStepper.xaml(.cs)` | `Controls/AxisStepper.cs` | Reescribir como UserControl |
| `Controls/FocusOnClickBehavior.cs` | patrón en cada panel | Reescribir: handler `Enter`/`MouseDown` → `EditorContext.SetFocus` |
| `Converters/*` | `Theme/` helpers o binding format | Convertir a métodos/`Binding.Format` |
| `Resources/Styles/Colors.xaml` | `Theme/EditorColors.cs` | **Traducir** hex → `Color.FromArgb` |
| `Resources/Styles/ControlStyles.xaml` | `Theme/EditorStyles.cs` | Traducir a helpers de estilizado |
| `Views/EditorWindow.xaml(.cs/.Menus/.Shortcuts)` | `Forms/MainForm.cs` + parciales | Reescribir layout con SplitContainer; portar menús y atajos |
| `Views/Panels/*View.xaml(.cs)` | `Panels/*Panel.cs` | Reescribir UI; reutilizar VM e item-models |
| `Views/Dialogs/*Dialog.xaml(.cs)` | `Forms/Dialogs/*Form.cs` | Reescribir UI; reutilizar VM |
| `Views/Panels/*Item.cs`, `Dialogs/*Node.cs` (item models) | `Panels/Models/` | **Reutilizar** (son POCOs) |
| `Drawers/*` (BehaviourEditor + 15 BuiltIn + Registry + Helper) | `Drawers/*` | Portar: `PropertyControlHelper` reconstruye controles WinForms en vez de MAUI |

---

## Traducción del tema (Colors.xaml → EditorColors.cs)

`EditorColors.cs` define `public static readonly Color` para cada token. Valores exactos a portar
(de `Resources/Styles/Colors.xaml`):

| Token | Hex | Uso |
|---|---|---|
| `ShellBackground` / `BgWindow` | `#1A1A1B` | fondo ventana |
| `PanelBackground` / `BgPanel` | `#1E1E20` | interior de panel |
| `PanelBackgroundAlt` / `BgChrome` | `#252528` | barras/cabeceras/toolbars |
| `ViewportBackground` | `#141416` | canvas del viewport |
| `InputBackground` | `#2A2A2E` | campos de texto |
| `InputBackgroundHover` | `#323237` | hover de input |
| `RowSelected` | `#2D4A6B` | fila seleccionada |
| `Border` / `BorderColor` | `#34343A` | divisores/bordes |
| `BorderFocus` / `AccentBlue` / `AxisBlue` | `#4A9EFF` | foco/acento/eje Z |
| `AccentBlueDim` | `#2F6FB0` | acento atenuado |
| `AxisRed` | `#E5484D` | eje X / stop |
| `AxisGreen` / `PlayGreen` / `FpsGreen` | `#46C66A` | eje Y / play / fps |
| `BuildErrorBg` | `#C73E3E` | fondo status build fallido |
| `BuildErrorFg` | `#FFFFFF` | texto error |
| `BuildOkFg` / `TextSecondary` | `#9A9AA2` | texto status normal / secundario |
| `TextPrimary` | `#E6E6E8` | texto principal |
| `TextMuted` / `TextDim` | `#6A6A72` | texto atenuado/placeholder |
| `BtnSuccessBg` | `#388E3C` | botón confirmar |
| `MaterialIcon` | `#E5484D` | icono material |

`EditorStyles.cs` traduce los estilos nombrados de `ControlStyles.xaml` a helpers
(`ApplyFlatButton`, `ApplyToolButton`, `ApplyInputShell`, `ApplySectionTitle`, `ApplyMonoLabel`,
`ApplyBadge`, `ApplyAxisTag`, `ApplyPillButton`, `ApplyTabButton`…). Tamaños/alturas exactos:
toolbar 42px, menú 28px, status 24px, dockbar ~266px, panel jerarquía 268px, inspector 362px,
ToolButton 30×30, pills 26px, play/stop 36×28.

> Desde **.NET 10** WinForms tiene **modo oscuro nativo** (ya **no** en preview): se activa con
> `Application.SetColorMode(SystemColorMode.Dark)` en `Program.Main`. Es el **punto de partida**
> del tema y cubre la mayoría de controles estándar. Donde la paleta exacta del editor (los hex de
> esta sección) no quede idéntica con solo el modo oscuro nativo, se aplican `EditorColors`/
> `EditorStyles` **encima** y se recurre a `OwnerDraw`/renderers personalizados (`ThemedListView`,
> `ToolStripProfessionalRenderer` con `ColorTable` propia) únicamente en esos controles puntuales.

---

## Estrategia de ViewModels y data binding en WinForms

- **Reutilización:** copiar `ViewModels/` de Maui al nuevo proyecto, cambiar el namespace y quitar
  cualquier `using Microsoft.Maui.*`. La mayoría no los usa (lógica + `[ObservableProperty]` +
  `[RelayCommand]`). Donde un VM use tipos MAUI (p. ej. `Color`, `Keyboard`), sustituir por
  tipos neutros (`System.Drawing.Color`, enum propio) o moverlo a la capa de vista.
- **`ViewModelBase`:** versión WinForms idéntica salvo `On<TEvent>`:
  ```csharp
  void Wrapped(TEvent e) => UiDispatcher.Post(() => handler(e)); // antes: MainThread.BeginInvokeOnMainThread
  ```
  `UiDispatcher` captura el `SynchronizationContext` del hilo UI en `Program.Main` y hace
  `_ctx.Post(...)`. `Attach()/Detach()` se invocan desde el `Load`/`HandleDestroyed` del
  UserControl (equivalente a `OnHandlerChanged`).
- **Binding:** usar `Control.DataBindings.Add(...)` sobre `INotifyPropertyChanged`. Patrones:
  - `TextBox.Text` ↔ `vm.Property` con `DataSourceUpdateMode.OnPropertyChanged`.
  - `CheckBox.Checked`, `Label.Text`, `Control.Enabled`/`Visible` ↔ propiedades del VM.
  - Colecciones (`ObservableCollection<T>`) → `ListView`/`TreeView` repoblados en
    `ListChanged`/`CollectionChanged` (WinForms `ListView` no hace binding real de items; se
    refresca manualmente — patrón ya presente en los VMs vía eventos del bus).
  - Comandos (`IRelayCommand`): handler `button.Click += (_,_) => vm.XCommand.Execute(null);`
    y `vm.XCommand.CanExecuteChanged += (_,_) => button.Enabled = vm.XCommand.CanExecute(null);`
    Encapsular en `WinFormsBindingExtensions.BindCommand(button, command)`.

---

## Viewport: hospedaje de MonoGame real (pieza crítica)

**`MonoGameViewportHost : Control`** (doble buffer activado, `SetStyle(UserPaint|AllPaintingInWmPaint)`):

1. **Init device (off-screen):** crear un `GraphicsDevice` con `GraphicsAdapter.DefaultAdapter`,
   `GraphicsProfile.HiDef/Reach`, y `PresentationParameters` apuntando a `this.Handle` (DesktopGL
   admite crear `GraphicsDevice` con un HWND, pero el present a la ventana SDL no aplica aquí: se
   usa render-to-texture). Mantener un `RenderTarget2D` del tamaño del control, recreado en
   `OnResize`.
2. **Bucle:** `Application.Idle += OnIdle` con comprobación `IsApplicationIdle()` (PeekMessage),
   o un `System.Windows.Forms.Timer`/`HighResolution` a ~60 fps. En cada tick:
   `Update(gameTime)` (cámara, animación de gizmos) → `Draw` sobre el `RenderTarget2D`.
3. **Blit:** `renderTarget.GetData<Color>(buffer)` → copiar a `Bitmap` (32bppArgb, cuidando orden
   BGRA/premultiplied) → `Invalidate()` → en `OnPaint`, `e.Graphics.DrawImageUnscaled(bitmap,0,0)`.
4. **Contenido del Draw** (port de `ViewportRenderer`, ahora con primitivas reales):
   - `GizmoRenderer.DrawGrid` (grid adaptativo: misma fórmula `MinGridStepPx=40`, `GridScaleFactor=10`,
     etiqueta de escala `FormatGridCellSize` m/cm/km).
   - `ViewportSceneRenderer.DrawSceneObjects` — recorre `EditorContext.ActiveScene.RootGameObjects`
     recursivo; dibuja sprite real cuando hay `SpriteRenderer`, si no un rect placeholder.
   - `GizmoRenderer.DrawSelection` (rect + 4 handles de esquina, color `AccentBlue`).
   - `GizmoRenderer.DrawGizmoHandles` — Move/Rotate/Scale/Universal según
     `EditorContext.Gizmos.Mode` y `EnabledAxes`/`EnabledTools`; reusar constantes de
     `GizmoController` (ArrowLength, ArrowHeadSize=14, RotateRadius, ScaleHandleSize,
     UniversalScaleAxisRadius).
   - `DrawBehaviourGizmos` — círculos de radio por `[EditorRadiusPreview]` y por
     `BehaviourEditor.RadiusPreviewProperties` (reusar `BehaviourEditorRegistry`).
   - `DrawGizmo` (ejes esquina inf-izq) y `OrientationGizmoRenderer` (gizmo orientación esquina
     sup-der + `OrientationGizmoHitTest` → `ViewOrientation`).
   - Proyección por `ViewOrientation` (Front/Top/Right): portar `GetWorldCenter`,
     `GetVisibleRotation`, `GetVisibleScale`.
5. **`ResolveColor`:** ya no lee `Application.Current.Resources`; lee de `EditorColors` y convierte
   `System.Drawing.Color` → `Microsoft.Xna.Framework.Color`.
6. **Entrada:** handlers nativos del control (`MouseDown/Move/Up/Wheel`, `KeyDown`). Mapear a la
   misma lógica que `EditorWindow.xaml.cs`:
   - rueda → `Camera.ZoomAt(1.1^delta, cursor, size)`.
   - botón medio/derecho + arrastre → `Camera.Pan`.
   - clic izq → hit-test gizmo orientación, luego selección de objeto (`EditorContext.SetSelection`).
   - arrastre con gizmo activo → `GizmoController.BeginDrag/UpdateDrag/EndDrag` (genera comando undo).
   - teclas Q/W/E/R/T/H/U/G/Delete/F solo cuando el foco es `Viewport`.

`PrimitiveBatch` envuelve `DrawUserPrimitives` con `VertexPositionColor` para líneas, círculos
(polilíneas), triángulos de puntas de flecha y paths. Texto del viewport (etiquetas de eje,
escala de grid, "Front/Top/Right") con un `SpriteFont` propio. La matemática de la cámara y los
hit-tests se reutilizan/portan sin cambios de fórmula.

> El `EditorCamera2D` actual usa `PointF/SizeF` (System.Drawing) → se reutiliza tal cual; solo se
> convierte a `Vector2` al alimentar SpriteBatch.

---

## Layout de `MainForm` (SplitContainer anidados)

Estructura de docking equivalente a las 5 filas del `EditorWindow.xaml`:

```
MainForm (KeyPreview=true para atajos globales)
├─ MenuStrip            (Dock=Top, 28px)   File/Edit/Project/Debug/View
├─ ToolStrip            (Dock=Top, 42px)   gizmos Q/W/E/R/T/H/U, SNAP/NAV/RES, M/R/S/X/Y, Play/Stop
├─ StatusStrip          (Dock=Bottom,24px) build status (bg dinámico), objetos, plataforma, FPS
└─ SplitContainer V1    (Dock=Fill, Horizontal=fila cuerpo/dockbar)
   ├─ Panel1: SplitContainer H_main (vertical)
   │    ├─ Panel1: SceneHierarchyPanel        (268px)
   │    └─ Panel2: SplitContainer H_right (vertical)
   │         ├─ Panel1: MonoGameViewportHost  (centro, *)
   │         └─ Panel2: InspectorPanel         (362px, con tabs)
   └─ Panel2: DockBarPanel (TabControl temado, ~266px)
```

- Anchos/altos iniciales y `SplitterDistance` persistidos vía `EditorPreferences` (Core) en
  `SplitterMoved` (con throttling), exactamente como hoy.
- Resaltado de panel con foco: borde dibujado en `Paint` del panel cuando su VM `IsFocused`
  (sustituye al `Border` overlay de MAUI).
- Menús dinámicos: portar `EditorWindow.Menus.cs` construyendo `ToolStripMenuItem` en
  `MainForm.Menus.cs`. Atajos: portar `EditorWindow.Shortcuts.cs` a
  `MainForm.Shortcuts.cs` enrutando en `ProcessCmdKey`/`KeyDown` según `EditorContext.ActiveFocus`
  (Ctrl+S/Z/Y/B/F5 siempre; Q/W/E/R/T/H solo `Viewport`; Alt+letra solo `Global`).

---

## Paneles (Views/Panels → Panels/*Panel : UserControl)

Cada panel: `UserControl` que instancia su VM, hace `Attach()` en `Load` y `Detach()` en
`HandleDestroyed`, suscribe foco (`Enter`/`MouseDown` → `EditorContext.SetFocus(ctx)`), y enlaza
controles al VM. Inventario y contenido a reproducir:

| Panel | VM reutilizado | UI WinForms a construir |
|---|---|---|
| **SceneHierarchyPanel** | `SceneHierarchyViewModel` | toolbar (+/🗑/✎/↑) + búsqueda + `TreeView` temado con expand/collapse, rename inline, reparent drag, footer estado. Item: `HierarchyItem` |
| **InspectorPanel** | `InspectorViewModel` | `TabControl` (Inspector/Material/UITheme/Sprite); header objeto (checkbox activo, nombre, badge id); sección Transform colapsable con 3×3 `AxisStepper`; tarjetas de behaviour dinámicas (Drawers); botón "+ Add Behaviour" |
| **MaterialInspectorPanel** | `MaterialInspectorViewModel` | picker shader, picker render mode, secciones dinámicas por shader, `MaterialPreviewHost` (160px) |
| **AssetBrowserPanel** | `AssetBrowserViewModel` | toolbar (import/rename/delete/new folder) + filtro + breadcrumb + `TreeView` carpetas (200px) + `ListView` assets. Items: `AssetItem`, `FolderItem` |
| **ConsolePanel** | `ConsolePanelViewModel` | toggles Info/Warn/Error (color via BoolToColor), copy, clear, `ListView`/RichTextBox de log |
| **SceneManagerPanel** | `SceneManagerViewModel` | lista escenas (crear/cargar/borrar/rename). Item: `SceneItem` |
| **ScriptBrowserPanel** | `ScriptBrowserViewModel` | árbol scripts + crear. Item: `ScriptItem` |
| **LocalizationBrowserPanel** | `LocalizationBrowserViewModel` | grid clave/valor (`DataGridView`) |
| **InputMapEditorPanel** | `InputMapEditorViewModel` | grid acciones/bindings (`DataGridView`) |
| **TilemapPalettePanel** | `TilemapPaletteViewModel` | paleta tileset + pintar tiles (PictureBox/host) |
| **UIThemeInspectorPanel** | `UIThemeInspectorViewModel` | editor colores/fuentes del tema UI |
| **SpriteInspectorPanel** | `SpriteInspectorViewModel` | metadatos sprite (frames/animaciones) |
| **UndoHistoryPanel** | `UndoHistoryViewModel` | lista pila undo/redo (`ListView`) |
| **DockBarPanel** | `DockBarViewModel` | `TabControl` temado contenedor de: Scenes/Assets/Console/Localization/InputMaps/Tilemap/History/Scripts |

`DataGridView` y `ListView` se teman oscuros aprovechando el modo oscuro nativo de .NET 10 y, donde
no baste, aplicando colores propios + `OwnerDraw`.

---

## Diálogos (Views/Dialogs → Forms/Dialogs/*Form : Form)

Patrón: `Form` con `FormBorderStyle=FixedDialog`, `StartPosition=CenterParent`, `DialogResult`,
que instancia su `{Dialog}ViewModel : DialogViewModel<TResult>` y cierra al recibir
`CloseRequested`. Se conserva el `static Task<TResult?> ShowAsync(IWin32Window owner)` con
`TaskCompletionSource<TResult>` (equivalente al `ShowAsync(INavigation)` de MAUI).

| Diálogo | VM | Campos / contenido |
|---|---|---|
| **NewProjectForm** | `NewProjectViewModel` | nombre, carpeta padre (+browse), ruta .csproj (+browse), validación, Cancel/Create |
| **NewSceneForm** | `NewSceneViewModel` | nombre, ancho/alto mundo, validación, Cancel/Create |
| **AddBehaviourForm** | `AddBehaviourViewModel` | búsqueda + `TreeView` (`BehaviourTreeNode`) + Rescan, Cancel/Add |
| **NewBehaviourForm** | `NewBehaviourViewModel` | crear script behaviour (nombre, namespace) |
| **ScriptCreationForm** | `ScriptCreationViewModel` | nombre, namespace, clase base opcional |
| **ProjectSettingsForm** | `ProjectSettingsViewModel` | edición .csproj juego + world config |
| **WorldConfigForm** | `WorldConfigViewModel` | gravedad, damping, tamaño mundo |
| **LocaleCreationForm** | `LocaleCreationViewModel` | nueva clave/idioma |
| **RelativePathPickerForm** | `RelativePathPickerViewModel` | `TreeView` FS (`FileSystemNode`) + salida ruta relativa |
| **CodeGenProgressForm** | `CodeGenProgressViewModel` | `ProgressBar` modal durante codegen |
| **RgbaColorPickerForm** | (code-behind MAUI) | cuadro SV (220×220), tira Hue (20×220), slider alpha, swatch, hex `#RRGGBBAA`. Reescribir el dibujo con `Panel`+`OnPaint` GDI+ o un `MonoGameViewportHost` pequeño; lógica de color portada del code-behind |

`DialogService` (Infrastructure) reemplaza al de MAUI:
- `ConfirmAsync/AlertAsync` → `MessageBox.Show`.
- `PromptAsync` → mini-Form con un `TextBox` (WinForms no tiene prompt nativo).
- `ActionSheetAsync` → `ContextMenuStrip` o mini-Form de botones.
- `PickFileAsync` → `OpenFileDialog`; `SaveFileAsync` → `SaveFileDialog`;
  `PickFolderAsync` → `FolderBrowserDialog`.
- Todos devuelven `Task<...>` para no cambiar las firmas que consumen los VMs.

---

## Behaviours / Inspector de propiedades (Drawers/)

Portar `Drawers/`:
- `BehaviourEditor` (base), `BehaviourEditorRegistry` (mapa typeName→editor),
  `PropertyControlHelper` (constructor de controles), y los **15 BuiltIn** (AmbientLight,
  AudioZone, BillboardSprite, ChoicesPanel, DialogueBox, DirectionalLight2D, ParticleEmitter,
  PointLight2D, SpatialAudioListener, SpatialAudioSource, SpotLight2D, SpriteRenderer,
  SteeringController, Weather, YSortRenderer).
- `PropertyControlHelper` cambia su salida: en vez de construir vistas MAUI, construye controles
  WinForms (`AxisStepper`/`NumericUpDown` para int/float, `CheckBox` para bool, botón color →
  `RgbaColorPickerForm`, `TextBox` para string, `ComboBox` para enum). El mapeo por atributos de
  Core (`EditorPropertyAttribute`, `EditorRangeAttribute`, `EditorFilePickerAttribute`,
  `EditorHeaderAttribute`, `EditorHideAttribute`, `EditorReadOnlyAttribute`,
  `CustomBehaviourEditorAttribute`) se reutiliza sin cambios.
- Cada tarjeta = `CollapsibleSection` con botón de quitar behaviour. El binding bidireccional
  escribe en `EditorBehaviour.Properties` (JSON) y dispara el comando `SetPropertyCommand`.

---

## Fases de ejecución (incrementales y verificables)

> Cada fase deja la solución **compilando** y, a partir de la Fase 3, el editor **arrancable**.
> Trabajar sobre `feature/winforms_rollback`. Ficheros nuevos en **UTF-8 BOM** (regla de usuario).

### Fase 0 — Andamiaje del proyecto
- Crear `MonoGame.Editor.Winforms.csproj` (`net10.0-windows`, `UseWindowsForms=true`,
  `OutputType=WinExe`, `Nullable=enable`, `ImplicitUsings=enable`, `NoWarn` MVVMTK0045/CA1416).
- Referencia a `MonoGame.Editor.Core`; PackageRefs: `CommunityToolkit.Mvvm 8.*`, `Serilog 4.*`,
  `Serilog.Sinks.File 7.*`. Copiar `Resources/Shaders/**` con `CopyToOutputDirectory`.
- Añadir el proyecto a `MonoGame.Editor.slnx`.
- `Program.cs`: `[STAThread]`, `ApplicationConfiguration.Initialize()`,
  `Application.SetColorMode(SystemColorMode.Dark)` (modo oscuro nativo .NET 10), Serilog
  (`%APPDATA%\MonoGameEditor\logs\editor-.log`), `EditorBootstrapper.Init()`, `UiDispatcher.Capture()`,
  `Application.Run(new MainForm())` con un MainForm vacío.
- **Verificación:** `dotnet build src/Editor/MonoGame.Editor.slnx` OK; arranca ventana vacía.

### Fase 1 — Tema e infraestructura base
- `Theme/EditorColors.cs`, `EditorFonts.cs`, `EditorStyles.cs` (tabla §Tema).
- `Infrastructure/UiDispatcher.cs`, `WinFormsBindingExtensions.cs`, `DialogService.cs`,
  `EditorBootstrapper.cs`.
- `ViewModelBase` WinForms (adaptación de `On<TEvent>`), `DialogViewModel<T>` copiado.
- `Controls/`: `AxisStepper`, `CollapsibleSection`, `FlatIconButton`, `ToolStripToggleButton`,
  `ThemedListView` + renderers oscuros (`MenuStrip`/`ToolStrip`/`TabControl`).
- **Verificación:** un form de prueba muestra controles temados; `dotnet build` OK.

### Fase 2 — Shell principal (MainForm sin paneles)
- `MainForm` con `MenuStrip` (File/Edit/Project/Debug/View, ítems portados de `Menus.cs`),
  `ToolStrip` (botones gizmo/modo/transport enlazados a `EditorWindowViewModel`),
  `StatusStrip` (build status con bg dinámico, objetos, plataforma, FPS), y el esqueleto de
  `SplitContainer` anidados con panels vacíos.
- Portar `EditorWindowViewModel` (toolbar/status/transport/menús) y `MainForm.Shortcuts.cs`.
- **Verificación:** menús abren, toolbar refleja estado de `EditorContext.Gizmos`, atajos globales
  (Ctrl+S/Z/Y) llegan a los comandos; status bar reacciona a eventos build/FPS del bus.

### Fase 3 — Viewport MonoGame embebido (núcleo del riesgo)
- `MonoGameViewportHost` + `MonoGameRenderLoop` + `PrimitiveBatch` + `GizmoRenderer` +
  `OrientationGizmoRenderer` + `ViewportSceneRenderer`. Reutilizar `EditorCamera2D`.
- Portar toda la lógica de dibujo y hit-test de `ViewportRenderer` y la entrada de
  `EditorWindow.xaml.cs` (rueda, pan, selección, arrastre de gizmo → comandos undo).
- **Verificación:** se ve grid adaptativo, objetos de la escena activa, selección con handles,
  gizmos Move/Rotate/Scale; rueda hace zoom, arrastre mueve/rota/escala y genera undo; el gizmo
  de orientación cambia Front/Top/Right. Probar con una escena real del proyecto demo.

### Fase 4 — Panel Jerarquía + Inspector + Transform
- `SceneHierarchyPanel` (TreeView, búsqueda, +/🗑/✎/↑, rename, reparent).
- `InspectorPanel` con tabs y sección Transform (3×3 `AxisStepper` enlazados a la selección).
- Sincronía selección ↔ viewport ↔ jerarquía ↔ inspector vía bus.
- **Verificación:** seleccionar en jerarquía resalta en viewport e inspector; editar Transform en
  inspector mueve el objeto en viewport y viceversa; undo/redo coherente.

### Fase 5 — Tarjetas de Behaviour (Drawers) + Add/Remove
- Portar `Drawers/` (base, registry, helper, 15 BuiltIn) y `AddBehaviourForm`.
- Tarjetas colapsables con controles por tipo de propiedad; binding a `EditorBehaviour.Properties`.
- **Verificación:** añadir/quitar behaviours; editar propiedades (incl. color, file picker, rango)
  refleja en modelo y en gizmos de radio del viewport.

### Fase 6 — DockBar inferior: Scenes, Assets, Console
- `DockBarPanel` (TabControl temado) + `SceneManagerPanel`, `AssetBrowserPanel`, `ConsolePanel`.
- `DialogService` ya operativo para import/rename/delete y file pickers.
- **Verificación:** crear/cargar/borrar escena; importar/renombrar/borrar asset; log de consola
  con filtros Info/Warn/Error y copy/clear.

### Fase 7 — Diálogos de proyecto/escena/codegen
- `NewProjectForm`, `NewSceneForm`, `ProjectSettingsForm`, `WorldConfigForm`,
  `CodeGenProgressForm`, `RgbaColorPickerForm`.
- Cablear comandos de menú File/Project y la generación de código (progreso modal).
- **Verificación:** crear proyecto y escena de cero; abrir settings; ejecutar codegen con barra
  de progreso; elegir colores con el picker RGBA.

### Fase 8 — Paneles restantes
- `ScriptBrowserPanel`+`ScriptCreationForm`/`NewBehaviourForm`, `LocalizationBrowserPanel`+
  `LocaleCreationForm`, `InputMapEditorPanel`, `TilemapPalettePanel`, `UIThemeInspectorPanel`,
  `SpriteInspectorPanel`, `UndoHistoryPanel`, `MaterialInspectorPanel`+`MaterialPreviewHost`,
  `RelativePathPickerForm`.
- **Verificación:** cada panel reproduce su funcionalidad MAUI; preview de material renderiza.

### Fase 9 — Play mode, persistencia y pulido
- Play/Stop (`ExternalPlayLauncher`), build (`MgcbRunner`), persistencia de layout
  (`EditorPreferences`: splitters, tamaños, último proyecto), foco/atajos por contexto pulidos,
  iconos/glyphs definitivos, repaso de tema oscuro en todos los controles.
- **Verificación:** sesión completa equivalente a MAUI: abrir proyecto → editar → guardar →
  generar código → play. Comparar pantalla a pantalla con el editor MAUI.

### Fase 10 — Retirada de MAUI
- Eliminar `MonoGame.Editor.Maui` de la solución y del repo; actualizar
  `src/Editor/CLAUDE.md` y `src/Editor/MonoGame.Editor.slnx`; ajustar specs SDD en `docs/specs/`.
- **Verificación:** la solución compila sin MAUI; el editor WinForms es el único front-end.

---

## Verificación end-to-end (global)

```bash
# Compilar toda la solución del editor
dotnet build src/Editor/MonoGame.Editor.slnx

# Ejecutar el nuevo editor
dotnet run --project src/Editor/MonoGame.Editor.Winforms/MonoGame.Editor.Winforms.csproj
```

Recorrido manual de aceptación (debe igualar a MAUI):
1. Abrir un proyecto existente; cargar una escena → render en viewport con grid y objetos.
2. Seleccionar objeto en jerarquía y en viewport; editar Transform; gizmos Move/Rotate/Scale.
3. Añadir/quitar behaviour; editar propiedades (incl. color y radio con gizmo en viewport).
4. Undo/redo en cadena; guardar escena (Ctrl+S); marcar dirty/limpio en barra de título.
5. Assets: importar/renombrar/borrar; consola con filtros; cambiar de pestaña en dockbar.
6. Crear proyecto/escena nuevos; settings; generar código (progreso); Play (lanza el juego).
7. Persistencia: cerrar y reabrir → tamaños de paneles y último proyecto restaurados.

> No hay proyecto de tests en el editor (por diseño). La verificación es manual + `dotnet build`.

---

## Riesgos y mitigaciones

- **Embedding DesktopGL en WinForms (riesgo principal).** DesktopGL es SDL/OpenGL; no hay
  `SwapChainRenderTarget` (eso es WindowsDX) y no se pueden mezclar flavors. Mitigación: ruta
  off-screen `RenderTarget2D` + `GetData` + blit a `Bitmap`. Costes a vigilar: readback GPU→CPU por
  frame (aceptable a tamaño de viewport de editor) y orden de canales BGRA/premultiplied al copiar.
  - *Plan B (si el rendimiento no basta):* aislar el render MonoGame en un módulo que use
    `MonoGame.Framework.WindowsDX` + `SwapChainRenderTarget` y comunicar con Core por interfaces
    neutras (sin tipos de flavor). Es un cambio mayor; queda fuera del alcance inicial y se evalúa
    tras la Fase 3 con mediciones reales.
- **Tema oscuro en controles WinForms.** .NET 10 aporta modo oscuro nativo
  (`Application.SetColorMode(SystemColorMode.Dark)`), base del tema y ya **no** en preview. Aún así,
  no cubre al 100 % la paleta exacta del editor en todos los controles (`MenuStrip`/`ToolStrip`/
  `TabControl`/`ListView`/`TreeView`/`DataGridView`). Mitigación: aplicar `EditorColors`/
  `EditorStyles` encima y usar renderers/`OwnerDraw` centralizados en `Theme/` solo donde el modo
  oscuro nativo no baste, con controles base en `Controls/` desde la Fase 1. Antes de implementar un tema oscuro personalizado,
  preguntar al usuario para que lo compile, y pruebe para asegurar de que realmente no está tematizado ya nativamente.
- **Binding de colecciones.** WinForms no enlaza items de `TreeView`/`ListView` como XAML.
  Mitigación: repoblar en handlers del bus/`CollectionChanged` (patrón ya implícito en los VMs).
- **Hilos.** Eventos del bus pueden venir de hilos de fondo (build/codegen/watcher). Mitigación:
  `UiDispatcher.Post` en `On<TEvent>` y `Control.IsHandleCreated` antes de invocar.
- **Tipos MAUI colados en VMs.** Algún VM/dialog VM podría usar `Color`/`Keyboard` MAUI.
  Mitigación: sustituir por tipos neutros al copiar; detectar en compilación.

---

## Checklist de iteración (marcar al avanzar)

- [x] F0 Andamiaje y solución
- [x] F1 Tema + infraestructura + controles base
- [x] F2 Shell (MainForm: menú/toolbar/status/splits)
- [ ] F3 Viewport MonoGame embebido + entrada + gizmos
- [ ] F4 Jerarquía + Inspector + Transform
- [ ] F5 Behaviours (Drawers) + Add/Remove
- [ ] F6 DockBar: Scenes/Assets/Console
- [ ] F7 Diálogos proyecto/escena/codegen/color
- [ ] F8 Paneles restantes + Material preview
- [ ] F9 Play/build/persistencia/pulido
- [ ] F10 Retirada de MAUI + docs
```
