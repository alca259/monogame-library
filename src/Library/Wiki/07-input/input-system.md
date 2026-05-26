# Sistema de Input

**Namespace:** `Alca.MonoGame.Kernel.Input`

El sistema de input abstrae teclado, ratón y gamepad en acciones remap eables. Las acciones se agrupan en `InputActionMap` y se cargan/descargan en el `InputManager`.

---

## Clases principales

| Clase | Descripción |
|---|---|
| `InputManager` | Punto de entrada global (`Core.Input`) |
| `InputAction` | Acción lógica (saltar, atacar…) con bindings múltiples |
| `InputActionMap` | Colección de acciones para un contexto (gameplay, menú…) |
| `InputBinding` | Enlace entre acción y tecla/botón/ratón concretos |
| `KeyboardInfo` | Snapshot de teclado con consultas pressed/held/released |
| `MouseInfo` | Snapshot de ratón con posición, delta y botones |
| `InputSerializer` | Guarda y carga mapas de input como JSON |

---

## InputManager

Disponible como `Core.Input`.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Keyboard` | `KeyboardInfo` | Estado del teclado |
| `Mouse` | `MouseInfo` | Estado del ratón |
| `GamePads` | `GamePadInfo[4]` | Estados de hasta 4 gamepads |
| `MousePosition` | `Vector2` | Posición del ratón cacheada (sin allocation) |

### Métodos

| Método | Descripción |
|---|---|
| `LoadMap(map)` | Activa un `InputActionMap`; sus acciones se actualizan cada frame |
| `UnloadMap()` | Desactiva el mapa actual |
| `IsKeyPressed(key)` | `true` el frame en que la tecla pasa de up a down |
| `IsKeyHeld(key)` | `true` mientras la tecla esté pulsada |
| `IsKeyReleased(key)` | `true` el frame en que la tecla pasa de down a up |
| `Update(gameTime)` | Actualiza todos los dispositivos y el mapa activo |

---

## InputAction

Una acción lógica que puede mapearse a múltiples dispositivos.

### Constructor

```csharp
new InputAction(string name,
                Keys[]?        keys         = null,
                Buttons[]?     padButtons   = null,
                MouseButton[]? mouseButtons = null)
```

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Name` | `string` | Identificador único |
| `IsPressed` | `bool` | `true` el frame en que se activa (up → down) |
| `IsReleased` | `bool` | `true` el frame en que se desactiva (down → up) |
| `IsHeld` | `bool` | `true` mientras esté activa |

---

## InputActionMap

Agrupa acciones para un contexto de juego.

### Métodos

| Método | Descripción |
|---|---|
| `Register(action)` | Añade una acción (reemplaza si existe con el mismo nombre) |
| `Unregister(name)` | Elimina una acción por nombre |
| `Get(name)` | Devuelve la acción o `null` |
| `GetAllActions()` | Vista de sólo lectura de todas las acciones |

---

## KeyboardInfo y MouseInfo

Wrappers sobre `KeyboardState` y `MouseState` con comparación de frames.

### KeyboardInfo

```csharp
kb.WasKeyJustPressed(Keys.Space)   // pressed este frame
kb.WasKeyJustReleased(Keys.Space)  // released este frame
kb.IsKeyDown(Keys.Left)            // held down
```

### MouseInfo

```csharp
mouse.WasButtonJustPressed(MouseButton.Left)
mouse.Position        // Point en pantalla
mouse.PositionDelta   // Delta desde el frame anterior
mouse.ScrollWheelDelta
```

---

## InputBinding

Enlace entre una acción y un input físico.

```csharp
public readonly struct InputBinding
{
    public DeviceType DeviceType { get; init; }
    public int        Code       { get; init; }   // castable a Keys / Buttons / MouseButton
    public string     ToDisplayString();           // "Space", "A (Gamepad)", "Left"
}
```

---

## InputSerializer

Guarda y carga mapas de input en disco como JSON.

```csharp
// Guardar
await inputSerializer.Save(gameplayMap, "Saves/keybindings.json");

// Cargar y aplicar
var loadedMap = await inputSerializer.Load("Saves/keybindings.json");
Core.Input.LoadMap(loadedMap);
```

---

## Ejemplo completo: controles del juego con serialización

```csharp
// Definición de acciones
public static class GameActions
{
    public const string Jump   = "Jump";
    public const string Attack = "Attack";
    public const string MoveLeft  = "MoveLeft";
    public const string MoveRight = "MoveRight";
}

// Creación del mapa
public static InputActionMap CreateDefaultMap()
{
    var map = new InputActionMap();

    map.Register(new InputAction(GameActions.Jump,
        keys:       [Keys.Space, Keys.Up],
        padButtons: [Buttons.A]));

    map.Register(new InputAction(GameActions.Attack,
        keys:       [Keys.Z],
        padButtons: [Buttons.X]));

    map.Register(new InputAction(GameActions.MoveLeft,
        keys:       [Keys.Left, Keys.A],
        padButtons: [Buttons.DPadLeft]));

    map.Register(new InputAction(GameActions.MoveRight,
        keys:       [Keys.Right, Keys.D],
        padButtons: [Buttons.DPadRight]));

    return map;
}

// En la escena de gameplay
public sealed class GameplayScene : Scene
{
    private InputActionMap _inputMap = null!;
    private readonly InputSerializer _serializer = new();

    public override async Task LoadContentAsync()
    {
        // Intentar cargar keybindings guardados, o usar los por defecto
        try
        {
            _inputMap = await _serializer.Load("Saves/keybindings.json");
        }
        catch
        {
            _inputMap = CreateDefaultMap();
        }
        Core.Input.LoadMap(_inputMap);
    }

    public override void UnloadContent()
    {
        base.UnloadContent();
        Core.Input.UnloadMap();
    }
}

// En el PlayerController (GameBehaviour)
public override void Update(GameTime gameTime)
{
    var jump   = _inputMap.Get(GameActions.Jump)!;
    var left   = _inputMap.Get(GameActions.MoveLeft)!;
    var right  = _inputMap.Get(GameActions.MoveRight)!;

    if (jump.IsPressed)
        _rb.LinearVelocity = new Vector2(_rb.LinearVelocity.X, -_jumpForce);

    float dir = 0;
    if (left.IsHeld)  dir -= 1f;
    if (right.IsHeld) dir += 1f;
    _rb.LinearVelocity = new Vector2(dir * _speed, _rb.LinearVelocity.Y);
}
```

---

## Notas

- `InputManager.Update` es llamado automáticamente por el `Core`; no lo llames manualmente.
- Las acciones del mapa activo se actualizan zero-alloc con iteración indexada.
- `InputBinding.ToDisplayString()` es útil para mostrar hints de controles en UI.

---

## Ver también

- [Core →](../01-core/core.md)
- [UI Controls →](../05-ui/controls.md)
