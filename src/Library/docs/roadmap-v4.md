# Roadmap v4: Weather & Climatology System

**Reglas transversales a todos los desarrollos:**
- Al terminar, actualizar este fichero marcando los TODOs completados.

## Executive Summary

### v3 Accomplishments (Phases 11–18) ✅

v3 cierra las brechas de integración entre subsistemas, añade 12 características faltantes y mejora la arquitectura de ECS, iluminación y navegación. Se entregaron: `ParticleEmitterBehaviour`, integración automática Escena↔GameWorld, física de colisiones (capas, máscaras, raycast), FSM genérica, `TimerManager`, `EventBus`, debug draw, shake de cámara, fade UI, crossfade de audio, Steering Behaviors, sync automático `[NetSync]`, pipeline GPU de iluminación, A* async, y benchmarks de rendimiento.

### v4 Objective

v4 añade el **sistema de climatología** completo: temperatura en tiempo real, 10 tipos de clima predefinidos, extensibilidad con `WeatherTypeId` string-based, transiciones suaves entre perfiles, integración con iluminación, física, partículas y audio espacial.

---

## Transversal Rules (inherited from v1–v3)

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

## PHASE 19 ✅ COMPLETADA — Weather System

> **Goal:** Sistema de climatología que distingue temperatura (dato numérico continuo) de clima (estado cualitativo), con transiciones suaves, 10 tipos predefinidos, extensibilidad ilimitada y cero asignaciones de heap en el game loop.

**Complexity:** Medium–High  
**Depends on:** Phases 1–18 (all complete)

---

### Milestone 19.1 ✅ — Data layer

**Archivos nuevos en `Alca.MonoGame.Kernel/Weather/`:**

- **`WeatherTypeId.cs`** — `readonly struct` wrapper de `string`; 10 tipos estáticos predefinidos (`Sunny`, `HeatWave`, `Cloudy`, `Fog`, `Storm`, `Thunderstorm`, `HailStorm`, `Blizzard`, `ColdSnap`, `OrangeWind`); igualdad case-insensitive.
- **`PrecipitationIntensity.cs`** — enum `{ None, Low, Medium, High, VeryHigh }`.
- **`WeatherProfile.cs`** — `readonly struct` con 22 campos; método estático `Lerp(from, to, t)`.
- **`WeatherProfiles.cs`** — clase estática con los 10 perfiles predefinidos y método `Get(WeatherTypeId)`.
- **`WindState.cs`** — `readonly struct`; `ComputeEffectiveForce(float, float)` zero-alloc con turbulencia sinusoidal.
- **`LightningStrikeEvent.cs`** — `readonly struct` con Position, Intensity, ImpulseRadius, ImpulseStrength.

---

### Milestone 19.2 ✅ — WeatherWorld + GameWorld

**Nuevo:** `Alca.MonoGame.Kernel/Weather/WeatherWorld.cs`  
**Modificado:** `Alca.MonoGame.Kernel/ECS/GameWorld.cs`

- `WeatherWorld`: catálogo de 16 entradas iniciales, temperatura sinusoidal, viento interpolado, restricción viento↔niebla, transición Lerp, subsistemas opcionales, evento `LightningStruck`.
- `GameWorld.WeatherWorld`: propiedad opcional; `Update` llamado automáticamente.

---

### Milestone 19.3 ✅ — WeatherBehaviour (ECS)

**Nuevo:** `Alca.MonoGame.Kernel/Weather/WeatherBehaviour.cs`

- `sealed class WeatherBehaviour : GameBehaviour`
- Auto-registro/desregistro en `Awake`/`OnDestroy`.
- `ReceivesWind`, `WindForceMultiplier`, `ReceivesLightningImpulse`.
- Requiere `RigidBody2D` hermano añadido antes del componente.

---

### Milestone 19.4 ✅ — WeatherParticleLayer

**Nuevo:** `Alca.MonoGame.Kernel/Weather/WeatherParticleLayer.cs`

- 5 efectos MonoGame.Extended: lluvia, nieve, granizo, niebla, viento.
- `LoadContent(rain, snow, hail, fog, wind)` — crea efectos y cachea `LinearGravityModifier`.
- Viento actualizado O(1) mutando los modificadores cacheados — cero alloc en `Update`.
- Selección de efecto por heurística de temperatura/viento del perfil activo.
- Dibujado con `BlendState.Additive` por efecto.

