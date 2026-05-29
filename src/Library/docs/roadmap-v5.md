# Roadmap v5: Triggers, Transitions, Noise, Day/Night, Shaders, 2.5D & Dialogue

**Reglas transversales a todos los desarrollos:**
- Al terminar, actualizar este fichero marcando los TODOs completados.

## Executive Summary

### v4 Accomplishments (Phase 19) ✅

v4 añade el sistema de climatología completo: temperatura en tiempo real, 10 tipos de clima predefinidos, extensibilidad con `WeatherTypeId` string-based, transiciones suaves entre perfiles, integración con iluminación, física, partículas y audio espacial.

### v5 Objective

v5 añade 7 sistemas orientados a cubrir el 90% de los patrones necesarios en juegos 2D/2.5D:

| Fase | Sistema | Depende de |
|------|---------|-----------|
| 20 | Lightweight Trigger Volumes (sin física) | ECS base |
| 21 | Scene Transitions adicionales | SceneManager existente |
| 22 | Ruido Procedural (Perlin / Simplex) | Mathematics/ |
| 23 | Day/Night Cycle | LightingWorld, GameWorld |
| 24 | Shader Library 2D | Material, SpriteMaterial, PostProcessEffect |
| 25 | Soporte 2.5D (isométrico, Y-Sort, Normal Maps) | Sprites, Camera2D, Phase 24 |
| 26 | Sistema de Diálogo / Narrativa | UI, ECS, Localization |

---

## Transversal Rules (inherited from v1–v4)

- `sealed` por defecto; `abstract` solo para clases base explícitas.
- File-scoped namespaces: `namespace Alca.MonoGame.Kernel.{Module};`
- `_camelCase` campos privados, `PascalCase` miembros públicos.
- Sin LINQ en `Update()`/`Draw()` — solo loops `for` indexados.
- Sin `new` de tipos referencia en `Update()`/`Draw()` — structs permitidos.
- XML docs en todos los miembros públicos.
- `#nullable enable` en todo el proyecto.
- Dependency Injection vía `Microsoft.Extensions.DependencyInjection`.
- Cada fase termina con tests xUnit en `src/Library/Alca.MonoGame.Kernel.UnitTests/` reflejando la estructura de carpetas del código fuente.
- Al completar, marcar el TODO correspondiente en este fichero.

---

## PHASE 20 ✅ COMPLETADA — Lightweight Trigger Volumes

> **Goal:** Zonas de activación AABB/Circle puras, sin requerir `Physics2DWorld` ni `RigidBody2D`. Útiles para recoger ítems, activar diálogos, cambiar música de habitación o disparar eventos de forma ligera.

**Complexity:** Low–Medium  
**Depends on:** Phases 1–19 (all complete)

**Nota:** `Collider2D.IsTrigger` ya existe en Physics/ basado en `Aether.Physics2D`. Este sistema es independiente y no requiere el motor de física.

---

### Milestone 20.1 — Data types

**Archivos nuevos en `Alca.MonoGame.Kernel/Physics/Triggers/`:**

- **`TriggerShapeType.cs`** — `enum { AABB, Circle }`.
- **`TriggerOverlapInfo.cs`** — `readonly struct`; campos `TriggerZone2D Self`, `TriggerZone2D Other`; devuelto en eventos de solapamiento.

---

### Milestone 20.2 — TriggerWorld

**Nuevo:** `Alca.MonoGame.Kernel/Physics/Triggers/TriggerWorld.cs`

- `sealed class TriggerWorld`; registrado como servicio en `GameWorld`.
- `Register(TriggerZone2D)` / `Unregister(TriggerZone2D)` — llamados automáticamente desde `TriggerZone2D.Awake/OnDestroy`.
- `Update(GameTime)` — loop `for` indexado O(n²) sobre todos los triggers registrados; comprueba AABB vs AABB, Circle vs Circle y AABB vs Circle.
- Resultado por frame: distingue Enter, Stay, Exit usando sets de solapamientos previos (`HashSet<int>` pre-asignado por instancia de trigger).
- Cero heap allocations en Update: sets pre-asignados; swapping de listas con `_current` / `_previous` pre-inicializadas.

---

### Milestone 20.3 — TriggerZone2D

**Nuevo:** `Alca.MonoGame.Kernel/Physics/Triggers/TriggerZone2D.cs`

- `sealed class TriggerZone2D : GameBehaviour`.
- Propiedades: `TriggerShapeType Shape`, `Vector2 Size` (para AABB), `float Radius` (para Circle), `Vector2 Offset`.
- Propiedad computada `WorldBounds`: `RectangleF` basado en `Entity.Transform.Position + Offset`.
- Propiedad computada `WorldCenter`: `Vector2` para el círculo.
- Eventos: `Action<TriggerOverlapInfo>? OnEnter`, `OnStay`, `OnExit`.
- Auto-registro en `Awake`/auto-desregistro en `OnDestroy`.
- `LayerMask` (int bitmask) para filtrar con qué otras zonas puede solapar.

---

### Milestone 20.4 — GameWorld integration

**Modificado:** `Alca.MonoGame.Kernel/ECS/GameWorld.cs`

- Propiedad `TriggerWorld? TriggerWorld` (opcional, igual que `Physics2DWorld`).
- `Update` llama `TriggerWorld?.Update(gameTime)` tras el update de entidades.
- ServiceCollection extension: `AddTriggerWorld()`.

---

### Milestone 20.5 — Wiki + Tests

**Nuevo:** `Wiki/08-physics/trigger-volumes.md`

