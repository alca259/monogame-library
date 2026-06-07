# Arquitectura general del editor

## Tres proyectos, tres responsabilidades

La solución tiene una separación limpia en tres capas:

```
MonoGame.Editor.Core             ← Lógica pura, sin UI
MonoGame.Editor.Maui             ← Presentación, páginas y vistas MAUI
MonoGame.Editor.SourceGenerator  ← Roslyn Source Generator (netstandard2.0)
```

**Core** contiene todo lo que el editor "sabe hacer": manipular escenas, ejecutar comandos, serializar datos, generar código, gestionar proyectos, lanzar el proceso de juego. No tiene ninguna dependencia de UI.

**Maui** contiene las vistas, diálogos y controles. Solo llama a Core; nunca al revés.

**SourceGenerator** es un `IIncrementalGenerator` de Roslyn empaquetado como analizador en `GameApp.csproj`. Lee los `*.scene.json` declarados como `<AdditionalFiles>` y emite una clase estática por escena compatible con AOT (sin reflexión en runtime).

---

## Patrón de comunicación entre paneles

Los paneles **nunca se llaman directamente entre sí**. Toda comunicación pasa por el bus de eventos:

```
Panel A  →  EditorEventBus.Publish(evento)  →  Panel B recibe el evento
```

Esto significa que si, por ejemplo, el usuario selecciona una entidad en la jerarquía, la jerarquía publica `GameObjectSelectedEvent` y el inspector (que está suscrito a ese evento) se actualiza solo. Sin referencias cruzadas.

### Cómo publicar un evento

```csharp
_context.EventBus.Publish(new GameObjectSelectedEvent(entidad));
```

### Cómo suscribirse a un evento

```csharp
_context.EventBus.Subscribe<GameObjectSelectedEvent>(OnGameObjectSelected);

private void OnGameObjectSelected(GameObjectSelectedEvent e)
{
    // e.GameObject puede ser null (deselección)
    RefreshInspector(e.GameObject);
}
```

---

## EditorContext — La fuente de verdad

`EditorContext` es un singleton que almacena el estado global del editor en tiempo de ejecución:

| Propiedad | Qué guarda |
|-----------|-----------|
| `ActiveScene` | La escena que está abierta ahora mismo |
| `ActiveProject` | El proyecto de juego que está cargado |
| `SelectedObject` | La entidad seleccionada en la jerarquía |
| `State` | Estado del editor: `Editing` o `Playing` |
| `IsSceneDirty` | Si hay cambios sin guardar en la escena |
| `EventBus` | El bus de eventos para comunicación entre paneles |
| `CommandStack` | El historial de comandos para undo/redo |
| `Logger` | El logger del editor |

Acceso desde cualquier parte:
```csharp
EditorContext.Instance.ActiveScene
EditorContext.Instance.EventBus.Publish(...)
```

---

## Máquina de estados del editor

El editor tiene dos estados distintos que afectan a lo que se puede hacer:

```
┌────────────┐  Play  ┌────────────┐
│            │───────►│            │
│  Editing   │        │  Playing   │
│            │◄───────│            │
└────────────┘  Stop  └────────────┘
```

- **Editing**: modo normal. Gizmos visibles, game loop parado, viewport del editor activo.
- **Playing**: snapshot de escena guardado en memoria, proceso externo `GameApp.exe` en ejecución.
- **Stop**: se restaura el snapshot, se vuelve a `Editing`. Los cambios hechos durante play se descartan.

Cuando el estado cambia, se publica `EditorStateChangedEvent(oldState, newState)`.

---

## Lista completa de eventos del bus

### Eventos de proyecto y escena

| Evento | Cuándo se publica |
|--------|-------------------|
| `ProjectOpenedEvent(project?)` | Al cargar o cerrar un proyecto |
| `SceneLoadedEvent(scene?)` | Al cargar o cerrar una escena |
| `SceneCreatedEvent(scene)` | Al crear una escena nueva |
| `SceneDirtyChangedEvent(isDirty)` | Cuando hay o deja de haber cambios sin guardar |
| `GameCsprojChangedEvent(project)` | Cuando cambia la ruta al .csproj del juego |

### Eventos de entidades y selección

| Evento | Cuándo se publica |
|--------|-------------------|
| `GameObjectSelectedEvent(gameObject?)` | Al seleccionar o deseleccionar una entidad |
| `GameObjectTransformChangedEvent(gameObject)` | Al mover/rotar/escalar una entidad |
| `BehaviourAddedEvent(behaviour, gameObject)` | Al añadir un behaviour a una entidad |
| `TilemapLayerSelectedEvent(layer)` | Al seleccionar una capa de tilemap |

### Eventos de estado del editor

| Evento | Cuándo se publica |
|--------|-------------------|
| `EditorStateChangedEvent(old, new)` | Al cambiar entre Editing/Playing |
| `UndoPerformedEvent(description)` | Al ejecutar Deshacer |
| `RedoPerformedEvent(description)` | Al ejecutar Rehacer |

### Eventos de assets y build

| Evento | Cuándo se publica |
|--------|-------------------|
| `AssetImportedEvent(assetInfo)` | Al detectar un archivo nuevo/modificado en Content |
| `BuildOutputLineEvent(line, isError)` | Cada línea de salida del compilador |
| `CodeGenStartedEvent(sceneName)` | Al comenzar la generación de código |
| `CodeGenCompletedEvent(result)` | Al terminar la generación de código |

### Eventos de editores especializados

