# Atajos de teclado y referencia rápida

---

## Atajos globales del editor

| Atajo | Acción |
|-------|--------|
| `Ctrl+Z` | Deshacer (Undo) |
| `Ctrl+Y` | Rehacer (Redo) |
| `Ctrl+S` | Guardar escena |
| `Ctrl+Shift+S` | Guardar escena como... |
| `Ctrl+G` | Generar código de la escena activa |
| `Ctrl+B` | Compilar el juego |
| `Ctrl+F5` | Ejecutar el juego |

---

## Atajos del viewport (cuando el foco está en el viewport)

### Modos de gizmo

| Tecla | Modo | Descripción |
|-------|------|-------------|
| `Q` | Select | Solo seleccionar entidades (sin handles de transform) |
| `W` | Move | Mover entidad. Handles: flecha roja (X), verde (Y), cuadrado central (XY) |
| `E` | Rotate | Rotar entidad. Handle: círculo amarillo |
| `R` | Scale | Escalar entidad. Handles: cuadrados rojos/verdes + cuadrado central (uniforme) |
| `T` | Rect | Bounding box punteado (solo visualización, no edita) |

### Herramientas del viewport

| Tecla | Acción |
|-------|--------|
| `G` | Activar/desactivar overlay de cuadrícula |
| `H` | Activar/desactivar herramienta de mano (paneo con ratón) |

### Modificadores durante drag de gizmo

| Tecla | Efecto |
|-------|--------|
| `Ctrl` (al soltar) | Snapping: redondea la posición final al tamaño de celda de la cuadrícula |

### Navegación de la cámara del editor

| Acción del ratón | Efecto |
|-----------------|--------|
| Rueda del ratón | Zoom in / Zoom out |
| Clic central + arrastrar | Paneo de la cámara |
| Con `H` activo: clic izquierdo + arrastrar | Paneo de la cámara |

---

## Atajos en la jerarquía de escena

| Tecla / Acción | Efecto |
|----------------|--------|
| `F2` | Renombrar la entidad seleccionada en línea |
| `Delete` | Eliminar la entidad seleccionada |
| `Clic` | Seleccionar entidad |
| `Drag & Drop` | Reparentar: arrastar nodo sobre otro para cambiar su padre |

---

## Acciones de los menús principales

### Menú File

| Opción | Atajo | Acción |
|--------|-------|--------|
| New Project | — | Crea un proyecto nuevo |
| Open Project | — | Abre un proyecto existente |
| New Scene | — | Crea una escena nueva |
| Open Scene | — | Abre una escena existente |
| Save Scene | `Ctrl+S` | Guarda la escena actual |
| Save Scene As | `Ctrl+Shift+S` | Guarda la escena con nuevo nombre/ubicación |
| Recent Projects | — | Lista de proyectos recientes |

### Menú Project

| Opción | Atajo | Acción |
|--------|-------|--------|
| Build Game | `Ctrl+B` | Compila el proyecto de juego |
| Run Game | `Ctrl+F5` | Ejecuta el juego en un proceso externo |
| Generate Scene Code | `Ctrl+G` | Genera `.Generated.cs` de la escena activa |
| Generate All Scenes | — | Genera código para todas las escenas del proyecto |
| Rescan Behaviours | — | Rescana la DLL/fuente buscando GameBehaviours disponibles |
| New Behaviour... | — | Abre el diálogo para crear un nuevo behaviour |
| Project Settings... | — | Abre la configuración del proyecto |

### Menú Scene

| Opción | Acción |
|--------|--------|
| Configure World Subsystems... | Abre el diálogo de configuración de Physics, Lighting, Navigation y Audio |

### Menú View

| Opción | Acción |
|--------|--------|
| Hierarchy | Mostrar/ocultar panel de jerarquía |
| Inspector | Mostrar/ocultar panel de inspector |
| Asset Browser | Mostrar/ocultar panel de assets |
| Console | Mostrar/ocultar panel de consola |
| Scene Manager | Mostrar/ocultar panel de gestión de escenas |
| Localization | Mostrar/ocultar panel de localización |
| Input Map Editor | Mostrar/ocultar panel de editor de input maps |
| Tilemap Palette | Mostrar/ocultar panel de paleta de tilemaps |
| Undo History | Mostrar/ocultar panel de historial de deshacer |
| Scripts | Mostrar/ocultar explorador de scripts |
| *(separador)* | — |
| Reset Layout | Restaura la disposición y visibilidad de paneles por defecto |

---

## Botones de la barra de herramientas principal (ToolStrip)

| Botón | Acción |
|-------|--------|
| ▶ Play | Entra en modo Play |
| ⏹ Stop | Sale del modo Play y restaura la escena |

Los botones Play/Stop cambian de color de acento cuando están activos.

---

## Menú contextual del inspector (secciones de behaviour)

| Acción | Cómo acceder |
|--------|-------------|
| Colapsar/Expandir sección | Clic en el encabezado con el chevron `▼`/`▶` |
| Eliminar behaviour | Botón `✕` en el encabezado de la sección |

---

## Referencia rápida: flujos más habituales

### Crear una escena y poblarla

1. `File → New Scene` → nombre y tamaño.
2. Jerarquía `+` → crear entidades.
3. Seleccionar entidad → Inspector → `+ Add Behaviour` → seleccionar tipo.
4. Editar propiedades en el inspector.
5. `Ctrl+S` para guardar.

### Probar el juego en el editor

1. Tener la escena guardada.
2. Clic en ▶ **Play** — compila `GameApp.csproj` y lanza `GameApp.exe --scene {ruta}`.
3. Para terminar: clic en ⏹ **Stop** (termina el proceso externo).

### Generar código y compilar

1. `Ctrl+G` (generar código de la escena).
2. `Ctrl+B` (compilar el juego).
3. Si hay errores, se ven en la consola con color rojo.
4. Después del build, el editor rescana los behaviours automáticamente.

### Añadir localización

1. Panel **Localization** (parte inferior).
2. `Add Key` → nombre de la clave.
3. Editar valores por locale en la tabla.
4. Botón `Save` para guardar los `.json`.

### Configurar inputs

1. Panel **Input Map Editor** (parte inferior).
2. Seleccionar el archivo `.input.json` del selector.
3. `Add Action` → nombre de la acción.
4. Seleccionar la acción → `Add Binding` → elegir Device y tecla/botón.

---

## Resumen de preferencias persistidas

Las siguientes preferencias se guardan automáticamente en `AppData\MonogameEditor\preferences.json` y se restauran al reabrir el editor:

| Preferencia | Descripción |
|-------------|-------------|
| Ancho del panel izquierdo | Tamaño del panel de jerarquía |
| Ancho del panel derecho | Tamaño del panel del inspector |
| Alto del panel inferior | Tamaño del área de consola/assets |
| Visibilidad de paneles | Qué paneles están visibles |
| `AssetBrowserSplitterDistance` | División entre árbol y lista en el Asset Browser |
| `RecentProjects` | Lista de los últimos proyectos abiertos |
| `LastProjectPath` | Último proyecto abierto (para auto-cargar al iniciar) |
| `BehaviourSectionCollapsed` | Qué secciones de behaviour están colapsadas en el inspector |
