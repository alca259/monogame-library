# Paneles de la interfaz

El editor está compuesto por un formulario principal (`EditorForm`) y varios paneles que pueden mostrarse u ocultarse. La comunicación entre paneles ocurre exclusivamente a través del bus de eventos (ver [arquitectura](arquitectura.md)).

---

## Layout general del formulario principal

```
┌─────────────────────────────────────────────────────────┐
│  MenuStrip (Archivo, Proyecto, Escena, Vista, Ayuda)    │
│  ToolStrip (Play ▶ / Pause ⏸ / Stop ⏹ + modos gizmo)   │
├──────────────┬──────────────────────────┬───────────────┤
│              │                          │               │
│  Jerarquía  │     Viewport             │  Inspector    │
│  de escena  │   (MonoGameControl)      │               │
│             │                          │               │
├──────────────┴──────────────────────────┴───────────────┤
│   Consola  │ Assets │ Escenas │ Localización │ InputMap  │
│  (panel inferior con pestañas)                          │
└─────────────────────────────────────────────────────────┘
│  StatusStrip (estado del editor, info de posición)      │
└─────────────────────────────────────────────────────────┘
```

---

## Panel: Jerarquía de escena (`SceneHierarchyPanel`)

Muestra la estructura jerárquica de entidades de la escena activa.

### Controles

- **Barra de herramientas superior**: botón `+` (crear entidad raíz), botón papelera (eliminar seleccionada), campo de búsqueda, contador de entidades.
- **TreeView**: árbol con nodos por entidad. Íconos diferenciados por tipo (entidad genérica, cámara, luz, partículas, tilemap).
- **Barra inferior**: muestra el total de objetos en la escena.

### Comportamiento

- Al hacer doble clic en un nodo se selecciona la entidad (publica `GameObjectSelectedEvent`).
- Se puede arrastrar un nodo sobre otro para reorganizar la jerarquía (reparenting).
- Renombrar en línea con F2.
- La búsqueda filtra el árbol de forma incremental (case-insensitive). Si un padre no coincide pero un hijo sí, el padre se muestra igualmente para poder llegar al hijo.
- Las entidades instanciadas desde prefab muestran su nombre en azul.
- Al cargar una escena (`SceneLoadedEvent`) el árbol se reconstruye completo.
- Al hacer undo/redo el árbol se reconstruye para reflejar el estado actual.

### Menú contextual (clic derecho sobre nodo)

| Opción | Acción |
|--------|--------|
| Create Empty | Crea una entidad hija de la seleccionada |
| Create Child | Igual que Create Empty |
| Duplicate | Copia la entidad y toda su jerarquía |
| Rename | Activa el modo de edición en línea |
| Delete | Elimina la entidad (con confirmación si tiene hijos) |
| Toggle Active | Activa/desactiva la entidad |
| Save as Prefab | Guarda la entidad como archivo `.prefab.json` |
| Apply Prefab | Aplica los cambios del editor al prefab de origen |
| Revert Prefab | Descarta los cambios y restaura desde el prefab de origen |

---

## Panel: Inspector (`InspectorPanel`)

Muestra y permite editar las propiedades de la entidad seleccionada.

### Secciones del inspector

**1. Cabecera de entidad** (fija en la parte superior):
- Checkbox `Active`: activa/desactiva la entidad.
- TextBox con el nombre de la entidad (editable, confirma al salir del campo → `RenameEntityCommand`).
- ComboBox de tags: permite añadir y quitar etiquetas.
- Label con los primeros 8 caracteres del GUID de la entidad (solo lectura).

**2. Cabecera de prefab** (solo visible si la entidad viene de un prefab):
- Botones: `Apply Prefab`, `Revert`, `Save as Prefab`.

**3. Sección Transform** (siempre visible):
- **Position X / Y**: posición en el mundo 2D.
- **Depth Z**: profundidad para vista 2.5D (parallax).
- **Rotation**: ángulo en grados.
- **Scale X / Y**: escala del objeto.

Cada cambio manual en el transform genera el comando correspondiente (`MoveEntityCommand`, `RotateEntityCommand`, etc.) para poder deshacerse.