| Sección | Contenido |
|---------|-----------|
| Overview | Diferencia vs `Collider2D.IsTrigger`, cuándo usar cada uno |
| Setup | Registro en `GameWorld`, `TriggerZone2D` en escena |
| API | Propiedades, eventos OnEnter/Stay/Exit, LayerMask |
| Quickstart | Zona de pickup de ítem |

**Modificados:** `Wiki/08-physics/overview.md`, `Wiki/INDEX.md`

**Tests (`UnitTests/Physics/Triggers/`):**

| Archivo | Tests representativos |
|---------|----------------------|
| `TriggerWorldTests.cs` | Register/Unregister; AABB-AABB overlap; Circle-Circle; AABB-Circle; Enter/Stay/Exit state machine; LayerMask filtra correctamente |
| `TriggerZone2DTests.cs` | WorldBounds calcula offset; Shape Circle usa Radius; eventos se disparan |

---

## PHASE 21 ✅ COMPLETADA — Scene Transitions adicionales

> **Goal:** Ampliar `SceneManager` con un sistema de transiciones enchufables. Fade queda como default retrocompatible. Se añaden Slide, CircleWipe y Dissolve.

**Complexity:** Medium  
**Depends on:** Phases 1–20

---

### Milestone 21.1 — ISceneTransition + FadeTransition

**Nuevos en `Alca.MonoGame.Kernel/Scenes/Transitions/`:**

- **`ISceneTransition.cs`** — interface:
  - `void BeginTransitionOut(float durationSeconds)` — empieza salida.
  - `void BeginTransitionIn(float durationSeconds)` — empieza entrada.
  - `void Update(GameTime gameTime)`.
  - `void Draw(SpriteBatch spriteBatch, Viewport viewport)`.
  - `bool IsTransitionOutComplete { get; }`.
  - `bool IsTransitionInComplete { get; }`.
  - `void Reset()`.

- **`FadeTransition.cs`** — `sealed class FadeTransition : ISceneTransition`; extrae la lógica de fade actual de `SceneManager`. Color configurable (default `Color.Black`).

---

### Milestone 21.2 — SlideTransition + CircleWipeTransition

**Nuevos en `Alca.MonoGame.Kernel/Scenes/Transitions/`:**

- **`SlideDirection.cs`** — `enum { Left, Right, Up, Down }`.
- **`SlideTransition.cs`** — `sealed class SlideTransition : ISceneTransition`; captura la escena saliente en un `RenderTarget2D` (obtenido de `RenderTargetManager`); desliza el frame capturado con interpolación `SmoothStep`.
- **`CircleWipeTransition.cs`** — `sealed class CircleWipeTransition : ISceneTransition`; iris radial de fuera-a-dentro (out) y de dentro-a-fuera (in); dibuja usando `SpriteBatch` con stencil o dibujando un quad con shader de círculo; parámetro `WipeCenter` (default centro de pantalla).

---

### Milestone 21.3 — DissolveTransition

**Nuevo:** `Alca.MonoGame.Kernel/Scenes/Transitions/DissolveTransition.cs`

- `sealed class DissolveTransition : ISceneTransition`; captura el frame de la escena saliente en `RenderTarget2D`; dibuja el frame capturado con una opacidad que decrece según un threshold sobre una textura de ruido precargada.
- Constructor acepta `Texture2D? noiseTexture`; si es `null`, genera una textura de ruido en `Initialize`.
- `DissolveEdgeColor` configurable.

---

### Milestone 21.4 — SceneManager refactor

**Modificado:** `Alca.MonoGame.Kernel/Scenes/SceneManager.cs`

- `RequestChange(Scene scene, ISceneTransition? transition = null)` — usa `FadeTransition` por defecto si `transition` es null.
- `PushScene(Scene scene, ISceneTransition? transition = null)`.
- `PopScene(ISceneTransition? transition = null)`.
- La lógica interna de fade queda eliminada del SceneManager y delegada en `ISceneTransition`.
- `DefaultTransitionDuration` (float, segundos) se aplica a cualquier transición que no tenga duración propia.

---

### Milestone 21.5 — Wiki + Tests

**Nuevo:** `Wiki/03-scenes/transitions.md`

| Sección | Contenido |
|---------|-----------|
| Overview | ISceneTransition, tipos disponibles |
| FadeTransition | Color, duración |
| SlideTransition | SlideDirection, velocidad |
| CircleWipeTransition | WipeCenter |
| DissolveTransition | NoiseTexture, EdgeColor |
| Custom Transitions | Implementar ISceneTransition propio |

**Modificados:** `Wiki/03-scenes/scene-manager.md`, `Wiki/INDEX.md`

**Tests (`UnitTests/Scenes/Transitions/`):**

| Archivo | Tests representativos |
|---------|----------------------|
| `FadeTransitionTests.cs` | BeginOut avanza alpha; IsTransitionOutComplete a 1.0; Reset reinicia |
| `SlideTransitionTests.cs` | BeginOut desplaza offset; IsComplete cuando offset >= viewport |
| `CircleWipeTransitionTests.cs` | Radius crece/decrece con tiempo |

---

## PHASE 22 ✅ COMPLETADA — Ruido Procedural

> **Goal:** Implementar Perlin Noise y Simplex Noise con API estática zero-alloc, más un helper `NoiseMap` para generar mapas 2D precomputados.

**Complexity:** Low–Medium  
**Depends on:** Phases 1–19

---

### Milestone 22.1 — PerlinNoise

**Nuevo:** `Alca.MonoGame.Kernel/Mathematics/Noise/PerlinNoise.cs`

