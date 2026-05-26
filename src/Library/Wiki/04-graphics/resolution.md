# ResolutionManager

**Namespace:** `Alca.MonoGame.Kernel.Graphics`

`ResolutionManager` hace que el juego se vea igual independientemente de la resolución de pantalla del usuario, mediante una **resolución virtual** de diseño. Se accede a través de `Core.Resolution`.

---

## Concepto

Defines una resolución virtual (p.ej. 1280×720) en la que diseñas el juego. `ResolutionManager` calcula cómo escalar esa área a cualquier pantalla real:

- **`ScaleMatrix`** — escala no uniforme que llena toda la pantalla (adecuado para UI fullscreen que puede distorsionarse).
- **`WorldScaleMatrix`** — escala uniforme que mantiene el ratio de aspecto (letterboxing para el mundo del juego).
- **`LetterboxViewport`** — viewport recortado que excluye las bandas negras.

---

## Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `VirtualWidth` | `int` | Ancho de diseño en píxeles |
| `VirtualHeight` | `int` | Alto de diseño en píxeles |
| `ScaleMatrix` | `Matrix` | Escala no uniforme para UI |
| `WorldScaleMatrix` | `Matrix` | Escala uniforme para el mundo (letterboxing) |
| `LetterboxViewport` | `Viewport` | Viewport con offset para las bandas negras |

---

## Constructor

```csharp
new ResolutionManager(graphicsDevice, window, virtualWidth = 1920, virtualHeight = 1080)
```

`Core` lo crea automáticamente con resolución virtual `1920×1080`. Para cambiarlo, sobreescribe en `PostInitialize`:

```csharp
protected override void PostInitialize()
{
    // La resolución virtual ya está establecida en el constructor
    // Core.Resolution.VirtualWidth y VirtualHeight son read-only
}
```

> Si necesitas una resolución virtual diferente a 1920×1080, deberás registrar el servicio con parámetros personalizados en `ConfigureServices`.

---

## Conversión de coordenadas

### `ScreenToVirtual(Vector2 screenPos)`

Convierte una posición en píxeles reales a coordenadas virtuales, considerando el letterboxing:

```csharp
// Posición del ratón en coordenadas virtuales
var virtualMousePos = Core.Resolution.ScreenToVirtual(Core.Input.MousePosition);
```

---

## Uso en Draw

### UI (sin letterbox — llena pantalla)

```csharp
spriteBatch.Begin(transformMatrix: Core.Resolution.ScaleMatrix);
// Dibujar UI...
spriteBatch.End();
```

### Mundo del juego (con letterbox — sin distorsión)

```csharp
// Establecer el viewport con letterboxing
Core.GraphicsDevice.Viewport = Core.Resolution.LetterboxViewport;

var worldMatrix = Core.Resolution.WorldScaleMatrix * _camera.GetTransformMatrix(Core.Resolution.LetterboxViewport);
spriteBatch.Begin(transformMatrix: worldMatrix);
// Dibujar mundo...
spriteBatch.End();

// Restaurar viewport para UI
Core.GraphicsDevice.Viewport = Core.GraphicsDevice.Adapter.CurrentDisplayMode.TitleSafeArea; // o el viewport completo
```

---

## Actualización automática

`ResolutionManager` se suscribe a `GameWindow.ClientSizeChanged` y recalcula sus matrices automáticamente cuando la ventana cambia de tamaño.

---

## Ejemplo: juego 1280×720 en pantalla FullHD

```csharp
// En Core constructor:
// Se crea con virtual 1920×1080. Para usar 1280×720, registra en ConfigureServices:
protected override void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<ResolutionManager>(sp =>
        new ResolutionManager(
            sp.GetRequiredService<GraphicsDevice>(),
            Window,
            virtualWidth: 1280,
            virtualHeight: 720));
}
```

En pantalla 1920×1080 con virtual 1280×720:
- `WorldScaleMatrix` escala `×1.5` (1920/1280 = 1.5)
- `LetterboxViewport` = `Viewport(0, 0, 1920, 1080)` (no hay letterbox porque el ratio coincide)

En pantalla 2560×1440:
- Factor mínimo = `min(2560/1280, 1440/720) = 2`
- `LetterboxViewport` = `Viewport(0, 0, 2560, 1440)` — llena sin barras

En pantalla 1920×1200 (ratio 16:10):
- Factor mínimo = `min(1920/1280, 1200/720) = 1.5`
- Viewport = `Viewport(0, 75, 1920, 1080)` — barras negras arriba y abajo de 75px

---

## Ver también

- [Camera2D →](camera-2d.md)
- [Core →](../01-core/core.md)