---

### Milestone 19.5 ✅ — LightningController

**Nuevo:** `Alca.MonoGame.Kernel/Weather/LightningController.cs`

- Entidad flash con `PointLight2D` creada en constructor (inactiva).
- Temporizador aleatorio en `[LightningMinInterval, LightningMaxInterval]`.
- Dispatch de impulso itera `List<WeatherBehaviour>` — cero alloc.
- Eleva `WeatherWorld.RaiseLightningStruck(in LightningStrikeEvent)`.
- `TriggerStrikeAt(Vector2)` para disparo manual.

---

### Milestone 19.6 ✅ — WeatherAudioLayer

**Nuevo:** `Alca.MonoGame.Kernel/Weather/WeatherAudioLayer.cs`

- 3 `SoundEffectInstance` en loop: lluvia, viento, trueno ambient.
- Pool pre-asignado de truenos espaciales (`thunderPoolSize` por defecto 4).
- Volumen interpolado con `FadeSpeed` — cero alloc en `Update`.
- `Channel` opcional para routing de `AudioMixer`.

---

### Milestone 19.7 ✅ — WeatherScene demo

**Nuevo:** `Alca.MonoGame.Demo/Scenes/WeatherScene.cs`  
**Modificados:** `DemoGame.cs`, `UIScene_Menu.cs`

- 10 botones de tipos predefinidos + botón "Radioactive Rain" custom.
- Slider de duración de transición (0–10 s).
- Labels de info: CurrentWeather, CurrentTemperature, CurrentWind (StringBuilder reutilizado).
- 12 entidades hoja con `WeatherBehaviour.ReceivesWind=true` y física.
- Overlay de niebla (quad full-screen, alpha = `FogDensity`).
- Botón "Boost Wind" que demuestra `ModifyProfile()` en runtime.
- Fondo con `SkyColor` del perfil activo.

---

### Milestone 19.8 ✅ — Wiki + Tests

**Nuevos (wiki):** `src/Library/Wiki/13-weather/`

| Archivo | Contenido |
|---|---|
| `overview.md` | Diagrama del sistema, distinción temperatura/clima, tabla de tipos, quickstart |
| `profiles.md` | Referencia completa de `WeatherProfile`, catálogo, `CustomData`, `WeatherTypeId` |
| `weather-world.md` | API de `WeatherWorld`, `RegisterCustomWeather`, `ModifyProfile`, temperatura, viento |
| `behaviour.md` | `WeatherBehaviour`, `ReceivesWind`, `ReceivesLightningImpulse` |
| `particles.md` | `WeatherParticleLayer`, setup, heurística de selección, camera-follow |
| `lightning.md` | `LightningController`, `TriggerStrikeAt`, evento `LightningStruck` |
| `audio.md` | `WeatherAudioLayer`, setup, `LoadSounds`, FadeSpeed, channel routing |

**Modificados:** `Wiki/README.md`, `Wiki/INDEX.md`, `Wiki/02-ecs/game-world.md`

**Tests (86 tests en `UnitTests/Weather/`):**

| Archivo | Tests |
|---|---|
| `WeatherTypeIdTests.cs` | 12 — igualdad, case-insensitivity, predefinidos distintos |
| `WeatherProfileTests.cs` | 9 — Lerp en t=0/0.5/1, bools, CustomData |
| `WindStateTests.cs` | 7 — IsCalm, ComputeEffectiveForce, WorldUnitsPerKmh |
| `WeatherWorldTests.cs` | 19 — SetWeather, transición, temperatura, RegisterCustomWeather, ModifyProfile, event |
| `WeatherBehaviourTests.cs` | 9 — registro, ApplyWindForce, ApplyLightningImpulse |
| `LightningControllerTests.cs` | 12 — flash, temporizador, evento, Dispose |
| `WeatherAudioLayerTests.cs` | 9 — null sounds, Update, Dispose |
| `WeatherParticleLayerTests.cs` | 9 — null textures, Update, Draw, Dispose |

---

## Acceptance Checklist