- `sealed class PerlinNoise` con constructor que acepta `int seed` (default 0); genera tabla de permutaciones en constructor, sin allocs posteriores.
- `float Get(float x)` — ruido 1D.
- `float Get(float x, float y)` — ruido 2D clásico de Perlin; resultado en [-1, 1].
- `float Get01(float x, float y)` — igual pero en [0, 1].
- `float Fractal(float x, float y, int octaves, float persistence, float lacunarity)` — fBm; cero alloc; resultado en [-1, 1].

---

### Milestone 22.2 — SimplexNoise

**Nuevo:** `Alca.MonoGame.Kernel/Mathematics/Noise/SimplexNoise.cs`

- `sealed class SimplexNoise` con constructor `int seed`.
- `float Get(float x, float y)` — Simplex 2D; resultado en [-1, 1].
- `float Get(float x, float y, float z)` — Simplex 3D.
- `float Get01(float x, float y)` — en [0, 1].
- Implementación basada en gradientes de Stefan Gustavson (sin dependencias externas).

---

### Milestone 22.3 — NoiseMap

**Nuevo:** `Alca.MonoGame.Kernel/Mathematics/Noise/NoiseMap.cs`

- `sealed class NoiseMap`; constructor `NoiseMap(int width, int height)` pre-asigna `float[,] _data`.
- `void Generate(PerlinNoise noise, float scale, int octaves, float persistence, float lacunarity)` — rellena `_data` en [0, 1]; cero alloc tras la construcción.
- `void Generate(SimplexNoise noise, float scale)` — variante Simplex.
- `float this[int x, int y]` — acceso directo.
- `Texture2D ToTexture(GraphicsDevice gd)` — genera textura en escala de grises; único punto de alloc.

---

### Milestone 22.4 — Wiki + Tests

**Nuevo:** `Wiki/12-misc/procedural-noise.md`

| Sección | Contenido |
|---------|-----------|
| Overview | Perlin vs Simplex, casos de uso |
| PerlinNoise | API, Fractal fBm, seeds |
| SimplexNoise | API 2D / 3D |
| NoiseMap | Generate, ToTexture, ejemplo de mapa de altura |

**Modificados:** `Wiki/12-misc/mathematics.md`, `Wiki/INDEX.md`

**Tests (`UnitTests/Mathematics/Noise/`):**

| Archivo | Tests representativos |
|---------|----------------------|
| `PerlinNoiseTests.cs` | Get(0,0) == 0; mismo seed → mismos valores; resultado en [-1,1]; Fractal octaves > 1 distintos de octaves == 1 |
| `SimplexNoiseTests.cs` | Get(0,0) == 0; 3D Get difiere de 2D; resultado en [-1,1] |
| `NoiseMapTests.cs` | Generate rellena sin alloc secundario; valores en [0,1]; ToTexture produce textura correcta |

---

## PHASE 23 ✅ COMPLETADA — Day/Night Cycle

> **Goal:** Servicio que hace avanzar el tiempo del día y modifica automáticamente `AmbientLight` del `LightingWorld`, interpolando entre 4 keyframes configurables (Medianoche, Amanecer, Mediodía, Atardecer).

**Complexity:** Medium  
**Depends on:** Phases 1–22

---

### Milestone 23.1 — TimeOfDay + DayNightKeyframe

**Nuevos en `Alca.MonoGame.Kernel/Lighting/DayNight/`:**

- **`TimeOfDay.cs`** — `readonly struct`; encapsula `float _hours` en [0, 24); propiedades `Hours`, `Minutes`, `TotalSeconds`; helpers `bool IsDaytime` (6 ≤ h < 20), `bool IsNighttime`; `static TimeOfDay Lerp(TimeOfDay a, TimeOfDay b, float t)` con wrap correcto; `static TimeOfDay FromHours(float h)`.

- **`DayNightKeyframe.cs`** — `readonly struct`; campos `TimeOfDay Time`, `Color AmbientColor`, `float AmbientIntensity` [0, 1], `float SunAngleDegrees`.

---

### Milestone 23.2 — DayNightProfile

**Nuevo:** `Alca.MonoGame.Kernel/Lighting/DayNight/DayNightProfile.cs`

- `sealed class DayNightProfile`; contiene 4 `DayNightKeyframe` fijos: `Midnight` (0h), `Sunrise` (6h), `Noon` (12h), `Sunset` (20h).
- `float DayDurationSeconds` — duración de un día completo en segundos de tiempo real (default 600).
- `DayNightKeyframe Sample(TimeOfDay time)` — interpola linealmente entre los dos keyframes más cercanos; cero alloc.
- Constructor con parámetros opcionales para los 4 keyframes + `DayDurationSeconds`.
- `static DayNightProfile Default` — perfil estándar con colores de día/noche.

---

### Milestone 23.3 — DayNightCycle

**Nuevo:** `Alca.MonoGame.Kernel/Lighting/DayNight/DayNightCycle.cs`

- `sealed class DayNightCycle`; servicio registrado en `GameWorld`.
- Constructor: `DayNightCycle(DayNightProfile profile, LightingWorld? lightingWorld = null)`.
- Propiedades: `TimeOfDay CurrentTime { get; }`, `float TimeScale` (multiplicador, default 1f), `bool Paused`.
- `void SetTime(TimeOfDay time)` — cambio instantáneo.
- `Update(GameTime)` — avanza `CurrentTime` según `DayDurationSeconds * TimeScale`; aplica `DayNightProfile.Sample()` a `LightingWorld.AmbientLight` si no es null; cero alloc.
- Eventos (Action): `OnSunrise`, `OnNoon`, `OnSunset`, `OnMidnight` — disparados una vez por cruce de umbral; usa flags booleanos por keyframe para evitar doble disparo.
- Integración opcional con `WeatherWorld`: expone `CurrentTime` para que `WeatherWorld` ajuste `TemperatureBase`.

