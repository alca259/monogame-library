# Flujos principales de trabajo

Esta página describe paso a paso los flujos de trabajo más importantes del editor. Cada sección explica qué ocurre internamente cuando el usuario realiza una acción.

---

## Crear un nuevo proyecto

**Menú**: `File → New Project`

1. Se abre `NewProjectDialog` con campos para: nombre del proyecto, carpeta padre, ruta al `.csproj` del juego (opcional), carpeta de Content y carpeta de Localization.
2. Al seleccionar el `.csproj`, el diálogo rellena automáticamente las rutas de Content y Localization con valores sugeridos (`{csprojDir}/Content` y `{csprojDir}/Localization`).
3. El usuario confirma con OK.
4. `EditorForm` llama a `ProjectManager.Create(name, parentPath, csprojPath, contentRelative, localizationRelative)`.
5. `ProjectManager.Create()` hace:
   - Crea la carpeta `{parentPath}/{name}/`.
   - Crea las subcarpetas `Editor/`, `Editor/Scenes/`, `Editor/Prefabs/`.
   - Crea las carpetas `Content/` y `Localization/` si no existen.
   - Escribe `Editor/project.json` con las rutas relativas.
6. El proyecto se convierte en el activo: `context.SetActiveProject(project)`.
7. Se publica `ProjectOpenedEvent(project)`.
8. Todos los paneles reaccionan:
   - El gestor de behaviours escanea tipos disponibles.
   - El gestor de escenas escanea `.scene.json`.
   - `ContentWatcher` empieza a monitorear la carpeta Content.
   - Se cargan las `ProjectSettings` de `Editor/settings.json`.

**Formato de `project.json`**:
```json
{
  "name": "MiJuego",
  "version": "1.0",
  "gameCsprojPath": "src/MiJuego/MiJuego.csproj",
  "contentPath": "src/MiJuego/Content",
  "localizationPath": "src/MiJuego/Localization"
}
```

---

## Abrir un proyecto existente

**Menú**: `File → Open Project`

1. Se abre un `FolderBrowserDialog` para seleccionar la carpeta raíz del proyecto.
2. `ProjectManager.Load(carpeta)` deserializa `Editor/project.json`.
3. Se construye `EditorProject` con todas las rutas absolutas calculadas.
4. Mismo flujo que al crear: `SetActiveProject` → `ProjectOpenedEvent`.

---

## Crear una escena nueva

**Menú**: `File → New Scene`

1. Se abre `NewSceneDialog` con campos para nombre de la escena y tamaño del mundo (opcional; 0 = sin límites).
2. Al confirmar se crea un `EditorScene` en memoria (sin ScenePath todavía).
3. Se llama a `context.SetActiveScene(scene)`.
4. Se publican `SceneCreatedEvent(scene)` y `SceneLoadedEvent(scene)`.
5. La jerarquía se vacía, el inspector queda en blanco.
6. La escena NO está guardada en disco. Al intentar guardar la primera vez, pedirá nombre y ubicación.

---

## Guardar una escena

**Atajo**: `Ctrl+S`

1. Si la escena no tiene `ScenePath`, se abre `SaveFileDialog` apuntando a `Editor/Scenes/`.
2. `SceneSerializer.SaveAsync(scene, path)` serializa la escena completa a JSON (formato human-readable).
3. `context.MarkSceneClean()` se llama al terminar.
4. Se publica `SceneDirtyChangedEvent(false)`, el título del formulario pierde el asterisco `*`.
5. **Si** `ProjectSettings.GenerateOnSave` está activado y hay un `.csproj` configurado:
   - Se publica `CodeGenStartedEvent(scene.Name)`.
   - Se ejecuta `SceneCodeGenerator.GenerateSceneAsync(...)`.
   - Se publica `CodeGenCompletedEvent(result)`.
   - El resultado aparece en la consola.

---

## Crear una entidad

**Acciones posibles**:
- Botón `+` en la jerarquía.
- Clic derecho en la jerarquía → `Create Empty`.
- Clic derecho sobre una entidad → `Create Child`.

