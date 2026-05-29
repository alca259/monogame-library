# Alca.MonoGame.Kernel

**Framework de juegos 2D/3D para MonoGame — .NET 10 · C# 14**

`Alca.MonoGame.Kernel` es una librería de alto nivel construida sobre MonoGame que proporciona todos los sistemas necesarios para desarrollar juegos completos: ECS, física, navegación, audio espacial, UI, networking, iluminación dinámica y mucho más. Está diseñada con un enfoque estricto en rendimiento (zero-alloc en el game loop) y arquitectura limpia (DI, patrones ECS, sealed classes).

---

## Requisitos

| Componente | Versión mínima |
|---|---|
| .NET | 10.0 |
| MonoGame.Framework.DesktopGL | 3.8.x |
| MonoGame.Extended | 6.0.x |
| Aether.Physics2D.MG | 2.2.x |
| LiteNetLib | 1.3.x |
| Microsoft.Extensions.DependencyInjection | 10.0.x |
| Microsoft.Extensions.Localization | 10.0.x |

---

## Inicio rápido

### 1. Crear el juego

Hereda de `Core` en lugar de `Game`:

```csharp
using Alca.MonoGame.Kernel;
using Alca.MonoGame.Kernel.Scenes;

public sealed class MyGame : Core
{
    public MyGame()
        : base(title: "Mi Juego", width: 1280, height: 720, fullScreen: false)
    {
    }

    protected override void PostInitialize()
    {
        // Carga la primera escena al arrancar
        SceneManager.RequestChange(new MainMenuScene());
    }
}
```

### 2. Punto de entrada

```csharp
using var game = new MyGame();
game.Run();
```

### 3. Crear una escena básica

```csharp
using Alca.MonoGame.Kernel.Scenes;
using Alca.MonoGame.Kernel.ECS;
using Microsoft.Xna.Framework;

public sealed class MainMenuScene : Scene
{
    protected override void PreInitialize()
    {
        EnableUI(); // Crea el UIRoot para esta escena
    }

    protected override GameWorld? CreateWorld()
    {
        return new GameWorld();
    }

    protected override void InitializeWorld()
    {
        var logo = World!.CreateEntity("Logo", new Vector2(640, 360));
        // Añadir behaviours...
    }

    protected override void LoadContent()
    {
        // Content.Load<Texture2D>("textura") — usa el ContentManager propio de la escena
    }
}
```

---

## Módulos disponibles

| Módulo | Descripción | Documentación |
|---|---|---|
| **Core** | Clase base `Core`, DI, ciclo de vida del juego | [01-core/core.md](01-core/core.md) |
| **ECS** | Entidades, componentes, `GameWorld`, jerarquías | [02-ecs/overview.md](02-ecs/overview.md) |
| **Scenes** | Gestor de escenas, transiciones fade, overlays | [03-scenes/overview.md](03-scenes/overview.md) |
| **Graphics 2D** | Cámaras, sprites, animación, partículas, post-proceso | [04-graphics/](04-graphics/) |
| **Graphics 3D** | Cámaras 3D, MeshRenderer, PrimitiveBatch | [04-graphics/rendering-3d.md](04-graphics/rendering-3d.md) |
| **UI** | 20+ controles, 5 layouts, foco, interacción | [05-ui/overview.md](05-ui/overview.md) |
| **Audio** | AudioMixer, audio 3D espacial, SoundEffectPool | [06-audio/overview.md](06-audio/overview.md) |
| **Input** | Action maps remapeables, teclado/ratón/gamepad | [07-input/input-system.md](07-input/input-system.md) |
| **Physics 2D** | Aether.Physics2D, RigidBody, Colliders, Joints | [08-physics/overview.md](08-physics/overview.md) |
| **Lighting 2D** | Luces CPU/GPU: Ambient, Directional, Point, Spot | [09-lighting/overview.md](09-lighting/overview.md) |
| **Navigation** | A* pathfinding, NavAgent, Steering Behaviors | [10-navigation/overview.md](10-navigation/overview.md) |
| **Network** | UDP cliente/servidor (LiteNetLib), NetFields | [11-network/overview.md](11-network/overview.md) |
| **Persistence** | Sistema de guardado con slots, async | [12-misc/persistence.md](12-misc/persistence.md) |
| **StateMachine** | FSM genérica zero-alloc | [12-misc/state-machine.md](12-misc/state-machine.md) |
| **Tweening** | Animaciones de propiedades, EasingCatalog | [12-misc/tweening.md](12-misc/tweening.md) |
| **EventBus** | Bus de eventos global con prioridades | [12-misc/event-bus.md](12-misc/event-bus.md) |
| **Mathematics** | MathUtils, BoundingHelpers, GeometryUtility | [12-misc/mathematics.md](12-misc/mathematics.md) |
| **Localization** | Multi-idioma con JSON, cambio en runtime | [12-misc/localization.md](12-misc/localization.md) |
| **Platform** | Detección de plataforma, lifecycle | [12-misc/platform.md](12-misc/platform.md) |
| **Debug** | DebugDraw, DebugOverlay, comandos | [12-misc/debug.md](12-misc/debug.md) |
| **AsyncContent** | Carga asíncrona de assets con progreso | [12-misc/async-content.md](12-misc/async-content.md) |
| **Timers** | TimerManager, cooldowns, respawns | [12-misc/timers.md](12-misc/timers.md) |
| **Weather** | WeatherWorld, perfiles, viento, rayos, audio | [13-weather/overview.md](13-weather/overview.md) |
| **Trigger Volumes** | Zonas de activación ligeras AABB/Circle sin motor de física | [08-physics/trigger-volumes.md](08-physics/trigger-volumes.md) |
| **Scene Transitions** | Transiciones enchufables: Fade, Slide, CircleWipe, Dissolve | [03-scenes/transitions.md](03-scenes/transitions.md) |
| **Procedural Noise** | Ruido Perlin/Simplex, NoiseMap, fBm | [12-misc/procedural-noise.md](12-misc/procedural-noise.md) |
| **Day/Night Cycle** | Ciclo día/noche con keyframes, TimeScale, eventos | [09-lighting/day-night.md](09-lighting/day-night.md) |
| **Shader Library 2D** | Outline, Flash, Dissolve, Glow, Silhouette, CRT | [04-graphics/shader-library.md](04-graphics/shader-library.md) |
| **Soporte 2.5D** | IsometricHelper, YSort, Billboard, NormalMap, IsometricCamera | [04-graphics/twopointfived.md](04-graphics/twopointfived.md) |
| **Dialogue / Narrative** | Sistema de diálogos y narrativa con TypewriterEffect | [14-dialogue/overview.md](14-dialogue/overview.md) |

---

## Principios de diseño

- **Zero-alloc en el game loop**: nunca se instancian clases en `Update`/`Draw`. Los pools y buffers se pre-asignan en `Initialize`/`LoadContent`.
- **Sealed by default**: todas las clases de implementación son `sealed` salvo que estén marcadas `abstract`.
- **Dependency Injection**: los servicios del kernel se registran automáticamente en un contenedor `Microsoft.Extensions.DependencyInjection`.
- **ECS puro**: la lógica se encapsula en `GameBehaviour`. Las entidades son contenedores de comportamientos, no jerarquías de herencia profundas.
- **Sin LINQ en hot paths**: `Update` y `Draw` usan `for`/`foreach` sobre arrays o `List<T>`.

---

## Índice completo

Consulta [INDEX.md](INDEX.md) para la lista detallada de todas las clases y su ubicación en la wiki.