---

### Milestone 23.4 — GameWorld integration

**Modificado:** `Alca.MonoGame.Kernel/ECS/GameWorld.cs`

- Propiedad `DayNightCycle? DayNightCycle`.
- `Update` llama `DayNightCycle?.Update(gameTime)`.
- Extension: `AddDayNightCycle(DayNightProfile? profile = null)`.

---

### Milestone 23.5 — Wiki + Tests

**Nuevo:** `Wiki/09-lighting/day-night.md`

| Sección | Contenido |
|---------|-----------|
| Overview | Concepto, integración con LightingWorld y WeatherWorld |
| DayNightProfile | Keyframes, interpolación, perfil Default |
| DayNightCycle | Setup, TimeScale, Paused, eventos, SetTime |
| Quickstart | Ciclo básico en 10 minutos con colores personalizados |

**Modificados:** `Wiki/09-lighting/overview.md`, `Wiki/02-ecs/game-world.md`, `Wiki/INDEX.md`

**Tests (`UnitTests/Lighting/DayNight/`):**

| Archivo | Tests representativos |
|---------|----------------------|
| `TimeOfDayTests.cs` | FromHours(25) wrappea a 1h; Lerp entre 23h y 1h usa wrap corto; IsDaytime correcto |
| `DayNightProfileTests.cs` | Sample(6h) devuelve Sunrise; Sample(9h) interpola entre Sunrise y Noon |
| `DayNightCycleTests.cs` | Update avanza tiempo; TimeScale 2× avanza doble; OnSunrise se dispara una vez; Paused congela tiempo; SetTime(12h) dispara OnNoon si umbral cruzado |

---

## PHASE 24 ✅ COMPLETADA — Shader Library 2D

> **Goal:** Librería de materiales y efectos de post-proceso listos para usar, sin requerir que el desarrollador escriba HLSL. Construida sobre `SpriteMaterial` y `PostProcessEffect` existentes.

**Complexity:** Medium–High  
**Depends on:** Phases 1–23

---

### Milestone 24.1 — OutlineMaterial + FlashMaterial

**Nuevos archivos HLSL en `Content/Effects/`:**

- **`Outline.fx`** — sample alpha del sprite + sample de 4 píxeles adyacentes; si algún vecino tiene alpha > threshold y el píxel central es transparente → pinta `OutlineColor`.
- **`Flash.fx`** — mezcla `SpriteTexture` color con `FlashColor` según `FlashIntensity`; si `FlashIntensity == 1` → color sólido.

**Nuevos en `Alca.MonoGame.Kernel/Graphics/Shaders/`:**

- **`OutlineMaterial.cs`** — `sealed class OutlineMaterial : SpriteMaterial`; parámetros: `Color OutlineColor`, `float OutlineThickness` (pixels, default 1), `float AlphaThreshold` (default 0.1f); actualiza `Effect` en `Apply()`.
- **`FlashMaterial.cs`** — `sealed class FlashMaterial : SpriteMaterial`; parámetros: `Color FlashColor`, `float FlashIntensity` [0, 1]; útil para hit flash en `Update` con lerp hacia 0.

---

### Milestone 24.2 — DissolveMaterial + GlowMaterial

**Nuevos archivos HLSL:**

- **`Dissolve.fx`** — sample `NoiseTexture` en UV; si `noiseValue < Progress` → discard; borde coloreado si `noiseValue < Progress + EdgeWidth`.
- **`Glow.fx`** — múltiples samples desplazados de `SpriteTexture` sumados con `BlendState.Additive`; `GlowRadius` controla desplazamiento en UV.

**Nuevos en `Alca.MonoGame.Kernel/Graphics/Shaders/`:**

- **`DissolveMaterial.cs`** — `sealed class DissolveMaterial : SpriteMaterial`; parámetros: `float Progress` [0, 1], `Color EdgeColor`, `float EdgeWidth`, `Texture2D? NoiseTexture` (si null usa ruido de `NoiseMap` 64×64 generado en `Initialize`).
- **`GlowMaterial.cs`** — `sealed class GlowMaterial : SpriteMaterial`; parámetros: `Color GlowColor`, `float GlowIntensity`, `int GlowRadius` (pixels).

---

### Milestone 24.3 — SilhouetteMaterial + CRTPostEffect

**Nuevos archivos HLSL:**

- **`Silhouette.fx`** — si `alpha > threshold` → output `(SilhouetteColor.rgb, alpha)`; ignora color original.
- **`CRT.fx`** — post-proceso: scanlines horizontales con brillo alternado, distorsión barrel leve, viñeta radial.

**Nuevos archivos C#:**

- **`Alca.MonoGame.Kernel/Graphics/Shaders/SilhouetteMaterial.cs`** — `sealed class SilhouetteMaterial : SpriteMaterial`; parámetro: `Color SilhouetteColor`.
- **`Alca.MonoGame.Kernel/Graphics/Effects/CRTPostEffect.cs`** — `sealed class CRTPostEffect : PostProcessEffect`; parámetros: `float ScanlineIntensity` [0, 1], `float BarrelDistortion` [0, 1], `float VignetteRadius` [0, 1].

---

### Milestone 24.4 — Wiki + Tests

**Nuevo:** `Wiki/04-graphics/shader-library.md`