1. Se ejecuta `CreateEntityCommand(parentObjeto, posición)` en el `CommandStack`.
2. El comando crea un nuevo `EditorGameObject` con GUID único y lo añade al padre indicado (o a la raíz de la escena si no hay padre).
3. La jerarquía reconstruye el árbol.
4. La nueva entidad queda seleccionada.
5. El inspector muestra los valores de transformación por defecto.

---

## Añadir un behaviour a una entidad

1. Con una entidad seleccionada, en el inspector hacer clic en `+ Add Behaviour`.
2. Se abre `AddBehaviourDialog`: árbol agrupado por namespace con los `GameBehaviour` disponibles. Campo de búsqueda para filtrar.
3. El usuario selecciona un tipo y confirma.
4. Se ejecuta `AddBehaviourCommand(entidad, tipoSeleccionado)`.
5. El comando crea un `EditorBehaviour` con `TypeName` = nombre completo del tipo y `Properties` vacío.
6. El inspector añade dinámicamente una nueva sección con los controles del behaviour.
7. Se publica `BehaviourAddedEvent`.

---

## Editar propiedades en el inspector

1. El usuario modifica un campo (por ejemplo, escribe un nuevo valor de velocidad en un NumericUpDown).
2. El inspector captura el evento `Leave` (o `ValueChanged` con debounce).
3. Se ejecuta `SetPropertyCommand<T>(entidad, behaviour, nombrePropiedad, valorAnterior, valorNuevo)`.
4. El comando actualiza `behaviour.Properties[nombrePropiedad]` con el nuevo valor serializado como `JsonElement`.
5. La escena se marca como sucia.

---

## Editar transforms con gizmos

1. El usuario selecciona una entidad (aparece el gizmo en el viewport).
2. Mueve el cursor sobre un handle del gizmo (flecha de eje, círculo de rotación, etc.).
3. Hace clic: `GizmoController.BeginDrag(worldPos)` guarda la posición inicial de la entidad.
4. Arrastra: `GizmoController.UpdateDrag(worldPos)` modifica directamente `entity.Position/Rotation/Scale` y publica `GameObjectTransformChangedEvent` para actualizar el inspector en tiempo real.
5. Suelta el ratón: `GizmoController.EndDrag()` crea el comando correspondiente (`MoveEntityCommand`, `RotateEntityCommand` o `ScaleEntityCommand`) y lo devuelve al `EditorForm`.
6. `EditorForm` ejecuta el comando en el `CommandStack`.
7. **Nota**: el movimiento ya ocurrió durante el drag. El comando solo registra el cambio para poder deshacerlo (guarda posición inicial y final).

**Snapping a cuadrícula**: Si se mantiene pulsada la tecla `Ctrl` al soltar, la posición final se redondea al tamaño de celda de la cuadrícula (`GridCellSize`).

---

## Guardar una entidad como prefab

1. Clic derecho sobre una entidad en la jerarquía → `Save as Prefab`.
2. Se abre `SaveFileDialog` apuntando a `Editor/Prefabs/`.
3. `PrefabManager.Save(entidad, ruta)`:
   - Temporalmente pone `PrefabPath = ""` para no guardar la referencia circular.
   - Serializa la entidad (con todos sus behaviours e hijos) como JSON.
   - Restaura `PrefabPath`.
4. El archivo `.prefab.json` queda en disco.

---

## Instanciar un prefab

**Formas de instanciar**:
- Arrastrar un `.prefab.json` desde el Asset Browser al viewport o a la jerarquía.
- Clic derecho en la jerarquía → `Paste Prefab`.

1. `PrefabManager.Instantiate(ruta)`:
   - Carga la plantilla del prefab.
   - Hace una copia profunda (clona todos los campos, behaviours e hijos).
   - Marca la copia: `clone.PrefabPath = ruta`.
2. Se ejecuta `CreateEntityCommand` con la instancia clonada.
3. En la jerarquía, la instancia aparece con el nombre en azul.

---

## Revertir instancia de prefab al original

