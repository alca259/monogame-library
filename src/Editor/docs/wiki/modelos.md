# Modelos de datos

Esta página describe todas las clases de dominio del editor: cómo representan la escena, las entidades, los behaviours y la configuración del proyecto.

---

## Escena: `EditorScene`

Representa una escena completa del juego tal como la gestiona el editor.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Name` | `string` | Nombre de la escena |
| `ScenePath` | `string` | Ruta absoluta al archivo `.scene.json`. Vacía si la escena no está guardada aún. |
| `WorldSize` | `EditorVector2` | Tamaño del mundo en píxeles (0,0 = sin límites) |
| `WorldConfig` | `EditorWorldConfig?` | Configuración de subsistemas (Physics, Lighting, Navigation, Audio). `null` si no hay subsistemas. |
| `RootGameObjects` | `List<EditorGameObject>` | Lista de entidades raíz (sin padre) de la escena |

**Serialización**: los campos se guardan en `.editor/scenes/{nombre}.scene.json`. Los vínculos de padre a hijo se reconstruyen automáticamente al deserializar (no se guardan en el JSON para evitar referencias circulares).

---

## Entidad: `EditorGameObject`

Representa una entidad del juego en el editor. Es el equivalente a un `GameEntity` del Kernel, pero en formato editable y serializable.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `Guid` | Identificador único estable (no cambia al renombrar) |
| `Name` | `string` | Nombre visible en la jerarquía |
| `Active` | `bool` | Si la entidad está activa |
| `Position` | `EditorVector2` | Posición en el mundo 2D (X, Y) |
| `PositionZ` | `float` | Profundidad para parallax / vista 2.5D |
| `Rotation` | `float` | Rotación en grados |
| `Scale` | `EditorVector2` | Escala del objeto |
| `Behaviours` | `List<EditorBehaviour>` | Componentes adjuntos |
| `Children` | `List<EditorGameObject>` | Entidades hijas |
| `Parent` | `EditorGameObject?` | Referencia al padre (`[JsonIgnore]`, se reconstruye al cargar) |
| `PrefabPath` | `string` | Ruta al prefab de origen. Vacía si no es una instancia de prefab. |
| `Tags` | `List<string>` | Etiquetas definidas por el usuario |

**Propiedades calculadas** (no serializadas):
- `LocalPosition` = posición relativa al padre.
- `LocalRotation` = rotación relativa al padre.
- `LocalScale` = escala relativa al padre.

---

## Behaviour: `EditorBehaviour`

Representa un componente adjunto a una entidad, en un formato editable.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `TypeName` | `string` | Nombre completo del tipo en el assembly (`AssemblyQualifiedName`). Ej: `"MiJuego.Behaviours.PlayerMovement, MiJuego"` |
| `Properties` | `Dictionary<string, JsonElement>` | Valores de las propiedades del behaviour, indexadas por nombre |
| `Enabled` | `bool` | Si el behaviour está habilitado |

Las propiedades del behaviour se guardan como `JsonElement` para poder serializar/deserializar cualquier tipo sin perder el dato. Cuando el inspector muestra los controles, lee y escribe en este diccionario.

---

## Vector: `EditorVector2`

Un `readonly record struct` con X e Y. Se usa en lugar de `Vector2` de MonoGame para que `Core` no dependa de ningún assembly de MonoGame.

```csharp
public readonly record struct EditorVector2(float X, float Y)
{
    public static EditorVector2 Zero => new(0, 0);
    public static EditorVector2 One  => new(1, 1);
}
```

---

## Configuración del mundo: `EditorWorldConfig`

Define los subsistemas opcionales que tendrá el `GameWorld` en runtime.

| Propiedad | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `UsePhysics2D` | `bool` | `false` | Activar Physics2DWorld |
| `GravityX` | `float` | `0` | Gravedad en X |
| `GravityY` | `float` | `-9.8` | Gravedad en Y |
| `UseLighting` | `bool` | `false` | Activar LightingWorld |
| `AmbientColorRgba` | `int[]` | `[0,0,0,255]` | Color ambiente (RGBA) |
| `UseNavigation` | `bool` | `false` | Activar NavGrid + Pathfinder |
| `NavGridWidth` | `int` | `32` | Ancho de la cuadrícula de navegación |
| `NavGridHeight` | `int` | `32` | Alto de la cuadrícula de navegación |
| `NavGridCellSize` | `float` | `32` | Tamaño de cada celda en píxeles |
| `NavGridOriginX/Y` | `float` | `0` | Origen de la cuadrícula |
| `UseAudio` | `bool` | `false` | Activar AudioController |

Si `WorldConfig` es `null` en la escena, se genera simplemente `new GameWorld()`.

---

## Proyecto: `EditorProject`

Representa el proyecto de juego cargado en el editor.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Name` | `string` | Nombre del proyecto |
| `RootPath` | `string` | Ruta absoluta a la carpeta raíz del proyecto |
| `BaseNamespace` | `string` | Namespace base para generación de código |
| `SolutionPath` | `string` | Ruta absoluta a `src/{Name}.slnx` |
| `EditorPath` | `string` | `{RootPath}/.editor/` |
| `ConfigPath` | `string` | `{EditorPath}/config/` — contiene `settings.json` |
| `LogsPath` | `string` | `{EditorPath}/logs/` |
| `ScenesPath` | `string` | `{EditorPath}/scenes/` |
| `PrefabsPath` | `string` | `{EditorPath}/prefabs/` |
| `ContentPath` | `string` | Configurable (por defecto `{RootPath}/src/GameApp/Content`) |
| `LocalizationPath` | `string` | Configurable (por defecto `{RootPath}/src/GameApp/i18n`) |
| `GameCsprojPath` | `string` | Ruta al `.csproj` del juego (apunta a `src/GameApp/GameApp.csproj` tras scaffolding) |

