# Roadmap v2: Evolución de Alca.MonoGame.Kernel

## Contexto

Las fases 1–6 han sido completadas con 702 tests de cobertura. Este roadmap cubre las fases 7+ que extienden la librería con jerarquía de entidades, física 2D y mejoras de developer experience.

**Referencia de implementación:** Cuando haya duda sobre cómo implementar algo, consultar la [documentación de Unity](https://docs.unity3d.com/ScriptReference/). Adaptar al estilo del proyecto si no viola las normas o no hay un equivalente propio.

**Reglas transversales a todos los archivos:**
- `sealed` por defecto; `abstract` solo si es base class explícita
- File-scoped namespaces: `namespace Alca.MonoGame.Kernel.{Módulo};`
- `_camelCase` para campos privados, PascalCase para públicos
- Sin LINQ en `Update()`/`Draw()` — solo `for` indexado
- Sin `new` de clases en `Update()`/`Draw()` (structs sí)
- XML docs en todos los miembros públicos (single-line `<summary>` si cabe)
- `#nullable enable` asumido en todo el proyecto
- **Se debe usar siempre Dependency Injection**: `Microsoft.Extensions.DependencyInjection`

**Reglas transversales a todos los desarrollos:**
- Al terminar, actualizar este fichero marcando los TODOs completados.

---

## FASE 7 — ECS Evolution ✅ COMPLETADA

> **Objetivo:** Jerarquía de entidades padre/hijo, API Unity-like completa en `GameEntity`, espacio local/mundial en `TransformBehaviour`, y utilidades geométricas.

### Milestone 7.1 — Entity Hierarchy ✅

**`ECS/GameEntity.cs`** — **MODIFICAR**

- Campo `_parent` (`GameEntity?`) y `_children` (`List<GameEntity>`, capacidad 4 pre-allocated)
- Propiedades: `Parent`, `ChildCount`, `Children`, `Root`
- `SetParent(GameEntity? newParent)` — actualiza referencias de ambos lados
- `IsChildOf(GameEntity other)` — recorre cadena de padres
- `GetSiblingIndex()`, `SetAsFirstSibling()`, `SetAsLastSibling()`
- `Find(string name)` — BFS en hijos; no usar en game loop
- `DetachChildren()` — desvincula todos los hijos
- `TraverseDown(Action<GameEntity>)` — DFS: this + todos los descendientes
- `TraverseUp(Action<GameEntity>)` — this + todos los ancestros
- Interno: `AddChildInternal`, `RemoveChildInternal`

> **SendMessage/BroadcastMessage:** No se implementan como métodos dedicados. Usar `TraverseDown` / `TraverseUp` compuesto con `EventBus.Publish<T>` o llamadas directas. Ejemplo: `entity.TraverseDown(e => e.GetComponent<IDamageable>()?.TakeDamage(10))`.

---

### Milestone 7.2 — GameBehaviour Non-Null Entity Guarantee ✅

**`ECS/GameBehaviour.cs`** — **MODIFICAR**

- Backing field `_entity` (`GameEntity?`) privado
- `Entity` property lanza `InvalidOperationException` si se accede antes de estar adjunto
- `EntityOrNull` property (`protected`) para uso interno de subclases que necesitan acceso null-safe
- `SetEntityInternal(GameEntity entity)` — set-once guard; lanza si ya está adjunto
- En `GameEntity.Add<T>()`: cambia `behaviour.Entity = this` → `behaviour.SetEntityInternal(this)`

**Garantía:** `Entity` es no-null desde `Awake()` en adelante. No se puede reasignar a otro `GameEntity`.

---

### Milestone 7.3 — Unity-like GameEntity API ✅

**`ECS/GameEntity.cs`** — **MODIFICAR** (continuación de 7.1)

**Tags (de milestone 3.4, incluido aquí):**
- `_tags` (HashSet<string>, capacidad 8 pre-allocated)
- `AddTag(string)`, `RemoveTag(string)`, `HasTag(string)`, `GetTags()`

**Factory de componentes:**
- `AddComponent<T>() where T : GameBehaviour, new()` — crea + añade atómicamente; `Entity` queda seteada antes de `Awake()`

**Enumeración por índice:**
- `_allBehavioursList` (List<GameBehaviour>) sincronizado con `_behaviours` en `Add<T>()`
- `GetComponentCount()`, `GetComponentAtIndex(int)`, `GetComponentIndex(GameBehaviour)`

**Múltiples componentes del mismo tipo:**
- `GetComponents<T>(List<T> results)` — sin alloc; List pasada por caller

**Consultas de jerarquía:**
- `GetComponentInChildren<T>(bool includeInactive = false)` — DFS
- `GetComponentsInChildren<T>(List<T>, bool includeInactive = false)`
- `GetComponentInParent<T>(bool includeInactive = false)` — sube cadena de padres
- `GetComponentsInParent<T>(List<T>, bool includeInactive = false)`

**Sugar:**
- `CompareTag(string tag)` → `HasTag(tag)`
- `SetActive(bool active)` → `Active = active`

**`ECS/GameWorld.cs`** — **MODIFICAR**
- `GetEntitiesByTag(string tag, List<GameEntity> results)` — sin alloc
- `GetBehavioursWithInterface<T>(List<T> results) where T : class` — todos los T en el mundo

---

### Milestone 7.4 — Enhanced TransformBehaviour ✅

**`ECS/TransformBehaviour.cs`** — **MODIFICAR**

**Cambio de diseño:** backing fields `_localPosition`, `_localRotation`, `_localScale`.

**Espacio local (nuevas propiedades):**
- `LocalPosition`, `LocalRotation`, `LocalScale` — lectura/escritura directa sobre backing fields
- `LocalPosition2d`, `LocalRotation2d`, `LocalScale2d` — conveniencia 2D

**Espacio mundial (propiedades modificadas):**
- `Position` — world space: getter computa desde la cadena; setter convierte world→local
- `Rotation` — world Euler: suma de la cadena (válido para 2D y 3D simple)
- `Scale` — alias backward-compat de `LocalScale` (getter+setter; preserva código existente)
- `LossyScale` — world scale computada (read-only, producto de la cadena)
- `Position2d`, `Rotation2d`, `Scale2d` — conveniencia world-space

**Backward compatibility:** Para entidades sin padre, `LocalPosition == Position`. Tests anteriores siguen pasando sin cambios.

**Matrices:**
- `LocalToWorldMatrix` — composición de toda la cadena
- `WorldToLocalMatrix` — `Matrix.Invert(LocalToWorldMatrix)`

**Transformaciones de coordenadas:**
- `TransformPoint(Vector3)`, `InverseTransformPoint(Vector3)` — aplican traslación + rotación + escala
- `TransformDirection(Vector3)`, `InverseTransformDirection(Vector3)` — solo rotación (via `TransformNormal`)

**Operaciones:**
- `Translate(Vector3, bool worldSpace = false)`
- `Rotate(Vector3, bool worldSpace = false)`
- `LookAt(Vector3 worldTarget)` — ajusta `LocalRotation2d` (solo 2D)
- `SetPositionAndRotation`, `SetLocalPositionAndRotation`
- `GetPositionAndRotation(out, out)`, `GetLocalPositionAndRotation(out, out)`

**Navegación de jerarquía (delega a Entity):**
- `ParentTransform`, `Root`, `ChildCount`, `GetChild(int)`, `IsChildOf(TransformBehaviour)`

---

### Milestone 7.5 — GeometryUtility ✅

**`Mathematics/GeometryUtility.cs`** — `static sealed class GeometryUtility`

Equivalente a `UnityEngine.GeometryUtility`:

```csharp
// BoundingBox desde array de posiciones + transform matrix (sin alloc)
public static BoundingBox CalculateBounds(ReadOnlySpan<Vector3> positions, Matrix transform)

// Extraer 6 planos del frustum en un array pre-allocated (Near/Far/Left/Right/Top/Bottom)
public static void CalculateFrustumPlanes(BoundingFrustum frustum, Plane[] planes)

// Test AABB contra array de planos — para frustum culling
public static bool TestPlanesAABB(ReadOnlySpan<Plane> planes, BoundingBox bounds)

// Crear Plane desde polígono; false si el polígono es degenerado (< 3 verts o colineales)
public static bool TryCreatePlaneFromPolygon(ReadOnlySpan<Vector3> vertices, out Plane plane)
```

> Usa `ReadOnlySpan<T>` en lugar de arrays para evitar allocations (diferencia respecto a la API de Unity).

**Tests:** `UnitTests/Mathematics/GeometryUtilityTests.cs`

---

## FASE 8 — Sistema de Física 2D 🔜 PENDIENTE

> **Objetivo:** Simulación física 2D completa (RigidBody, Colliders, Joints, Gravity, Friction) wrappeando **Aether.Physics2D** — el mismo patrón de integración que MonoGame.Extended.

### Prerequisito

Añadir a `Alca.MonoGame.Kernel.csproj`:
```xml
<PackageReference Include="Aether.Physics2D" Version="2.2.*" />
```

---

### Milestone 8.1 — Physics World

**`Physics/Physics2DWorld.cs`** — `sealed class Physics2DWorld`
- Wrappea `tainicom.Aether.Physics2D.Dynamics.World`
- `Gravity` (Vector2) — gravedad global (default 0, -9.8)
- `Step(GameTime)` — avanza la simulación física; llamar desde `Update()` del juego
- `VelocityIterations`, `PositionIterations` (int) — calidad de simulación
- Integración opcional en `GameWorld.Update()` si el usuario registra un `Physics2DWorld`

---

### Milestone 8.2 — RigidBody2D

**`Physics/RigidBody2D.cs`** — `sealed class RigidBody2D : GameBehaviour`
- Wrappea `AetherBody`
- `IsStatic` (bool), `Mass` (float)
- `LinearVelocity` (Vector2), `AngularVelocity` (float)
- `LinearDamping`, `AngularDamping` (float)
- `GravityScale` (float, 1.0 = gravedad normal)
- `ApplyForce(Vector2)`, `ApplyImpulse(Vector2)`, `ApplyTorque(float)`
- `Update()` — sincroniza `Transform.Position2d` y `Transform.Rotation2d` con el cuerpo físico

---

### Milestone 8.3 — Colliders 2D

**`Physics/Collider2D.cs`** — `abstract class Collider2D : GameBehaviour`
- `IsTrigger` (bool) — trigger vs solid
- `Friction` (float), `Restitution` (float, bounciness), `Density` (float)
- Eventos de colisión (via `EventBus` o callbacks):
  - `Action<Collider2D>? OnCollisionEnter`, `OnCollisionExit`
  - `Action<Collider2D>? OnTriggerEnter`, `OnTriggerExit`

**`Physics/BoxCollider2D.cs`** — `sealed class BoxCollider2D : Collider2D`
- `Size` (Vector2), `Offset` (Vector2)

**`Physics/CircleCollider2D.cs`** — `sealed class CircleCollider2D : Collider2D`
- `Radius` (float), `Offset` (Vector2)

**`Physics/PolygonCollider2D.cs`** — `sealed class PolygonCollider2D : Collider2D`
- `_vertices` (Vector2[], pre-allocated), `SetPath(ReadOnlySpan<Vector2>)`

---

### Milestone 8.4 — Joints 2D

**`Physics/Joint2D.cs`** — `abstract class Joint2D : GameBehaviour`
- `ConnectedBody` (RigidBody2D?), `EnableCollision` (bool)

**`Physics/DistanceJoint2D.cs`** — `sealed class DistanceJoint2D : Joint2D`
- `Distance` (float), `MinDistance`, `MaxDistance`, `DampingRatio`, `Frequency`

**`Physics/HingeJoint2D.cs`** — `sealed class HingeJoint2D : Joint2D`
- `Anchor` (Vector2), `UseMotor` (bool), `MotorSpeed`, `MaxMotorTorque`

**`Physics/SpringJoint2D.cs`** — `sealed class SpringJoint2D : Joint2D`
- `Distance`, `DampingRatio`, `Frequency`

---

## FASE 9.x — Sistema de Iluminación 2D 🔜 PENDIENTE

> **Objetivo:** Iluminación global y local para escenas 2D — luz ambiental, direccional, puntual y cono — como `GameBehaviour` adjunto a `GameEntity`, con un servicio `LightingWorld` que agrega contribuciones sin allocations en el game loop.

---

### Milestone 9.x.1 — LightBehaviour (base)

**`Lighting/LightBehaviour.cs`** — `abstract class LightBehaviour : GameBehaviour`

- `Color` (Color, default `Color.White`)
- `Intensity` (float, rango 0–1, default 1)
- `LightingLayer` (LightingLayer, default `World`)
- `Range` (float) — radio de influencia; 0 = sin límite (luz direccional / ambiental)
- `IsContributing` — computed: `Active && Intensity > 0`
- `abstract void Contribute(LightContribution accumulator)` — cada subclase implementa su propia contribución

> `LightContribution` es un `struct` — sin alloc en Update.

---

### Milestone 9.x.2 — Tipos de luz

**`Lighting/AmbientLight.cs`** — `sealed class AmbientLight : LightBehaviour`

- Sin propiedades extra.
- Contribuye un color base uniforme a toda la escena.
- Caso de uso: iluminación de fondo (cielo de día, noche oscura, interior neutro).

**`Lighting/DirectionalLight2D.cs`** — `sealed class DirectionalLight2D : LightBehaviour`

- `Direction` (Vector2) — dirección de la luz; toma por defecto `Transform.Right`
- Iluminación uniforme en toda la escena, sin atenuación por distancia.
- Caso de uso: sol, luna, luz cenital de nivel.

**`Lighting/PointLight2D.cs`** — `sealed class PointLight2D : LightBehaviour`

- `Range` (float) — radio de influencia
- `FalloffExponent` (float, default 2.0) — curva de atenuación: `1 - (dist / range) ^ falloff`
- Posición tomada de `Transform.Position2d`
- Caso de uso: antorcha, lámpara, explosión, fuego de campamento.

**`Lighting/SpotLight2D.cs`** — `sealed class SpotLight2D : LightBehaviour`

- `Range` (float)
- `InnerAngle` (float, grados) — cono interno con intensidad máxima
- `OuterAngle` (float, grados) — cono de degradado suave
- `Direction` (Vector2) — tomada de `Transform.Right` o sobreescrita manualmente
- Caso de uso: linterna, foco de escenario, cono de visión enemigo.

---

### Milestone 9.x.3 — LightContribution (struct)

**`Lighting/LightContribution.cs`** — `readonly struct LightContribution`

```csharp
public readonly struct LightContribution
{
    public Color Accumulated { get; }
    public void Add(Color color, float weight); // Color.Lerp(acc, color, weight)
}
```

- Sin heap allocation.
- Usado internamente por `LightingWorld.Resolve()`.

---

### Milestone 9.x.4 — LightingLayer (enum)

**`Lighting/LightingLayer.cs`** — `enum LightingLayer`

- Valores sugeridos: `World`, `UI`, `Underground`, `Overlay`
- Permite aislar grupos de luces por capa lógica.
- `LightingWorld.Resolve()` y `GetLightsInRange()` filtran por layer.

---

### Milestone 9.x.5 — LightingWorld

**`Lighting/LightingWorld.cs`** — `sealed class LightingWorld`

- `_lights` (List<LightBehaviour>, capacidad 32 pre-allocated) — sin LINQ en Update
- `Register(LightBehaviour)`, `Unregister(LightBehaviour)`
- `AmbientColor` (Color) — fallback global si no hay `AmbientLight` activa (default `Color.Black`)
- `Resolve(Vector2 worldPosition, LightingLayer layer)` → `Color` — color de iluminación acumulado en ese punto; apto para uso en CPU o como input de shader
- `GetLightsInRange(Vector2 position, float radius, LightingLayer layer, List<LightBehaviour> results)` — sin alloc
- `FillShaderParameters(Effect effect)` — setea arrays de posición/color/rango para un Effect GLSL/HLSL estándar (opcional; solo si el usuario usa rendering con shaders)
- Registrado como **singleton** en el `IServiceCollection` del proyecto

---

### Milestone 9.x.6 — Integración con GameEntity / GameWorld

- `LightBehaviour.Awake()` llama a `LightingWorld.Register(this)` automáticamente vía DI.
- `LightBehaviour.OnDestroy()` (o equivalente en el ciclo de vida) llama a `Unregister(this)`.
- `LightBehaviour` respeta `Active`: cuando el `GameEntity` se desactiva, `IsContributing` devuelve `false` y la luz deja de acumularse.
- `GameWorld` expone `LightingWorld?` como propiedad si está registrado en DI — no obligatorio para proyectos que no usen iluminación dinámica.

---

### Tests esperados

`UnitTests/Lighting/PointLight2DTests.cs`

- `Resolve_WithPointLight_AtCenter_ReturnsFullIntensity`
- `Resolve_WithPointLight_BeyondRange_ReturnsAmbientOnly`
- `Resolve_WithMultipleLights_AccumulatesCorrectly`
- `LightBehaviour_WhenEntityDeactivated_DoesNotContribute`
- `LightingWorld_Register_Unregister_UpdatesLightCount`
- `SpotLight2D_OutsideCone_ReturnsZeroContribution`

---

### Estructura de carpetas

src/Library/Alca.MonoGame.Kernel/
├── Lighting/
│   ├── AmbientLight.cs
│   ├── DirectionalLight2D.cs
│   ├── LightBehaviour.cs          (abstract)
│   ├── LightContribution.cs       (struct)
│   ├── LightingLayer.cs           (enum)
│   ├── LightingWorld.cs           (sealed, singleton DI)
│   ├── PointLight2D.cs
│   └── SpotLight2D.cs

---

### Verificación

Crear una `LightingWorld`, añadir un `PointLight2D` con `Range = 100` e `Intensity = 1` a una entidad en `(0, 0)`:
- `Resolve((0, 0), LightingLayer.World)` → `Color.White`
- `Resolve((100, 0), LightingLayer.World)` → `Color.Black` (en el límite exacto del rango)
- Desactivar el `GameEntity` → `IsContributing == false` → `Resolve` devuelve `AmbientColor`

## FASE 10+ — Por Definir

Ideas para roadmaps futuros (sin especificación todavía):
- **10.x — Animation System:** Sprite animation clips, Animation State Machine, blending
- **10.x — Prefab System:** Serializable entity templates con JSON
- **10.x — NavMesh 2D:** Navegación por waypoints o grid-based pathfinding (A*)
- **10.x — Extended Audio:** Spatial audio, audio zones, mixer
- **10.x — Networking:** P2P o cliente/servidor básico para juegos multijugador pequeños


---

## Estructura de Carpetas ECS + Physics

```
src/Library/Alca.MonoGame.Kernel/
├── ECS/
│   ├── GameBehaviour.cs       (modificado: Entity set-once, EntityOrNull)
│   ├── GameEntity.cs          (modificado: jerarquía, API Unity-like, tags)
│   ├── GameEntityPool.cs
│   ├── GameWorld.cs           (modificado: GetEntitiesByTag, GetBehavioursWithInterface)
│   ├── IPoolable.cs
│   └── TransformBehaviour.cs  (modificado: local/world space, matrices)
├── Mathematics/
│   ├── BoundingHelpers.cs
│   ├── Circle.cs
│   ├── GeometryUtility.cs     (nuevo)
│   └── MathUtils.cs
├── Physics/                   (FASE 8 — pendiente)
│   ├── BoxCollider2D.cs
│   ├── CircleCollider2D.cs
│   ├── Collider2D.cs
│   ├── DistanceJoint2D.cs
│   ├── HingeJoint2D.cs
│   ├── Joint2D.cs
│   ├── Physics2DWorld.cs
│   ├── PolygonCollider2D.cs
│   ├── RigidBody2D.cs
│   └── SpringJoint2D.cs
└── ...


```

---

## Verificación por Fase

- **Fase 7:** `dotnet test` — todos los tests anteriores siguen pasando (backward compat). Test de jerarquía: `child.SetParent(parent)` → `child.Transform.Position == parent.Transform.Position + child.Transform.LocalPosition`. Test Entity no-null: `new MyBehaviour().Entity` lanza `InvalidOperationException`.
- **Fase 8:** Crear una `Physics2DWorld`, añadir un `RigidBody2D` con `GravityScale=1` a una entidad, llamar `Step()` en 60 fps durante 1 segundo — la entidad debe caer ~4.9 unidades en Y.

---

## Referencia Técnica del Proyecto

### Solución y proyectos

```
src/Library/
├── Alca.MonoGame.Kernel/                  ← librería principal
│   └── Alca.MonoGame.Kernel.csproj
├── Alca.MonoGame.Kernel.UnitTests/        ← tests xUnit
│   └── Alca.MonoGame.Kernel.UnitTests.csproj
```

### Dependencias NuGet (Kernel)

| Paquete | Versión | Notas |
|---------|---------|-------|
| `MonoGame.Framework.DesktopGL` | 3.8.* | `PrivateAssets=All` |
| `MonoGame.Extended` | 6.0.* | Particles, Tweening, Tiled, BitmapFonts |
| `Microsoft.Extensions.DependencyInjection` | 10.0.* | DI container |
| `Microsoft.Extensions.Localization` | 10.0.* | IStringLocalizer |
| `Aether.Physics2D` | 2.* | **FASE 8** — añadir cuando se implemente |

