# MonoGame Editor MAUI — Roadmap técnico (v1)

**Stack**: .NET 10 · C# 14 · MAUI · MonoGame · MonoGame.Extended · MonoGame.Framework.WindowsDX · Alca.MonoGame.Kernel · System.Text.Json  
**Objetivo**: Migrar la interfaz del editor de WinForms a MAUI conservando toda la funcionalidad existente, adoptando el diseño del prototipo HTML (`MonoGame Editor standalone.html`). La lógica reside íntegramente en `MonoGame.Editor.Core`; este proyecto solo añade una capa de presentación nueva.  
**Reglas transversales a todos los desarrollos:**
- Al terminar cada fase, actualizar este fichero marcando el estado.
- `MonoGame.Editor.Core` **nunca se modifica** — todo cambio de comportamiento ya disponible en Core debe usarse tal cual.
- Comunicación entre vistas exclusivamente a través de `IEditorEventBus` — las vistas nunca se llaman directamente entre sí.
- **Sin ViewModels ni bindings complejos**: cada vista suscribe directamente los eventos del EventBus en su code-behind (`.xaml.cs`), igual que los paneles WinForms.
- **NO SE DEBEN HACER TESTS UNITARIOS** en este proyecto de editor.
- Toda operación I/O debe ser `async/await`.

---

## Estilo visual e interfaz

Referencia exacta: prototipo HTML `MonoGame Editor (standalone).html`.

- **Paleta de colores** — solo modo oscuro, sin selector de tema:

| Variable | Valor | Uso |
|----------|-------|-----|
| `--bg-window` | `#1b1b1c` | Fondo general de la ventana |
| `--bg-chrome` | `#2a2a2c` | Barras de herramientas, dock, cabeceras de panel |
| `--bg-panel` | `#212123` | Fondo de paneles y vistas laterales |
| `--bg-raised` | `#2d2d30` | Hover de ítems, filas activas |
| `--bg-input` | `#19191a` | Campos de entrada |
| `--bg-viewport` | `#1d1d1e` | Fondo del viewport MonoGame |
| `--text-primary` | `#d6d6d8` | Texto principal |
| `--text-secondary` | `#a7a7ab` | Etiquetas, subtítulos |
| `--text-dim` | `#86868b` | Texto deshabilitado |
| `--accent-blue` | `#2f81f7` | Selección, resaltado activo, herramienta activa |
| `--accent-green` | `#3fb950` | Play, éxito |
| `--accent-red` | `#f0524f` | Stop, error, advertencia de build |
| `--accent-amber` | `#e3a33b` | Advertencias |
| `--border` | `#3a3a3d` | Bordes entre paneles |
| `--axis-x` | `#e06c6c` | Eje X en transform y gizmo |
| `--axis-y` | `#7cc47c` | Eje Y en transform y gizmo |

- **Tipografía**: `Segoe UI` 12sp para UI; `Cascadia Code` / `Consolas` 11sp para consola e IDs.
- **Esquinas**: 5dp de radio en botones, campos, tarjetas de componente.
- **Iconos**: PNG 16×16 o 24×24 con fondo transparente; Segoe Fluent Icons donde sea posible.
- **Sin gradientes ni sombras saladas** — superficies planas. Solo sombra sutil en tarjetas del inspector.

---

## Estructura de la solución

```
MonoGame.Editor.slnx
├── Alca.MonoGame.Kernel              # Librería existente (referencia, nunca modificar)
├── MonoGame.Editor.Core              # Lógica del editor, sin UI ← NO TOCAR
├── MonoGame.Editor.WinForms          # UI WinForms existente (se mantiene en paralelo)
├── MonoGame.Editor.SourceGenerator   # Roslyn generator (sin cambios)
└── MonoGame.Editor.Maui              # ← NUEVO: UI MAUI
```

**Estructura interna de `MonoGame.Editor.Maui`:**