1. Clic derecho sobre la instancia en la jerarquía → `Revert from Prefab`.
2. Se ejecuta `RevertPrefabCommand`:
   - Guarda una copia del estado actual (para el Undo).
   - Llama a `PrefabManager.Instantiate(entity.PrefabPath)` para obtener el template limpio.
   - Reemplaza la entidad actual por la nueva instancia en el mismo lugar de la jerarquía.
3. Al hacer Undo, se restaura el estado previo a la reversión.

---

## Aplicar cambios de una instancia al prefab

1. Clic derecho → `Apply Prefab` (o botón en el inspector).
2. Se ejecuta `ApplyPrefabCommand`:
   - Guarda un backup del archivo de prefab actual (para Undo).
   - Llama a `PrefabManager.Save(entidad, entity.PrefabPath)`.
   - Ahora el archivo de prefab contiene los valores de esta instancia.

---

## Crear un Behaviour nuevo (esqueleto de código)

**Menú**: `Project → New Behaviour...`

1. Se abre `NewBehaviourDialog` con campos: nombre de clase, namespace, subcarpeta y métodos de ciclo de vida a incluir (Awake, Start, Update, Draw, OnDestroy).
2. `BehaviourSkeletonGenerator.GenerateBehaviourSkeletonAsync(...)` crea el archivo `.cs`:
   - Genera la clase con los métodos seleccionados y sus firmas correctas.
   - Incluye los `using` necesarios (incluyendo `Microsoft.Xna.Framework.Graphics` si se seleccionó `Draw`).
3. Añade el archivo al `.csproj` del juego si el proyecto usa inclusiones explícitas.
4. El archivo aparece en el Asset Browser.
5. Para que el editor lo reconozca como behaviour disponible, hay que compilar el proyecto (`Project → Build Game`) y luego `Project → Rescan Behaviours`.

**Importante**: la firma de `Draw` es `(GameTime gameTime, SpriteBatch spriteBatch)` — con dos parámetros.

---

## Generar código de una escena

**Menú**: `Project → Generate Scene Code` | **Atajo**: `Ctrl+G`

Ver [Generación de código](codegen.md) para el flujo detallado.

---

## Compilar el juego desde el editor

**Menú**: `Project → Build Game` | **Atajo**: `Ctrl+B`

1. Se llama a `MgcbRunner.RunDotnetBuildAsync(GameCsprojPath, BuildConfiguration, callback)`.
2. Cada línea de salida de MSBuild se envía al `ConsolePanel` vía `BuildOutputLineEvent`.
3. Si el build termina con éxito, se ejecuta automáticamente un rescan de behaviours (`GameBehaviourScanner.ScanAssemblyAsync(dllPath)`).

---

## Ejecutar el juego desde el editor

**Menú**: `Project → Run Game` | **Atajo**: `Ctrl+F5`

Lanza `dotnet run --project "{GameCsprojPath}"` como un proceso externo. La salida aparece en la consola.

---

## Configurar los ajustes del proyecto

**Menú**: `Project → Project Settings...`

Se abre `ProjectSettingsDialog` con cuatro pestañas:

| Pestaña | Qué configura |
|---------|---------------|
| General | Nombre, versión, ruta al `.csproj`, namespace raíz |
| Content | Carpeta de Content, archivo `.mgcb`, configuración de build |
| Localization | Carpeta de Localization, locale por defecto, locales soportados |
| Code Generation | Carpeta de salida del código generado, auto-generación al guardar |

Los cambios se guardan en `Editor/settings.json`.

---

## Configurar los subsistemas del mundo

**Menú**: `Scene → Configure World Subsystems...`

Se abre `WorldConfigDialog` con secciones para cada subsistema:

| Subsistema | Parámetros configurables |
|------------|-------------------------|
| Physics 2D | Activar/desactivar, gravedad X e Y |
| Lighting | Activar/desactivar, color ambiente RGBA |
| Navigation | Activar/desactivar, ancho/alto de la cuadrícula, tamaño de celda, origen X/Y |
| Audio | Activar/desactivar |

Al confirmar, se actualiza `scene.WorldConfig` y la escena se marca como sucia. Al guardar y generar código, `CreateWorld()` emitirá el código de inicialización de esos subsistemas.
