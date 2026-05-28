# Phase 19 — Weather System Specification

**Phase:** 19  
**Status:** ✅ Implemented  
**Namespace:** `Alca.MonoGame.Kernel.Weather`

---

## Overview

The weather system simulates atmospheric conditions in real time: temperature, wind, precipitation, fog, lightning, and ambient audio. It integrates with the ECS, lighting, physics, and spatial audio subsystems through clean opt-in wiring — zero configuration required for basic use, full control available when needed.

### Core distinction: temperature ≠ weather type

| Concept | Type | Description |
|---|---|---|
| **Weather type** | `WeatherTypeId` | Qualitative state defining light, wind, particles, and audio |
| **Temperature** | `float` (°C) | Continuous numeric value oscillating within the profile's `[TemperatureMin, TemperatureMax]` range |

---

## Public API Summary

### WeatherTypeId

```csharp
public readonly struct WeatherTypeId : IEquatable<WeatherTypeId>
{
    public string Value { get; init; }

    // 10 predefined types
    public static readonly WeatherTypeId Sunny;
    public static readonly WeatherTypeId HeatWave;
    public static readonly WeatherTypeId Cloudy;
    public static readonly WeatherTypeId Fog;
    public static readonly WeatherTypeId Storm;
    public static readonly WeatherTypeId Thunderstorm;
    public static readonly WeatherTypeId HailStorm;
    public static readonly WeatherTypeId Blizzard;
    public static readonly WeatherTypeId ColdSnap;
    public static readonly WeatherTypeId OrangeWind;

    // Equality is case-insensitive
    public bool Equals(WeatherTypeId other);
    public static bool operator ==(WeatherTypeId left, WeatherTypeId right);
    public static bool operator !=(WeatherTypeId left, WeatherTypeId right);
}
```

### WeatherProfile

```csharp
public readonly struct WeatherProfile
{
    // Temperature
    public float TemperatureMin { get; init; }
    public float TemperatureMax { get; init; }

    // Wind
    public float WindSpeedMinKmh { get; init; }
    public float WindSpeedMaxKmh { get; init; }
    public Vector2 WindDirection { get; init; }
    public float WindTurbulence { get; init; }

    // Lighting
    public Color AmbientColor { get; init; }
    public float AmbientIntensity { get; init; }
    public Color SkyColor { get; init; }

    // Fog
    public Color FogColor { get; init; }
    public float FogDensity { get; init; }  // forced to 0 when WindSpeedMaxKmh > 0

    // Precipitation
    public bool HasPrecipitation { get; init; }
    public PrecipitationIntensity PrecipitationLevel { get; init; }

    // Lightning
    public bool HasLightning { get; init; }
    public float LightningMinInterval { get; init; }
    public float LightningMaxInterval { get; init; }

    // Audio
    public float RainVolume { get; init; }
    public float WindVolume { get; init; }
    public float ThunderVolume { get; init; }

    // Game data
    public string? CustomData { get; init; }

    // Interpolation
    public static WeatherProfile Lerp(in WeatherProfile from, in WeatherProfile to, float t);
}
```

### WindState

```csharp
public readonly struct WindState
{
    public Vector2 Direction { get; init; }
    public float SpeedKmh { get; init; }
    public float Turbulence { get; init; }

    public bool IsCalm { get; }

    // Zero-alloc effective force with two-sinusoid turbulence
    public Vector2 ComputeEffectiveForce(float totalElapsedSeconds, float worldUnitsPerKmh);
}
```

### WeatherWorld

```csharp
public sealed class WeatherWorld
{
    // Read state
    public WeatherTypeId CurrentWeather { get; }
    public float CurrentTemperature { get; }
    public bool IsTransitioning { get; }
    public float TransitionProgress { get; }
    public WeatherProfile ActiveProfile { get; }
    public WindState CurrentWind { get; }

    // Integration wiring (set before first Update)
    public Lighting.LightingWorld? LightingWorld { get; set; }
    public Audio.AudioController? AudioController { get; set; }
    public Audio.AudioMixer? AudioMixer { get; set; }
    public float WorldUnitsPerKmh { get; set; }

    // Subsystems (read-only access)
    public LightningController? Lightning { get; }
    public WeatherParticleLayer? Particles { get; }
    public WeatherAudioLayer? Audio { get; }

    // Event
    public event Action<LightningStrikeEvent>? LightningStruck;

    // Catalog management
    public void RegisterCustomWeather(WeatherTypeId id, in WeatherProfile profile);
    public void ModifyProfile(WeatherTypeId id, in WeatherProfile profile);
    public bool TryGetProfile(WeatherTypeId id, out WeatherProfile profile);

    // Control
    public void SetWeather(WeatherTypeId type, float transitionDuration = 3f);
    public void SetWeatherImmediate(WeatherTypeId type);

    // Subsystem registration
    public void EnableParticles(WeatherParticleLayer layer);
    public void EnableLightning(LightningController controller);
    public void EnableAudio(WeatherAudioLayer layer);

    // Game loop (called automatically by GameWorld)
    public void Update(GameTime gameTime);
}
```

### WeatherBehaviour

```csharp
public sealed class WeatherBehaviour : GameBehaviour
{
    public bool ReceivesWind { get; set; }
    public float WindForceMultiplier { get; set; }
    public bool ReceivesLightningImpulse { get; set; }

    public override void Awake();      // caches RigidBody2D; registers with WeatherWorld
    public override void OnDestroy();  // unregisters
}
```

