# Paneles de la interfaz

El editor está compuesto por un formulario principal (`EditorForm`) y varios paneles que pueden mostrarse u ocultarse. La comunicación entre paneles ocurre exclusivamente a través del bus de eventos (ver [arquitectura](arquitectura.md)).

---

## Layout general del formulario principal

```
┌─────────────────────────────────────────────────────────┐
│  MenuStrip (Archivo, Proyecto, Escena, Vista, Ayuda)    │
│  ToolStrip (Play ▶ / Stop ⏹ + modos gizmo)              │
├──────────────┬──────────────────────────┬───────────────┤
│              │                          │               │
│  Jerarquía  │     Viewport             │  Inspector    │
│  de escena  │   (MonoGameControl)      │               │
│             │                          │               │
├──────────────┴──────────────────────────┴───────────────┤
│  Assets │ Consola │ Escenas │ Localización │ Scripts │ UI Theme ...│
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

Explorador de dos paneles para los assets del proyecto.

- **Panel izquierdo**: árbol de carpetas de `src/GameApp/Content/`.
- **Panel derecho**: lista de assets de la carpeta seleccionada con filtro de texto, toggle lista/iconos y breadcrumb de ruta.

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
| `.sprite.json` | Sprite |
| `.mat.json` | Material |
| `.uitheme.json` | UI Theme |
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

- Se carga al recibir `ProjectOpenedEvent` escaneando `{EditorPath}/scenes/*.scene.json`.
- Doble clic en una escena la carga (pregunta antes si hay cambios sin guardar).
- Si se elimina la escena activa, se cierra y la escena queda vacía.

---

## Panel: Explorador de scripts (`ScriptBrowserPanel`)

Explorador de dos paneles para los archivos de código del proyecto. Visible y oculto desde **Vista > Scripts**.

- **Panel izquierdo**: árbol de carpetas con carga perezosa (*lazy-load*) enraizado en `{RootPath}/src/GameScripts`.
- **Panel derecho**: lista de archivos `.cs` de la carpeta seleccionada (columnas: nombre y tamaño).
- **Botón "New Script"**: abre `ScriptCreationDialog` en la carpeta actualmente seleccionada (o en la raíz si ninguna está seleccionada).

---

## Panel: Editor de localización (`LocalizationBrowserPanel`)

Editor tabular para los archivos de localización del juego (`.json` en la carpeta `Localization`).

### Controles

- **Panel izquierdo**: árbol de carpetas con la estructura de la carpeta `Localization` del proyecto. Al seleccionar una carpeta, se recargan automáticamente los archivos de locale de esa carpeta.
- **Barra superior**: Add Key, Remove Key, Add Locale, Import .json, Export .csv, Save.
- **Campo de filtro**: filtra las claves visibles sin reconstruir la tabla.
- **DataGridView**: primera columna = clave (solo lectura), una columna por locale (editable).
- **Barra inferior**: contador de claves y locales.

### Comportamiento

- Se carga al recibir `ProjectOpenedEvent` construyendo el árbol de carpetas y seleccionando la raíz, lo que dispara la carga inicial de los archivos de locale.
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

## Panel: Editor de temas UI (`UIThemeInspectorPanel`)

Inspector para archivos de tema de interfaz (`.uitheme.json`). Se activa automáticamente cuando se selecciona un asset de tipo `UITheme` en el Asset Browser.

### Controles

- **Título**: muestra el nombre del tema en la parte superior.
- **Secciones por tipo de control** (Panel, Button, Dropdown, ProgressBar, TextBox), cada una con:
  - **Texture**: campo de texto con la ruta relativa a la textura nine-slice (sin extensión) + botón `...` que abre un `OpenFileDialog` filtrado a imágenes. La ruta se convierte automáticamente en relativa a `Content`.
  - **Left / Right**: par de `NumericUpDown` con el inset izquierdo y derecho (0–512 px).
  - **Top / Bottom**: par de `NumericUpDown` con el inset superior e inferior (0–512 px).
  - **Tile edges / Tile center**: dos checkboxes que controlan si los bordes y el centro se tileán en vez de estirarse.
- **Botón "Save .uitheme.json"**: serializa todos los valores a JSON y los escribe en el archivo del asset.

### Comportamiento

- Suscribe `AssetSelectedEvent`. Si el asset seleccionado no es `UITheme`, muestra el mensaje *"Select a UI theme asset (.uitheme.json) to edit it."* y oculta todos los controles.
- Al cargar un asset existente, deserializa el JSON con `System.Text.Json` y rellena los controles. Si el archivo no existe o está malformado, arranca con valores por defecto (`EditorUITheme.CreateEmpty()`).
- El panel no guarda automáticamente al editar — el usuario debe pulsar **Save** explícitamente.

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

El viewport es un control WinForms personalizado que embebe un `GraphicsDevice` de MonoGame para renderizar la escena en **modo edición**. Durante el modo Play, el juego se ejecuta como proceso externo (ver [modo-juego](modo-juego.md)) y el viewport no se usa.

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
