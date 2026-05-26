# Camera2D

**Namespace:** `Alca.MonoGame.Kernel.Graphics.Camera`

`Camera2D` es la cámara ortográfica 2D del framework. Gestiona posición, zoom y rotación en el mundo, y proporciona la matriz de transformación necesaria para `SpriteBatch.Begin`. También ofrece conversión entre espacio de pantalla y espacio mundo, seguimiento suave de objetivos y confinamiento a límites.

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Position` | `Vector2` | Centro de la cámara en coordenadas mundo |
| `Zoom` | `float` | Nivel de zoom; clampeado entre `MinZoom` y `MaxZoom` |
| `Rotation` | `float` | Rotación de la cámara en radianes |
| `MinZoom` | `float` | Límite inferior del zoom (defecto: 0.1) |
| `MaxZoom` | `float` | Límite superior del zoom (defecto: 10) |

---

## Métodos principales

### `GetTransformMatrix(Viewport viewport)`

Retorna la matriz para pasar a `SpriteBatch.Begin`. La cámara recalcula la matriz solo cuando alguna propiedad ha cambiado (dirty flag).

```csharp
var transform = _camera.GetTransformMatrix(GraphicsDevice.Viewport);
spriteBatch.Begin(transformMatrix: transform);
// Dibuja sprites en coordenadas mundo...
spriteBatch.End();
```

### `ScreenToWorld(Vector2 screenPos, Viewport viewport)`

Convierte una posición en píxeles de pantalla a coordenadas mundo. Útil para el ratón:

```csharp
var mousePos = Core.Input.MousePosition;
var worldPos = _camera.ScreenToWorld(mousePos, GraphicsDevice.Viewport);
```

### `WorldToScreen(Vector2 worldPos, Viewport viewport)`

Convierte coordenadas mundo a píxeles de pantalla.

```csharp
var screenPos = _camera.WorldToScreen(enemy.Transform.Position2d, GraphicsDevice.Viewport);
```

### `Follow(Vector2 target, float lerpFactor)`

Mueve suavemente la cámara hacia el objetivo usando interpolación lineal. Llámalo cada frame.

```csharp
// Seguimiento suave (0.1 = lento, 1.0 = instantáneo)
_camera.Follow(_player.Transform.Position2d, lerpFactor: 0.1f);
```

### `ClampToBounds(Rectangle worldBounds)`

Evita que la cámara salga del área del nivel.

```csharp
_camera.ClampToBounds(new Rectangle(0, 0, 3200, 1800));
```

---

## Ejemplo completo: cámara que sigue al jugador

```csharp
public sealed class CameraController : GameBehaviour
{
    private Camera2D _camera = null!;
    private GameEntity? _player;

    private static readonly Rectangle WorldBounds = new(0, 0, 3200, 1800);

    public override void Awake()
    {
        _camera = new Camera2D
        {
            Zoom    = 1.5f,
            MinZoom = 0.5f,
            MaxZoom = 3.0f
        };
    }

    public override void Start()
    {
        _player = Entity.World.FindByName("Player");
    }

    public override void Update(GameTime gameTime)
    {
        if (_player is null) return;

        // Zoom con rueda del ratón
        float scroll = Core.Input.Mouse.ScrollWheelDelta;
        if (scroll != 0)
            _camera.Zoom += scroll * 0.001f;

        // Seguimiento suave
        _camera.Follow(_player.Transform.Position2d, lerpFactor: 0.08f);

        // Confinamiento al mundo
        _camera.ClampToBounds(WorldBounds);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // La cámara normalmente se aplica desde la Scene, no aquí
        // Este ejemplo muestra cómo obtener la matriz
        var matrix = _camera.GetTransformMatrix(Core.GraphicsDevice.Viewport);
        // Usado desde Scene.Draw
    }
}
```

Uso típico desde `Scene.Draw`:

```csharp
public override void Draw(GameTime gameTime)
{
    var viewport  = Core.GraphicsDevice.Viewport;
    var transform = _camera.GetTransformMatrix(viewport);

    Core.SpriteBatch.Begin(
        sortMode: SpriteSortMode.BackToFront,
        blendState: BlendState.AlphaBlend,
        samplerState: SamplerState.PointClamp,
        transformMatrix: transform);

    World!.Draw(gameTime, Core.SpriteBatch);

    Core.SpriteBatch.End();

    // UI (sin transformación de cámara)
    UIRoot?.DrawAll(Core.SpriteBatch);
}
```

---

## Con ResolutionManager

Combina la cámara con la resolución virtual para escalado correcto:

```csharp
var worldMatrix = Core.Resolution.WorldScaleMatrix * _camera.GetTransformMatrix(viewport);
spriteBatch.Begin(transformMatrix: worldMatrix);
```

---

## CameraEffects

`CameraEffects` proporciona efectos adicionales como screen shake. Consulta el código fuente en `Graphics/Camera/CameraEffects.cs`.

---

## Ver también

- [Camera3D →](camera-3d.md)
- [ResolutionManager →](resolution.md)
- [Scenes →](../03-scenes/scene.md)
