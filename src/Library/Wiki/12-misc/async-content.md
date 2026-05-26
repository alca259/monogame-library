# Carga Asíncrona de Contenido

**Namespace:** `Alca.MonoGame.Kernel.Content`

El sistema de carga asíncrona permite cargar assets en un hilo de fondo con seguimiento de progreso, evitando que la pantalla se congele durante la carga.

---

## AsyncContentLoader

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `MaxAssetsPerFrame` | `int` | Número máximo de assets a procesar por frame al hacer flush |
| `Token` | `CancellationToken` | Token de cancelación interno |

### Métodos

| Método | Descripción |
|---|---|
| `LoadAsync<T>(assetName, progress, ct)` | Inicia la carga de un asset; devuelve `Task<T>` |
| `FlushPending(content)` | Aplica al `ContentManager` los assets ya cargados en background |
| `Cancel()` | Cancela todas las cargas en curso |
| `Dispose()` | Cancela y libera recursos |

---

## ContentGroupBuilder

API fluida para definir grupos de assets a cargar juntos.

```csharp
var group = new ContentGroupBuilder()
    .Add<Texture2D>("Textures/player")
    .Add<Texture2D>("Textures/enemies")
    .Add<SpriteFont>("Fonts/main")
    .Add<Effect>("Effects/outline")
    .AddRange<SoundEffect>(["Audio/jump", "Audio/coin", "Audio/hurt"])
    .Build();
```

---

## ContentLoadGroup

Ejecuta la carga de todos los assets del grupo con un único `await`.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Count` | `int` | Número total de assets en el grupo |

### Métodos

```csharp
await group.LoadAllAsync(loader, progress, ct);
```

---

## Ejemplo: pantalla de loading con barra de progreso

```csharp
public sealed class LoadingScene : Scene
{
    private AsyncContentLoader _loader = null!;
    private ContentLoadGroup _group = null!;
    private ProgressBar _progressBar = null!;

    protected override void InitializeUI()
    {
        _progressBar = new ProgressBar
        {
            Value      = 0f,
            FillColor  = Color.DeepSkyBlue,
            ColorGradient = false,
            Pixel      = _pixel
        };
        UIRoot!.Add(_progressBar);
    }

    public override void PostInitialize()
    {
        base.PostInitialize();
        _loader = new AsyncContentLoader { MaxAssetsPerFrame = 3 };

        _group = new ContentGroupBuilder()
            .Add<Texture2D>("Textures/level1_tileset")
            .Add<Texture2D>("Textures/enemies_atlas")
            .Add<TiledMap>("Maps/level1")
            .Add<Song>("Audio/Music/level1")
            .Build();

        var progress = new Progress<float>(p => _progressBar.Value = p);
        _ = LoadGameAsync(progress);
    }

    private async Task LoadGameAsync(IProgress<float> progress)
    {
        await _group.LoadAllAsync(_loader, progress, default);
        Core.SceneManager.RequestChange(new GameplayScene());
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _loader.FlushPending(Content);  // aplica los assets listos al ContentManager
    }

    public override void UnloadContent()
    {
        base.UnloadContent();
        _loader.Dispose();
    }
}
```

---

## Notas

- `FlushPending` debe llamarse en el hilo principal (en `Update`) para que el `ContentManager` aplique los assets correctamente.
- `MaxAssetsPerFrame` controla cuántos assets se aplican por frame; valores bajos (1–3) suavizan el framerate durante la carga.
- Llama a `Cancel()` antes de `Dispose()` si la escena de carga puede cerrarse antes de terminar.

---

## Ver también

- [Escenas →](../03-scenes/scene.md)
- [Persistencia →](persistence.md)
