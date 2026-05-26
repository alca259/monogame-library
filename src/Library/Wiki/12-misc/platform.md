# PlatformManager

**Namespace:** `Alca.MonoGame.Kernel.Platform`

`PlatformManager` detecta la plataforma de ejecución y expone eventos del ciclo de vida del sistema operativo. Disponible como `Core.Platform`.

---

## PlatformType

```csharp
public enum PlatformType
{
    Desktop,
    Mobile,
    Console
}
```

---

## PlatformManager

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `CurrentPlatform` | `PlatformType` | Plataforma detectada en tiempo de ejecución |
| `IsDesktop` | `bool` | `true` en Windows, macOS, Linux |
| `IsMobile` | `bool` | `true` en iOS o Android |
| `IsConsole` | `bool` | `true` en consolas |
| `VirtualWidth` | `int` | Resolución virtual del `ResolutionManager` |
| `VirtualHeight` | `int` | Resolución virtual del `ResolutionManager` |
| `SupportedOrientations` | `DisplayOrientation` | Orientaciones permitidas (útil en móvil) |

### Eventos

| Evento | Descripción |
|---|---|
| `ScreenResized` | La ventana o pantalla cambió de tamaño |
| `AppPaused` | La aplicación fue enviada a segundo plano (móvil/consola) |

---

## Ejemplo: configuración dependiente de plataforma

```csharp
public sealed class MyGame : Core
{
    protected override void PostInitialize()
    {
        base.PostInitialize();

        if (Core.Platform.IsDesktop)
        {
            // Controles con teclado
            _inputMap = CreateKeyboardMap();
        }
        else if (Core.Platform.IsMobile)
        {
            // Controles táctiles y orientación forzada
            Core.Platform.SupportedOrientations = DisplayOrientation.LandscapeLeft
                                                 | DisplayOrientation.LandscapeRight;
            _inputMap = CreateTouchMap();
        }

        Core.Platform.AppPaused  += () => AutoSave();
        Core.Platform.ScreenResized += () => RecreateRenderTargets();
    }
}
```

---

## Ejemplo: resolución adaptativa

```csharp
Core.Platform.ScreenResized += () =>
{
    var vp = Core.GraphicsDevice.Viewport;
    _rtManager.Resize(vp.Width, vp.Height);
    _lightPipeline.Resize(vp.Width, vp.Height);
};
```

---

## Notas

- `PlatformManager` suscribe automáticamente al evento `GameWindow.ClientSizeChanged`; no es necesario hacerlo manualmente.
- `AppPaused` es llamado cuando el SO envía la aplicación a segundo plano — úsalo para pausar timers y guardar estado automáticamente.
- En Desktop, `IsConsole = false` y `IsMobile = false` siempre.

---

## Ver también

- [ResolutionManager →](../04-graphics/resolution.md)
- [Core →](../01-core/core.md)