```
MonoGame.Editor.Maui/
├── MonoGame.Editor.Maui.csproj
├── App.xaml / App.xaml.cs
├── Platforms/
│   ├── Windows/               # Handlers nativos Windows (SwapChainPanel)
│   └── MacCatalyst/           # Handlers nativos macOS (futuro)
├── Controls/
│   ├── MonoGameView.cs        # Control abstracto del viewport
│   └── NumericSpinnerView.xaml # Spinner numérico con etiqueta de eje (X/Y/Z)
├── Views/
│   ├── EditorWindow.xaml      # Ventana principal
│   ├── Panels/
│   │   ├── SceneHierarchyView.xaml
│   │   ├── InspectorView.xaml
│   │   ├── AssetBrowserView.xaml
│   │   ├── ConsolePanelView.xaml
│   │   ├── SceneManagerView.xaml
│   │   ├── LocalizationBrowserView.xaml
│   │   ├── InputMapEditorView.xaml
│   │   ├── TilemapPaletteView.xaml
│   │   ├── ScriptBrowserView.xaml
│   │   ├── SpriteInspectorView.xaml
│   │   ├── MaterialInspectorView.xaml
│   │   ├── UIThemeInspectorView.xaml
│   │   └── UndoHistoryView.xaml
│   └── Dialogs/
│       ├── NewProjectDialog.xaml
│       ├── NewSceneDialog.xaml
│       ├── ProjectSettingsDialog.xaml
│       ├── AddBehaviourDialog.xaml
│       ├── NewBehaviourDialog.xaml
│       ├── ScriptCreationDialog.xaml
│       ├── CodeGenProgressDialog.xaml
│       ├── LocaleCreationDialog.xaml
│       ├── WorldConfigDialog.xaml
│       └── RgbaColorPickerDialog.xaml
├── Resources/
│   ├── Styles/
│   │   ├── Colors.xaml        # Variables de color del tema
│   │   ├── ControlStyles.xaml # Estilos de botones, inputs, etc.
│   │   └── PanelStyles.xaml   # Estilos de cabeceras y tarjetas
│   └── Images/                # Iconos PNG
└── Program.cs
```

**Reglas de arquitectura:**
- `MonoGame.Editor.Maui` referencia únicamente `MonoGame.Editor.Core` (y MonoGame para el viewport).
- Los `.xaml.cs` suscriben y desuscriben eventos del EventBus en `OnAppearing` / `OnDisappearing`.
- **Patrón de panel** (equivalente a los paneles WinForms):
  ```csharp
  public sealed partial class SceneHierarchyView : ContentView
  {
      private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

      public SceneHierarchyView() => InitializeComponent();

      protected override void OnHandlerChanged()
      {
          base.OnHandlerChanged();
          if (Handler is not null) Subscribe();
          else Unsubscribe();
      }

      private void Subscribe()   => _bus.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
      private void Unsubscribe() => _bus.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);

      private void OnSceneLoaded(SceneLoadedEvent e)
          => MainThread.BeginInvokeOnMainThread(() => RebuildTree(e.Scene));
  }
  ```

---

## Layout de referencia (prototipo HTML)

El editor tiene **5 zonas horizontales fijas**:

```
┌─────────────────────────────────────────────────────┐
│  TitleBar (34px) — Logo · Proyecto · Dirty · Chrome  │
├─────────────────────────────────────────────────────┤
│  MenuBar (28px) — File · Edit · Project · Debug      │
├─────────────────────────────────────────────────────┤
│  Toolbar (42px) — Herramientas · Modos · Play/Stop   │
├────────────┬─────────────────────┬───────────────────┤
│ Hierarchy  │     Viewport         │   Inspector       │
│  (268px)   │   (flex-1)           │    (362px)        │
│            │                      │                   │
│            │                      │  Tabs:            │
│            │                      │  Inspector /      │
│            │                      │  Material /       │
│            │                      │  UI Theme         │
├────────────┴─────────────────────┴───────────────────┤
│  Dock (266px) — Tabs: Assets · Console · Scenes · …  │
├─────────────────────────────────────────────────────┤
│  StatusBar (24px) — Build · Objetos · Plataforma · FPS│
└─────────────────────────────────────────────────────┘
```

**Tabs del Dock (9 tabs):** Assets · Console · Scenes · Localization · Input Maps · Tilemap · History · Scripts · Sprite Editor

**Tabs del Inspector (3 tabs):** Inspector · Material Editor · UI Theme Editor

---

## Fase 0 — Proyecto MAUI y shell básico

**Clases clave:** `App`, `EditorWindow`, `MauiProgram`, recursos de estilos.

### 0.1 Crear proyecto