- [x] `WeatherWorld.CurrentWeather` y `CurrentTemperature` devuelven valores correctos en cualquier momento
- [x] Registrar clima custom con JSON → `TryGetProfile` devuelve el perfil; `CustomData` accesible
- [x] Intentar clima con fog + wind → warning en debug, `FogDensity` forzado a 0
- [x] `ModifyProfile()` en runtime → cambio visible en el siguiente frame
- [x] `dotnet test` — todos los tests de `UnitTests/Weather/` en verde (86/86)
- [x] Demo Scene 42: 10 tipos, temperatura visible, viento mueve entidades, niebla overlay, ModifyProfile

---

## Technical Reference (v4 additions)

### Nuevos archivos

```
src/Library/Alca.MonoGame.Kernel/Weather/
├── WeatherTypeId.cs
├── PrecipitationIntensity.cs
├── WeatherProfile.cs
├── WeatherProfiles.cs
├── WindState.cs
├── LightningStrikeEvent.cs
├── WeatherWorld.cs
├── WeatherBehaviour.cs
├── WeatherParticleLayer.cs
├── LightningController.cs
└── WeatherAudioLayer.cs

src/Library/Alca.MonoGame.Kernel.UnitTests/Weather/
├── WeatherTypeIdTests.cs
├── WeatherProfileTests.cs
├── WindStateTests.cs
├── WeatherWorldTests.cs
├── WeatherBehaviourTests.cs
├── LightningControllerTests.cs
├── WeatherAudioLayerTests.cs
└── WeatherParticleLayerTests.cs

src/Library/Wiki/13-weather/
├── overview.md
├── profiles.md
├── weather-world.md
├── behaviour.md
├── particles.md
├── lightning.md
└── audio.md

src/Library/docs/
├── roadmap-v4.md  ← este fichero
└── specs/phase19-weather.md
```

### Archivos modificados

| Archivo | Cambio |
|---|---|
| `ECS/GameWorld.cs` | + `WeatherWorld? WeatherWorld` + llamada `WeatherWorld?.Update(gameTime)` |
| `Wiki/README.md` | + fila Weather en tabla de módulos |
| `Wiki/INDEX.md` | + sección 13 completa |
| `Wiki/02-ecs/game-world.md` | + fila `WeatherWorld` en tabla de propiedades |
| `Demo/DemoGame.cs` | + `services.AddTransient<WeatherScene>()` |
| `Demo/UIScene_Menu.cs` | + entrada 42 en la lista del menú |

### New NuGet Dependencies

Ninguna. El sistema de clima reutiliza:
- `MonoGame.Extended` (existing) — `ParticleEffect`, `ParticleEmitter`, `LinearGravityModifier`
- `Aether.Physics2D.MG` (existing) — `RigidBody2D` en `WeatherBehaviour`

### Key Design Decisions

**¿Por qué `WeatherTypeId` es un `readonly struct` wrapper de `string` en lugar de un `enum`?**  
Un `enum` cerrado impediría que los desarrolladores registrasen tipos de clima custom sin modificar la librería. `WeatherTypeId` permite `new WeatherTypeId("my_weather")` con igualdad case-insensitive y rendimiento de struct (sin heap alloc).

**¿Por qué la restricción viento↔niebla se aplica en `WeatherWorld` y no en `WeatherProfile`?**  
`WeatherProfile` es un `readonly struct` puro de datos — no debería contener lógica de validación. `WeatherWorld` es el servicio con contexto de dominio; es el lugar correcto para aplicar restricciones físicas y emitir advertencias de depuración.

**¿Por qué los efectos de partículas no se limpian entre fotogramas?**  
Los `LinearGravityModifier` cacheados en `LoadContent` permiten actualizar la dirección del viento mutando directamente el campo `Direction`, sin crear nuevos modificadores ni efectos. Esto garantiza cero heap allocations en `Update`.

**¿Por qué `LightningController` crea su entidad flash en el constructor y no en `LoadContent`?**  
El controlador no es un `GameBehaviour` — es un servicio independiente. Al pre-asignar la entidad en el constructor, el `Update` no necesita comprobar si la entidad existe, garantizando código de hot-path sin ramas defensivas.
