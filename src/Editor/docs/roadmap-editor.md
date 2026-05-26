# MonoGame Editor — Roadmap técnico (v3)

**Stack**: .NET 10 · C# 14 · WinForms · MonoGame · MonoGame.Extended · MonoGame.Framework.WindowsDX · Alca.MonoGame.Kernel · System.Text.Json  
**Objetivo**: Editor de juegos 2D/3D estilo Unity/Valve Hammer Editor, integrado con Visual Studio, con viewport MonoGame embebido, jerarquía ECS, gizmos de transform, editores especializados y pipeline de build. Toda acción en el editor repercute en los ficheros `.cs` del proyecto de juego seleccionado siguiendo los patrones de `Alca.MonoGame.Kernel`.  
**Reglas transversales a todos los desarrollos:**
- Al terminar, actualizar este fichero marcando los TODOs completados.
- Dentro de la carpeta src/murder-main-reference/Readme.md hay un editor de referencia basado en Monogame FNA que puede usarse como base.

---

## Estilo visual e interfaz

Modo oscuro nativo de WinForms (.NET 10). No hay modo claro ni selector de tema.

- `Application.SetColorMode(SystemColorMode.Dark)` en `Program.cs` antes de lanzar el formulario
- Controles estándar heredan colores del sistema (`SystemColors`) sin configuración adicional
- Controles personalizados usan `SystemColors` para coherencia con el tema
- Control de cualquier excepción no controlada en Program.cs con Log de Serilog a disco y un MessageBox.
- Tipografía: `Segoe UI` 9pt en todos los controles
- Iconos: Segoe Fluent Icons o PNG 16×16 / 24×24 con fondo transparente
- Superficies planas, sin gradientes ni sombras
- Botones Play/Pause/Stop con color de acento cuando están activos

---

## Estructura de la solución

```
MonoGame.Editor.sln
├── Alca.MonoGame.Kernel          # Librería existente (referencia, nunca modificar)
├── MonoGame.Editor.Core          # Lógica del editor, sin UI — referencia Kernel + System.Text.Json
└── MonoGame.Editor.WinForms      # Aplicación WinForms — referencia Editor.Core
```

- El nuget de Alca.MonoGame.Kernel está una carpeta local "F:\Dev\NugetLocal\Alca.MonoGame.Kernel.1.0.0.nupkg" en principio está registrada como fuente de nugets bajo el nombre "DevLocal"

**Reglas de arquitectura:**
- `MonoGame.Editor.Core` no contiene ninguna referencia a `System.Windows.Forms`
- `MonoGame.Editor.WinForms` solo referencia `MonoGame.Editor.Core`
- La comunicación entre paneles es exclusivamente a través de `IEditorEventBus` — los paneles nunca se llaman directamente entre sí
- `EditorContext` es la fuente de verdad única del estado del editor en tiempo de ejecución
- Los `.json` de escena son la fuente de verdad para edición, legibles y versionables con git
- Toda la aplicación del editor debe ser async/await en la medida de lo posible.
- En este proyecto de Editor, **NO SE DEBE HACER NINGÚN TEST UNITARIO**.

---

## Máquina de estados del editor

```
┌────────────┐  Play  ┌────────────┐  Pause  ┌────────────┐
│            │───────►│            │────────►│            │
│  Editing   │        │  Playing   │         │  Paused    │
│            │◄───────│            │◄────────│            │
└────────────┘  Stop  └────────────┘ Resume  └────────────┘
      ▲                                              │
      └──────────────────── Stop ───────────────────┘
```

- **Editing**: cámara independiente del juego, gizmos visibles, game loop parado (render del editor activo)
- **Playing**: snapshot de escena guardado en memoria, game loop real activo, cámara del juego
- **Paused**: render activo, `Update` no ejecuta lógica de juego, inspector editable en caliente
- **Stop**: restaura snapshot, vuelve a `Editing`

---

## Fase 0 — Fundamentos y contratos base ✅ COMPLETADA

Clases clave: `EditorContext`, `IEditorEventBus`, `EditorEventBus`, `EditorPreferences`, `EditorProject`, `EditorState`, eventos tipados (`GameObjectSelectedEvent`, `SceneLoadedEvent`, `ProjectOpenedEvent`, `AssetImportedEvent`, `BehaviourAddedEvent`, `UndoPerformedEvent`, `RedoPerformedEvent`, `EditorStateChangedEvent`).

**`EditorProject`** — representa el proyecto de juego abierto
```csharp
sealed class EditorProject
{
    string Name { get; }
    string RootPath { get; }         // raíz del proyecto (donde vive el .sln/.slnx)
    string EditorPath { get; }       // {RootPath}/Editor/   ← todos los ficheros del editor
    string ScenesPath { get; }       // {EditorPath}/Scenes/
    string PrefabsPath { get; }      // {EditorPath}/Prefabs/
    string ContentPath { get; }      // configurable; default {RootPath}/Content
    string LocalizationPath { get; } // configurable; default {RootPath}/Localization
}
```

---

## Fase 1 — WinForms shell con viewport MonoGame embebido ✅ COMPLETADA

Clases clave: `EditorForm` (MenuStrip + ToolStrip + StatusStrip + SplitContainers), `MonoGameControl` (SwapChainRenderTarget, hilo separado), `EditorCamera2D` (Pan/Zoom con ratón), reutilización de `Camera2D` del Kernel.

---

## Fase 2 — Sistema Undo/Redo ✅ COMPLETADA

Clases clave: `IEditorCommand`, `CommandStack`, 18 comandos concretos (Create/Delete/Rename/Reparent/Move/Rotate/Scale/SetProperty/AddBehaviour/RemoveBehaviour/PaintTile/EraseTile/ApplyPrefab/RevertPrefab). Shortcuts Ctrl+Z / Ctrl+Y activos.

---

## Fase 3 — Jerarquía de escena e Inspector ✅ COMPLETADA

Clases clave: `SceneHierarchyPanel` (TreeView + drag-drop + menú contextual), `InspectorPanel` (Transform section + secciones por behaviour via reflexión + `EditorPropertyAttribute`), `AddBehaviourDialog`, `SceneSerializer`, `GameObjectRegistry`.

Modelos de datos: `EditorScene`, `EditorGameObject`, `EditorBehaviour`, `EditorVector2`. Clases del Kernel reutilizadas: `GameWorld`, `GameEntity`, `GameBehaviour`, `TransformBehaviour`, `GameEntityPool`.

---

## Fase 4 — Gizmos de transform y grid/snap ✅ COMPLETADA