**4. Secciones de behaviours** (una por cada behaviour adjunto):
- Encabezado colapsable con el nombre del behaviour y botón de eliminar.
- Controles generados dinámicamente por reflexión leyendo las propiedades marcadas con `[EditorProperty]`.
- Los controles varían según el tipo de la propiedad (ver tabla más abajo).

**5. Botón "Add Behaviour"** (al final del inspector):
- Abre `AddBehaviourDialog` para seleccionar un `GameBehaviour` disponible.

### Tipos de control por tipo de propiedad

| Tipo C# | Control en el inspector |
|---------|------------------------|
| `float`, `int` | `NumericUpDown` |
| `bool` | `CheckBox` |
| `string` | `TextBox` |
| `Vector2` | Dos `NumericUpDown` (X e Y) en la misma fila |
| `Vector3` | Tres `NumericUpDown` (X, Y, Z) en la misma fila |
| `Color` | Rectángulo de color + botón `...` que abre `ColorDialog` |
| `enum` (sin flags) | `ComboBox` con los valores del enum |
| `enum` con `[Flags]` | `CheckedListBox` con todos los valores marcables |
| Referencia a asset | TextBox (solo lectura) + botón `...` que abre `OpenFileDialog` |

---

## Panel: Asset Browser (`AssetBrowserPanel`)

Explorador de archivos del proyecto enfocado en assets de juego.

### Controles

- **Barra superior**: botones Import, Refresh, New Folder; campo de filtro de texto con debounce de 150ms; toggle entre vista lista y vista iconos grandes.
- **Breadcrumb**: muestra la ruta actual con enlaces clicables para navegar.
- **Panel izquierdo**: árbol de carpetas.
- **Panel derecho**: lista de assets de la carpeta seleccionada.

### Tipos de asset reconocidos

| Extensión | Tipo |
|-----------|------|
| `.png`, `.jpg`, `.bmp` | Texture |
| `.wav`, `.ogg`, `.mp3` | Audio |
| `.ttf`, `.spritefont` | Font |
| `.tmx` | TiledMap |
| `.scene.json` | Scene |
| `.prefab.json` | Prefab |
| `.particles.json` | Particles |
| `.anim.json` | Animation |
| `.input.json` | InputMap |
| `.cs` | Script |

### Menú contextual

| Opción | Acción |
|--------|--------|
| Open with External Editor | Abre el archivo con el programa predeterminado |
| Reveal in Explorer | Abre el explorador de Windows con el archivo seleccionado |
| Rename | Renombra el archivo en línea |
| Delete | Elimina el archivo (con confirmación) |
| Copy Relative Path | Copia la ruta relativa al portapapeles |

Los archivos `.Generated.cs` muestran un ícono especial y un tooltip indicando que son auto-generados.

### ContentWatcher

En segundo plano, `ContentWatcher` monitorea la carpeta `Content` con `FileSystemWatcher`. Cuando detecta un archivo nuevo o modificado, publica `AssetImportedEvent` para que el Asset Browser refresque automáticamente.

---

## Panel: Consola (`ConsolePanel`)

Muestra mensajes del editor, salida de compilación y logs del juego.

### Controles

- **Barra superior**: botón Clear, botón Copy, desplegable de filtro (All / Debug / Info / Warning / Error).
- **RichTextBox**: fondo oscuro, fuente Consolas 9pt, solo lectura.

### Colores por nivel

| Nivel | Color |
|-------|-------|
| Debug | Gris |
| Info | Color del sistema (blanco en modo oscuro) |
| Warning | Dorado |
| Error | Rojo (y negrita) |

### Detección de patrones MSBuild

La consola reconoce automáticamente la salida de MSBuild:
- Líneas con `error CS` → se muestran como `Error`
- Líneas con `warning CS` → se muestran como `Warning`
- `Build succeeded` → verde
- `Build FAILED` → rojo

Formato de cada línea: `[HH:mm:ss] [NIVEL] mensaje`

---

## Panel: Gestor de escenas (`SceneManagerPanel`)

Lista todas las escenas del proyecto y permite gestionar su ciclo de vida.

### Controles

- **Barra de herramientas**: New Scene, Open Scene, Delete Scene.
- **ListView**: columnas "Name" y "Modified". La escena activa aparece en negrita.
- **Barra inferior**: contador de escenas.

### Comportamiento

