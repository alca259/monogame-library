# Arquitectura general del editor

## Dos proyectos, dos responsabilidades

La solución tiene una separación limpia en dos capas:

```
MonoGame.Editor.Core         ← Lógica pura, sin UI
MonoGame.Editor.WinForms     ← Presentación, controles WinForms
```

**Core** contiene todo lo que el editor "sabe hacer": manipular escenas, ejecutar comandos, serializar datos, generar código, gestionar proyectos. No tiene ninguna dependencia de WinForms.

**WinForms** contiene los formularios, paneles, diálogos y controles. Solo llama a Core; nunca al revés.

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
| `State` | Estado del editor: `Editing`, `Playing` o `Paused` |
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

El editor tiene tres estados distintos que afectan a lo que se puede hacer:

```
┌────────────┐  Play  ┌────────────┐  Pause  ┌────────────┐
│            │───────►│            │────────►│            │
│  Editing   │        │  Playing   │         │  Paused    │
│            │◄───────│            │◄────────│            │
└────────────┘  Stop  └────────────┘ Resume  └────────────┘
      ▲                                              │
      └──────────────────── Stop ───────────────────┘
```

- **Editing**: modo normal. Gizmos visibles, game loop parado, viewport del editor activo.
- **Playing**: snapshot de escena guardado en memoria, game loop ejecutándose, viewport del juego activo.
- **Paused**: render activo (se ve el juego), `Update` no ejecuta lógica, inspector editable en caliente.
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
| `EditorStateChangedEvent(old, new)` | Al cambiar entre Editing/Playing/Paused |
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
├── PlayMode/        ← Conversión de escena a GameWorld para ejecutar
├── Prefabs/         ← Gestión de prefabs
├── Preferences/     ← Preferencias persistidas del editor
├── Project/         ← Gestión de proyectos (crear, cargar, configurar)
├── Registry/        ← Registro de GameBehaviour disponibles
├── Serialization/   ← Serialización JSON de escenas y prefabs
└── Tilemaps/        ← Importador y modelo de tilemaps
```

## Organización de carpetas en WinForms

```
MonoGame.Editor.WinForms/
├── Controls/        ← MonoGameControl (viewport), EditorCamera2D
├── Dialogs/         ← Todos los diálogos modales
├── Gizmos/          ← GizmoRenderer (dibuja los gizmos con GPU)
├── Panels/          ← Todos los paneles del editor
├── Rendering/       ← EditModeRenderer (dibuja escena en modo edición)
└── EditorForm.cs    ← Formulario principal
```

---

## Flujo de datos en una operación típica (ejemplo: mover entidad)

1. El usuario hace clic y arrastra el gizmo de movimiento en el viewport.
2. `EditorForm` detecta el MouseMove y llama `GizmoController.UpdateDrag(worldPos)`.
3. `GizmoController` modifica directamente `selectedObject.Position` (en tiempo real, sin comando aún).
4. Publica `GameObjectTransformChangedEvent` para que el inspector muestre los valores al instante.
5. Al soltar el ratón, `EditorForm` llama `GizmoController.EndDrag()`.
6. `GizmoController` devuelve un `MoveEntityCommand` con la posición inicial y final.
7. `EditorForm` ejecuta `context.CommandStack.Execute(command)`.
8. `CommandStack` llama `command.Execute()` (que ya no mueve nada, el movimiento ya ocurrió) y guarda la operación en el historial.
9. `CommandStack` llama `context.MarkSceneDirty()`.
10. Se publica `SceneDirtyChangedEvent(true)` y el título del formulario muestra el asterisco `*`.