Clases clave: `GizmoController`, `GizmoRenderer`, `GizmoDragAxis`, `GizmoMode` (Select/Move/Rotate/Scale). Shortcuts Q/W/E/R. Grid overlay toggle G con snap a Ctrl. Clases del Kernel reutilizadas: `Camera2D`, `DrawHelper`, `PrimitiveBatch`, `GeometryUtility`, `ResolutionManager`.

---

## Fase 5 — Asset Browser e integración Content Pipeline ✅ COMPLETADA

Clases clave: `AssetBrowserPanel` (SplitContainer folder tree + ListView + preview), `AssetClassifier`, `AssetInfo`, `AssetType`, `ContentWatcher` (FileSystemWatcher), `MgcbRunner`. Clases del Kernel reutilizadas: `AsyncContentLoader`, `ContentLoadGroup`, `Sprite`.

---

## Fase 6 — Editor de Tilemaps ✅ COMPLETADA

Clases clave: `TilemapPalettePanel`, `EditorTilemapAsset`, `EditorTileLayer`, `EditorTileset`, `TilemapImporter`, `PaintTileCommand`, `EraseTileCommand`, `TilemapLayerSelectedEvent`. Clases del Kernel reutilizadas: `TiledMapRenderer`, `TiledObjectLayer`.

---

## Fase 7 — Sistema de Prefabs ✅ COMPLETADA

Clases clave: `PrefabManager`, `PrefabSerializer`, `ApplyPrefabCommand`, `RevertPrefabCommand`. Prefabs en jerarquía marcados con icono azul. Inspector muestra botones Apply/Revert.

---

## Fase 0.5 — Proyecto con referencias al juego ✅ COMPLETADA

### Objetivo

Conectar el editor al proyecto de juego real. Sin `GameCsprojPath`, el editor no puede saber dónde está el código fuente del juego para ninguna fase de CodeGen futura. Esta es la base de toda la integración de código.

### Proyecto: MonoGame.Editor.Core

**`Project/EditorProject.cs`** — nuevas propiedades:

```csharp
/// <summary>Absolute path to the main game .csproj file. Empty string if not configured.</summary>
public string GameCsprojPath { get; }

/// <summary>Absolute path to the game source folder (directory containing GameCsprojPath).
/// Empty string if GameCsprojPath is not set.</summary>
public string GameSourcePath { get; }
```

Constructor actualizado:
```csharp
public EditorProject(
    string name,
    string rootPath,
    string gameCsprojPath = "",
    string contentRelativePath = "Content",
    string localizationRelativePath = "Localization")
```

Lógica interna: `GameSourcePath = string.IsNullOrWhiteSpace(gameCsprojPath) ? string.Empty : Path.GetDirectoryName(gameCsprojPath) ?? string.Empty`. Las rutas `ContentPath` y `LocalizationPath` se resuelven relativas a `GameSourcePath` si no está vacío, de lo contrario relativas a `RootPath`.

**`Project/ProjectManager.cs`** — cambios:

Clase interna `ProjectFileData` extendida:
```csharp
[JsonPropertyName("gameCsprojPath")]
public string GameCsprojPath { get; set; } = string.Empty;
```

Formato `project.json` resultante:
```json
{
  "name": "MyGame",
  "version": "1.0",
  "gameCsprojPath": "src/MyGame/MyGame.csproj",
  "contentPath": "src/MyGame/Content",
  "localizationPath": "src/MyGame/Localization"
}
```

Métodos actualizados:
- `Create(string name, string parentPath, string gameCsprojPath = "", string contentRelativePath = "Content", string localizationRelativePath = "Localization")` — acepta la ruta al .csproj opcional
- `Load(string projectPath)` — lee `gameCsprojPath` del JSON, lo pasa al constructor
- `Initialize(string projectPath)` — nueva sobrecarga que acepta `gameCsprojPath` opcional; puede auto-detectarlo
- `WriteProjectFile(EditorProject project)` — escribe `gameCsprojPath` relativo a `RootPath`

Nuevo método:
```csharp
/// <summary>
/// Scans rootPath up to 3 levels deep for the first .csproj containing a MonoGame
/// PackageReference. Returns null if none found.
/// </summary>
public static string? FindGameCsproj(string rootPath)
```

**`Events/GameCsprojChangedEvent.cs`** (nuevo):
```csharp
public sealed record GameCsprojChangedEvent(EditorProject Project) : IEditorEvent;
```

### Proyecto: MonoGame.Editor.WinForms

**`Dialogs/NewProjectDialog.Designer.cs`** — ampliar de 3 a 6 filas, `ClientSize` de 480×130 a 480×240:

Controles nuevos (siguiendo la restricción WinForms Designer — **nunca usar `[...]` collection expressions**):

```csharp
// Row 3: Game .csproj
private System.Windows.Forms.Label _csprojLabel;
private System.Windows.Forms.TableLayoutPanel _csprojRow;
private System.Windows.Forms.TextBox _csprojTextBox;        // ReadOnly = true
private System.Windows.Forms.Button _browseCsprojButton;

// Row 4: Content folder
private System.Windows.Forms.Label _contentLabel;
private System.Windows.Forms.TableLayoutPanel _contentRow;
private System.Windows.Forms.TextBox _contentTextBox;       // ReadOnly = true
private System.Windows.Forms.Button _browseContentButton;

// Row 5: Localization folder
private System.Windows.Forms.Label _localizationLabel;
private System.Windows.Forms.TableLayoutPanel _localizationRow;
private System.Windows.Forms.TextBox _localizationTextBox;  // ReadOnly = true
private System.Windows.Forms.Button _browseLocalizationButton;
```

`_gridPanel` ampliado a 6 filas de `SizeType.Absolute, 28F`. Altura total: 168px. Altura del Form: 240px.

Un separador visual (`Label` de 1px altura, `BackColor = SystemColors.ControlDark`, ColumnSpan=2) entre fila 2 (Full path) y fila 3 (Game .csproj).

Etiquetas: "Game .csproj:" / "Content folder:" / "Localization folder:" — `ContentAlignment.MiddleLeft`, misma anchura de columna 0 (110px).

**`Dialogs/NewProjectDialog.cs`** — nuevas propiedades y handlers:

```csharp
/// <summary>Absolute path to the game .csproj file. Empty string if not chosen.</summary>
public string GameCsprojPath => _csprojTextBox.Text.Trim();

/// <summary>Absolute path to the game Content folder. Empty string if not set.</summary>
public string ContentPath => _contentTextBox.Text.Trim();

/// <summary>Absolute path to the game Localization folder. Empty string if not set.</summary>
public string LocalizationPath => _localizationTextBox.Text.Trim();
```