- Tipo: `dotnet new maui` orientado a Windows (target `net10.0-windows10.0.19041.0`).
- Agregar al `MonoGame.Editor.slnx`.
- Dependencias NuGet:

| Paquete | Versión |
|---------|---------|
| `Microsoft.Maui.Controls` | 10.0.* |
| `MonoGame.Framework.WindowsDX` | 3.8.* |
| `Serilog` | 4.* |
| `Serilog.Sinks.File` | 6.* |

- Referencia de proyecto: `MonoGame.Editor.Core`.

### 0.2 Configurar Serilog

Igual que en WinForms: inicializar en `MauiProgram.cs`, mismo `LogsPath` de `EditorPreferences`.  
Capturar excepciones no controladas con `Application.UnhandledException` y mostrar `DisplayAlert`.

### 0.3 Layout de la ventana principal

`EditorWindow.xaml` usa un `Grid` con 5 filas (alturas fijas para chrome, menú, toolbar, estado; flex para el cuerpo y dock):

```xml
<Grid RowDefinitions="34,28,42,*,266,24">
    <views:TitleBarView       Grid.Row="0"/>
    <views:MenuBarView        Grid.Row="1"/>
    <views:ToolbarView        Grid.Row="2"/>
    <Grid Grid.Row="3" ColumnDefinitions="268,*,362">
        <panels:SceneHierarchyView  Grid.Column="0"/>
        <controls:MonoGameView      Grid.Column="1" x:Name="Viewport"/>
        <panels:InspectorView       Grid.Column="2"/>
    </Grid>
    <panels:DockBarView       Grid.Row="4"/>
    <views:StatusBarView      Grid.Row="5"/>
</Grid>
```

### 0.4 Tema oscuro

- `Colors.xaml` define todas las variables de color de la sección **Estilo visual**.
- `ControlStyles.xaml` aplica estilos implícitos a `Button`, `Entry`, `Label`, `Border`.
- Forzar tema oscuro: `Application.Current.UserAppTheme = AppTheme.Dark`.
- Sin modo claro ni selector.

### 0.5 TitleBar personalizado

Reemplazar el chrome por defecto (Windows) con un `TitleBar` MAUI personalizado:
- Icono de la app (logo "M" azul).
- Nombre del proyecto + escena activa (`EditorContext.ActiveProject`, `EditorContext.ActiveScene`).
- Indicador de cambios sin guardar (punto azul `●`), suscrito a `SceneDirtyChangedEvent`.
- Controles de ventana estándar (Minimizar / Maximizar / Cerrar) usando `Window.TitleBar`.

---

## Fase 1 — Control MonoGame embebido (`MonoGameView`)

**Clases clave:** `MonoGameView`, `MonoGameViewHandler` (Windows), `EditorGameLoop`.

### 1.1 Arquitectura del control

`MonoGameView` es un `View` abstracto MAUI. En Windows se implementa mediante un handler personalizado que inyecta un `SwapChainPanel` de WinUI 3 como vista nativa, replicando el patrón de `MonoGame.Editor.WinForms/Controls/MonoGameControl.cs`.

```
MonoGameView (MAUI View)
    └── MonoGameViewHandler (Windows)
            ├── SwapChainPanel  ← superficie de render nativa Win32
            ├── SwapChainRenderTarget  ← target MonoGame
            └── Thread de render dedicado  ← game loop a 60 fps
```

### 1.2 Handler Windows

```csharp
// Platforms/Windows/MonoGameViewHandler.Windows.cs
internal sealed class MonoGameViewHandler : ViewHandler<MonoGameView, SwapChainPanel>
{
    protected override SwapChainPanel CreatePlatformView() => new SwapChainPanel();

    protected override void ConnectHandler(SwapChainPanel nativeView)
    {
        base.ConnectHandler(nativeView);
        nativeView.SizeChanged += OnSizeChanged;
        StartRenderThread(nativeView);
    }

    private void StartRenderThread(SwapChainPanel panel) { /* ... */ }
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) { /* resize RenderTarget */ }
}
```

### 1.3 Integración con EditModeRenderer

`EditorGameLoop` expone los mismos métodos que en WinForms:
- `Initialize(GraphicsDevice)` — conecta `EditModeRenderer`, `GizmoRenderer`, `EditorCamera2D`.
- `Update(GameTime)` — lógica de cámara, input del viewport.
- `Draw(GameTime)` — llama `EditModeRenderer.Draw`, `GizmoRenderer.Draw`.

