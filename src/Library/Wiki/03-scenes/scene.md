# Scene

**Namespace:** `Alca.MonoGame.Kernel.Scenes`
**Implementa:** `IDisposable`

`Scene` es la clase base abstracta para todas las pantallas del juego. Proporciona un `ContentManager` propio (que se descarga automáticamente al salir de la escena), integración opcional con `GameWorld` y un `UIRoot` opcional.

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Content` | `ContentManager` | Cargador de assets propio de esta escena |
| `World` | `GameWorld?` | Mundo ECS; `null` hasta que `CreateWorld` retorna uno |
| `UIRoot` | `UIRoot?` | Raíz del árbol UI; `null` hasta llamar `EnableUI()` |
| `IsDisposed` | `bool` | `true` si la escena ya ha sido descargada |
| `IsOverlay` | `bool` | `virtual`, defecto `false`; cuando `true` la escena bajo el overlay sigue visible |

---

## Ciclo de vida (hooks virtuales)

### Orden de ejecución en `Initialize()`

```
PreInitialize()
  └─> CreateWorld()       → asigna World
  └─> InitializeWorld()
  └─> LoadContent()
  └─> PostInitialize()
        └─> InitializeUI()
```

### `PreInitialize()`

Se llama **antes** de crear el mundo. Es el lugar correcto para llamar a `EnableUI()`:

```csharp
protected override void PreInitialize()
{
    EnableUI(); // crea UIRoot con tamaño de resolución virtual
}
```

### `CreateWorld()`

Sobreescribe para crear y configurar el `GameWorld`. Retorna `null` (defecto) para escenas sin ECS.

```csharp
protected override GameWorld? CreateWorld()
{
    return new GameWorld
    {
        PhysicsWorld = new Physics2DWorld(new Vector2(0, 600f)),
        LightingWorld = new LightingWorld()
    };
}
```

### `InitializeWorld()`

Se llama después de asignar `World`. Aquí se crean las entidades iniciales.

```csharp
protected override void InitializeWorld()
{
    var player = World!.CreateEntity("Player", new Vector2(400, 300));
    player.Add(new SpriteRendererBehaviour(Content.Load<Texture2D>("player")));
    player.AddComponent<PlayerController>();
}
```

### `LoadContent()`

Carga todos los assets. Usa `Content.Load<T>()` (no `Core.Content`) para que se descarguen al finalizar la escena.

```csharp
public override void LoadContent()
{
    _playerTexture = Content.Load<Texture2D>("Sprites/player");
    _font          = Content.Load<SpriteFont>("Fonts/ui");
    _music         = Content.Load<Song>("Audio/gameplay_theme");
}
```

### `PostInitialize()` / `InitializeUI()`

`PostInitialize` llama a `InitializeUI`. Sobreescribe `InitializeUI` para construir el árbol de UI:

```csharp
protected override void InitializeUI()
{
    var hud = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
    UIRoot!.Add(hud);

    hud.Add(new Label { Text = "Vida:", Font = _font });
    _healthBar = new ProgressBar { MinValue = 0, MaxValue = 100, Value = 100 };
    hud.Add(_healthBar);
}
```

### `Update(GameTime)` y `Draw(GameTime)`

La implementación base llama a `World?.Update(gameTime)` y `World?.Draw(...)` + `UIRoot?.DrawAll(...)`. Si sobreescribes, llama a `base`:

```csharp
public override void Update(GameTime gameTime)
{
    base.Update(gameTime); // actualiza el mundo ECS

    // lógica adicional de escena
    if (_gameOver)
        Core.SceneManager.RequestChange(new GameOverScene());
}
```

### `UnloadContent()`

La implementación base llama a `World?.Destroy()` y `Content.Unload()`. Se llama automáticamente al cambiar de escena.

---

## `EnableUI()`

Crea un `UIRoot` de pantalla completa (tamaño = resolución virtual si `ResolutionManager` está activo). Idempotente — se puede llamar varias veces sin efecto duplicado.

```csharp
protected override void PreInitialize()
{
    EnableUI();
    // UIRoot ya está disponible a partir de aquí
}
```

---

## Ejemplo completo: GameplayScene

```csharp
using Alca.MonoGame.Kernel.Scenes;
using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Physics;
using Alca.MonoGame.Kernel.UI.Controls;
using Alca.MonoGame.Kernel.UI.Layout;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class GameplayScene : Scene
{
    private Texture2D _playerTex = null!;
    private Texture2D _groundTex = null!;
    private SpriteFont _font = null!;
    private ProgressBar _healthBar = null!;

    protected override void PreInitialize()
    {
        EnableUI();
    }

    protected override GameWorld? CreateWorld()
    {
        return new GameWorld
        {
            PhysicsWorld    = new Physics2DWorld(new Vector2(0, 600f)),
            AudioController = Core.Audio,
            AudioMixer      = new AudioMixer()
        };
    }

    protected override void InitializeWorld()
    {
        // Suelo
        var ground = World!.CreateEntity("Ground", new Vector2(400, 550));
        ground.Add(new SpriteRendererBehaviour(_groundTex));
        ground.Add(new RigidBody2D { IsStatic = true });
        ground.Add(new BoxCollider2D { Width = 800, Height = 32 });

        // Jugador
        var player = World.CreateEntity("Player", new Vector2(400, 300));
        player.Add(new SpriteRendererBehaviour(_playerTex));
        player.Add(new RigidBody2D { Mass = 1f });
        player.Add(new BoxCollider2D { Width = 32, Height = 48 });
        player.AddComponent<PlayerController>();
    }

    public override void LoadContent()
    {
        _playerTex = Content.Load<Texture2D>("Sprites/player");
        _groundTex = Content.Load<Texture2D>("Sprites/ground");
        _font      = Content.Load<SpriteFont>("Fonts/hud");
    }

    protected override void InitializeUI()
    {
        // HUD superior
        var hud = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        hud.Bounds = new Rectangle(10, 10, 300, 30);
        UIRoot!.Add(hud);

        hud.Add(new Label { Text = "HP", Font = _font, Color = Color.White });
        _healthBar = new ProgressBar { Value = 100 };
        hud.Add(_healthBar);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Sincronizar barra de vida
        var player = World?.FindByName("Player");
        if (player is not null && player.TryGetComponent<HealthComponent>(out var health))
            _healthBar.Value = health!.CurrentHp;
    }
}
```

---

## Implementar una escena de overlay (pausa)

```csharp
public sealed class PauseMenuScene : Scene
{
    // IsOverlay = true → la escena de gameplay sigue dibujándose debajo
    public override bool IsOverlay => true;

    protected override void PreInitialize() => EnableUI();

    protected override void InitializeUI()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12
        };
        UIRoot!.Add(panel);

        var resumeBtn = new Button(null, "Reanudar");
        resumeBtn.Clicked += () => Core.SceneManager.PopScene();
        panel.Add(resumeBtn);

        var exitBtn = new Button(null, "Salir al menú");
        exitBtn.Clicked += () => Core.SceneManager.RequestChange(new MainMenuScene());
        panel.Add(exitBtn);
    }
}
```

---

## Notas

- El `Content` de la escena es **diferente** al `Core.Content` global. Los assets cargados aquí se descargan solos al hacer `Dispose`.
- Llama siempre a `base.Update(gameTime)` y `base.Draw(gameTime)` al sobreescribir para que `World` y `UIRoot` se actualicen.
- `Initialize()` es llamado automáticamente por `SceneManager` — no lo llames manualmente.

---

## Ver también

- [SceneManager →](scene-manager.md)
- [GameWorld →](../02-ecs/game-world.md)
- [Scenes Overview →](overview.md)