Handler `OnBrowseCsprojClick`:
```csharp
private void OnBrowseCsprojClick(object? sender, EventArgs e)
{
    using OpenFileDialog dlg = new()
    {
        Title  = "Select the main game .csproj",
        Filter = "MonoGame Project (*.csproj)|*.csproj",
        InitialDirectory = Directory.Exists(ParentPath) ? ParentPath
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    };

    if (dlg.ShowDialog(this) != DialogResult.OK) return;

    _csprojTextBox.Text = dlg.FileName;
    AutoFillGamePaths(Path.GetDirectoryName(dlg.FileName)!);
}
```

Método `AutoFillGamePaths(string csprojDir)`:
- `_contentTextBox.Text = Path.Combine(csprojDir, "Content")`
- `_localizationTextBox.Text = Path.Combine(csprojDir, "Localization")`
- Ambos se rellenan como sugerencia aunque la carpeta no exista aún

Handlers `OnBrowseContentClick` / `OnBrowseLocalizationClick`: abren `FolderBrowserDialog`.

`UpdatePreviewAndOk()` actualizado:
- OK se habilita con `ProjectName` válido y `ParentPath` existente
- `GameCsprojPath` es **opcional** — si vacío, OK sigue habilitado
- Si `GameCsprojPath` relleno pero el archivo no existe: `_previewValueLabel.ForeColor = Color.OrangeRed` + texto de advertencia

**`EditorForm.cs`** — handler `OnFileNewProjectClick` actualizado:

```csharp
private async void OnFileNewProjectClick(object? sender, EventArgs e)
{
    using NewProjectDialog dlg = new();
    if (dlg.ShowDialog(this) != DialogResult.OK) return;

    try
    {
        EditorProject project = await Task.Run(() =>
            ProjectManager.Create(
                dlg.ProjectName,
                dlg.ParentPath,
                dlg.GameCsprojPath,
                string.IsNullOrWhiteSpace(dlg.ContentPath)
                    ? "Content"
                    : Path.GetRelativePath(Path.Combine(dlg.ParentPath, dlg.ProjectName), dlg.ContentPath),
                string.IsNullOrWhiteSpace(dlg.LocalizationPath)
                    ? "Localization"
                    : Path.GetRelativePath(Path.Combine(dlg.ParentPath, dlg.ProjectName), dlg.LocalizationPath)))
            .ConfigureAwait(true);

        _context.SetActiveProject(project);
        _context.EventBus.Publish(new ProjectOpenedEvent(project));
    }
    catch (Exception ex)
    {
        MessageBox.Show(this, ex.Message, "Error creating project", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
```

---

## Fase 8 — Calidad UI: Paneles Existentes Mejorados ✅ COMPLETADA

### Objetivo

Llevar cada panel del estado "mínimo funcional" a un nivel comparable con herramientas profesionales. Los paneles actuales tienen UX deficiente: la consola no filtra, el inspector no tiene cabecera de entidad, el browser no tiene búsqueda ni menú contextual.

### 8a — ConsolePanel mejorado

**Proyecto: MonoGame.Editor.Core**

`Core/Logging/LogLevel.cs`:
```csharp
public enum LogLevel { Debug, Info, Warning, Error }
```

`Core/Logging/LogEntry.cs`:
```csharp
public readonly record struct LogEntry(DateTime Timestamp, LogLevel Level, string Message);
```

`Core/Logging/IEditorLogger.cs`:
```csharp
public interface IEditorLogger
{
    void Log(string message, LogLevel level = LogLevel.Info);
    void LogWarning(string message);
    void LogError(string message);
    void LogDebug(string message);
    void Clear();
}
```

`Events/LogEntryAddedEvent.cs`:
```csharp
public sealed record LogEntryAddedEvent(LogEntry Entry) : IEditorEvent;
```

`EditorContext` expone `IEditorLogger Logger { get; }` — implementación interna publica entradas via `IEditorEventBus`.

**Proyecto: MonoGame.Editor.WinForms**

`Panels/ConsolePanel.cs` — diseño WinForms:

| Control | Configuración |
|---------|---------------|
| `ToolStrip` (Dock=Top, Height=28) | Barra superior del panel |
| `ToolStripButton` "Clear" | Limpia la salida |
| `ToolStripButton` "Copy" | Copia selección al clipboard |
| `ToolStripSeparator` | División visual |
| `ToolStripDropDownButton` "Filter ▼" | Items: All / Debug / Info / Warning / Error |
| `RichTextBox` (Dock=Fill, ReadOnly=true) | `BackColor=#1E1E1E`, `Font=Consolas 9pt` |

Color-coding por `LogLevel`:
- `Debug` → `Color.DimGray`
- `Info` → `SystemColors.ControlText`
- `Warning` → `Color.Goldenrod`
- `Error` → `Color.IndianRed` + Bold

Formato de línea: `[HH:mm:ss] [LEVEL] message`

API pública:
```csharp
public void AppendLine(string message, LogLevel level = LogLevel.Info);
public void AppendBuildLine(string line);  // detecta patrones MSBuild para colorear
public void Clear();
```

`AppendBuildLine` detecta:
- `"error CS"` → `LogLevel.Error`
- `"warning CS"` → `LogLevel.Warning`
- `"Build succeeded"` → `LogLevel.Info` + `Color.LightGreen`
- `"Build FAILED"` → `LogLevel.Error`

---

### 8b — SceneHierarchyPanel mejorado

`ToolStrip` (Dock=Top, Height=25):

| Control | Configuración |
|---------|---------------|
| `ToolStripButton` "+" | Crea entidad raíz (`CreateEntityCommand`) |
| `ToolStripButton` "trash" | Elimina entidad seleccionada (con confirmación si tiene hijos) |
| `ToolStripSeparator` | — |
| `ToolStripTextBox` Search | Width=110, PlaceholderText="Search...", TextChanged filtra árbol |
| `ToolStripLabel` contador | Alineado derecha: "{n} entities" |

`TreeView` con `ImageList` (16×16):
- Índice 0: cubo gris (GameEntity genérico)
- Índice 1: cámara
- Índice 2: luz
- Índice 3: partículas
- Índice 4: tilemap

Filtrado incremental: `RebuildTree()` aplica `MatchesFilter(EditorGameObject, string filter)` que retorna `true` si el nombre contiene el filtro (OrdinalIgnoreCase) o cualquier descendiente lo hace. Nodos sin match se omiten.

`Label` de estado (Dock=Bottom, Height=18, Font=7.5pt, ForeColor=GrayText): "{n} objects in scene".

---

### 8c — InspectorPanel mejorado

**Proyecto: MonoGame.Editor.Core**

`EditorGameObject` añade:
```csharp
/// <summary>User-defined tags. Serialized with the scene.</summary>
public List<string> Tags { get; } = [];
```

Nuevo `SetTagsCommand` en `Commands/`.