Reutilizar directamente:
- `MonoGame.Editor.Core/Gizmos/GizmoController.cs`
- `MonoGame.Editor.WinForms/Rendering/EditModeRenderer.cs` (copiar a Core o al nuevo proyecto)
- `MonoGame.Editor.WinForms/Controls/EditorCamera2D.cs` (ídem)

### 1.4 Barra de info del viewport

Franja superior superpuesta al viewport (overlay `AbsoluteLayout`):
```
Camera: Editor  |  Zoom: 100%  |  Grid: 26px  |  0, 0
```
Actualizada frame a frame por el thread de render vía `MainThread.BeginInvokeOnMainThread`.

---

## Fase 2 — Viewport completo (cámara, grid, gizmos, snapping)

**Clases clave:** `EditorCamera2D`, `EditModeRenderer`, `GizmoRenderer`, `GizmoController`.

### 2.1 Input del viewport

Capturar eventos de puntero/teclado sobre el `MonoGameView` usando handlers nativos Windows:
- **Pan**: botón medio o `H` + arrastrar.
- **Zoom**: rueda del ratón.
- **Selección**: click izquierdo → publicar `GameObjectSelectedEvent`.
- **Arrastrar gizmo**: detectar handle más cercano, publicar comandos `MoveCommand`, `RotateCommand`, `ScaleCommand`.

### 2.2 Grid y snapping

Replicar los toggles del toolbar: `SNAP` activa snap-to-grid. La tecla `Ctrl` alterna snap temporalmente.  
Tamaño de grid configurable desde `EditorPreferences`. Mostrado en la barra de info del viewport.

### 2.3 Modos de gizmo

| Tecla | Modo |
|-------|------|
| `Q` | Select |
| `W` | Move |
| `E` | Rotate |
| `R` | Scale |
| `T` | Rect Transform |

El toolbar indica el modo activo con fondo azul (`--accent-blue`).

### 2.4 Toggles de viewport

Píldoras en el toolbar: **2D** · **SNAP** · **NAV** · **RES**.  
Cada toggle actualiza flags en `EditorContext` o `EditorPreferences` y dispara el re-render correspondiente.

---

## Fase 3 — Panel Hierarchy

**Referencia WinForms:** `MonoGame.Editor.WinForms/Panels/SceneHierarchyPanel.cs`

### 3.1 Estructura visual

```
┌─ SceneHierarchyView ──────────────────────┐
│ [+] [🗑] [🔍 Search…]         1 entity    │
│ ─ Scene ──────────────────────────────────│
│ ▶ ☑ 🎮 GameObject                          │
│   ▶ ☑ 🎮 ChildObject                       │
│ ──────────────────────────────────────────│
│ 1 object in scene                          │
└────────────────────────────────────────────┘
```

### 3.2 Control de árbol

MAUI no tiene un `TreeView` nativo en v1. Opciones:
- **Opción A (recomendada):** `CollectionView` con items recursivos y padding de indentación (`Margin.Left = depth * 16`). Cada fila es un `HierarchyItemView` con checkbox de visibilidad, icono de tipo, nombre editable inline y twirl de expansión.
- **Opción B:** Usar CommunityToolkit o una librería de terceros con TreeView MAUI.

### 3.3 Comportamientos

- **Crear entidad**: botón `+` → inline name entry → publicar `CreateGameObjectCommand`.
- **Eliminar**: botón `🗑` (o `Delete`) → publicar `DeleteGameObjectCommand`.
- **Renombrar**: doble clic en el nombre → entrada inline editable → publicar `RenameGameObjectCommand`.
- **Seleccionar**: click en ítem → publicar `GameObjectSelectedEvent`.
- **Búsqueda**: campo "Search…" filtra el árbol en tiempo real sin modificar la estructura real.
- **Reparentar**: drag-and-drop entre nodos → publicar `ReparentGameObjectCommand`.
- **Menú contextual**: Create Child / Duplicate / Delete / Save as Prefab / Apply Prefab / Revert Prefab.

### 3.4 Eventos suscritos

| Evento | Acción en la vista |
|--------|--------------------|
| `SceneLoadedEvent` | Reconstruir árbol completo |
| `GameObjectSelectedEvent` | Resaltar ítem seleccionado |
| `SceneDirtyChangedEvent` | Actualizar contador de objetos |