| Sección | Contenido |
|---------|-----------|
| Overview | Cuándo usar cada material vs shader custom |
| OutlineMaterial | OutlineColor, OutlineThickness, AlphaThreshold |
| FlashMaterial | FlashColor, FlashIntensity, patrón de hit flash con Tweening |
| DissolveMaterial | Progress, EdgeColor, NoiseTexture custom |
| GlowMaterial | GlowColor, GlowRadius, GlowIntensity |
| SilhouetteMaterial | SilhouetteColor, uso con oclusión |
| CRTPostEffect | Setup en pipeline de post-proceso, parámetros |

**Modificados:** `Wiki/04-graphics/shaders.md`, `Wiki/INDEX.md`

**Tests (`UnitTests/Graphics/Shaders/`):**

| Archivo | Tests representativos |
|---------|----------------------|
| `OutlineMaterialTests.cs` | Constructor válido; Apply() no lanza; OutlineThickness actualiza parámetro del Effect |
| `FlashMaterialTests.cs` | FlashIntensity 0 → sin cambio visual; Apply() no lanza |
| `DissolveMaterialTests.cs` | Progress fuera de [0,1] se clampea; NoiseTexture null genera textura default |

---

## PHASE 25 ✅ COMPLETADA — Soporte 2.5D

> **Goal:** Helpers y componentes para juegos isométricos y top-down con profundidad. Y-Sort automático, conversiones de coordenadas isométricas, cámara isométrica y Normal Maps para que los sprites 2D reaccionen a la iluminación dinámica.

**Complexity:** Medium–High  
**Depends on:** Phases 1–24

---

### Milestone 25.1 — IsometricHelper

**Nuevo:** `Alca.MonoGame.Kernel/Mathematics/IsometricHelper.cs`

- `sealed class IsometricHelper`; constructor `IsometricHelper(float tileWidth, float tileHeight)`.
- `Vector2 WorldToScreen(Vector2 worldPos)` — proyección isométrica estándar.
- `Vector2 ScreenToWorld(Vector2 screenPos)` — inversa.
- `float DepthFromWorldY(float worldY, float worldHeight)` — normaliza a [0, 1] para `LayerDepth`; entidades más abajo (Y mayor) se dibujan encima.
- `static readonly IsometricHelper Default` — tile 64×32 estándar.

---

### Milestone 25.2 — YSortRendererBehaviour

**Nuevo:** `Alca.MonoGame.Kernel/Graphics/Sprites/YSortRendererBehaviour.cs`

- `sealed class YSortRendererBehaviour : GameBehaviour`; misma API que `SpriteRendererBehaviour`.
- `float WorldHeight` — altura máxima del mundo en píxeles para normalizar Y.
- `int YOffset` — ajuste fino del punto de anclaje vertical (útil para sprites con sombra).
- En `Draw()`: calcula `LayerDepth = 1f - MathHelper.Clamp((Transform.Position.Y + YOffset) / WorldHeight, 0f, 1f)` y lo pasa a `SpriteBatch.Draw`.
- Sin LINQ; sin allocs en Draw.

---

### Milestone 25.3 — IsometricCamera + BillboardSpriteBehaviour

**Nuevos archivos:**

- **`Alca.MonoGame.Kernel/Graphics/Camera/IsometricCamera.cs`** — `sealed class IsometricCamera`; encapsula `Camera2D`; expone `IsometricHelper TileHelper`; override `ScreenToWorld(Vector2)` que usa `TileHelper.ScreenToWorld`; método `CenterOn(Vector2 worldPos)`.

- **`Alca.MonoGame.Kernel/Graphics/Sprites/BillboardSpriteBehaviour.cs`** — `sealed class BillboardSpriteBehaviour : GameBehaviour`; requiere un `Camera2D` referenciado; en `Draw()` cancela la rotación del Transform con la rotación inversa de la cámara para que el sprite siempre quede vertical en pantalla; útil para efectos, sombras o NPCs en 2.5D.

---

### Milestone 25.4 — NormalMapSpriteMaterial + shader

**Nuevo archivo HLSL:** `Content/Effects/NormalMapSprite.fx`

- Sample `SpriteTexture` (albedo) + `NormalMapTexture`.
- Para cada `PointLight2D` del buffer (máx 8, igual que el pipeline de `LightingWorld`): calcula ángulo luz→fragmento, dot con normal decodificada del normal map, acumula difuso.
- Combina ambiental de `LightingWorld` + difuso acumulado.

**Nuevo:** `Alca.MonoGame.Kernel/Graphics/Sprites/NormalMapSpriteMaterial.cs`

- `sealed class NormalMapSpriteMaterial : SpriteMaterial`; parámetros: `Texture2D NormalMap`, `float NormalStrength` [0, 1], `Color AmbientColor`.
- Método `SyncLights(LightingWorld lightingWorld)` — copia datos de las luces activas al buffer del shader; llamado desde el sistema de renderizado antes de `Draw`.

---

### Milestone 25.5 — Wiki + Tests

**Nuevo:** `Wiki/04-graphics/twopointfived.md`

| Sección | Contenido |
|---------|-----------|
| Overview | 2.5D vs 3D, cuándo usarlo, tipos de perspectiva |
| IsometricHelper | WorldToScreen / ScreenToWorld, DepthFromWorldY |
| YSortRendererBehaviour | WorldHeight, YOffset, diferencia con SpriteRendererBehaviour |
| IsometricCamera | Setup, CenterOn, ScreenToWorld isométrico |
| BillboardSpriteBehaviour | Configuración, requiere Camera2D |
| NormalMapSpriteMaterial | NormalMap, SyncLights, integración con LightingWorld |

**Modificados:** `Wiki/04-graphics/sprites.md`, `Wiki/04-graphics/camera-2d.md`, `Wiki/INDEX.md`

