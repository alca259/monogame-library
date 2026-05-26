# Cámaras 3D

**Namespace:** `Alca.MonoGame.Kernel.Graphics.Camera`

El framework incluye cuatro implementaciones de cámara 3D que heredan de la clase abstracta `Camera3D`. Cada una implementa sus propias matrices `View` y `Projection`.

---

## Camera3D (clase base abstracta)

### Propiedades abstractas

| Propiedad | Tipo | Descripción |
|---|---|---|
| `View` | `Matrix` | Matriz de vista (posición y orientación de la cámara) |
| `Projection` | `Matrix` | Matriz de proyección (perspectiva o ortográfica) |

### Propiedades comunes

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Position` | `Vector3` | Posición de la cámara en el mundo |
| `Target` | `Vector3` | Punto al que mira la cámara |

### Método

```csharp
BoundingFrustum frustum = camera.GetFrustum();
// Útil para frustum culling
```

---

## FixedCamera3D

Cámara estática en posición fija. Sin movimiento automático.

```csharp
var camera = new FixedCamera3D
{
    Position = new Vector3(0, 5, 10),
    Target   = Vector3.Zero
};
```

---

## FirstPersonCamera3D

Cámara de primera persona. Controla la orientación mediante yaw (horizontal) y pitch (vertical).

```csharp
var fpsCam = new FirstPersonCamera3D
{
    Position = new Vector3(0, 1.8f, 0)
};

// En Update:
fpsCam.Yaw   += mouseX * sensitivity;
fpsCam.Pitch -= mouseY * sensitivity;
fpsCam.Pitch  = MathHelper.Clamp(fpsCam.Pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);

// Mover en la dirección que mira:
fpsCam.Position += fpsCam.Forward * speed * dt;
```

---

## ThirdPersonCamera3D

Cámara orbital alrededor de un objetivo. Controla distancia, ángulo de elevación y rotación horizontal.

```csharp
var tpsCam = new ThirdPersonCamera3D
{
    Target   = playerPosition,
    Distance = 5f,
    Elevation = 30f,   // grados sobre el plano XZ
    Azimuth   = 180f   // ángulo horizontal en grados
};

// En Update, orbitar con el ratón:
tpsCam.Azimuth   += mouseX * 0.5f;
tpsCam.Elevation -= mouseY * 0.5f;
tpsCam.Elevation  = MathHelper.Clamp(tpsCam.Elevation, 5f, 85f);
tpsCam.Target     = player.Transform.Position;
```

---

## TopDownCamera3D

Vista cenital/isométrica desde arriba.

```csharp
var topDown = new TopDownCamera3D
{
    Target  = new Vector3(0, 0, 0),
    Height  = 20f,       // altura desde el target
    Angle   = 45f        // ángulo de inclinación en grados (0 = zenith perfecto)
};
```

---

## Uso en Draw (ejemplo con FixedCamera3D)

```csharp
// En Scene.Draw:
var effect = new BasicEffect(Core.GraphicsDevice)
{
    View       = _camera.View,
    Projection = _camera.Projection,
    World      = Matrix.Identity
};

foreach (var pass in effect.CurrentTechnique.Passes)
{
    pass.Apply();
    // Dibuja geometría 3D...
}
```

---

## Ejemplo completo: cámara de tercera persona con colisión

```csharp
public sealed class TpsController : GameBehaviour
{
    private ThirdPersonCamera3D _camera = null!;

    public override void Awake()
    {
        _camera = new ThirdPersonCamera3D
        {
            Distance  = 6f,
            Elevation = 25f
        };
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var mouse = Core.Input.Mouse;

        // Orbitar con clic derecho mantenido
        if (mouse.IsRightButtonDown)
        {
            _camera.Azimuth   += mouse.DeltaX * 100f * dt;
            _camera.Elevation -= mouse.DeltaY * 100f * dt;
            _camera.Elevation  = MathHelper.Clamp(_camera.Elevation, 5f, 80f);
        }

        // Zoom con rueda
        _camera.Distance -= mouse.ScrollWheelDelta * 0.01f;
        _camera.Distance  = MathHelper.Clamp(_camera.Distance, 2f, 20f);

        // Seguir al jugador
        _camera.Target = Entity.Transform.Position;
    }
}
```

---

## Ver también

- [Camera2D →](camera-2d.md)
- [Rendering 3D →](rendering-3d.md)