### WeatherParticleLayer

```csharp
public sealed class WeatherParticleLayer : IDisposable
{
    public Vector2 EmitterPosition { get; set; }
    public float EmitterWidth { get; set; }

    public void LoadContent(
        Texture2D? rainTexture,
        Texture2D? snowTexture,
        Texture2D? hailTexture,
        Texture2D? fogTexture,
        Texture2D? windTexture);

    public void Update(GameTime gameTime, in WeatherProfile profile, in WindState wind);
    public void Draw(SpriteBatch spriteBatch);
    public void Dispose();
}
```

### LightningController

```csharp
public sealed class LightningController : IDisposable
{
    // Geometry
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float StrikeY { get; set; }

    // Flash
    public float FlashIntensity { get; set; }
    public float FlashRange { get; set; }
    public float FlashDuration { get; set; }
    public Color FlashColor { get; set; }

    // Impulse
    public float ImpulseRadius { get; set; }
    public float ImpulseStrength { get; set; }

    // Audio
    public Audio.AudioController? AudioController { get; set; }
    public WeatherAudioLayer? AudioLayer { get; set; }

    // State
    public bool IsFlashing { get; }

    public LightningController(WeatherWorld weatherWorld, ECS.GameWorld gameWorld);
    public void Update(GameTime gameTime, in WeatherProfile profile, List<WeatherBehaviour> behaviours);
    public void TriggerStrikeAt(Vector2 worldPosition);
    public void Dispose();
}
```

### WeatherAudioLayer

```csharp
public sealed class WeatherAudioLayer : IDisposable
{
    public float FadeSpeed { get; set; }
    public Audio.AudioMixerChannel? Channel { get; set; }

    public void LoadSounds(
        SoundEffect? rainSfx,
        SoundEffect? windSfx,
        SoundEffect? thunderAmbSfx,
        SoundEffect? thunderStrikeSfx,
        int thunderPoolSize = 4);

    public void Update(GameTime gameTime, in WeatherProfile profile);
    public void PlayThunderStrike(Vector2 position, Audio.AudioController audioController);
    public void Dispose();
}
```

---

## Constraint: wind ↔ fog mutual exclusion

```
if (profile.WindSpeedMaxKmh > 0 && profile.FogDensity > 0)
    → Debug.WriteLine warning
    → FogDensity forced to 0 in applied profile
```

Applied in `RegisterCustomWeather`, `ModifyProfile`, and implicitly through `SetWeather` / `SetWeatherImmediate`.

---

## GameWorld integration

```csharp
// GameWorld.cs (modification)
public Weather.WeatherWorld? WeatherWorld { get; set; }

// In GameWorld.Update():
WeatherWorld?.Update(gameTime);
```

---

## Lerp rules for WeatherProfile

| Field type | Lerp rule |
|---|---|
| `float` | `MathHelper.Lerp(from, to, t)` |
| `Color` | Component-wise lerp on R, G, B, A |
| `Vector2` | `Vector2.Lerp(from, to, t)` |
| `bool` | Switches to `to` value when `t >= 0.5` |
| `PrecipitationIntensity` | Switches to `to` value when `t >= 0.5` |
| `string?` (CustomData) | Always adopts `to` value |

---

## Default catalog

| TypeId | TempMin | TempMax | WindMin | WindMax | FogDensity | HasPrecipitation | HasLightning |
|---|---|---|---|---|---|---|---|
| `sunny` | 24 | 24 | 2 | 4 | 0 | false | false |
| `heat_wave` | 40 | 40 | 2 | 4 | 0 | false | false |
| `cloudy` | 15 | 15 | 2 | 4 | 0 | false | false |
| `fog` | 15 | 15 | 0 | 0 | 0.75 | true | false |
| `storm` | 15 | 15 | 2 | 4 | 0 | true | false |
| `thunderstorm` | 20 | 20 | 10 | 15 | 0 | true | true (5–15 s) |
| `hail_storm` | 30 | 30 | 4 | 8 | 0 | true | false |
| `blizzard` | -5 | 0 | 2 | 4 | 0.2 | true | false |
| `cold_snap` | -10 | -10 | 30 | 33 | 0 | false | false |
| `orange_wind` | 10 | 10 | 60 | 75 | 0 | true | false |

---

## Unit test coverage (86 tests)

See `src/Library/Alca.MonoGame.Kernel.UnitTests/Weather/` for the full test suite.

Key scenarios verified:
- `WeatherTypeId` equality is case-insensitive; all 10 predefined types are distinct.
- `WeatherProfile.Lerp` at `t=0`, `t=0.5`, `t=1`; bool switch at `t>=0.5`; `CustomData` always from `to`.
- `WindState.IsCalm`; `ComputeEffectiveForce` scales with `WorldUnitsPerKmh`; zero when calm.
- `WeatherWorld` starts in `Sunny`; `SetWeather` initiates transition; `CurrentTemperature` stays within profile range.
- `RegisterCustomWeather` + wind/fog constraint enforcement.
- `ModifyProfile` updates active profile on next frame.
- `LightningController` flash inactive by default; active after `TriggerStrikeAt`; ends after `FlashDuration`; `HasLightning=false` never fires.
- `WeatherAudioLayer` all-null sounds do not throw; `Dispose` is idempotent.
- `WeatherParticleLayer` all-null textures do not throw; `Draw(null)` is safe.