---

## Fase 4 — Panel Inspector

**Referencia WinForms:** `MonoGame.Editor.WinForms/Panels/InspectorPanel.cs`

### 4.1 Estructura visual

```
┌─ InspectorView ───────────────────────────────────┐
│ Tabs: [Inspector] [Material Editor] [UI Theme]     │
│───────────────────────────────────────────────────│
│ ☑  GameObject                    d2266e63 [+ tag]  │
│───────────────────────────────────────────────────│
│ ▼ Transform                                        │
│   Position  [X: 0.00] [Y: 0.00]                   │
│   Rotation  [Z: 0.00]                             │
│   Scale     [X: 1.00] [Y: 1.00]                   │
│   Depth     [Z: 0.00]                             │
│───────────────────────────────────────────────────│
│ ▼ AudioZone    ●On  [×]                            │
│   Radius        [______]                           │
│   Fade In Time  [______]                           │
│   Fade Out Time [______]                           │
│───────────────────────────────────────────────────│
│ ▶ ParticleEmitterBehaviour    ●On  [×]             │
│───────────────────────────────────────────────────│
│ [+ Add Behaviour…]                                 │
└────────────────────────────────────────────────────┘
```

### 4.2 Control `NumericSpinnerView`

Control reutilizable para campos numéricos con etiqueta de eje:

```xml
<!-- Controls/NumericSpinnerView.xaml -->
<Grid ColumnDefinitions="18,*,30">
    <!-- Etiqueta X/Y/Z con color de eje -->
    <Border Grid.Column="0" BackgroundColor="{StaticResource AxisXColor}">
        <Label Text="X" TextColor="White"/>
    </Border>
    <!-- Campo numérico -->
    <Entry Grid.Column="1" Keyboard="Numeric" BackgroundColor="{StaticResource BgInput}"/>
    <!-- Botones ▲▼ -->
    <Grid Grid.Column="2" RowDefinitions="*,*">
        <Button Grid.Row="0" Text="▲" Clicked="OnIncrement"/>
        <Button Grid.Row="1" Text="▼" Clicked="OnDecrement"/>
    </Grid>
</Grid>
```

Propiedades: `AxisLabel`, `AxisColor`, `Value` (`float`), `Step`, `Format`.

### 4.3 Generación dinámica de tarjetas de behaviour

Replicar la reflexión de `InspectorPanel.cs` (WinForms):
- Por cada `EditorBehaviour` en `SelectedObject.Behaviours`, crear una `BehaviourCardView`.
- Cada `BehaviourCardView` lee los `[EditorProperty]` del tipo mediante reflexión y genera `NumericSpinnerView`, `Entry`, `Switch`, `Picker` según el tipo de dato.
- Cambios en los campos publican el `SetPropertyCommand` correspondiente.

### 4.4 Eventos suscritos

| Evento | Acción |
|--------|--------|
| `GameObjectSelectedEvent` | Mostrar/actualizar todo el inspector |
| `BehaviourAddedEvent` | Añadir nueva tarjeta al final |
| `BehaviourRemovedEvent` | Eliminar tarjeta correspondiente |
| `PropertyChangedEvent` | Actualizar solo el campo afectado |

---

## Fase 5 — Dock Assets

**Referencia WinForms:** `MonoGame.Editor.WinForms/Panels/AssetBrowserPanel.cs`

### 5.1 Estructura visual

