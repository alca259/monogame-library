# Hoja de ruta: flujos completos del editor

Documento de referencia para comprender todos los flujos internos del editor antes de modificarlo manualmente. Para cada flujo se indica el punto de entrada, las clases implicadas con sus rutas de archivo, los eventos publicados y el estado que cambia en `EditorContext`.

> Los flujos de operaciones concretas (crear entidad, añadir behaviour, etc.) están documentados con mayor detalle en [flujos.md](flujos.md). Este documento complementa esa guía con el **mapa completo** de todos los flujos existentes, incluyendo los de infraestructura.

---

## Índice

1. [Arranque y ciclo de vida de la aplicación](#1-arranque-y-ciclo-de-vida-de-la-aplicación)
2. [Gestión de proyectos](#2-gestión-de-proyectos)
3. [Gestión de escenas](#3-gestión-de-escenas)
4. [Gestión de entidades](#4-gestión-de-entidades)
5. [Gestión de behaviours y propiedades](#5-gestión-de-behaviours-y-propiedades)
6. [Transformaciones con gizmos](#6-transformaciones-con-gizmos)
7. [Sistema de undo/redo](#7-sistema-de-undoredo)
8. [Modo juego (Play / Stop)](#8-modo-juego-play--stop)9. [Flujos de assets y ContentWatcher](#9-flujos-de-assets-y-contentwatcher)
10. [Generación de código](#10-generación-de-código)
11. [Sistema de prefabs](#11-sistema-de-prefabs)
12. [Tilemaps](#12-tilemaps)
13. [Editor de input maps](#13-editor-de-input-maps)
14. [Editor de localización](#14-editor-de-localización)
15. [Compilación del juego](#15-compilación-del-juego)
16. [Logging y consola](#16-logging-y-consola)
17. [Preferencias y persistencia de la UI](#17-preferencias-y-persistencia-de-la-ui)
18. [Registro de tipos (GameObjectRegistry)](#18-registro-de-tipos-gameobjectregistry)
19. [Modelo de hilos (threading)](#19-modelo-de-hilos-threading)

---

## 1. Arranque y ciclo de vida de la aplicación

### Entrada
`MauiProgram.cs` en `MonoGame.Editor.Maui/MauiProgram.cs`

### Secuencia de arranque

```
MauiProgram.CreateMauiApp()
  │
  ├─ Configurar Serilog → %APPDATA%/MonoGameEditor/logs/editor-*.log
  ├─ Registrar manejadores de excepción globales
  ├─ EditorContext.Instance  ← singleton (se crea aquí por primera vez)
  └─ App.CreateWindow() → EditorWindow
```

### Dentro de `EditorWindow` (constructor)
**Archivo:** `MonoGame.Editor.Maui/Views/EditorWindow.xaml.cs`

```
EditorWindow(context)
  │
  ├─ InitializeComponent()                ← generado por XAML
  ├─ EditorPreferences.Load()             ← restaura tamaños de paneles, visibilidad
  ├─ new GameObjectRegistry()             ← en memoria, vacío hasta que se abre proyecto
  ├─ new ContentWatcher()                 ← FileSystemWatcher inactivo hasta que se abre proyecto
  ├─ Instanciar todas las vistas          ← cada vista recibe _context como parámetro
  ├─ Suscribir todos los eventos del bus  ← EditorWindow suscribe sus propios manejadores
  └─ Restaurar layout de preferencias     ← anchuras de paneles, visibilidad de vistas
```

### Cierre de la aplicación

1. El usuario cierra la ventana → cierre de ventana MAUI
2. Si hay cambios sin guardar, se muestra un diálogo de confirmación.
3. `EditorPreferences.Save()` persiste el layout actual.
4. `ContentWatcher.Dispose()` detiene el monitor de archivos.
5. `_playLauncher?.Stop()` mata el proceso del juego si estaba en marcha.

---

## 2. Gestión de proyectos

### Crear proyecto nuevo
**Archivos clave:**
- `MonoGame.Editor.Maui/Views/Dialogs/NewProjectDialog.xaml.cs`
- `MonoGame.Editor.Core/Project/ProjectManager.cs`
- `MonoGame.Editor.Core/Project/ProjectScaffolder.cs`

```
File → New Project
  → NewProjectDialog (nombre, carpeta padre)
  → ProjectManager.Create(name, parentPath)
      → Crea {parentPath}/{name}/
      → Crea .editor/config/, .editor/logs/, .editor/scenes/, .editor/prefabs/
      → Escribe project.json en la raíz
      → ProjectScaffolder.Scaffold(project) → genera src/ completo
  → context.SetActiveProject(project)
  → EventBus.Publish(ProjectOpenedEvent(project))
```

**Estado que cambia:** `EditorContext.ActiveProject`

**Paneles que reaccionan a `ProjectOpenedEvent`:**
- `SceneManagerView` — escanea `.editor/scenes/*.scene.json`
- `AssetBrowserView` — apunta a `src/GameApp/Content/`
- `ScriptBrowserView` — apunta a `src/GameScripts/`
- `LocalizationBrowserView` — carga `ProjectSettings.LocalizationPath`
- `InputMapEditorView` — escanea `*.input.json` en `GameSourcePath`
- `GameObjectRegistry` — escanea assemblies y fuente del juego

### Abrir proyecto existente
**Archivo:** `MonoGame.Editor.Core/Project/ProjectManager.cs`

```
File → Open Project
  → Selector de carpeta nativo → ruta raíz
  → ProjectManager.Load(carpeta)
      → Deserializa {carpeta}/project.json
      → Construye EditorProject con todas las rutas absolutas
  → context.SetActiveProject(project)
  → EventBus.Publish(ProjectOpenedEvent(project))
      (mismo flujo que crear proyecto)
```

### Configuración del proyecto
**Archivo:** `MonoGame.Editor.Maui/Views/Dialogs/ProjectSettingsDialog.xaml.cs`

```
Project → Project Settings...
  → ProjectSettingsDialog (4 pestañas: General, Content, Localization, Code Generation)
  → Al Aceptar: ProjectSettings guardado en .editor/config/settings.json
  → Si cambió la ruta al .csproj: EventBus.Publish(GameCsprojChangedEvent(project))
```

---

## 3. Gestión de escenas

### Crear escena nueva
**Archivos clave:**
- `MonoGame.Editor.Maui/Views/Dialogs/NewSceneDialog.xaml.cs`
- `MonoGame.Editor.Core/Models/EditorScene.cs`

```
File → New Scene
  → NewSceneDialog (nombre, tamaño del mundo)
  → Crea EditorScene en memoria (sin ScenePath todavía)
  → context.SetActiveScene(scene)
  → EventBus.Publish(SceneCreatedEvent(scene))
  → EventBus.Publish(SceneLoadedEvent(scene))
```

**Estado que cambia:** `EditorContext.ActiveScene`, `EditorContext.IsSceneDirty = false`

### Cargar escena
**Archivo:** `MonoGame.Editor.Core/Serialization/SceneSerializer.cs`

```
SceneManagerPanel → doble clic en escena
  → Si hay cambios sin guardar: MessageBox de confirmación
  → SceneSerializer.LoadAsync(path)
      → Lee JSON del disco
      → Deserializa EditorScene con System.Text.Json
      → Restaura Parent links (excluidos del JSON)
  → context.SetActiveScene(scene)
  → EventBus.Publish(SceneLoadedEvent(scene))
```

### Guardar escena (Ctrl+S)
**Archivo:** `MonoGame.Editor.Core/Serialization/SceneSerializer.cs`

```
Ctrl+S
  → Si ScenePath es null: selector de archivos nativo → .editor/scenes/
  → SceneSerializer.SaveAsync(scene, path)
      → Serializa EditorScene a JSON indentado
      → Parent links excluidos de la serialización
  → context.MarkSceneClean()
  → EventBus.Publish(SceneDirtyChangedEvent(false))
  → Si ProjectSettings.GenerateOnSave: [ver flujo 10 - Generación de código]
```

**Estado que cambia:** `EditorContext.IsSceneDirty = false`

### Marcado de escena como sucia

Cualquier `CommandStack.Execute(cmd)` llama automáticamente a `context.MarkSceneDirty()` → publica `SceneDirtyChangedEvent(true)` → el título de `EditorWindow` muestra `*`.

### Configurar subsistemas del mundo
**Archivo:** `MonoGame.Editor.Maui/Views/Dialogs/WorldConfigDialog.xaml.cs`

```
Scene → Configure World Subsystems...
  → WorldConfigDialog (Physics 2D, Lighting, Navigation, Audio)
  → Al Aceptar: actualiza scene.WorldConfig
  → scene marcada como sucia
```

**Archivo del modelo:** `MonoGame.Editor.Core/Models/EditorWorldConfig.cs`

---

## 4. Gestión de entidades

### Crear entidad
**Archivo:** `MonoGame.Editor.Core/Commands/CreateEntityCommand.cs`

```
Botón + en jerarquía / Clic derecho → Create Empty / Create Child
  → CommandStack.Execute(new CreateEntityCommand(parent, position))
      → Crea EditorGameObject con GUID único
      → Lo añade a parent.Children (o a scene.RootGameObjects si no hay padre)
      → Publica GameObjectSelectedEvent(nuevaEntidad)
  → Jerarquía reconstruye el árbol
  → Inspector muestra la nueva entidad
```

### Duplicar entidad

```
Clic derecho → Duplicate
  → Copia profunda de EditorGameObject + todos sus hijos y behaviours
  → CommandStack.Execute(new CreateEntityCommand(parent, copiaClonada))
```

### Renombrar entidad (F2 o inspector)
**Archivo:** `MonoGame.Editor.Core/Commands/RenameEntityCommand.cs`

```
F2 en jerarquía / TextBox nombre en inspector pierde foco
  → CommandStack.Execute(new RenameEntityCommand(entidad, nombreNuevo))
      → Actualiza entidad.Name
  → Jerarquía refresca el nodo
```

### Eliminar entidad (Delete)
**Archivo:** `MonoGame.Editor.Core/Commands/DeleteEntityCommand.cs`

```
Tecla Delete / Clic derecho → Delete
  → Si tiene hijos: MessageBox de confirmación
  → CommandStack.Execute(new DeleteEntityCommand(entidad))
      → Guarda índice y referencia al padre (para undo)
      → Elimina la entidad del árbol
  → context.SelectedObject = null
  → EventBus.Publish(GameObjectSelectedEvent(null))
```

### Reparentar entidad (drag & drop en jerarquía)
**Archivo:** `MonoGame.Editor.Core/Commands/ReparentEntityCommand.cs`

```
Arrastrar nodo → soltar sobre otro nodo (o sobre el árbol vacío)
  → SceneHierarchyPanel detecta el drop
  → CommandStack.Execute(new ReparentEntityCommand(entidad, nuevoPadre, índice))
      → Elimina entidad del padre actual
      → La inserta en nuevoPadre.Children en la posición indicada
  → Jerarquía reconstruye el árbol
```

### Selección de entidad
**Archivo:** `MonoGame.Editor.Maui/Views/Panels/SceneHierarchyView.xaml.cs`

```
Clic en nodo del TreeView
  → SceneHierarchyPanel.SetSelection(entidad)
  → context.SelectedObject = entidad
  → EventBus.Publish(GameObjectSelectedEvent(entidad))
  → InspectorPanel actualiza sus controles
  → GizmoController.SetTarget(entidad)
  → GizmoRenderer muestra gizmos
```

Deselección: mismo flujo con `null` como argumento.

---

## 5. Gestión de behaviours y propiedades

### Añadir behaviour
**Archivos:**
- `MonoGame.Editor.Maui/Views/Dialogs/AddBehaviourDialog.xaml.cs`
- `MonoGame.Editor.Core/Commands/AddBehaviourCommand.cs`

```
Inspector → + Add Behaviour
  → AddBehaviourDialog (árbol por namespace, campo de búsqueda)
  → Usuario selecciona tipo y acepta
  → CommandStack.Execute(new AddBehaviourCommand(entidad, typeName))
      → Crea EditorBehaviour { TypeName, Properties = {} }
      → Lo añade a entidad.Behaviours
  → EventBus.Publish(BehaviourAddedEvent(behaviour, entidad))
  → Inspector añade dinámicamente la sección del nuevo behaviour
```

Los tipos disponibles provienen de `GameObjectRegistry` (ver sección 18).

### Eliminar behaviour
**Archivo:** `MonoGame.Editor.Core/Commands/RemoveBehaviourCommand.cs`

```
Inspector → botón × en la sección del behaviour
  → CommandStack.Execute(new RemoveBehaviourCommand(entidad, behaviour))
      → Guarda copia del behaviour completo (para undo)
      → Elimina de entidad.Behaviours
  → Inspector elimina la sección
```

### Editar propiedad en el inspector
**Archivo:** `MonoGame.Editor.Core/Commands/SetPropertyCommand.cs`

```
AxisStepper / Entry / CheckBox pierde el foco
  → Inspector captura evento Leave / ValueChanged
  → CommandStack.Execute(new SetPropertyCommand<T>(entidad, behaviour, prop, valorAnterior, valorNuevo))
      → Actualiza behaviour.Properties[prop] como JsonElement
  → Inspector no necesita recargarse (el control ya muestra el nuevo valor)
```

**Tipos de control por tipo de propiedad:** ver [paneles.md - Inspector](paneles.md#tipos-de-control-por-tipo-de-propiedad).

---

## 6. Transformaciones con gizmos

**Archivos clave:**
- `MonoGame.Editor.Core/Gizmos/GizmoController.cs` — lógica pura
- `MonoGame.Editor.Core/Gizmos/GizmoMode.cs` — enum de modos
- `MonoGame.Editor.Core/Gizmos/GizmoDragAxis.cs` — enum de ejes
- `MonoGame.Editor.Maui/Rendering/ViewportRenderer.cs` — rendering con GPU
- `MonoGame.Editor.Maui/Rendering/EditorCamera2D.cs` — conversión pantalla ↔ mundo

### Flujo de arrastre (mover como ejemplo)

```
Cursor sobre handle del gizmo → clic ratón
  → EditorWindow.OnViewportPointerDown()
  → GizmoController.BeginDrag(worldPos)
      → HitTest: determina qué handle se pulsó (eje X, eje Y, ambos)
      → Guarda posición inicial de la entidad

Cursor se mueve
  → EditorWindow.OnViewportPointerMoved()
  → GizmoController.UpdateDrag(worldPos)
      → Calcula delta según el eje activo
      → Modifica directamente entidad.Position (en tiempo real, sin comando)
      → EventBus.Publish(GameObjectTransformChangedEvent(entidad))
          → Inspector actualiza los AxisStepper de Position X/Y

Botón ratón suelta
  → EditorWindow.OnViewportPointerUp()
  → GizmoController.EndDrag()
      → Devuelve MoveEntityCommand(entidad, posInicial, posFinal)
  → EditorWindow ejecuta: CommandStack.Execute(command)
      → cmd.Execute() NO mueve nada (ya está en la posición final)
      → Guarda el comando en el historial para undo
```

### Snapping a cuadrícula

Si `Ctrl` está pulsado al soltar, `GizmoController.EndDrag()` redondea `posFinal` al múltiplo de `GridCellSize` más cercano antes de crear el comando.

### Conversión coordenadas pantalla ↔ mundo

```csharp
// Pantalla → mundo (necesario al recibir eventos de ratón)
Matrix inversa = Matrix.Invert(_camera.GetTransformMatrix(viewport));
Vector2 worldPos = Vector2.Transform(screenPos, inversa);
```

---

## 7. Sistema de undo/redo

**Archivo:** `MonoGame.Editor.Core/Commands/CommandStack.cs`

### Ejecutar un comando

```
Cualquier operación del editor
  → CommandStack.Execute(cmd)
      1. cmd.Execute()
      2. Añade cmd al historial (LinkedList, máx. 100)
      3. Vacía la pila de redo
      4. context.MarkSceneDirty()
```

### Deshacer (Ctrl+Z)

```
EditorWindow key handler → Ctrl+Z
  → CommandStack.Undo()
      1. Saca cmd del historial
      2. cmd.Undo()
      3. Guarda cmd en la pila de redo
      4. EventBus.Publish(UndoPerformedEvent(cmd.Description))
  → SceneHierarchyView reconstruye el árbol
  → InspectorView recarga los controles
  → UndoHistoryView actualiza la lista
```

### Rehacer (Ctrl+Y)

```
EditorWindow key handler → Ctrl+Y
  → CommandStack.Redo()
      1. Saca cmd de la pila de redo
      2. cmd.Execute()
      3. Guarda cmd en el historial
      4. EventBus.Publish(RedoPerformedEvent(cmd.Description))
  → Mismos refrescos que al deshacer
```

### UndoHistoryView
**Archivo:** `MonoGame.Editor.Maui/Views/Panels/UndoHistoryView.xaml.cs`

Suscribe a `UndoPerformedEvent` y `RedoPerformedEvent`. Muestra la lista de comandos en el historial usando `CommandStack.GetHistory()`. El comando actual aparece resaltado.

---

## 8. Modo juego (Play / Stop)

**Archivos clave:**
- `MonoGame.Editor.Core/PlayMode/ExternalPlayLauncher.cs`
- `MonoGame.Editor.Maui/Views/EditorWindow.xaml.cs` — orquesta el flujo

### Entrar en modo Play (F5 / botón ▶)

```
EditorWindow.OnPlayClick()
  │
  ├─ Si no hay escena activa: diálogo informativo + cancelar
  ├─ Si la escena tiene cambios: guardar automáticamente (flujo 3 - Guardar)
  │
  ├─ Build de GameApp.csproj (flujo 15 - Compilación)
  │     Si falla: cancelar modo Play, errores en consola
  │
  ├─ Calcula ruta al ejecutable:
  │     {RootPath}/src/GameApp/bin/Debug/net10.0/GameApp.exe
  │
  ├─ _playLauncher = new ExternalPlayLauncher()
  ├─ _playLauncher.Launch(exePath, scenePath, logLineCallback)
  │     → GameApp.exe --scene "{path/.editor/scenes/Scene.scene.json}"
  │     → stderr redirigido → logLineCallback → ConsolePanelView
  │
  └─ context.SetState(EditorState.Playing)
       → EventBus.Publish(EditorStateChangedEvent(Editing, Playing))
           → Inspector queda en solo lectura
           → Barra de herramientas: Play desactivado, Stop activo
```

### Detener modo Play (botón ⏹)

```
EditorWindow.OnStopClick()
  │
  ├─ _playLauncher.Stop()
  │     → Process.Kill(entireProcessTree: true)
  │
  └─ context.SetState(EditorState.Editing)
       → EventBus.Publish(EditorStateChangedEvent(Playing, Editing))
           → Inspector vuelve a ser editable
           → Viewport del editor se reactiva
```

---

## 9. Flujos de assets y ContentWatcher

**Archivos clave:**
- `MonoGame.Editor.Core/Assets/ContentWatcher.cs`
- `MonoGame.Editor.Core/Assets/AssetClassifier.cs`
- `MonoGame.Editor.Core/Assets/AssetInfo.cs`
- `MonoGame.Editor.Core/Assets/AssetType.cs`
- `MonoGame.Editor.Maui/Views/Panels/AssetBrowserView.xaml.cs`

### ContentWatcher — detección de cambios

```
Al abrir proyecto:
  → ContentWatcher.Start(contentPath)
      → FileSystemWatcher apunta a src/GameApp/Content/
      → Monitorea: Created, Changed, Deleted, Renamed

Al detectar archivo nuevo o modificado:
  → AssetClassifier.Classify(filePath) → AssetInfo { Path, Type, Name }
  → EventBus.Publish(AssetImportedEvent(assetInfo))
  → AssetBrowserView suscrita → refresca la lista del panel derecho
```

### Tipos de asset reconocidos por `AssetClassifier`

| Extensión | `AssetType` |
|-----------|------------|
| `.png`, `.jpg`, `.bmp` | `Texture` |
| `.wav`, `.ogg`, `.mp3` | `Audio` |
| `.ttf`, `.spritefont` | `Font` |
| `.tmx` | `TiledMap` |
| `.scene.json` | `Scene` |
| `.prefab.json` | `Prefab` |
| `.particles.json` | `Particles` |
| `.anim.json` | `Animation` |
| `.input.json` | `InputMap` |
| `.sprite.json` | `Sprite` |
| `.mat.json` | `Material` |
| `.uitheme.json` | `UITheme` |
| `.cs` | `Script` |

### Selección de asset

```
AssetBrowserPanel → clic en archivo
  → EventBus.Publish(AssetSelectedEvent(assetInfo))
  → Paneles suscriptores reaccionan según el tipo:
      UIThemeInspectorView   (AssetType.UITheme)
      SpriteInspectorView    (AssetType.Sprite)
      MaterialInspectorView  (AssetType.Material)
```

### SpriteInspectorView
**Archivo:** `MonoGame.Editor.Maui/Views/Panels/SpriteInspectorView.xaml.cs`

Modelo: `MonoGame.Editor.Core/Models/EditorSpriteMetadata.cs`

Al seleccionar un asset de tipo `Sprite`, carga y muestra la vista previa de la textura y los metadatos (nombre, atlas rect, pivot, etc.). Los cambios se serializan al archivo `.sprite.json`.

### MaterialInspectorView
**Archivo:** `MonoGame.Editor.Maui/Views/Panels/MaterialInspectorView.xaml.cs`

Al seleccionar un asset de tipo `Material`, muestra una vista previa del material y sus propiedades. Usa `MaterialPreviewRenderer` para el render de vista previa.

**Archivo del renderer:** `MonoGame.Editor.Maui/Rendering/MaterialPreviewRenderer.cs`

---

## 10. Generación de código

**Archivos clave:**
- `MonoGame.Editor.Core/CodeGen/SceneCodeGenerator.cs`
- `MonoGame.Editor.Core/CodeGen/BehaviourSkeletonGenerator.cs`
- `MonoGame.Editor.Core/CodeGen/ICodeGenService.cs`
- `MonoGame.Editor.Core/CodeGen/TypeDescriptor.cs`
- `MonoGame.Editor.Core/CodeGen/CsprojFileEditor.cs`
- `MonoGame.Editor.Core/CodeGen/CodeGenResult.cs`
- `MonoGame.Editor.Core/Commands/GenerateSceneCodeCommand.cs`
- `MonoGame.Editor.SourceGenerator/SceneSourceGenerator.cs`

### Generar código de escena (Ctrl+G / Generate Scene Code)

```
Project → Generate Scene Code
  → CommandStack.Execute(new GenerateSceneCodeCommand(scene, project, codeGenService))
  → EventBus.Publish(CodeGenStartedEvent(scene.Name))
  │
  → SceneCodeGenerator.GenerateSceneAsync(scene, project)
      1. Construye lista de usings necesarios (analiza TypeNames de todos los behaviours)
      2. Para cada objeto de la jerarquía:
           - Genera `var entity = new GameEntity("name");`
           - Para cada behaviour: `entity.AddBehaviour(new BehaviourType(args));`
           - Asigna propiedades marcadas con [EditorProperty]
      3. Calcula MD5 del contenido generado
      4. Si el hash no cambió respecto al archivo existente: no sobreescribe
      5. Si cambió: escribe {GameScriptsPath}/{SceneName}Scene.Generated.cs
      6. Si el .csproj usa inclusiones explícitas:
           CsprojFileEditor.AddFileIfMissing(csprojPath, generatedFilePath)
  │
  → EventBus.Publish(CodeGenCompletedEvent(result))
  → ConsolePanel muestra el resultado
```

### Generación automática al guardar

Si `ProjectSettings.GenerateOnSave == true`, el flujo de guardado de escena llama directamente a `SceneCodeGenerator.GenerateSceneAsync()` tras serializar el JSON (sin pasar por `CommandStack`, ya que el guardado no es deshacible).

### Generar esqueleto de behaviour

```
Project → New Behaviour...
  → NewBehaviourDialog (nombre, namespace, subcarpeta, métodos)
  → BehaviourSkeletonGenerator.GenerateBehaviourSkeletonAsync(config)
      → Crea archivo .cs con la clase y los métodos seleccionados
      → Firma correcta de Draw: (GameTime gameTime, SpriteBatch spriteBatch)
      → CsprojFileEditor.AddFileIfMissing(...) si el proyecto usa inclusiones explícitas
```

### Roslyn SourceGenerator (en tiempo de compilación)
**Archivo:** `MonoGame.Editor.SourceGenerator/SceneSourceGenerator.cs`

Este generador corre dentro del compilador de `GameApp.csproj`, no en el editor:

```
dotnet build GameApp.csproj
  → SceneSourceGenerator (IIncrementalGenerator)
      → Lee *.scene.json declarados como <AdditionalFiles>
      → Por cada escena: emite {SceneName}_Scene.g.cs con clase estática
        compatible AOT (sin reflexión en runtime)
```

---

## 11. Sistema de prefabs

**Archivos clave:**
- `MonoGame.Editor.Core/Prefabs/PrefabManager.cs`
- `MonoGame.Editor.Core/Prefabs/PrefabSerializer.cs`
- `MonoGame.Editor.Core/Commands/ApplyPrefabCommand.cs`
- `MonoGame.Editor.Core/Commands/RevertPrefabCommand.cs`

### Guardar entidad como prefab

```
Clic derecho → Save as Prefab
  → Selector de archivos nativo → .editor/prefabs/
  → PrefabManager.Save(entidad, ruta)
      → Temporalmente pone PrefabPath = "" (evita referencia circular)
      → PrefabSerializer.Save() serializa la entidad a JSON
      → Restaura PrefabPath
```

### Instanciar un prefab

```
Arrastrar .prefab.json al viewport o jerarquía
  → PrefabManager.Instantiate(ruta)
      → PrefabSerializer.Load() → carga la plantilla
      → Copia profunda (clona entidad + hijos + behaviours)
      → clone.PrefabPath = ruta
  → CommandStack.Execute(new CreateEntityCommand(parent, clone))
  → En la jerarquía, el nombre del clon aparece en azul
```

### Aplicar cambios al prefab (Apply Prefab)

```
Clic derecho → Apply Prefab (o botón en inspector)
  → CommandStack.Execute(new ApplyPrefabCommand(entidad))
      → Guarda backup del archivo .prefab.json actual (para undo)
      → PrefabManager.Save(entidad, entidad.PrefabPath)
```

### Revertir instancia al prefab (Revert Prefab)

```
Clic derecho → Revert from Prefab
  → CommandStack.Execute(new RevertPrefabCommand(entidad))
      → Guarda estado actual (para undo)
      → PrefabManager.Instantiate(entidad.PrefabPath) → template limpio
      → Reemplaza entidad en la jerarquía por el template
```

---

## 12. Tilemaps

**Archivos clave:**
- `MonoGame.Editor.Core/Tilemaps/TilemapImporter.cs`
- `MonoGame.Editor.Core/Tilemaps/EditorTilemapAsset.cs`
- `MonoGame.Editor.Core/Tilemaps/EditorTileLayer.cs`
- `MonoGame.Editor.Core/Tilemaps/EditorTileset.cs`
- `MonoGame.Editor.Core/Commands/PaintTileCommand.cs`
- `MonoGame.Editor.Core/Commands/EraseTileCommand.cs`
- `MonoGame.Editor.Maui/Views/Panels/TilemapPaletteView.xaml.cs`

### Importar tilemap

```
AssetBrowser → doble clic en .tmx
  → TilemapImporter.Import(path)
      → Lee el archivo .tmx (XML formato Tiled)
      → Construye EditorTilemapAsset con EditorTileset[] y EditorTileLayer[]
      → Cada EditorTileset contiene el GID base y la referencia a la textura
```

### Seleccionar capa de tilemap

```
TilemapPalettePanel → clic en capa
  → EventBus.Publish(TilemapLayerSelectedEvent(layer))
  → TilemapPalettePanel muestra los tiles de ese tileset
```

### Pintar tile

```
TilemapPalettePanel → tile seleccionado → clic en viewport
  → EditorWindow detecta clic en celda del tilemap
  → CommandStack.Execute(new PaintTileCommand(layer, cellX, cellY, tileGid))
      → Guarda GID anterior de esa celda (para undo)
      → Actualiza layer.Tiles[cellY, cellX] = tileGid
```

### Borrar tile

```
TilemapPalettePanel → modo borrar → clic en viewport
  → CommandStack.Execute(new EraseTileCommand(layer, cellX, cellY))
      → Guarda GID anterior (para undo)
      → Actualiza layer.Tiles[cellY, cellX] = 0 (sin tile)
```

---

## 13. Editor de input maps

**Archivos clave:**
- `MonoGame.Editor.Core/Input/InputEditorModel.cs`
- `MonoGame.Editor.Core/Input/InputActionEntry.cs`
- `MonoGame.Editor.Core/Input/InputBindingEntry.cs`
- `MonoGame.Editor.Core/Commands/AddInputActionCommand.cs`
- `MonoGame.Editor.Core/Commands/RemoveInputActionCommand.cs`
- `MonoGame.Editor.Core/Commands/AddInputBindingCommand.cs`
- `MonoGame.Editor.Core/Commands/RemoveInputBindingCommand.cs`
- `MonoGame.Editor.Maui/Views/Panels/InputMapEditorView.xaml.cs`

### Cargar mapa de input

```
Al abrir proyecto (ProjectOpenedEvent)
  → InputMapEditorPanel escanea {GameSourcePath}/*.input.json
  → Deserializa InputEditorModel
  → EventBus.Publish(InputMapLoadedEvent(model))
  → Panel rellena el árbol de acciones y la tabla de bindings
```

### Añadir acción

```
InputMapEditorPanel → Add Action
  → CommandStack.Execute(new AddInputActionCommand(model, nombreAcción))
      → Añade InputActionEntry a model.Actions
  → Panel actualiza el árbol
```

### Añadir binding

```
InputMapEditorPanel → acción seleccionada → Add Binding
  → CommandStack.Execute(new AddInputBindingCommand(action, device, key))
      → Añade InputBindingEntry a action.Bindings
  → Panel actualiza la tabla de bindings
  → Al salir del panel o pulsar Save: serializa model a .input.json
```

---

## 14. Editor de localización

**Archivos clave:**
- `MonoGame.Editor.Core/Localization/LocalizationEditorModel.cs`
- `MonoGame.Editor.Core/Commands/SetLocalizationValueCommand.cs`
- `MonoGame.Editor.Maui/Views/Panels/LocalizationBrowserView.xaml.cs`
- `MonoGame.Editor.Maui/Views/Dialogs/LocaleCreationDialog.xaml.cs`

### Cargar localización

```
Al abrir proyecto (ProjectOpenedEvent)
  → LocalizationBrowserPanel construye árbol de carpetas en LocalizationPath
  → Al seleccionar carpeta: escanea *.json y construye LocalizationEditorModel
  → EventBus.Publish(LocalizationLoadedEvent(model))
  → Tabla de localización: primera columna = clave, una columna por locale
```

### Añadir locale

```
Botón Add Locale
  → LocaleCreationDialog (código locale validado: xx o xx-XX)
  → Crea {LocalizationPath}/{locale}.json con contenido {}
  → Recarga la vista
```

### Editar valor de clave

```
Edición de celda en la tabla
  → CommandStack.Execute(new SetLocalizationValueCommand(model, locale, clave, valorNuevo))
      → Actualiza model.Values[locale][clave]
  → Botón Save: serializa todos los archivos de locale al disco
```

---

## 15. Compilación del juego

**Archivos clave:**
- `MonoGame.Editor.Core/Assets/MgcbRunner.cs`
- `MonoGame.Editor.Core/Registry/GameObjectRegistry.cs`
- `MonoGame.Editor.Maui/Views/Panels/ConsolePanelView.xaml.cs`

### Build completo (Ctrl+B)

```
Project → Build Game
  → MgcbRunner.RunDotnetBuildAsync(GameCsprojPath, BuildConfiguration, callback)
      → Lanza: dotnet build {csprojPath} -c {config}
      → Cada línea de stdout/stderr:
          callback(línea) → EventBus.Publish(BuildOutputLineEvent(línea, esError))
          → ConsolePanel detecta patrones MSBuild (error CS / warning CS / Build succeeded)
  → Si build exitoso:
      → GameObjectRegistry.ScanAssemblyAsync(dllPath)
          → Carga el DLL compilado
          → Busca todas las clases que heredan de GameBehaviour
          → Actualiza el registro de tipos disponibles
```

### Rescan de behaviours manual

```
Project → Rescan Behaviours
  → GameObjectRegistry.ScanAssemblyAsync(dllPath)  (igual que tras build exitoso)
```

---

## 16. Logging y consola

**Archivos clave:**
- `MonoGame.Editor.Core/Logging/IEditorLogger.cs`
- `MonoGame.Editor.Core/Logging/LogEntry.cs`
- `MonoGame.Editor.Core/Logging/LogLevel.cs`
- `MonoGame.Editor.Maui/Views/Panels/ConsolePanelView.xaml.cs`
- `MonoGame.Editor.Maui/Globals.cs` — instancia del logger accesible

### Flujo de un mensaje de log

```
Cualquier parte del editor:
  → context.Logger.Log(mensaje, nivel)

IEditorLogger (implementado en Maui/Globals.cs)
  → Crea LogEntry { Message, Level, Timestamp }
  → EventBus.Publish(LogEntryAddedEvent(entry))
  → Serilog también lo persiste en %APPDATA%/MonoGameEditor/logs/editor-*.log

ConsolePanelView (suscrita a LogEntryAddedEvent)
  → Añade línea al área de texto con el color correspondiente al nivel
  → Formato: [HH:mm:ss] [NIVEL] mensaje
```

### Niveles y colores

| Nivel | Color en consola |
|-------|-----------------|
| `Debug` | Gris |
| `Info` | Blanco (modo oscuro) |
| `Warning` | Dorado |
| `Error` | Rojo (negrita) |

### Detección de patrones MSBuild

`ConsolePanel` analiza cada `BuildOutputLineEvent` buscando patrones:
- `error CS` → nivel `Error`
- `warning CS` → nivel `Warning`
- `Build succeeded` → verde
- `Build FAILED` → rojo

---

## 17. Preferencias y persistencia de la UI

**Archivo:** `MonoGame.Editor.Core/Preferences/EditorPreferences.cs`

**Ruta en disco:** `%APPDATA%/MonoGameEditor/preferences.json`

### Qué se persiste

| Preferencia | Descripción |
|-------------|-------------|
| Anchuras de `SplitContainer` | Tamaños de los paneles izquierdo, central y derecho |
| Visibilidad de paneles | Qué paneles están visibles (consola, scripts, etc.) |
| Lista de proyectos recientes | Rutas de los últimos proyectos abiertos |
| Estados de secciones | Qué secciones del inspector están colapsadas |
| Configuración de cuadrícula | Tamaño de celda (`GridCellSize`), visibilidad del grid |
| Modo de gizmo activo | El último modo de gizmo seleccionado |

### Cuándo se guarda y carga

- **Carga:** `EditorPreferences.Load()` en el constructor de `EditorWindow`
- **Guardado:** `EditorPreferences.Save()` en el cierre de `EditorWindow`
- **Formato:** JSON plano con `System.Text.Json`

### Configuración del proyecto (distinta de las preferencias)

`ProjectSettings` se guarda en `.editor/config/settings.json` dentro del proyecto y es **versionable con git**:

| Ajuste | Descripción |
|--------|-------------|
| `RootNamespace` | Namespace raíz para la generación de código |
| `GeneratedCodeFolder` | Carpeta de salida de los `.Generated.cs` |
| `GenerateOnSave` | Auto-generar código al guardar la escena |
| `Locales` | Códigos de locale soportados |
| `BuildConfiguration` | `Debug` / `Release` |

---

## 18. Registro de tipos (GameObjectRegistry)

**Archivo:** `MonoGame.Editor.Core/Registry/GameObjectRegistry.cs`

Mantiene el catálogo de `GameBehaviour` disponibles para añadir a entidades. Tiene dos fuentes de tipos:

### Fuente 1: Assemblies compilados

```
GameObjectRegistry.ScanAssemblyAsync(dllPath)
  → Carga el DLL con Assembly.LoadFrom(dllPath)
  → Reflexión: busca todas las clases public sealed que heredan de GameBehaviour
  → Filtra las que tienen al menos un constructor
  → Almacena: TypeName (assembly-qualified) → Type
```

Se llama:
- Tras un build exitoso
- Al abrir un proyecto (si hay un DLL previo compilado)
- Al ejecutar "Rescan Behaviours" manualmente

### Fuente 2: Código fuente (sin compilar)

```
GameObjectRegistry.ScanSourceAsync(GameScriptsPath)
  → Lee todos los archivos .cs de GameScripts/
  → Análisis de texto: busca "class X : GameBehaviour" con regex
  → Guarda nombres de clase pendientes (sin reflexión real)
  → Aparecen en AddBehaviourDialog marcados como "pending compile"
```

Esto permite al editor mostrar scripts recién creados antes de compilar.

---

## 19. Modelo de hilos (threading)

El editor usa tres contextos de ejecución:

### Hilo UI (MAUI)

- Todas las vistas, paneles y diálogos MAUI
- `EditorWindow` y todos los event handlers del bus
- `CommandStack.Execute/Undo/Redo` (siempre desde el hilo UI)
- Publicaciones del `EventBus` (síncronas en el hilo que publica)

### Hilo de render (MonoGameControl)

- `MonoGameControl` mantiene su propio bucle de mensajes
- `EditModeRenderer.Draw()` — dibuja la escena en modo edición
- `GizmoRenderer.Draw()` — dibuja los gizmos
- `NavGridPreviewRenderer` y `ResolutionPreviewRenderer`
- Interactúa con el hilo UI a través de `Invoke()` cuando necesita acceder a datos del editor

### Hilos en segundo plano (Task/async)

- `GameObjectRegistry.ScanSourceAsync()` — análisis de archivos fuente
- `GameObjectRegistry.ScanAssemblyAsync()` — carga de DLLs
- `SceneSerializer.SaveAsync() / LoadAsync()` — I/O de escenas
- `MgcbRunner.RunDotnetBuildAsync()` — proceso de compilación
- `ExternalPlayLauncher` — lectura de stderr del proceso externo

### Sincronización

- `EditorContext` usa `lock` interno para toda escritura de estado
- `EventBus` usa `lock` para el registro y desregistro de manejadores (pero no para la publicación)
- Las vistas que actualizan controles MAUI desde hilos de fondo usan `MainThread.BeginInvokeOnMainThread()`

---

## Resumen de la cadena típica de eventos

Para cualquier acción del editor, la cadena habitual es:

```
Input del usuario (ratón, teclado, botón)
  → Vista MAUI captura el evento
  → Crea y ejecuta un IEditorCommand via CommandStack
      → Modifica el modelo (EditorScene / EditorGameObject / EditorBehaviour)
      → context.MarkSceneDirty()
  → EventBus.Publish(evento específico)
      → Paneles suscritos se actualizan
      → Jerarquía / Inspector / Consola refrescan su vista
```

Los **invariantes** que siempre se cumplen:
1. El modelo solo se modifica a través de comandos (undo/redo garantizado).
2. Los paneles nunca se comunican directamente entre sí (solo via EventBus).
3. `EditorContext` es la única fuente de verdad del estado global.
4. Los archivos `.g.cs` generados por el compilador XAML de MAUI no se modifican manualmente.
