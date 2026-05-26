# Core — Clase base del juego

**Namespace:** `Alca.MonoGame.Kernel`
**Hereda de:** `Microsoft.Xna.Framework.Game`

`Core` es el punto de entrada de cualquier juego que use esta librería. Reemplaza a la clase `Game` de MonoGame y se encarga de inicializar automáticamente todos los servicios del kernel mediante un contenedor de inyección de dependencias (`Microsoft.Extensions.DependencyInjection`).

---

## Propiedades estáticas

Todos los servicios del kernel están disponibles como propiedades estáticas de `Core`, lo que facilita su acceso desde cualquier punto del juego:

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Instance` | `Core` | Instancia singleton del juego |
| `Graphics` | `GraphicsDeviceManager` | Control de presentación y resolución |
| `GraphicsDevice` | `GraphicsDevice` | Dispositivo GPU |
| `SpriteBatch` | `SpriteBatch` | Batch compartido para rendering 2D |
| `Content` | `ContentManager` | Cargador de assets global |
| `Input` | `InputManager` | Teclado, ratón y gamepads |
| `Audio` | `AudioController` | Reproducción de audio |
| `SceneManager` | `SceneManager` | Gestión de escenas y overlays |
| `Tweening` | `TweeningManager` | Animaciones de propiedades |
| `Localization` | `LocalizationManager` | Strings multi-idioma |
| `Resolution` | `ResolutionManager` | Resolución virtual y letterboxing |
| `Platform` | `PlatformManager` | Detección de plataforma |
| `UIInteraction` | `UIInteractionManager` | Hit testing de UI |
| `UIFocus` | `UIFocusManager` | Navegación por teclado/gamepad en UI |
| `UIOverlay` | `UIOverlayManager` | Elementos flotantes (dropdowns, tooltips) |
| `Timers` | `TimerManager` | Temporizadores de juego |
| `Window` | `GameWindow` | Ventana del juego (título, eventos de texto) |
| `ExitOnEscape` | `bool` | Si `true` (defecto), pulsar Escape cierra el juego |

---

## Constructor

```csharp
protected Core(string title, int width, int height, bool fullScreen)
```

| Parámetro | Descripción |
|---|---|
| `title` | Texto que aparece en la barra de título de la ventana |
| `width` | Ancho inicial de la ventana en píxeles |
| `height` | Alto inicial de la ventana en píxeles |
| `fullScreen` | `true` para arrancar en pantalla completa |

> Solo puede existir **una instancia** de `Core`. Crear una segunda lanza `InvalidOperationException`.

---

## Ciclo de vida / Hooks virtuales

El ciclo de arranque de `Core` sigue este orden:

```
Core.Initialize()
  └─> PreInitialize()           ← Override aquí para preparar antes de DI
  └─> base.Initialize()         ← MonoGame inicializa GraphicsDevice
  └─> Registra servicios del kernel
  └─> ConfigureServices(services) ← Override aquí para registrar tus propios servicios
  └─> Construye el contenedor DI
  └─> Resuelve y asigna todas las propiedades estáticas
  └─> PostInitialize()          ← Override aquí para cargar la primera escena
```

### `PreInitialize()`

Se llama antes de que MonoGame inicialice el `GraphicsDevice`. Úsalo para configuraciones que no requieren el GPU.

```csharp
protected override void PreInitialize()
{
    ExitOnEscape = false; // Deshabilitar salida con Escape
    IsFixedTimeStep = true;
    TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
}
```

### `ConfigureServices(IServiceCollection services)`

Punto de extensión para registrar servicios propios en el contenedor DI. Se llama después de que los servicios del kernel ya están registrados pero **antes** de construir el contenedor.

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<PlayerRepository>();
    services.AddSingleton<LevelManager>();
    services.AddSingleton<SaveSystem>();
}
```

### `PostInitialize()`

Se llama justo después de que el contenedor DI está construido y todos los servicios están disponibles. Es el lugar correcto para cargar la primera escena y realizar la configuración inicial.

```csharp
protected override void PostInitialize()
{
    // Cargar idioma inicial
    Localization.LoadLanguage("es-ES");

    // Configurar resolución virtual
    Resolution.SetVirtualResolution(1280, 720);

    // Cargar la primera escena
    SceneManager.RequestChange(new MainMenuScene());
}
```

---

## Método `GetService<T>()`

Permite resolver cualquier servicio registrado en el contenedor DI, incluyendo los propios:

```csharp
public static T GetService<T>() where T : notnull
```

```csharp
// Desde cualquier parte del código:
var playerRepo = Core.GetService<PlayerRepository>();
var levelManager = Core.GetService<LevelManager>();
```

---

## Game loop interno

`Core` gestiona el game loop. En cada frame, el orden de ejecución en `Update` es:

1. `Input.Update(gameTime)` — Captura el estado de todos los dispositivos
2. `Audio.Update()` — Limpia instancias de audio detenidas
3. `Tweening.Update(gameTime)` — Avanza tweens activos
4. `Timers.Update(gameTime)` — Dispara temporizadores vencidos
5. Comprueba `ExitOnEscape` (tecla Escape → `Exit()`)
6. Comprueba F11 para toggle fullscreen
7. `UIInteraction.Update(...)` — Hit testing del árbol UI activo
8. `SceneManager.Update(gameTime)` — Actualiza la escena/overlay activos

En `Draw`:

1. `SceneManager.Draw(gameTime)` — Dibuja scene base + overlays + fade

---

## Ejemplo completo

```csharp
using Alca.MonoGame.Kernel;
using Alca.MonoGame.Kernel.Scenes;
using Microsoft.Extensions.DependencyInjection;

public sealed class MyGame : Core
{
    public MyGame()
        : base("Mi Juego Épico", 1280, 720, fullScreen: false)
    {
    }

    protected override void PreInitialize()
    {
        ExitOnEscape = false;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // Servicios propios del juego
        services.AddSingleton<GameStateService>();
        services.AddSingleton<AchievementSystem>();
    }

    protected override void PostInitialize()
    {
        // Idioma por defecto
        Localization.LoadLanguage("es-ES");

        // Primera escena
        SceneManager.RequestChange(new SplashScene());
    }
}
```

Punto de entrada del programa:

```csharp
using var game = new MyGame();
game.Run();
```

---

## Atajos de teclado incorporados

| Tecla | Comportamiento |
|---|---|
| `Escape` | Cierra el juego si `ExitOnEscape = true` (defecto) |
| `F11` | Alterna entre modo ventana y pantalla completa |

---

## Notas importantes

- `Core` es una **clase `abstract`**: no se puede instanciar directamente.
- Solo puede existir **una instancia** durante toda la vida del proceso.
- Los servicios registrados en `ConfigureServices` son **singletons** por defecto. Asegúrate de que son thread-safe si los usas desde hilos de fondo.
- El `SpriteBatch` estático es compartido por todo el kernel. Si creas tu propio `SpriteBatch`, asegúrate de no interferir con los `Begin`/`End` del kernel.

---

## Ver también

- [Gestión de escenas →](../03-scenes/overview.md)
- [Resolución virtual →](../04-graphics/resolution.md)
- [Localización →](../12-misc/localization.md)
- [Temporizadores →](../12-misc/timers.md)
