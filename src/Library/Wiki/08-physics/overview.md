# Física 2D — Visión General

**Namespace:** `Alca.MonoGame.Kernel.Physics`

El módulo de física 2D envuelve **Aether.Physics2D.MG 2.2.x** con componentes ECS que sincronizan automáticamente la posición del cuerpo con el `TransformBehaviour` de la entidad.

---

## Arquitectura

```
GameWorld.PhysicsWorld (Physics2DWorld)
    │
    ├── Physics2DQuery         ← raycasts y overlap tests
    │
    └── [por entidad]:
         ├── RigidBody2D       ← cuerpo dinámico o estático
         └── Collider2D        ← BoxCollider2D / CircleCollider2D / PolygonCollider2D
              └── Joint2D      ← DistanceJoint2D / HingeJoint2D / SpringJoint2D
```

---

## Integración con GameWorld

```csharp
protected override GameWorld? CreateWorld()
{
    return new GameWorld
    {
        PhysicsWorld = new Physics2DWorld(gravity: new Vector2(0, 600f))
    };
}
```

`Physics2DWorld.Step` es llamado automáticamente por el `GameWorld` cada frame.

---

## Unidades

- Las unidades físicas están en **metros**. Aether recomienda que las entidades tengan un tamaño de 0.1–10 unidades.
- Para un juego 2D en píxeles, se recomienda usar una escala de conversión (ej. 1 metro = 64 píxeles) o trabajar directamente en píxeles con gravedad y masas ajustadas.

---

## Orden de configuración de componentes

Para que los colliders se adjunten al cuerpo correcto:

1. Añadir `RigidBody2D` a la entidad.
2. Añadir `Collider2D` (Box/Circle/Polygon) después.
3. Añadir `Joint2D` último, apuntando a su `ConnectedBody`.

Si no hay `RigidBody2D`, el collider crea un cuerpo estático implícitamente.

---

## Ver también

- [Physics2DWorld →](physics-world.md)
- [RigidBody2D →](rigid-body.md)
- [Colliders →](colliders.md)
- [Joints →](joints.md)
- [Queries →](queries.md)