---

## Configuración del proyecto: `ProjectSettings`

Configuración adicional del proyecto. Se guarda en `.editor/config/settings.json`.

| Propiedad | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `RootNamespace` | `string` | `""` | Namespace raíz para la generación de código |
| `GeneratedCodeFolder` | `string` | `"Generated"` | Subcarpeta dentro de `GameSourcePath` para el código generado |
| `GenerateOnSave` | `bool` | `false` | Regenerar código automáticamente al guardar la escena |
| `DefaultLocale` | `string` | `"en-US"` | Locale por defecto |
| `SupportedLocales` | `List<string>` | `["en-US"]` | Lista de locales del juego |
| `BuildConfiguration` | `string` | `"Debug"` | Configuración de MSBuild (`Debug` / `Release`) |
| `GameAppCsprojRelPath` | `string` | `""` | Ruta relativa al `.csproj` del ejecutable |
| `GameScriptsCsprojRelPath` | `string` | `""` | Ruta relativa al `.csproj` de la librería de scripts |
| `ContentRelPath` | `string` | `"Content"` | Ruta relativa a la carpeta de Content |
| `LocalizationRelPath` | `string` | `"Localization"` | Ruta relativa a la carpeta de localización |

---

## Assets: `AssetInfo` y `AssetType`

`AssetInfo` es un record inmutable que describe un archivo en la carpeta Content.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `AbsolutePath` | `string` | Ruta absoluta al archivo |
| `RelativePath` | `string` | Ruta relativa a la carpeta Content |
| `Name` | `string` | Nombre del archivo sin extensión |
| `Type` | `AssetType` | Tipo del asset |
| `Extension` | `string` | Extensión del archivo |
| `SizeBytes` | `long` | Tamaño en bytes |

`AssetType` es un enum:

```
Unknown, Texture, Audio, Font, TiledMap,
Scene, Prefab, Particles, Animation, InputMap, Script
```

`AssetClassifier.Classify(path)` determina el tipo por extensión, usando comparación de sufijos compuestos (`.scene.json`, `.prefab.json`, etc.) antes de extensiones simples.

---

## Tilemaps: `EditorTilemapAsset`, `EditorTileLayer`, `EditorTileset`

Modelos de datos para trabajar con tilemaps importados desde archivos `.tmx` (formato Tiled).

### `EditorTilemapAsset`

| Propiedad | Descripción |
|-----------|-------------|
| `FilePath` | Ruta al archivo `.tmx` |
| `MapWidth`, `MapHeight` | Dimensiones del mapa en tiles |
| `TileWidth`, `TileHeight` | Tamaño de cada tile en píxeles |
| `Tilesets` | Array de `EditorTileset` |
| `Layers` | Array de `EditorTileLayer` |

`GetTilesetForGid(gid)`: busca qué tileset contiene el tile con ese ID global.