```
┌─ AssetBrowserView ─────────────────────────────────────────┐
│ [Folder tree 220px] │ Content > New Folder  [🔍]  [≡]      │
│                     │ ─────────────────────────────────────│
│ 📁 Content          │ Name              │ Type    │ Size    │
│   📁 New Folder     │ 🟥 NewMaterial.mat │ Material│ 120 B  │
│                     │                                       │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 Árbol de carpetas

`CollectionView` con items anidados similar a Hierarchy (misma técnica de indentación). Iconos de carpeta en ámbar (`--accent-amber`).

### 5.3 Lista de assets

`CollectionView` con columnas: Name · Type · Size. Clic en una fila selecciona el asset. Doble clic abre el asset con el editor especializado según `AssetType`. Iconos por tipo de asset (PNG 16×16).

### 5.4 Breadcrumb

`HorizontalStackLayout` con botones de segmento separados por `>`. Cada segmento navega a esa carpeta.

### 5.5 Menú contextual de asset

Open in Explorer / Rename / Delete / Copy Path / Import to Content.

### 5.6 Eventos suscritos

| Evento | Acción |
|--------|--------|
| `ProjectOpenedEvent` | Poblar árbol de carpetas |
| `AssetImportedEvent` | Refrescar lista actual |
| `ContentWatcherEvent` | Refrescar lista (cambio en disco) |

---

## Fase 6 — Dock Console y Dock Scenes

### 6.1 Console (`ConsolePanelView`)

**Referencia WinForms:** `MonoGame.Editor.WinForms/Panels/ConsolePanel.cs`

- `CollectionView` de `LogEntry` con template de celda coloreada por `LogLevel`:
  - Debug → `--text-dim`
  - Info → `--text-primary`
  - Warning → `--accent-amber`
  - Error → `--accent-red`
- Filtros (checkboxes en toolbar de panel): Debug / Info / Warning / Error.
- Botón "Clear".
- Auto-scroll al último ítem cuando llega nuevo log.
- Detección de patrones MSBuild (error/warning codes) para colorear build output.
- Suscrito a `IEditorLogger` (Observable pattern o evento `LogEntryAddedEvent`).

### 6.2 Scene Manager (`SceneManagerView`)

**Referencia WinForms:** `MonoGame.Editor.WinForms/Panels/SceneManagerPanel.cs`

- `CollectionView` de escenas (nombre + timestamp de modificación + indicador `*` de dirty).
- Doble clic en escena → `LoadSceneCommand`.
- Botón "New Scene" → abrir `NewSceneDialog`.
- Menú contextual: Open / Rename / Delete.
- Suscrito a `SceneLoadedEvent`, `SceneDirtyChangedEvent`.

---

## Fase 7 — Dock paneles restantes

**Referencia WinForms:** carpeta `MonoGame.Editor.WinForms/Panels/`

### 7.1 Localization Browser (`LocalizationBrowserView`)

- Grid editable de clave → valores por locale (columnas dinámicas).
- Botones: Add Key / Delete Key / Add Locale / Import / Export.
- Suscrito a `ProjectOpenedEvent`.

### 7.2 Input Map Editor (`InputMapEditorView`)

- Panel izquierdo: árbol de acciones de input.
- Panel derecho: lista de bindings por dispositivo (Keyboard / Gamepad).
- Botones: Add Action / Remove Action / Add Binding / Remove Binding.

### 7.3 Tilemap Palette (`TilemapPaletteView`)

- Vista previa del tileset.
- Selector de tile con brush.
- Herramientas: Paint / Erase / Flood Fill.
- Tamaño de brush (1×1, 2×2, 3×3).
- Suscrito a `TilesetLoadedEvent`.

### 7.4 Script Browser (`ScriptBrowserView`)

- Árbol agrupado por namespace de tipos `GameBehaviour` disponibles.
- Doble clic → añadir behaviour al objeto seleccionado.
- Botón "Rescan Assemblies".

### 7.5 Sprite Inspector (`SpriteInspectorView`)

- Vista previa del sprite (región recortada).
- Campos: Region (X/Y/W/H), Pivot (X/Y), Frame count, Frame duration.
- Solo activo cuando el asset seleccionado en Assets es de tipo Sprite.

### 7.6 Material Inspector (`MaterialInspectorView`)

- Selector de shader.
- Propiedades dinámicas según el efecto.
- Preview del material (renderizado en tiempo real con `MaterialPreviewRenderer`).

### 7.7 UI Theme Inspector (`UIThemeInspectorView`)

- Propiedades de `EditorUITheme`: colores, fuentes, márgenes.
- Suscrito a `UIThemeSelectedEvent`.

### 7.8 Undo History (`UndoHistoryView`)

- `CollectionView` de `IEditorCommand` del `CommandStack`.
- Click en ítem → saltar a ese estado de undo.
- Ítem actual marcado con acento azul.
- Suscrito a `UndoPerformedEvent`, `RedoPerformedEvent`.

---

## Fase 8 — Diálogos

**Referencia WinForms:** carpeta `MonoGame.Editor.WinForms/Dialogs/`

En MAUI los diálogos son `ContentPage` presentadas como `Shell.Current.GoToAsync` modal, o `Page.ShowPopupAsync` con la librería CommunityToolkit.Maui.

| Diálogo MAUI | Equivalente WinForms | Contenido |
|--------------|----------------------|-----------|
| `NewProjectDialog` | `NewProjectDialog` | Nombre, ruta .csproj del juego |
| `NewSceneDialog` | `NewSceneDialog` | Nombre, tamaño del mundo |
| `ProjectSettingsDialog` | `ProjectSettingsDialog` | Tabs: General / Content / Localization / CodeGen |
| `AddBehaviourDialog` | `AddBehaviourDialog` | Lista buscable de tipos de Behaviour |
| `NewBehaviourDialog` | `NewBehaviourDialog` | Nombre, namespace, clase base |
| `ScriptCreationDialog` | `ScriptCreationDialog` | Nombre y tipo de script |
| `CodeGenProgressDialog` | `CodeGenProgressDialog` | Progress bar + log de salida |
| `LocaleCreationDialog` | `LocaleCreationDialog` | Código de idioma + nombre |
| `WorldConfigDialog` | `WorldConfigDialog` | Bounds físicos del mundo, gravedad |
| `RgbaColorPickerDialog` | `RgbaColorPickerDialog` | Sliders RGBA + preview |

**Nota sobre `ProjectSettingsDialog`**: las tabs de MAUI usan `TabbedPage` o un `TabBar` con `ContentView` intercambiables. Evitar binding complejo: asignar valores en `OnAppearing`, leer en "Aceptar".

---

## Fase 9 — Menú y atajos de teclado

**Referencia WinForms:** `MonoGame.Editor.WinForms/EditorForm.cs` (MenuStrip + ToolStrip)

### 9.1 MenuBar

MAUI 10 soporta `MenuBar` nativo en escritorio. Cada `MenuBarItem` publica el comando Core correspondiente.

```
File
  ├── New Project…         Ctrl+Shift+N
  ├── Open Project…        Ctrl+Shift+O
  ├── Save Scene           Ctrl+S
  ├── Save Scene As…       Ctrl+Shift+S
  ├── Recent Projects      ►
  └── Exit                 Alt+F4