| Evento | Cuándo se publica |
|--------|-------------------|
| `InputMapLoadedEvent(model)` | Al cargar un mapa de input |
| `LocalizationLoadedEvent(model)` | Al cargar los archivos de localización |
| `LogEntryAddedEvent(entry)` | Al añadir una línea al log |

---

## Organización de carpetas en Core

```
MonoGame.Editor.Core/
├── Assets/          ← Clasificación y monitoreo de assets
├── Attributes/      ← EditorPropertyAttribute (reflexión para inspector)
├── CodeGen/         ← Generadores de código C#
├── Commands/        ← Todos los comandos undo/redo
├── Events/          ← Definición de todos los eventos del bus
├── Gizmos/          ← Lógica de los gizmos (sin rendering)
├── Input/           ← Modelo del editor de input maps
├── Localization/    ← Modelo del editor de localización
├── Logging/         ← Logger del editor
├── Models/          ← Modelos de datos (escena, entidad, behaviour...)
├── PlayMode/        ← ExternalPlayLauncher: lanza GameApp.exe como proceso externo
├── Prefabs/         ← Gestión de prefabs
├── Preferences/     ← Preferencias persistidas del editor
├── Project/         ← Gestión de proyectos (crear, cargar, configurar, scaffolding)
├── Registry/        ← Registro de GameBehaviour disponibles
├── Serialization/   ← Serialización JSON de escenas y prefabs
└── Tilemaps/        ← Importador y modelo de tilemaps
```

## Subsistemas adicionales de Core

### ContentWatcher

`ContentWatcher` (`Assets/ContentWatcher.cs`) envuelve un `FileSystemWatcher` que apunta a `src/GameApp/Content/`. Se activa al abrir un proyecto y publica `AssetImportedEvent` cada vez que detecta un archivo nuevo o modificado. El `AssetBrowserPanel` y otros paneles reaccionan a ese evento para refrescar su vista.

### GameObjectRegistry

`GameObjectRegistry` (`Registry/GameObjectRegistry.cs`) mantiene el catálogo de `GameBehaviour` disponibles para añadir a entidades. Tiene dos fuentes:

1. **Assemblies compilados** (`ScanAssemblyAsync`): reflexión sobre el DLL de `GameScripts` para encontrar clases que heredan de `GameBehaviour`.
2. **Código fuente** (`ScanSourceAsync`): análisis de texto de los `.cs` en `GameScripts/` para mostrar scripts recién creados antes de compilar (aparecen marcados como "pending compile").

Se popula al abrir un proyecto, tras cada build exitoso, y al ejecutar "Rescan Behaviours" manualmente.

---

## Modelo de hilos (threading)

El editor usa tres contextos de ejecución:

| Hilo | Qué ejecuta |
|------|------------|
| **UI** (MAUI) | Todas las vistas, event handlers del bus, `CommandStack`, publicaciones del `EventBus` |
| **Render** (MonoGameControl) | `EditModeRenderer`, `GizmoRenderer`, `NavGridPreviewRenderer`, `ResolutionPreviewRenderer` |
| **Background** (Task/async) | Scan de assemblies y fuente, I/O de escenas, compilación, lectura de stderr del proceso externo |

Cuando un hilo de fondo necesita actualizar la UI usa `MainThread.BeginInvokeOnMainThread()`. `EditorContext` usa `lock` internamente para toda escritura de estado.

---

## Organización de carpetas en Maui

```
MonoGame.Editor.Maui/
├── Controls/        ← Controles MAUI personalizados (AxisStepper, etc.)
├── Platforms/       ← Código específico por plataforma (Windows)
├── Rendering/       ← ViewportRenderer, EditorCamera2D, MaterialPreviewRenderer
├── Views/
│   ├── Dialogs/     ← Todos los diálogos modales
│   ├── Panels/      ← Todos los paneles del editor (Views)
│   ├── EditorWindow.xaml.cs ← Ventana principal
│   └── TitleBarView.cs
└── MauiProgram.cs   ← Punto de entrada MAUI
```

## Organización del SourceGenerator

```
MonoGame.Editor.SourceGenerator/
└── SceneSourceGenerator.cs   ← IIncrementalGenerator: *.scene.json → *_Scene.g.cs
```

Se referencia desde `GameApp.csproj` como:
```xml
<ProjectReference Include="..." OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
<AdditionalFiles Include="../../../.editor/scenes/**/*.scene.json" />
```

---

## Flujo de datos en una operación típica (ejemplo: mover entidad)

1. El usuario hace clic y arrastra el gizmo de movimiento en el viewport.
2. `EditorWindow` detecta el MouseMove y llama `GizmoController.UpdateDrag(worldPos)`.
3. `GizmoController` modifica directamente `selectedObject.Position` (en tiempo real, sin comando aún).
4. Publica `GameObjectTransformChangedEvent` para que el inspector muestre los valores al instante.
5. Al soltar el ratón, `EditorWindow` llama `GizmoController.EndDrag()`.
6. `GizmoController` devuelve un `MoveEntityCommand` con la posición inicial y final.
7. `EditorWindow` ejecuta `context.CommandStack.Execute(command)`.
8. `CommandStack` llama `command.Execute()` (que ya no mueve nada, el movimiento ya ocurrió) y guarda la operación en el historial.
9. `CommandStack` llama `context.MarkSceneDirty()`.
10. Se publica `SceneDirtyChangedEvent(true)` y el título del formulario muestra el asterisco `*`.
