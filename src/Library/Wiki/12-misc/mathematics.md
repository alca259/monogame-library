# Matemáticas y Geometría

**Namespace:** `Alca.MonoGame.Kernel.Mathematics`

Colección de utilidades matemáticas y geométricas complementarias a la API base de MonoGame.

---

## MathUtils

Funciones estáticas para operaciones vectoriales y de interpolación.

| Método | Descripción |
|---|---|
| `DistanceSquared(a, b)` | Distancia al cuadrado entre dos puntos 2D (sin `sqrt`) |
| `AngleBetween(from, to)` | Ángulo en radianes desde `from` hacia `to` |
| `AngleToVector2(radians)` | Convierte un ángulo a vector unitario |
| `WrapAngle(angle)` | Normaliza un ángulo al rango [−π, π] |
| `Clamp(value, min, max)` | Clampea un float entre min y max |
| `Lerp(a, b, t)` | Interpolación lineal para float, Vector2 o Color |
| `SmoothStep(a, b, t)` | Interpolación suavizada (curva cúbica) |
| `MapRange(value, inMin, inMax, outMin, outMax)` | Mapea un valor de un rango a otro |

```csharp
// Ángulo de giro de un proyectil hacia el objetivo
float angle = MathUtils.AngleBetween(bullet.Position, target.Position);
Vector2 dir = MathUtils.AngleToVector2(angle);

// Mapear velocidad (0-1) a frecuencia de sonido (0.5-2.0)
float pitch = MathUtils.MapRange(speed, 0f, maxSpeed, 0.5f, 2.0f);

// Suavizar movimiento de cámara
cameraPos = MathUtils.SmoothStep(cameraPos, targetPos, 0.1f);
```

---

## Circle

Struct inmutable que representa un círculo 2D.

```csharp
public readonly struct Circle
{
    public readonly int X;
    public readonly int Y;
    public readonly int Radius;
    public readonly Point Location;
    public static Circle Empty { get; }
    public readonly bool IsEmpty { get; }
    public readonly int Top    { get; }
    public readonly int Bottom { get; }
    public readonly int Left   { get; }
    public readonly int Right  { get; }

    public Circle(int x, int y, int radius)
    public Circle(Point location, int radius)
    public bool Intersects(Circle other)
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Color color, float thickness = 1f)
}
```

```csharp
var hitbox = new Circle(entity.Transform.Position2d.ToPoint(), radius: 24);
var pickup = new Circle(pickupPos.ToPoint(), radius: 16);

if (hitbox.Intersects(pickup))
    CollectPickup();
```

---

## BoundingHelpers

Utilidades para volúmenes de bounding 3D.

| Método | Descripción |
|---|---|
| `CreateBoundingSphere(center, radius)` | Crea una esfera de bounding |
| `CreateBoundingBox(min, max)` | Crea una caja AABB |
| `RayIntersectsPlane(ray, plane, out distance)` | Test de intersección rayo-plano |
| `RayIntersectsSphere(ray, sphere, out distance)` | Test de intersección rayo-esfera |
| `ScreenToWorldRay(screenPos, view, projection, viewport)` | Genera un rayo desde pantalla al mundo 3D |

```csharp
// Picking de objetos 3D con el ratón
var ray = BoundingHelpers.ScreenToWorldRay(
    Core.Input.MousePosition, _camera.View, _camera.Projection,
    Core.GraphicsDevice.Viewport);

if (BoundingHelpers.RayIntersectsSphere(ray, objectBounds, out float dist))
    SelectObject(dist);
```

---

## GeometryUtility

Operaciones geométricas avanzadas para 3D.

| Método | Descripción |
|---|---|
| `CalculateBounds(positions, transform)` | AABB de un conjunto de vértices transformados |
| `CalculateFrustumPlanes(frustum, planes)` | Extrae los 6 planos del frustum |
| `TestPlanesAABB(planes, bounds)` | Frustum culling: `true` si la caja está dentro |
| `TryCreatePlaneFromPolygon(vertices, out plane)` | Calcula el plano de un polígono convexo |

---

## Notas

- `DistanceSquared` es más eficiente que `Distance` para comparar distancias relativas (evita `sqrt`).
- `Circle.Draw` es para debug; en producción usa tu propio renderer de círculos.
- `GeometryUtility.TestPlanesAABB` es útil para frustum culling manual de entidades 3D.

---

## Ver también

- [Rendering 3D →](../04-graphics/rendering-3d.md)
- [Queries de física →](../08-physics/queries.md)