Edit
  ├── Undo                 Ctrl+Z
  ├── Redo                 Ctrl+Y
  ├── Cut                  Ctrl+X
  ├── Copy                 Ctrl+C
  ├── Paste                Ctrl+V
  ├── Duplicate            Ctrl+D
  ├── Delete               Del
  └── Select All           Ctrl+A

Project
  ├── Project Settings…
  ├── Build Content        Ctrl+B
  ├── Run                  F5
  ├── Run (Debug)          F5
  ├── Rescan Behaviours
  └── Generate Code

Debug
  ├── Play                 F5
  ├── Pause                F6
  └── Stop                 Shift+F5
```

### 9.2 Toolbar

Botones de herramientas (Select/Move/Rotate/Scale/Rect/Pan) como `ImageButton` con estado activo.  
Píldoras de toggle (2D / SNAP / NAV / RES) como `Button` con `BackgroundColor` condicional.  
Transport: `Play` (verde) y `Stop` (rojo).

### 9.3 Status Bar

```
[⚠ Build failed (exit 1)] | 1 object in scene | [spacer] | x64 · Debug | 30 FPS
```

- Estado del build suscrito a `BuildFinishedEvent` (fondo rojo en error, verde en éxito).
- FPS actualizado cada segundo desde el thread de render.
- Suscrito a `SceneLoadedEvent` para el contador de objetos.

### 9.4 Atajos de teclado

Registrar mediante `Window.KeyboardAccelerators` o `Shell` shortcuts los mismos atajos que en WinForms (`Ctrl+Z`, `Ctrl+Y`, `Ctrl+S`, `Q/W/E/R/T/H`, `G` para toggle grid, `Delete`, `F2` para renombrar, `F5` para play, `Shift+F5` para stop).

---

## Fase 10 — Play / Stop / Pause

**Referencia WinForms:** `MonoGame.Editor.WinForms/EditorForm.cs` (botones Play/Stop), `MonoGame.Editor.Core/PlayMode/`

### 10.1 Transición Editing → Playing

1. Botón Play o `F5`.
2. `PlayModeManager.EnterPlay()` (Core) guarda snapshot de la escena en memoria.
3. `EditorContext.State = EditorState.Playing`.
4. El `MonoGameView` cambia de `EditModeRenderer` al game loop real.
5. El inspector queda en modo lectura.
6. Publicar `EditorStateChangedEvent`.

### 10.2 Pause / Resume

- Botón Pause (`F6`): `EditorContext.State = EditorState.Paused`. El `Update` del juego se congela, el render sigue activo. El inspector se vuelve editable en caliente.
- Resume: vuelve a `Playing`.

### 10.3 Stop

- Botón Stop (`Shift+F5`): `PlayModeManager.ExitPlay()` restaura snapshot, vuelve a `Editing`.

### 10.4 Visual de los controles de transporte

| Estado | Play | Pause | Stop |
|--------|------|-------|------|
| Editing | Verde activo | Gris deshabilitado | Gris deshabilitado |
| Playing | Verde (indica estado) | Gris activo | Rojo activo |
| Paused | Verde (resume) | Gris (indica estado) | Rojo activo |

---

## Fase 11 — Build pipeline

**Referencia WinForms:** `MonoGame.Editor.WinForms/EditorForm.cs` (Build menu), `MonoGame.Editor.Core/Assets/MgcbRunner.cs`

### 11.1 Build Content (MGCB)

- Botón "Build Content" (`Ctrl+B`) o menú Project → Build Content.
- Invoca `MgcbRunner.RunAsync()` del Core.
- Salida de línea por línea redirigida al panel Console (publicando `LogEntryAddedEvent`).
- Al terminar publica `BuildFinishedEvent` con `ExitCode`.
- StatusBar muestra resultado con color.

### 11.2 Dotnet Build

- Menú Project → Build Solution.
- Mismo patrón que MGCB pero con `dotnet build`.
- CodeGenProgressDialog para indicar progreso.

### 11.3 Launch Game

- Menú Project → Run (`F5`).
- Lanza el ejecutable del juego con `Process.Start`.
- Si el ejecutable no existe, pregunta si desea compilar primero.

### 11.4 Generate Code

- Menú Project → Generate Code.
- Invoca `SceneCodeGenerator` y `BehaviourSkeletonGenerator` del Core.
- Muestra progreso en `CodeGenProgressDialog`.
- Opción "Auto-generate on save" en ProjectSettings.

---

## Fase 12 — Paridad WinForms y QA

### 12.1 Checklist de paridad funcional

- [ ] Crear / abrir / guardar / renombrar / eliminar proyectos
- [ ] Crear / abrir / guardar / renombrar / eliminar escenas
- [ ] CRUD completo de GameObjects (crear, renombrar, eliminar, reparentar)
- [ ] Inspector: transform, behaviours, tags
- [ ] Undo / Redo con historial de 100 niveles
- [ ] Asset browser: navegación, importar, renombrar, eliminar
- [ ] Tilemap: pintar, borrar, flood fill
- [ ] Localization: CRUD de claves y locales
- [ ] Input Map: CRUD de acciones y bindings
- [ ] Prefabs: guardar, aplicar, revertir
- [ ] CodeGen: generar código desde escenas y behaviours
- [ ] Play / Pause / Stop con snapshot
- [ ] Build Content (MGCB)
- [ ] Launch game
- [ ] Todos los atajos de teclado
- [ ] Tema oscuro coherente en toda la UI
- [ ] Viewport: pan, zoom, grid, snap, gizmos en los 4 modos

### 12.2 Diferencias intencionales vs WinForms

| Característica | WinForms | MAUI |
|----------------|----------|------|
| Plataforma | Solo Windows | Windows (y futuro macOS) |
| Renderer MonoGame | WindowsDX | WindowsDX (Windows) |
| Tema | SystemColors dark mode | Variables de color propias |
| Paneles flotantes | No (paneles fijos) | No (paneles fijos, igual) |
| Estilos visuales | Sistema operativo | Prototipo HTML custom |

### 12.3 Lo que NO cambia

- `MonoGame.Editor.Core` — cero modificaciones.
- `MonoGame.Editor.SourceGenerator` — cero modificaciones.
- `MonoGame.Editor.WinForms` — se mantiene en paralelo, no se elimina.
- Formato de `.scene.json` — compatible 100%.
- `EditorContext` y `IEditorEventBus` — misma API.