**Tests:**

| Archivo | Tests representativos |
|---------|----------------------|
| `UnitTests/Mathematics/IsometricHelperTests.cs` | WorldToScreen→ScreenToWorld roundtrip; DepthFromWorldY normaliza en [0,1] |
| `UnitTests/Graphics/Sprites/YSortRendererBehaviourTests.cs` | LayerDepth disminuye al aumentar Y; clamp a [0,1] |

---

## PHASE 26 ✅ COMPLETADA — Sistema de Diálogo / Narrativa

> **Goal:** Sistema completo de diálogos con branching, typewriter effect zero-alloc, speaker portraits y selección de opciones. Compatible con Localization y InputManager existentes.

**Complexity:** Medium–High  
**Depends on:** Phases 1–25

---

### Milestone 26.1 — Data types

**Nuevos en `Alca.MonoGame.Kernel/Dialogue/`:**

- **`DialogueCondition.cs`** — `readonly struct`; campos `string Key`, `string Value`; `bool IsEmpty`.
- **`DialogueChoice.cs`** — `readonly struct`; campos `string LocalizationKey`, `int NextLineIndex`, `DialogueCondition Condition`.
- **`DialogueLine.cs`** — `readonly struct`; campos `string SpeakerId`, `string LocalizationKey`, `string PortraitKey`, `DialogueChoice[] Choices` (puede ser vacío → sin opciones, avance automático); propiedad `bool HasChoices`.

---

### Milestone 26.2 — DialogueScript

**Nuevo:** `Alca.MonoGame.Kernel/Dialogue/DialogueScript.cs`

- `sealed class DialogueScript`; almacena `DialogueLine[]` inmutable.
- Constructor `DialogueScript(DialogueLine[] lines)`.
- `int LineCount { get; }`.
- `ref readonly DialogueLine GetLine(int index)` — acceso por referencia sin copia.
- `bool TryGetLine(int index, out DialogueLine line)`.
- Builder estático fluent: `DialogueScript.Builder` con `AddLine(...)`, `AddChoice(...)`, `Build()`.

---

### Milestone 26.3 — DialogueManager

**Nuevo:** `Alca.MonoGame.Kernel/Dialogue/DialogueManager.cs`

- `sealed class DialogueManager`; servicio registrado en `GameWorld`.
- `void StartDialogue(DialogueScript script)` — abre script, lanza `OnStarted`.
- `void Advance()` — avanza a siguiente línea (si sin opciones); lanza `OnLineChanged`.
- `void SelectChoice(int choiceIndex)` — elige opción y salta a `NextLineIndex`; lanza `OnChoiceMade`.
- `void EndDialogue()` — cierra script; lanza `OnEnded`.
- `bool IsActive { get; }`.
- `ref readonly DialogueLine CurrentLine { get; }`.
- Eventos: `Action<DialogueScript>? OnStarted`, `Action<DialogueLine>? OnLineChanged`, `Action<int>? OnChoiceMade`, `Action? OnEnded`.
- Función `Func<DialogueCondition, bool>? ConditionEvaluator` — inyectable desde el juego para validar condiciones de choices; si null todas las condiciones se consideran cumplidas.

---

### Milestone 26.4 — TypewriterEffect

**Nuevo:** `Alca.MonoGame.Kernel/Dialogue/TypewriterEffect.cs`

- `sealed class TypewriterEffect`; zero-alloc: pre-asigna `StringBuilder _buffer` con capacidad fija; usa índice de carácter `_charIndex`.
- Constructor: `TypewriterEffect(int maxCapacity = 512)`.
- `void SetText(string fullText)` — copia a buffer interno, resetea índice; cero alloc si `fullText.Length <= maxCapacity`.
- `void Advance(float deltaTime)` — incrementa `_charIndex` según `CharsPerSecond`; acumula tiempo fraccionario en `_accumulator`.
- `void CompleteInstantly()` — `_charIndex = _buffer.Length`.
- `ReadOnlySpan<char> CurrentText` — devuelve `_buffer.ToString(0, _charIndex)` … **nota:** para evitar alloc en Draw se puede exponer `void Draw(SpriteBatch, SpriteFont, Vector2, Color)` que use `StringBuilder` directamente.
- `bool IsComplete { get; }`.
- `float CharsPerSecond` (default 30f).
- `Action? OnComplete`.

---

### Milestone 26.5 — DialogueBoxBehaviour + ChoicesPanelBehaviour

**Nuevos en `Alca.MonoGame.Kernel/Dialogue/`:**

- **`DialogueBoxBehaviour.cs`** — `sealed class DialogueBoxBehaviour : GameBehaviour`; crea internamente un `Panel` + `Label` (speaker) + `Label` (texto) en `Initialize`; referencia `DialogueManager` inyectado; suscribe a `OnLineChanged` y llama `TypewriterEffect.SetText`; en `Update`: `TypewriterEffect.Advance(dt)`; detecta input de avance (Space o botón A de gamepad) vía `InputManager`; si `IsComplete` → llama `DialogueManager.Advance()`; si no → `TypewriterEffect.CompleteInstantly()`.
- Propiedad `Texture2D? PortraitTexture` — sprite de retrato del speaker; actualizado al cambiar línea.
- `bool Visible` — oculta/muestra el cuadro.

- **`ChoicesPanelBehaviour.cs`** — `sealed class ChoicesPanelBehaviour : GameBehaviour`; crea `StackPanel` con `Button[]` pre-asignados (máx configurable, default 4) en `Initialize`; en `OnLineChanged` actualiza textos y visibilidad; en click de botón → `DialogueManager.SelectChoice(index)`; soporte de navegación por teclado/gamepad vía `UIFocusManager`.

