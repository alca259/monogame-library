# Editor de escenas (`src/Editor/`)

App de escritorio .NET MAUI (DesktopGL/WinUI unpackaged, JIT — **no** se publica
AOT) para editar escenas del Kernel. Solución propia: `MonoGame.Editor.slnx`.
El editor **no tiene proyecto de tests**.

> Este fichero se carga automáticamente al trabajar con ficheros bajo `src/Editor/`.
> Las convenciones generales de C#/MonoGame siguen vigentes (ficheros de instrucciones
> de usuario). No duplicar reglas genéricas.

## Comandos

```bash
# Compilar solo el editor
dotnet build src/Editor/MonoGame.Editor.slnx
```

## Proyectos

- `MonoGame.Editor.Core` — estado, modelos y lógica (sin UI). Núcleo:
  `EditorContext` (singleton `EditorContext.Instance`: escena/selección/proyecto/
  foco activos), `IEditorEventBus`/`EditorEventBus` (**único canal entre paneles**),
  eventos en `Events/`, `CommandStack` (undo/redo), `GizmoController`, managers
  (`Project/`, `Assets/`, `Registry/`, `CodeGen/`, `Localization/`, `Input/`).
- `MonoGame.Editor.SourceGenerator` — genera `*_Scene.g.cs` desde `*.scene.json`
  (no afecta a la UI).
- `MonoGame.Editor.Maui` — front-end MVVM (CommunityToolkit.Mvvm).

## Flujo de trabajo (SDD)

Antes de codificar, leer la especificación en
`src/Editor/MonoGame.Editor.Maui/docs/specs/`. Diseñar primero las firmas públicas,
validar contra la API de MonoGame/MAUI y confirmar. No "vibe coding".

## Arquitectura MVVM (Maui) — dónde está cada cosa

- `ViewModels/` — `ViewModelBase` (base: `On<TEvent>()` suscribe al bus con
  marshalling a UI + auto-unsubscribe; `Attach()/Detach()`; `RegisterEvents()`;
  `IsFocused`/`FocusContext`). `EditorWindowViewModel` (toolbar, status bar,
  comandos de menú/transport). `ViewModels/Panels/*ViewModel`, `ViewModels/Dialogs/*ViewModel`.
- `Views/` — `EditorWindow.xaml(.cs)` (ventana principal), `Views/Panels/*`,
  `Views/Dialogs/*`. **Item models** (filas) junto a su vista: `HierarchyItem`,
  `AssetItem`, `FolderItem`, `SceneItem`, `ScriptItem`, `BehaviourTreeNode`.
- `Services/DialogService` (alertas/prompts/file·folder pickers, estático).
- `Converters/` — `BoolToColor` (`"#hexActivo|#hexInactivo"`), `Equals`, `InverseBool`.
  Registrados en `App.xaml`.
- `Controls/` — controles y behaviors (`AxisStepper`, `FocusOnClickBehavior`, …).
- `Resources/Styles/Colors.xaml` — tokens de tema; **se consumen como
  `{StaticResource Clave}` dentro de los `.xaml`, nunca desde `.cs`** (por eso una
  búsqueda en código no los encuentra). `ControlStyles.xaml` — estilos.

## Convenciones del editor (seguir al añadir/editar)

- **Acceso al estado:** vía `EditorContext.Instance` (singleton, sin contenedor DI).
- **Panel = VM:** cada panel tiene `{Panel}ViewModel : ViewModelBase`; el
  code-behind queda en `InitializeComponent(); BindingContext = _vm;` +
  `Attach()/Detach()` en `OnHandlerChanged`. Suscripciones al bus en
  `RegisterEvents()` con `On<TEvent>()`. Estado → `[ObservableProperty]`,
  acciones → `[RelayCommand]` (con `CanExecute` ligado a observables).
- **Diálogos:** `{Dialog}ViewModel : DialogViewModel<TResult>` (validación +
  evento `CloseRequested`); el code-behind conserva `static ShowAsync(INavigation)`
  + `TaskCompletionSource<T>` y cierra el modal al recibir `CloseRequested`.
- **Bindings compilados:** poner `x:DataType` de la VM en la raíz de cada vista.
  Para acceder a la VM desde plantillas con `BindingContext` propio (p. ej.
  `MenuFlyout` dentro de `DataTemplate`), exponer `public {VM} Vm => _vm;` en el
  code-behind y bindear con `{Binding Vm.XCommand, Source={x:Reference Root}}`.
- **Pragmatismo (lo que SÍ queda en code-behind):** interop nativa WinUI (teclado,
  rueda, arrastre de separadores, file/folder pickers), render/entrada del viewport
  y canvas (`GraphicsView`), y UI dinámica no plantillable (tarjetas de behaviour del
  Inspector, secciones de shader del Material, breadcrumb del AssetBrowser,
  `RgbaColorPickerDialog`). Esas piezas invocan comandos/propiedades de la VM.
- **Foco de panel / teclado por contexto:** clic en panel → `FocusOnClickBehavior`
  llama `EditorContext.Instance.SetFocus(<contexto>)` (publica `FocusChangedEvent`).
  El viewport marca `Viewport` en `OnViewportTapped`; abrir menú marca `Global`.
  Los atajos se enrutan en `EditorWindow.OnNativeKeyDown` según `ActiveFocus`
  (tools Q/W/E/R/T/H… solo en `Viewport`; Alt+letra de menús solo en `Global`;
  Ctrl+S/Z/Y/B/F5 siempre). El panel activo se resalta con un `Border` overlay
  (`InputTransparent`, `StrokeThickness` constante) ligado a `IsFocused`.
- **Warnings:** `MVVMTK0045` está suprimido en el `.csproj` del Maui (campos con
  `[ObservableProperty]`; solo afecta a publicación AOT/WinRT, que esta app no usa).
- **Codificación:** ficheros nuevos en UTF-8 BOM (regla de usuario).
