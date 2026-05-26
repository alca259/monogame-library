# Debug

**Namespace:** `Alca.MonoGame.Kernel.Debug`

Herramientas de visualización y diagnóstico en tiempo de ejecución: primitivos de debug, overlay de stats y consola de comandos.

---

## DebugDraw

Clase estática que acumula comandos de dibujo y los renderiza en el siguiente frame. Los comandos pueden tener una duración limitada.

### Propiedad

```csharp
public static bool IsEnabled { get; set; }
```

Si `false`, todos los métodos son no-op.

### Métodos

| Método | Descripción |
|---|---|
| `DrawLine(from, to, color, duration)` | Línea entre dos puntos |
| `DrawRect(rect, color, duration)` | Rectángulo sin relleno |
| `DrawCircle(center, radius, color, segments, duration)` | Círculo con N segmentos |
| `DrawPoint(pos, color, size, duration)` | Punto cuadrado |
| `DrawText(pos, text, color, duration)` | Texto flotante en el mundo |
| `Update(gameTime)` | Avanza los timers de los comandos con duración |
| `Draw(spriteBatch, camera, font)` | Renderiza todos los comandos activos |
| `Clear()` | Elimina todos los comandos pendientes |

`duration = 0f` significa que el comando se muestra un solo frame.

---

## DebugOverlay

Overlay de diagnóstico con FPS y valores custom.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IsVisible` | `bool` | Muestra/oculta el overlay |
| `FPS` | `float` | FPS calculados en el último segundo |

### Métodos

| Método | Descripción |
|---|---|
| `AddWatch(label, valueFunc)` | Añade un valor dinámico al overlay |
| `RemoveWatch(label)` | Elimina un watch por etiqueta |
| `Update(gameTime)` | Actualiza FPS y valores de watch |
| `Draw(spriteBatch, font)` | Renderiza el overlay en pantalla |

---

## Ejemplo: visualización de colliders en debug

```csharp
public sealed class PhysicsDebugDrawer : GameBehaviour
{
    private readonly List<Collider2D> _colliders = [];

    public override void Update(GameTime gameTime)
    {
        if (!DebugDraw.IsEnabled) return;

        Entity.World!.FindComponents(_colliders);
        foreach (var col in _colliders)
        {
            var pos = col.Entity.Transform.Position2d;
            switch (col)
            {
                case BoxCollider2D box:
                    DebugDraw.DrawRect(
                        new Rectangle((int)(pos.X - box.Size.X / 2),
                                      (int)(pos.Y - box.Size.Y / 2),
                                      (int)box.Size.X, (int)box.Size.Y),
                        col.IsTrigger ? Color.Yellow : Color.Lime);
                    break;

                case CircleCollider2D circle:
                    DebugDraw.DrawCircle(pos + circle.Offset, circle.Radius,
                                        col.IsTrigger ? Color.Yellow : Color.Cyan);
                    break;
            }
        }
        _colliders.Clear();
    }
}
```

---

## Ejemplo: overlay de diagnóstico con watches

```csharp
// En la escena, al inicializar:
_debugOverlay = new DebugOverlay();
_debugOverlay.IsVisible = true;

_debugOverlay.AddWatch("FPS",       () => _debugOverlay.FPS.ToString("F0"));
_debugOverlay.AddWatch("Entidades", () => World?.EntityCount.ToString() ?? "0");
_debugOverlay.AddWatch("Físicas",   () => World?.PhysicsWorld?.BodyCount.ToString() ?? "0");
_debugOverlay.AddWatch("Jugador X", () => _player?.Transform.Position2d.X.ToString("F1") ?? "-");

// En Update:
_debugOverlay.Update(gameTime);

// En Draw (después del resto de la escena):
_debugOverlay.Draw(Core.SpriteBatch, _debugFont);
```

---

## Notas

- Activa `DebugDraw.IsEnabled = true` en DEBUG builds y `false` en RELEASE para cero overhead.
- Los comandos con `duration > 0` se van eliminando automáticamente en `DebugDraw.Update`.
- `DebugOverlay.FPS` se actualiza una vez por segundo; es suficiente para diagnóstico visual.

---

## Ver también

- [Queries de física →](../08-physics/queries.md)
- [Colliders →](../08-physics/colliders.md)