---

### Milestone 26.6 — GameWorld integration + Wiki + Tests

**Modificado:** `Alca.MonoGame.Kernel/ECS/GameWorld.cs`

- Propiedad `DialogueManager? DialogueManager`.
- Extension: `AddDialogueManager()`.

**Nuevos en `Wiki/14-dialogue/`:**

| Archivo | Contenido |
|---------|-----------|
| `overview.md` | Arquitectura, diagrama de flujo, casos de uso |
| `script.md` | `DialogueLine`, `DialogueChoice`, `DialogueCondition`, Builder API |
| `manager.md` | `DialogueManager`, eventos, `ConditionEvaluator` |
| `typewriter.md` | `TypewriterEffect`, CharsPerSecond, zero-alloc pattern |
| `choices.md` | `ChoicesPanelBehaviour`, setup, navegación |

**Modificados:** `Wiki/02-ecs/game-world.md`, `Wiki/INDEX.md`, `Wiki/README.md`

**Tests (`UnitTests/Dialogue/`):**

| Archivo | Tests representativos |
|---------|----------------------|
| `DialogueLineTests.cs` | HasChoices false sin choices; HasChoices true con choices; struct es inmutable |
| `DialogueScriptTests.cs` | Builder AddLine/AddChoice/Build; GetLine(0) devuelve primera; TryGetLine fuera de rango false |
| `DialogueManagerTests.cs` | StartDialogue lanza OnStarted; Advance sin choices avanza; SelectChoice válido salta a NextLineIndex; SelectChoice inválido no avanza; EndDialogue lanza OnEnded; ConditionEvaluator false oculta choice |
| `TypewriterEffectTests.cs` | Advance(1/30) avanza 1 char; CompleteInstantly completa; SetText resetea; OnComplete dispara |

---

## Acceptance Checklist

- [x] `TriggerWorld` detecta Enter/Stay/Exit para AABB y Circle sin `Physics2DWorld` activo
- [x] `LayerMask` en `TriggerZone2D` filtra colisiones entre grupos correctamente
- [x] `SceneManager.RequestChange()` sin parámetro usa `FadeTransition` (retrocompatible)
- [x] `SlideTransition` y `CircleWipeTransition` capturan el frame saliente sin artifacts
- [x] `PerlinNoise.Get(x,y)` con mismo seed produce mismos valores; resultado en [-1, 1]
- [x] `SimplexNoise.Get(x,y,z)` produce valores continuos; sin artefactos de bloque
- [x] `NoiseMap.Generate()` no aloca heap tras la construcción del objeto
- [x] `DayNightCycle.Update()` avanza tiempo y actualiza `LightingWorld.AmbientLight` cada frame
- [x] Eventos `OnSunrise`/`OnNoon`/`OnSunset`/`OnMidnight` se disparan exactamente una vez por ciclo
- [x] `TimeScale 0` congela el tiempo; `TimeScale 2` duplica la velocidad
- [x] `OutlineMaterial` y `FlashMaterial` se aplican a sprites sin errores de compilación de shader
- [x] `DissolveMaterial.Progress = 1` hace el sprite invisible; `Progress = 0` sin cambio
- [x] `IsometricHelper.WorldToScreen → ScreenToWorld` roundtrip sin drift
- [x] `YSortRendererBehaviour` ordena entidades correctamente (entidad con mayor Y se dibuja encima)
- [x] `NormalMapSpriteMaterial.SyncLights()` integra con `LightingWorld` correctamente
- [x] `DialogueManager.StartDialogue()` lanza `OnStarted` y expone `CurrentLine`
- [x] `DialogueManager.Advance()` en línea con choices no avanza (requiere `SelectChoice`)
- [x] `TypewriterEffect.Advance()` cero allocs; `CurrentText` crece caracter a caracter
- [x] `ConditionEvaluator` false en choice → choice no aparece en `ChoicesPanelBehaviour`
- [x] `dotnet test` — todos los tests nuevos de UnitTests en verde

---

## Technical Reference (v5 additions)

### Archivos nuevos