- Se carga al recibir `ProjectOpenedEvent` escaneando `{EditorPath}/Scenes/*.scene.json`.
- Doble clic en una escena la carga (pregunta antes si hay cambios sin guardar).
- Si se elimina la escena activa, se cierra y la escena queda vacía.

---

## Panel: Editor de localización (`LocalizationBrowserPanel`)

Editor tabular para los archivos de localización del juego (`.json` en la carpeta `Localization`).

### Controles

- **Barra superior**: Add Key, Remove Key, Add Locale, Import .json, Export .csv, Save.
- **Campo de filtro**: filtra las claves visibles sin reconstruir la tabla.
- **DataGridView**: primera columna = clave (solo lectura), una columna por locale (editable).
- **Barra inferior**: contador de claves y locales.

### Comportamiento

- Se carga al recibir `ProjectOpenedEvent` leyendo todos los `.json` de la carpeta `Localization`.
- Cada edición de celda genera un `SetLocalizationValueCommand` (con soporte de undo/redo).
- El botón Save guarda todos los archivos de locale.

---

## Panel: Editor de Input Maps (`InputMapEditorPanel`)

Editor visual para configurar los bindings de entrada del juego.

### Layout

- **Panel izquierdo**: selector de archivo `.input.json`, árbol de acciones (actions). Barra con Add Action, Remove Action.
- **Panel derecho**: encabezado con el nombre de la acción seleccionada, tabla de bindings. Barra con Add Binding, Remove Binding.

### Columnas de la tabla de bindings

| Columna | Valores posibles |
|---------|-----------------|
| Device | Keyboard / Gamepad / Mouse |
| Key / Button | Valores del enum `Keys`, `Buttons` o `MouseButtons` según el device |

Al cambiar la columna Device en una fila, la columna Key/Button actualiza sus opciones automáticamente.

### Comportamiento

- Se escanea `GameSourcePath` buscando archivos `*.input.json` al abrir el proyecto.
- Cada operación (añadir/eliminar action o binding) usa comandos del CommandStack para undo/redo.

---

## Panel de tilemaps: Paleta (`TilemapPalettePanel`)

Panel especializado para edición de tilemaps importados desde archivos `.tmx` (formato Tiled).

### Controles

- Vista previa del tileset (cuadrícula de tiles).
- Selector de tile activo.
- Controles de zoom.
- Botones de modo: pintar o borrar.

### Comportamiento

- Aparece cuando se selecciona una entidad con un tilemap asociado.
- Al recibir `TilemapLayerSelectedEvent` muestra los tiles de ese tileset.
- Pintar un tile usa `PaintTileCommand`; borrar usa `EraseTileCommand`.

---

## Controles del viewport (`MonoGameControl` y `EditorCamera2D`)

El viewport es un control WinForms personalizado que embebe un `GraphicsDevice` de MonoGame para renderizar en tiempo real.

### MonoGameControl

- Publica el evento `RenderFrame` en cada frame.
- Propiedad `Camera`: instancia de `EditorCamera2D`.
- Propiedad `HandToolEnabled`: activa el modo de paneo con ratón.
- Propiedad `ClearColor`: color de fondo del viewport.

### EditorCamera2D

- `Pan(delta)`: mueve la cámara por el mundo.
- `Zoom`: nivel de zoom actual.
- `GetTransformMatrix(viewport)`: devuelve la matriz de transformación mundo→pantalla.
- La inversa permite convertir coordenadas de pantalla a coordenadas del mundo (necesario para gizmos y selección).

### Gizmos

Los gizmos son los controles visuales que aparecen sobre la entidad seleccionada para manipularla. Tienen dos capas:

- **`GizmoController`** (en Core): lógica pura. Detecta si el cursor está sobre un handle, gestiona el arrastre, calcula el nuevo valor.
- **`GizmoRenderer`** (en WinForms): rendering con GPU. Dibuja flechas, círculos, cuadrados y la cuadrícula.

### Modos de gizmo

| Modo | Tecla | Qué permite hacer |
|------|-------|-------------------|
| Select | Q | Solo seleccionar entidades |
| Move | W | Mover en X, Y, o ambos a la vez |
| Rotate | E | Rotar |
| Scale | R | Escalar en X, Y, o uniformemente |
| Rect | T | Visualizar bounding box (no edita) |

En modo Move con vista 2.5D activa, aparece además un handle azul para modificar la profundidad Z.