**Proyecto: MonoGame.Editor.WinForms**

**Cabecera de entidad** (`Panel` Dock=Top, Height=56, BackColor=ControlDarkDark):

| Control | Configuración |
|---------|---------------|
| `CheckBox` Active | Dock=Left, Width=18, sin texto; vinculado a `EditorGameObject.Active` via `SetPropertyCommand` |
| `TextBox` EntityName | Dock=Fill, Font=Segoe UI 10pt Bold, BackColor=ControlDarkDark; Leave → `RenameEntityCommand` |
| `ComboBox` Tags | Dock=Right, Width=90, DropDownStyle=DropDown; Enter confirma añadir tag |
| `Label` Id | Dock=Bottom, Height=16, Font=7pt, ForeColor=GrayText; primeros 8 chars del GUID |

**Controles por tipo** en `CreateControlForProperty`:

| Tipo C# | Control WinForms |
|---------|-----------------|
| `float`, `int` | `NumericUpDown` |
| `bool` | `CheckBox` |
| `string` | `TextBox` |
| `Vector2` | Dos `NumericUpDown` X/Y en línea |
| `Vector3` | Tres `NumericUpDown` X/Y/Z en línea |
| `Color` | `Panel` (24px, `BackColor=color actual`) + `Button` "..." → `ColorDialog` |
| `enum` (no flags) | `ComboBox` |
| `enum` con `[Flags]` | `CheckedListBox` `CheckOnClick=true` |
| Asset reference (`string` con atributo asset) | `TextBox` ReadOnly + `Button` "..." → `OpenFileDialog` filtrado |

**Secciones colapsables**: cada cabecera de sección (`Panel` 28px) gana un `Label` chevron "▼"/"▶" a la izquierda. Click alterna visibilidad del body. Estado de colapso persistido en `EditorPreferences` en `Dictionary<string, bool> BehaviourSectionCollapsed`.

**Botón "Add Behaviour"** estilizado: `FlatStyle.Flat`, padding `(4,2)`, texto "+ Add Behaviour", centrado en `Panel` con `Padding(4)`.

---

### 8d — AssetBrowserPanel mejorado

**Barra superior** (`TableLayoutPanel` 1 fila 3 columnas, Dock=Top, Height=28):

| Col | Control | Configuración |
|-----|---------|---------------|
| 0 (AutoSize) | `ToolStrip` | Botones: Import / Refresh / New Folder |
| 1 (Fill) | `TextBox` filtro | PlaceholderText="Filter assets...", TextChanged con debounce 150ms |
| 2 (AutoSize) | `ToolStripButton` | Toggle List/LargeIcon |

**Breadcrumb** (`FlowLayoutPanel`, Dock=Top, Height=22): cada segmento de ruta = `LinkLabel` clickable; separadores "›" entre ellos. Se reconstruye al navegar.

**Menú contextual del `ListView`** (`ContextMenuStrip`):
- "Open with External Editor" — `Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true })`
- "Reveal in Explorer" — `Process.Start("explorer.exe", $"/select,\"{path}\"")`
- Separator
- "Rename" — inicia `LabelEdit`
- "Delete" — confirmación via `MessageBox` + publicar `AssetImportedEvent`
- Separator
- "Copy Relative Path" — clipboard

**Modo LargeIcon**: `_contentView.View = View.LargeIcon`, `LargeImageList` 64×64. Texturas: `Image.FromFile` + `GetThumbnailImage(64, 64, null, IntPtr.Zero)`.

Debounce de búsqueda: `System.Windows.Forms.Timer` de 150ms reiniciado en cada `TextChanged`; al dispararse llama a `ShowFolderContents(currentFolder)` con filtro activo.

---

## Fase 9 — Gestión de Escenas Mejorada ✅ COMPLETADA

### Objetivo

El editor gestiona el ciclo de vida completo de escenas: nueva, abrir, guardar, renombrar, eliminar, con feedback visual de escena modificada y panel de gestión dedicado.

### Proyecto: MonoGame.Editor.Core

`EditorContext` ampliado:

```csharp
public bool IsSceneDirty { get { lock (_stateLock) return _isSceneDirty; } }

public void MarkSceneDirty()   { lock (_stateLock) _isSceneDirty = true;  EventBus.Publish(new SceneDirtyChangedEvent(true)); }
public void MarkSceneClean()   { lock (_stateLock) _isSceneDirty = false; EventBus.Publish(new SceneDirtyChangedEvent(false)); }
```

`CommandStack.Execute()` llama `EditorContext.Instance.MarkSceneDirty()` tras cada comando si hay escena activa.

Nuevos eventos:
- `SceneDirtyChangedEvent(bool IsDirty)` — publicado cuando el estado dirty cambia
- `SceneCreatedEvent(EditorScene Scene)` — publicado al crear una escena nueva

`EditorScene` añade:
```csharp
/// <summary>Optional 2D world bounds (pixels). Zero = unbounded.</summary>
public EditorVector2 WorldSize { get; set; } = EditorVector2.Zero;
```

### Proyecto: MonoGame.Editor.WinForms

**`Dialogs/NewSceneDialog.cs`** (nuevo):

| Control | Configuración |
|---------|---------------|
| `TableLayoutPanel` 4 filas | — |
| "Scene Name:" + `TextBox _nameBox` | Fila 0 |
| "World Width:" + `NumericUpDown _widthBox` | Fila 1, Min=0, Max=100000, Value=0 |
| "World Height:" + `NumericUpDown _heightBox` | Fila 2 |
| "Preview:" + `Label _previewLabel` | Fila 3, ForeColor=GrayText |
| `FlowLayoutPanel` Cancel + OK | Dock=Bottom |

Propiedades: `string SceneName`, `float WorldWidth`, `float WorldHeight`.

**`Panels/SceneManagerPanel.cs`** (nuevo):

| Control | Configuración |
|---------|---------------|
| `ToolStrip` (Dock=Top) | New Scene / Open Scene / Delete Scene |
| `ListView _sceneList` (Dock=Fill) | View=Details, FullRowSelect=true; columnas: "Name" 180px / "Modified" 130px |
| `Label` (Dock=Bottom) | "{n} scenes in project" |

Comportamiento: al `ProjectOpenedEvent` → escanear `ScenesPath/*.scene.json` y poblar la lista. Doble clic → carga escena (pregunta si hay cambios sin guardar). Escena activa marcada con item en negrita.