```
src/Library/Alca.MonoGame.Kernel/
├── Physics/Triggers/
│   ├── TriggerShapeType.cs
│   ├── TriggerOverlapInfo.cs
│   ├── TriggerZone2D.cs
│   └── TriggerWorld.cs
├── Scenes/Transitions/
│   ├── ISceneTransition.cs
│   ├── FadeTransition.cs
│   ├── SlideDirection.cs
│   ├── SlideTransition.cs
│   ├── CircleWipeTransition.cs
│   └── DissolveTransition.cs
├── Mathematics/Noise/
│   ├── PerlinNoise.cs
│   ├── SimplexNoise.cs
│   └── NoiseMap.cs
├── Lighting/DayNight/
│   ├── TimeOfDay.cs
│   ├── DayNightKeyframe.cs
│   ├── DayNightProfile.cs
│   └── DayNightCycle.cs
├── Graphics/Shaders/
│   ├── OutlineMaterial.cs
│   ├── FlashMaterial.cs
│   ├── DissolveMaterial.cs
│   ├── GlowMaterial.cs
│   └── SilhouetteMaterial.cs
├── Graphics/Effects/
│   └── CRTPostEffect.cs
├── Graphics/Sprites/
│   ├── YSortRendererBehaviour.cs
│   ├── BillboardSpriteBehaviour.cs
│   └── NormalMapSpriteMaterial.cs
├── Graphics/Camera/
│   └── IsometricCamera.cs
├── Mathematics/
│   └── IsometricHelper.cs
└── Dialogue/
    ├── DialogueCondition.cs
    ├── DialogueChoice.cs
    ├── DialogueLine.cs
    ├── DialogueScript.cs
    ├── DialogueManager.cs
    ├── TypewriterEffect.cs
    ├── DialogueBoxBehaviour.cs
    └── ChoicesPanelBehaviour.cs

Content/Effects/
├── Outline.fx
├── Flash.fx
├── Dissolve.fx
├── Glow.fx
├── Silhouette.fx
├── CRT.fx
└── NormalMapSprite.fx

src/Library/Alca.MonoGame.Kernel.UnitTests/
├── Physics/Triggers/
│   ├── TriggerWorldTests.cs
│   └── TriggerZone2DTests.cs
├── Scenes/Transitions/
│   ├── FadeTransitionTests.cs
│   ├── SlideTransitionTests.cs
│   └── CircleWipeTransitionTests.cs
├── Mathematics/Noise/
│   ├── PerlinNoiseTests.cs
│   ├── SimplexNoiseTests.cs
│   └── NoiseMapTests.cs
├── Lighting/DayNight/
│   ├── TimeOfDayTests.cs
│   ├── DayNightProfileTests.cs
│   └── DayNightCycleTests.cs
├── Graphics/Shaders/
│   ├── OutlineMaterialTests.cs
│   ├── FlashMaterialTests.cs
│   └── DissolveMaterialTests.cs
├── Mathematics/
│   └── IsometricHelperTests.cs
├── Graphics/Sprites/
│   └── YSortRendererBehaviourTests.cs
└── Dialogue/
    ├── DialogueLineTests.cs
    ├── DialogueScriptTests.cs
    ├── DialogueManagerTests.cs
    └── TypewriterEffectTests.cs

src/Library/Wiki/
├── 08-physics/trigger-volumes.md
├── 03-scenes/transitions.md          (actualizado)
├── 12-misc/procedural-noise.md
├── 09-lighting/day-night.md
├── 04-graphics/shader-library.md
├── 04-graphics/twopointfived.md
└── 14-dialogue/
    ├── overview.md
    ├── script.md
    ├── manager.md
    ├── typewriter.md
    └── choices.md

src/Library/docs/
├── roadmap-v5.md   ← este fichero
└── specs/          ← un fichero por fase (phase20-triggers.md, etc.)
```

### Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `ECS/GameWorld.cs` | + `TriggerWorld?`, `DayNightCycle?`, `DialogueManager?` + llamadas `?.Update(gameTime)` |
| `Scenes/SceneManager.cs` | `RequestChange/PushScene/PopScene` aceptan `ISceneTransition?`; lógica de fade delegada a `FadeTransition` |
| `Wiki/02-ecs/game-world.md` | + filas nuevas en tabla de propiedades |
| `Wiki/08-physics/overview.md` | + referencia a trigger volumes |
| `Wiki/09-lighting/overview.md` | + referencia a DayNightCycle |
| `Wiki/03-scenes/scene-manager.md` | + sección de transiciones enchufables |
| `Wiki/README.md` | + filas Dialogue en tabla de módulos |
| `Wiki/INDEX.md` | + sección 14 completa |

### New NuGet Dependencies

Ninguna. Los sistemas reutilizan:
- `MonoGame.Framework.DesktopGL` (existing) — shaders, RenderTarget2D, SpriteBatch
- `Microsoft.Extensions.DependencyInjection` (existing) — registro de servicios
- `MonoGame.Extended` (existing) — RenderTargetManager, Tweening en transiciones

### Key Design Decisions

**¿Por qué `TriggerWorld` es independiente de `Physics2DWorld` en lugar de reutilizar `Aether.Physics2D`?**  
`Physics2DWorld` es opcional y pesado; requiere `RigidBody2D` en cada entidad. Para casos simples como zonas de activación de diálogo o cambio de música, crear cuerpos físicos es overengineering. `TriggerWorld` es un loop AABB/Circle puro sin dependencias externas, activable de forma independiente.

**¿Por qué `ISceneTransition` en lugar de extender `SceneManager` directamente?**  
Permite que el desarrollador implemente transiciones personalizadas sin modificar la librería. `FadeTransition` es el default para retrocompatibilidad total; `SceneManager` no necesita conocer los detalles de ninguna transición concreta.

**¿Por qué `PerlinNoise` y `SimplexNoise` son instancias con seed en lugar de métodos estáticos con seed como parámetro?**  
Las tablas de permutación derivadas del seed se calculan una vez en el constructor y se cachean. Si fuesen métodos estáticos con seed como parámetro, cada llamada recalcularía la tabla o requeriría un caché global estático. La instancia es más limpia y permite múltiples generadores con seeds diferentes en paralelo.

**¿Por qué `TypewriterEffect` pre-asigna `StringBuilder` en lugar de manipular el `string` original?**  
Un `string` en C# es inmutable; substrings generan heap allocations. `StringBuilder` pre-asignado permite construir el texto visible sin allocs frame a frame. La propiedad `CurrentText` como `ReadOnlySpan<char>` (o método Draw directo) evita el `ToString()` que sí allocaría.

**¿Por qué `DayNightCycle` usa 4 keyframes fijos en lugar de una curva arbitraria?**  
4 keyframes cubren el 95% de los casos de uso (amanecer, mediodía, atardecer, noche) y simplifican la API. Para casos avanzados, `DayNightProfile` es una clase sellada que el desarrollador puede extender con un constructor personalizado con más keyframes. La interpolación lineal entre 2 keyframes adyacentes es O(1) y cero-alloc.
