# Joints 2D

**Namespace:** `Alca.MonoGame.Kernel.Physics`

Los joints conectan dos cuerpos rígidos (o un cuerpo al mundo) imponiendo restricciones de movimiento.

---

## Propiedades comunes (Joint2D)

| Propiedad | Tipo | Descripción |
|---|---|---|
| `ConnectedBody` | `RigidBody2D?` | Cuerpo B (null = anclado al mundo) |
| `EnableCollision` | `bool` | Si los dos cuerpos conectados pueden colisionar entre sí |

---

## DistanceJoint2D

Mantiene una distancia fija entre los dos anclajes.

```csharp
public sealed class DistanceJoint2D : Joint2D
{
    public float Distance      { get; set; }  // distancia objetivo (default: 1f)
    public float DampingRatio  { get; set; }  // amortiguación 0–1 (default: 0)
    public float Frequency     { get; set; }  // Hz de la "muelle" (default: 4f)
}
```

---

## HingeJoint2D

Articulación de bisagra — permite rotación libre alrededor de un punto. Con motor, puede usarse como actuador.

```csharp
public sealed class HingeJoint2D : Joint2D
{
    public Vector2 Anchor        { get; set; }   // punto de pivote en coordenadas locales
    public bool    UseMotor      { get; set; }
    public float   MotorSpeed    { get; set; }   // radianes/s
    public float   MaxMotorTorque { get; set; }  // torque máximo (default: 10f)
}
```

---

## SpringJoint2D

Resorte con distancia objetivo, frecuencia y amortiguación.

```csharp
public sealed class SpringJoint2D : Joint2D
{
    public float Distance     { get; set; }  // longitud en reposo (default: 1f)
    public float DampingRatio { get; set; }  // amortiguación 0–1 (default: 0.5f)
    public float Frequency    { get; set; }  // frecuencia en Hz (default: 4f)
}
```

---

## Ejemplo: cadena de eslabones

```csharp
protected override void InitializeWorld()
{
    GameEntity? prevLink = null;

    for (int i = 0; i < 8; i++)
    {
        var link = World!.CreateEntity($"Link{i}", new Vector2(300 + i * 30, 100));
        link.Add(new RigidBody2D { Mass = 0.5f });
        link.Add(new CircleCollider2D { Radius = 10f });

        if (prevLink is not null)
        {
            var joint = link.AddComponent<DistanceJoint2D>();
            joint.ConnectedBody = prevLink.GetComponent<RigidBody2D>();
            joint.Distance = 30f;
            joint.DampingRatio = 0.1f;
            joint.Frequency = 6f;
        }
        else
        {
            // Primer eslabón anclado al mundo
            var anchor = link.AddComponent<DistanceJoint2D>();
            anchor.ConnectedBody = null;
            anchor.Distance = 0f;
        }

        prevLink = link;
    }
}
```

---

## Ejemplo: puerta giratoria con motor

```csharp
var door = World!.CreateEntity("Door", new Vector2(500, 300));
door.Add(new RigidBody2D { Mass = 2f });
door.Add(new BoxCollider2D { Size = new Vector2(10, 80) });

var hinge = door.AddComponent<HingeJoint2D>();
hinge.ConnectedBody = null;          // anclado al mundo
hinge.Anchor = new Vector2(0, -40);  // pivote en la parte superior de la puerta
hinge.UseMotor = true;
hinge.MotorSpeed = 1.5f;             // radianes/s
hinge.MaxMotorTorque = 100f;
```

---

## Notas

- Los joints deben añadirse **después** del `RigidBody2D` y los colliders de la entidad.
- `ConnectedBody = null` ancla el joint al origen del mundo (útil para poleas o puertas fijas).
- En `DistanceJoint2D`, un `Frequency > 0` activa comportamiento de resorte; `Frequency = 0` es una barra rígida.

---

## Ver también

- [RigidBody2D →](rigid-body.md)
- [Colliders →](colliders.md)