**`EditorForm`** actualizado:
- `Text` del Form: `"MonoGame Editor — {project.Name} — {scene.Name}{isSceneDirty ? " *" : ""}"` (suscribir `SceneDirtyChangedEvent`)
- `FormClosing`: si `_context.IsSceneDirty` → `MessageBox` con Save/Discard/Cancel
- `File > New Scene` → `NewSceneDialog` → `SceneCreatedEvent`
- `File > Save Scene` (Ctrl+S) → guarda sin preguntar si `ScenePath` existe; llama `MarkSceneClean()` al terminar
- `File > Save Scene As` (Ctrl+Shift+S) → siempre `SaveFileDialog`

---

## Fase 10 — Editor de Localización ✅ COMPLETADA

### Objetivo

Editor visual para los archivos de localización del Kernel (`{LocalizationPath}/*.json`). Formato compatible con `Alca.MonoGame.Kernel.Localization.LocalizationManager`.

### Proyecto: MonoGame.Editor.Core

**`Localization/LocalizationEditorModel.cs`**:

```csharp
public sealed class LocalizationEditorModel
{
    public IReadOnlyList<string> Locales { get; }
    public IReadOnlyList<string> Keys { get; }

    public static Task<LocalizationEditorModel> LoadAsync(string localizationPath);
    public string GetValue(string locale, string key);
    public void SetValue(string locale, string key, string value);
    public Task SaveAsync();
    public void AddKey(string key);
    public void RemoveKey(string key);
    public void AddLocale(string locale);
}
```

**`Commands/SetLocalizationValueCommand.cs`**:
```csharp
public sealed class SetLocalizationValueCommand : IEditorCommand
{
    // Parameters: model, locale, key, oldValue, newValue
    public string Description => $"Set [{_locale}][{_key}]";
    // Execute: model.SetValue(locale, key, newValue)
    // Undo:    model.SetValue(locale, key, oldValue)
}
```

Nuevo evento: `LocalizationLoadedEvent(LocalizationEditorModel Model)`.

### Proyecto: MonoGame.Editor.WinForms

**`Panels/LocalizationBrowserPanel.cs`** (nuevo):

| Control | Configuración |
|---------|---------------|
| `ToolStrip` (Dock=Top) | Add Key / Remove Key / Add Locale / Import .json / Export .csv / Save |
| `TextBox _filterBox` (Dock=Top) | PlaceholderText="Filter keys..." |
| `DataGridView _grid` (Dock=Fill) | AllowUserToAddRows=false, AutoSizeColumnsMode=Fill |
| `Label` (Dock=Bottom) | "{n} keys, {m} locales" |

Configuración del grid:
- Primera columna: `DataGridViewTextBoxColumn` "Key", ReadOnly=**true**, Width=200, `DefaultCellStyle.BackColor=ControlLight`
- Columnas de locale: una `DataGridViewTextBoxColumn` editable por locale
- `CellEndEdit` → `SetLocalizationValueCommand` via `CommandStack`
- Filtrado: `row.Visible` alternado según filtro (sin rebuild)

Suscripción: `ProjectOpenedEvent` → `LoadAsync(project.LocalizationPath)` → poblar grid.

---

## Fase 11 — Editor de Mapas de Input ✅ COMPLETADA

### Objetivo

Panel visual para configurar los bindings de input. Reutiliza `InputActionMap`, `InputSerializer` del Kernel.

### Proyecto: MonoGame.Editor.Core

- `Input/InputEditorModel.cs` — wrapper de `InputActionMap` con operaciones orientadas al editor
- `Commands/AddInputActionCommand.cs`, `RemoveInputActionCommand.cs`, `AddInputBindingCommand.cs`, `RemoveInputBindingCommand.cs`
- `Events/InputMapLoadedEvent.cs`

### Proyecto: MonoGame.Editor.WinForms

**`Panels/InputMapEditorPanel.cs`** (nuevo) — `SplitContainer` vertical:

**Panel izquierdo (TreeView)**:

| Control | Configuración |
|---------|---------------|
| `ToolStrip` | Load File / Save File / Add Action / Remove Action |
| `ComboBox _mapFileSelector` (Dock=Top) | Archivos `.input.json` del proyecto |
| `TreeView _actionTree` (Dock=Fill) | FullRowSelect=true, HideSelection=false |

**Panel derecho (DataGridView)**:

| Control | Configuración |
|---------|---------------|
| `Label` cabecera | Nombre de la acción seleccionada, Font=Bold |
| `ToolStrip` | Add Binding / Remove Binding |
| `DataGridView _bindingsGrid` | — |

Columnas del grid:

| Columna | Tipo | Valores |
|---------|------|---------|
| "Device" | `DataGridViewComboBoxColumn` | Keyboard / Gamepad / Mouse |
| "Key / Button" | `DataGridViewComboBoxColumn` (dinámico) | `Keys` enum / `Buttons` enum / `MouseButtons` enum |

`CellValueChanged` en "Device" actualiza los items de la columna "Key / Button" de esa fila.

Suscripción: `ProjectOpenedEvent` → escanear `GameSourcePath` buscando `*.input.json` → poblar `_mapFileSelector`.

---

## Fase 12 — Project Settings y Build ✅ COMPLETADA

### Objetivo

Centralizar todas las configuraciones del proyecto en un diálogo profesional. Mejorar el pipeline de build con salida coloreada. Permitir lanzar el juego desde el editor.

### Proyecto: MonoGame.Editor.Core

**`Project/ProjectSettings.cs`**:

```csharp
public sealed class ProjectSettings
{
    public string RootNamespace { get; set; } = string.Empty;
    public string GeneratedCodeFolder { get; set; } = "Generated";
    public bool GenerateOnSave { get; set; } = false;
    public string DefaultLocale { get; set; } = "en-US";
    public List<string> SupportedLocales { get; set; } = ["en-US"];
    public string BuildConfiguration { get; set; } = "Debug";

    public static Task<ProjectSettings> LoadAsync(EditorProject project);
    public Task SaveAsync(EditorProject project);
}
```

Se serializa en `{EditorPath}/settings.json`.

`MgcbRunner` extendido con:
```csharp
public Task<int> RunDotnetBuildAsync(string csprojPath, string configuration, Action<string> onLine);
```

Nuevo evento: `BuildOutputLineEvent(string Line, bool IsError)`.

### Proyecto: MonoGame.Editor.WinForms

**`Dialogs/ProjectSettingsDialog.cs`** (nuevo) — `TabControl` con 4 pestañas:

**Pestaña "General"**:

| Control | Configuración |
|---------|---------------|
| "Project Name:" + `TextBox` | Editable |
| "Version:" + `TextBox` | Editable |
| "Game .csproj:" + browse row | `OpenFileDialog(*.csproj)` |
| "Editor folder:" + `TextBox` RO | Muestra `project.EditorPath` |
| "Root namespace:" + `TextBox` | Para CodeGen; guarda en `ProjectSettings` |

**Pestaña "Content"**:

| Control | Configuración |
|---------|---------------|
| "Content folder:" + browse row | `FolderBrowserDialog` |
| "MGCB file:" + browse row | Auto-detectado como `{ContentPath}/Content.mgcb` |
| "Build config:" + `ComboBox` | Debug / Release |
| `CheckBox` "Auto-build on Play" | — |

**Pestaña "Localization"**:

| Control | Configuración |
|---------|---------------|
| "Localization folder:" + browse row | — |
| "Default locale:" + `ComboBox` | Locales detectados + entrada manual |
| `DataGridView` "Supported locales" | Una columna "Locale", AllowUserToAddRows=true |

**Pestaña "Code Generation"**:

| Control | Configuración |
|---------|---------------|
| "Output folder:" + `TextBox` | Relativo a `GameSourcePath`; default "Generated" |
| `CheckBox` "Generate code on Scene Save" | Vinculado a `ProjectSettings.GenerateOnSave` |
| "Preview output path:" + `Label` | Ruta calculada dinámicamente, ForeColor=GrayText |
| `Button` "Generate All Scenes Now" | Deshabilitado hasta Phase 14 |

Acceso: menú `Project > Project Settings...`

**Menús nuevos en `EditorForm`**:
- `Project > Build Game` (Ctrl+B) → `MgcbRunner.RunDotnetBuildAsync` → `_consolePanel.AppendBuildLine`
- `Project > Run Game` (Ctrl+F5) → `Process.Start("dotnet", $"run --project \"{GameCsprojPath}\"")`

---

## Fase 13 — Infraestructura de CodeGen ✅ COMPLETADA

### Objetivo

Construir la capa de servicios que transforma el estado del editor en código C# siguiendo los patrones del Kernel. Esta fase sienta las bases; la integración completa ocurre en Fase 14.

### Proyecto: MonoGame.Editor.Core

**Nueva carpeta `CodeGen/`:**

**`CodeGen/ICodeGenService.cs`**:
```csharp
public interface ICodeGenService
{
    /// <summary>
    /// Generates or overwrites the partial class initializer for scene.
    /// Output: {GameSourcePath}/{GeneratedFolder}/Scenes/{SceneName}Scene.Generated.cs
    /// </summary>
    Task<CodeGenResult> GenerateSceneAsync(
        EditorScene scene,
        EditorProject project,
        ProjectSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scaffolds a new GameBehaviour subclass skeleton.
    /// </summary>
    Task<CodeGenResult> GenerateBehaviourSkeletonAsync(
        string className,
        string namespaceName,
        string relativeFolder,
        IReadOnlyList<string> lifecycleMethodsToOverride,
        EditorProject project,
        CancellationToken cancellationToken = default);
}
```

**`CodeGen/CodeGenResult.cs`**:
```csharp
public sealed record CodeGenResult(
    bool    Success,
    string  OutputPath,
    string? ErrorMessage = null);
```

**`CodeGen/SceneCodeGenerator.cs`** (implementa `ICodeGenService`):

- Genera `{SceneName}Scene.Generated.cs` con método `OnLoad(GameWorld world)` (ver Fase 14 para el patrón exacto)
- Usa `StringBuilder` (sin LINQ en el bucle de generación)
- Calcula hash MD5 del contenido antes de escribir: si coincide con el archivo existente, no sobreescribe (preserva timestamps)
- Si el archivo es nuevo: llama `CsprojFileEditor.EnsureFileIncludedAsync`
- Resuelve colisiones de nombres de variable añadiendo sufijo `_0`, `_1`, etc.
- Omite asignaciones de propiedades cuando el valor del editor coincide con el default del tipo

**`CodeGen/BehaviourSkeletonGenerator.cs`**:

Template generado:
```csharp
// Generated by MonoGame Editor
using Alca.MonoGame.Kernel.ECS;
using Microsoft.Xna.Framework;

namespace {namespaceName};

/// <summary>{className} behaviour.</summary>
public sealed class {className} : GameBehaviour
{
    // Lifecycle methods elegidos por el usuario...
}
```

**`CodeGen/CsprojFileEditor.cs`**:
```csharp
public static class CsprojFileEditor
{
    /// <summary>
    /// Verifica si el .csproj usa un Glob que cubre el archivo.
    /// Si no, añade un Compile Include explícito. Usa XmlDocument (sin Roslyn).
    /// La mayoría de proyectos SDK-style usan wildcard implícito — verificar antes de editar.
    /// </summary>
    public static Task EnsureFileIncludedAsync(string csprojPath, string absoluteFilePath);

    public static bool IsFileCoveredByGlob(string csprojPath, string absoluteFilePath);
}
```

**`CodeGen/GameBehaviourScanner.cs`**:
```csharp
public sealed class GameBehaviourScanner
{
    /// <summary>Escanea una DLL compilada buscando subclases de GameBehaviour.</summary>
    public static Task<IReadOnlyDictionary<string, TypeDescriptor>> ScanAssemblyAsync(string assemblyPath);

    /// <summary>
    /// Parsea .cs de sourcePath buscando clases que heredan GameBehaviour
    /// (parseo de texto simple, sin Roslyn — cubre el 95% de casos).
    /// </summary>
    public static Task<IReadOnlyList<string>> ScanSourceAsync(string sourcePath);
}
```

`TypeDescriptor` (record): `FullName`, `ShortName`, `Namespace`, `SourceFilePath?`.

Nuevos eventos:
```csharp
public sealed record CodeGenStartedEvent(string SceneName) : IEditorEvent;
public sealed record CodeGenCompletedEvent(CodeGenResult Result) : IEditorEvent;
```

Nuevo comando: `GenerateSceneCodeCommand` (Execute: llama `ICodeGenService.GenerateSceneAsync`; Undo: restaura backup del archivo previo si existía).

### Proyecto: MonoGame.Editor.WinForms

**`Dialogs/NewBehaviourDialog.cs`** (nuevo):

| Control | Configuración |
|---------|---------------|
| "Class name:" + `TextBox _classNameBox` | Validación: solo `[A-Za-z][A-Za-z0-9_]*` |
| "Namespace:" + `ComboBox _namespaceBox` | Items: namespaces detectados + texto libre |
| "Subfolder:" + row TextBox+Browse | Relativo a `GameSourcePath` |
| "Override methods:" + `CheckedListBox _methodsList` | Awake / Start / Update / Draw / OnDestroy |
| Cancel + Create | OK habilitado cuando ClassName válido |

Accesible desde `InspectorPanel` ("Create New..." en AddBehaviourDialog) y desde menú `Project > New Behaviour...`.

**`AddBehaviourDialog` mejorado**:

Reemplazar `ListBox` plano por `TreeView` agrupado por namespace (nodo = namespace, hijos = nombre corto). `TextBox` de búsqueda que filtra el árbol. Botón inferior "Create New..." → abre `NewBehaviourDialog`.

---

## Fase 14 — CodeGen Completo: Integración Total ✅ COMPLETADA

### Objetivo

El editor genera y mantiene código C# funcional del juego. Cada escena guardada produce un archivo `.Generated.cs` que registra entidades y behaviours siguiendo los patrones de `Alca.MonoGame.Kernel`. El desarrollador añade su lógica en la parte no generada de la clase parcial.

### Patrón de código generado

Para una escena "Gameplay" con un "Player" (`PlayerMovementBehaviour` Speed=5.0) y un "HUD_Root" con hijo "HealthBar":

**`Generated/Scenes/GameplayScene.Generated.cs`:**
```csharp
// AUTO-GENERATED by MonoGame Editor 2026-05-25T10:30:00 — DO NOT EDIT MANUALLY
// Source: Editor/Scenes/Gameplay.scene.json
// Safe to commit; regenerated automatically on scene save.
using Alca.MonoGame.Kernel.ECS;
using Microsoft.Xna.Framework;
using MyGame.Behaviours;

namespace MyGame.Scenes;

public sealed partial class GameplayScene : Scene
{
    /// <summary>Creates and registers all entities defined in the editor scene.</summary>
    protected override void OnLoad(GameWorld world)
    {
        // ── Entity: Player ──────────────────────────────────────────────────────
        var player_0 = world.CreateEntity("Player", new Vector2(100f, 200f));
        var playerMovement_0 = player_0.AddComponent<PlayerMovementBehaviour>();
        playerMovement_0.Speed = 5f;

        // ── Entity: HUD_Root ─────────────────────────────────────────────────────
        var hudRoot_1 = world.CreateEntity("HUD_Root", Vector2.Zero);
        var healthBar_2 = world.CreateEntity("HealthBar", new Vector2(10f, 10f));
        healthBar_2.SetParent(hudRoot_1);
    }
}
```

**`Scenes/GameplayScene.cs`** (archivo manual, **nunca sobreescrito**):
```csharp
namespace MyGame.Scenes;

public sealed partial class GameplayScene : Scene
{
    public override void Initialize()
    {
        base.Initialize();
        // Lógica personalizada del desarrollador
    }
}
```

### Proyecto: MonoGame.Editor.Core

`SceneCodeGenerator` completado con:
- Soporte para jerarquías de n niveles vía `entity.SetParent(parentEntity)`
- Generación automática de `using` statements basada en namespaces de los tipos de behaviour
- Soporte de tipos: `int`, `float`, `bool`, `string`, `Vector2`, `Vector3`, `Color`, `enum`
- Rutas de asset emitidas como string literal: `SpriteName = "Textures/player"`
- Propiedades con valor default del tipo: omitir línea para mantener el código limpio

`ProjectSettings` usado para resolver:
- `RootNamespace` → namespace de la clase (`{RootNamespace}.Scenes`)
- `GeneratedCodeFolder` → subcarpeta dentro de `GameSourcePath`
- `GenerateOnSave` → flag de generación automática

`GameObjectRegistry.Scan()` complementado con `ScanFromAssemblyAsync(string dllPath)`: tras `Project > Build Game` exitoso, rescan automático. Los tipos pending (detectados en source pero no compilados aún) se muestran en `AddBehaviourDialog` con estilo gris itálico.

### Proyecto: MonoGame.Editor.WinForms

**`EditorForm.OnFileSaveProjectClick`** integración CodeGen:

```csharp
// Después de guardar exitosamente el .scene.json:
if (_settings.GenerateOnSave && !string.IsNullOrEmpty(_context.ActiveProject?.GameCsprojPath))
{
    _context.EventBus.Publish(new CodeGenStartedEvent(scene.Name));
    _statusLabel.Text = "Generating code...";

    CodeGenResult result = await _codeGenService
        .GenerateSceneAsync(scene, project, _settings)
        .ConfigureAwait(true);

    if (result.Success)
        _consolePanel.AppendLine($"[CodeGen] Generated: {result.OutputPath}", LogLevel.Info);
    else
        _consolePanel.AppendLine($"[CodeGen] Error: {result.ErrorMessage}", LogLevel.Error);

    _context.EventBus.Publish(new CodeGenCompletedEvent(result));
    _statusLabel.Text = result.Success ? "Saved + code generated." : "Saved (code gen failed).";
}
```

**Menús nuevos en `EditorForm`**:
- `Project > Generate Scene Code` (Ctrl+G) — ejecuta `GenerateSceneCodeCommand` para la escena activa
- `Project > Generate All Scenes` — itera sobre todas las `.scene.json` y genera cada una
- Separator
- `Project > Rescan Behaviours` — ejecuta `GameBehaviourScanner.ScanAssemblyAsync` y actualiza `GameObjectRegistry`

**`Dialogs/CodeGenProgressDialog.cs`** (nuevo — form no-modal, esquina inferior derecha):

| Control | Configuración |
|---------|---------------|
| `Label` "Generating code..." | — |
| `ProgressBar _bar` | Style=Marquee durante operación; Blocks al terminar |
| `ListView _fileList` | View=Details; columnas "File" 220px / "Status" 70px |
| `Button` "Close" | Habilitado solo al terminar |

**`AssetBrowserPanel`**: archivos `.Generated.cs` muestran icono turquesa + tooltip "Auto-generated by MonoGame Editor — do not edit manually".

---

## Fase 15 — Play / Pause / Stop ✅ COMPLETADA

### Objetivo

Implementar la máquina de estados Editing → Playing → Paused → Editing con snapshot/restore de escena y game loop activo en el viewport.

### Comportamiento por estado

| Estado | Update | Draw | Gizmos |
|--------|--------|------|--------|
| Editing | — | Editor overlay | ✅ |
| Playing | `GameWorld.Update()` | `GameWorld.Draw()` + SpriteBatch | ❌ |
| Paused | — | `GameWorld.Draw()` + SpriteBatch | ✅ |
| Stop → Editing | — | — | ✅ (snapshot restaurado) |

### Proyecto: MonoGame.Editor.Core

**`PlayMode/PlayModeRunner.cs`** (nuevo):
- `EnsureInitialized(GraphicsDevice)` — crea `SpriteBatch` en el render thread
- `Update(TimeSpan elapsed)` — avanza `GameWorld.Update()` con delta time acumulado
- `Draw(TimeSpan elapsed)` — llama `SpriteBatch.Begin/GameWorld.Draw/End` con try-catch (behaviours sin content fallan silenciosamente)
- `IDisposable` — descarta SpriteBatch