### `EditorTileLayer`

| Propiedad | Descripción |
|-----------|-------------|
| `Name` | Nombre de la capa |
| `Width`, `Height` | Dimensiones en tiles |
| Datos | Array de `int?` (null = celda vacía, int = GID del tile) |

Métodos: `GetTile(col, row)`, `SetTile(col, row, gid)`, `ToCsvData()`.

### `EditorTileset`

| Propiedad | Descripción |
|-----------|-------------|
| `FirstGid` | ID global del primer tile de este tileset |
| `Name` | Nombre del tileset |
| `ImagePath` | Ruta a la imagen del tileset |
| `TileWidth`, `TileHeight` | Tamaño de cada tile |
| `Columns`, `TileCount` | Organización de la imagen |

`GetTileSourceRect(localId)`: calcula el rectángulo fuente en la imagen del tileset para un tile dado su ID local.

---

## Input Maps: `InputEditorModel`, `InputActionEntry`, `InputBindingEntry`

Modelos para el editor de mapas de input, compatibles con el formato del Kernel.

### `InputEditorModel`

| Propiedad/Método | Descripción |
|-----------------|-------------|
| `FilePath` | Ruta al archivo `.input.json` |
| `Actions` | Lista de `InputActionEntry` |
| `LoadAsync()` | Deserializa desde disco |
| `SaveAsync()` | Serializa a disco |
| `AddAction(name)` | Añade una nueva action |
| `RemoveAction(name)` | Elimina una action |
| `AddBinding(actionName, binding)` | Añade un binding a una action |
| `RemoveBinding(actionName, binding)` | Elimina un binding |

### `InputActionEntry`

```
Name: string
Bindings: List<InputBindingEntry>
```

### `InputBindingEntry` (record)

```
DeviceType: string   (Keyboard / Gamepad / Mouse)
Code: int            (valor del enum Keys, Buttons, etc.)
```

---

## Localización: `LocalizationEditorModel`

Modelo para el editor de localización. Carga todos los archivos `.json` de la carpeta `Localization`.

| Método | Descripción |
|--------|-------------|
| `LoadAsync(localizationPath)` | Carga todos los `.json` de la carpeta |
| `SaveAsync()` | Guarda todos los archivos de locale |
| `GetValue(locale, key)` | Lee el valor de una clave para un locale |
| `SetValue(locale, key, value)` | Establece el valor |
| `AddKey(key)` | Añade una clave nueva a todos los locales |
| `RemoveKey(key)` | Elimina una clave de todos los locales |
| `AddLocale(locale)` | Añade un nuevo locale (con todas las claves vacías) |

---

## Registro de behaviours: `GameObjectRegistry`

Almacena los tipos de `GameBehaviour` disponibles para añadir a las entidades. Se carga al abrir un proyecto.

Fuentes de datos:
1. **Escaneo de assembly** (`GameBehaviourScanner.ScanAssemblyAsync(dllPath)`): reflexión sobre la DLL compilada del juego. Encuentra las subclases concretas de `GameBehaviour`.
2. **Escaneo de código fuente** (`GameBehaviourScanner.ScanSourceAsync(sourcePath)`): parseo de texto de los archivos `.cs`. Útil antes de compilar. Los tipos encontrados solo en source (no en assembly) se muestran en itálica en el diálogo.

El registro expone `IReadOnlyDictionary<string, TypeDescriptor>` donde la clave es el nombre completo del tipo.

### `TypeDescriptor`

```
FullName: string         (nombre completo del tipo, ej: "MiJuego.Behaviours.PlayerMovement")
ShortName: string        (nombre corto, ej: "PlayerMovement")
Namespace: string        (namespace del tipo)
```

---

## Atributo de propiedades editables: `[EditorProperty]`

Marca qué propiedades de un `GameBehaviour` son visibles y editables en el inspector del editor.

```csharp
public sealed class PlayerMovement : GameBehaviour
{
    [EditorProperty]
    public float Speed { get; set; } = 5f;

    [EditorProperty]
    public bool CanJump { get; set; } = true;

    // Esta propiedad NO aparece en el inspector (sin atributo):
    private float _currentVelocity;
}
```

El inspector usa reflexión para leer todas las propiedades con `[EditorProperty]` y crea el control correspondiente según el tipo. Los cambios se guardan en `EditorBehaviour.Properties` como `JsonElement`.