**`PlayMode/SceneToWorldConverter.cs`** (nuevo):
- `Convert(EditorScene, GameObjectRegistry)` → `GameWorld`
- Conversión recursiva de jerarquía `EditorGameObject` → `GameEntity` con `SetParent`
- Instanciación de behaviours via `Activator.CreateInstance` + `GameEntity.Add<T>` via `MakeGenericMethod`
- Deserialización de propiedades `JsonElement` → tipos primitivos, Vector2/3, Color, enum

**`EditorContext.cs`** ampliado con:
- `TakePlaySnapshot()` — serializa `ActiveScene` a JSON en memoria
- `RestoreFromSnapshot()` — deserializa y devuelve el snapshot
- `ClearPlaySnapshot()` — limpia el snapshot tras restaurar

### Proyecto: MonoGame.Editor.WinForms

**`EditorForm.cs`** ampliado:
- Campo `_playRunner`
- `OnPlayClick` guarda contra escena nula (MessageBox informativo)
- `OnEditorStateChanged` llama `StartPlayMode()` al entrar en Playing desde Editing, y `StopPlayMode()` al volver a Editing
- `StartPlayMode()` — toma snapshot, crea `PlayModeRunner`
- `StopPlayMode()` — descarta runner, restaura snapshot via `SetActiveScene`
- `OnViewportRenderFrame` redirige a `_playRunner` durante Playing/Paused; en Paused también dibuja gizmos

---

## Resumen de archivos por fase

| Fase | Archivos Core | Archivos WinForms |
|------|---------------|-------------------|
| **0.5** | `EditorProject.cs`, `ProjectManager.cs`, `GameCsprojChangedEvent.cs` | `NewProjectDialog.cs`, `NewProjectDialog.Designer.cs`, `EditorForm.cs` |
| **8a** | `LogLevel.cs`, `LogEntry.cs`, `IEditorLogger.cs`, `LogEntryAddedEvent.cs`, `EditorContext.cs` | `ConsolePanel.cs` |
| **8b** | — | `SceneHierarchyPanel.cs` |
| **8c** | `EditorGameObject.cs`, `SetTagsCommand.cs` | `InspectorPanel.cs` |
| **8d** | — | `AssetBrowserPanel.cs` |
| **9** | `EditorContext.cs`, `EditorScene.cs`, `SceneDirtyChangedEvent.cs`, `SceneCreatedEvent.cs` | `NewSceneDialog.cs+Designer`, `SceneManagerPanel.cs`, `EditorForm.cs` |
| **10** | `LocalizationEditorModel.cs`, `SetLocalizationValueCommand.cs`, `LocalizationLoadedEvent.cs` | `LocalizationBrowserPanel.cs` |
| **11** | `InputEditorModel.cs`, 4 Input Commands, `InputMapLoadedEvent.cs` | `InputMapEditorPanel.cs` |
| **12** | `ProjectSettings.cs`, `MgcbRunner.cs`, `BuildOutputLineEvent.cs` | `ProjectSettingsDialog.cs+Designer`, `EditorForm.cs` |
| **13** | `ICodeGenService.cs`, `CodeGenResult.cs`, `SceneCodeGenerator.cs`, `BehaviourSkeletonGenerator.cs`, `CsprojFileEditor.cs`, `GameBehaviourScanner.cs`, `TypeDescriptor.cs`, `CodeGenStartedEvent.cs`, `CodeGenCompletedEvent.cs`, `GenerateSceneCodeCommand.cs` | `NewBehaviourDialog.cs+Designer`, `AddBehaviourDialog.cs` |
| **14** | `SceneCodeGenerator.cs` (completar), `GameObjectRegistry.cs` (extender) | `EditorForm.cs`, `InspectorPanel.cs`, `AssetBrowserPanel.cs`, `CodeGenProgressDialog.cs` |

---

## Resumen de clases del Kernel reutilizadas

| Fase | Clases del Kernel |
|------|-------------------|
| 0 | `EventBus` |
| 1 | `Camera2D`, `InputActionMap`, `InputSerializer`, `ResolutionManager` |
| 3 | `GameWorld`, `GameEntity`, `GameBehaviour`, `TransformBehaviour`, `GameEntityPool` |
| 4 | `Camera2D`, `DrawHelper`, `PrimitiveBatch`, `GeometryUtility`, `ResolutionManager` |
| 5 | `AsyncContentLoader`, `ContentLoadGroup`, `Sprite` |
| 6 | `TiledMapRenderer`, `TiledObjectLayer` |
| 8 | — |
| 9 | `SceneManager` (referencia conceptual de ciclo de vida) |
| 10 | `LocalizationManager` (formato de ficheros) |
| 11 | `InputActionMap`, `InputAction`, `InputBinding`, `InputSerializer` |
| 12 | `ResolutionManager`, `PlatformManager` |
| 13–14 | `GameWorld`, `GameEntity`, `GameBehaviour`, `TransformBehaviour`, `Scene` (generación de código) |

---

## Estructura de carpetas del proyecto de juego objetivo

```
MyGame/
├── MyGame.sln
├── src/
│   ├── MyGame.csproj             # Apuntado por gameCsprojPath en project.json
│   ├── Game.cs
│   ├── Behaviours/               # Generado por CodeGen o añadido manualmente
│   └── Scenes/
│       ├── GameplayScene.cs      # Parte manual (partial class)
│       └── Generated/
│           └── GameplayScene.Generated.cs  # AUTO-GENERADO por el editor
├── Content/                      # Ruta configurable
│   ├── Content.mgcb
│   ├── Textures/
│   ├── Audio/
│   ├── Fonts/
│   └── Maps/
├── Localization/                 # Ruta configurable
│   ├── es.json
│   └── en.json
└── Editor/                       # Ficheros del editor (versionables con git)
    ├── project.json              # Descriptor + rutas al proyecto de juego
    ├── settings.json             # ProjectSettings (namespace, build config, etc.)
    ├── Scenes/
    └── Prefabs/
```

---

## Convenciones técnicas

- Comunicación entre paneles exclusivamente via `IEditorEventBus`
- Todas las operaciones de edición van a `CommandStack` para soporte de Undo/Redo
- `EditorContext` como fuente de verdad: escena activa, selección, estado, proyecto
- Los `.json` de escena son la fuente de verdad para edición (human-readable, versionables con git)
- Los `.Generated.cs` son artefactos derivables del `.scene.json` — seguros para commitear pero regenerables
- El Kernel gestiona la ejecución del juego; el editor construye sobre él sin duplicar código
- Todos los textos de la UI del editor en inglés
- Clases `sealed` por defecto, campos `_camelCase`, namespaces file-scoped
- Sin LINQ en bucles de generación de código (`StringBuilder` directo)
