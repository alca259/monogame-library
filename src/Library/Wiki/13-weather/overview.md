# Weather System — Visión general

**Namespace:** `Alca.MonoGame.Kernel.Weather`

El sistema de climatología simula condiciones atmosféricas en tiempo real: temperatura, viento, precipitación, niebla, rayos y audio ambiente. Está diseñado para integrarse con el motor ECS, la iluminación dinámica y el sistema de audio espacial.

---

## Distinción clave: temperatura ≠ clima

| Concepto | Tipo | Descripción |
|---|---|---|
| **Clima** (`WeatherTypeId`) | Estado cualitativo | Define luz ambiente, viento, partículas y audio. Se consulta con `WeatherWorld.CurrentWeather`. |
| **Temperatura** (`float`, ºC) | Valor numérico continuo | Oscila dentro del rango `[TemperatureMin, TemperatureMax]` del perfil activo. Se consulta con `WeatherWorld.CurrentTemperature`. |

El clima puede cambiar de forma instantánea o con transición suave. La temperatura se interpola suavemente entre el mínimo y el máximo usando una oscilación sinusoidal, independientemente del clima.

---

## Diagrama del sistema

```
WeatherWorld
├── Catalog<WeatherTypeId, WeatherProfile>   ← 10 predefinidos + tipos custom
├── ActiveProfile  ──── interpolado en transición
├── CurrentWind (WindState)
├── CurrentTemperature (float ºC)
│
├── WeatherParticleLayer  ──── lluvia, nieve, granizo, niebla, viento
├── LightningController   ──── temporizador, flash, impulso, audio
├── WeatherAudioLayer     ──── loops ambient + truenos espaciales
│
└── List<WeatherBehaviour>  ← entidades ECS registradas
        ↓ ApplyWindForce / ApplyLightningImpulse
    RigidBody2D (Physics2DWorld)
```

---

## Tipos de clima predefinidos

| WeatherTypeId | TempMin | TempMax | WindMax (km/h) | Niebla | Precipitación | Rayos |
|---|---|---|---|---|---|---|
| `sunny` | 24 | 24 | 4 | No | No | No |
| `heat_wave` | 40 | 40 | 4 | No | No | No |
| `cloudy` | 15 | 15 | 4 | No | No | No |
| `fog` | 15 | 15 | 0 | 0.75 | Muy Alta | No |
| `storm` | 15 | 15 | 4 | No | Alta | No |
| `thunderstorm` | 20 | 20 | 15 | No | Media | Sí (5–15 s) |
| `hail_storm` | 30 | 30 | 8 | No | Media | No |
| `blizzard` | -5 | 0 | 4 | 0.2 | Alta | No |
| `cold_snap` | -10 | -10 | 33 | No | No | No |
| `orange_wind` | 10 | 10 | 75 | No | Alta | No |

### Restricción viento ↔ niebla

`WindSpeedMaxKmh > 0` y `FogDensity > 0` son mutuamente exclusivos. Si un perfil infringe esta regla, `WeatherWorld` emite una advertencia de depuración y fuerza `FogDensity = 0`.

---

## Inicio rápido

```csharp
using Alca.MonoGame.Kernel.Weather;

// 1. Crear el WeatherWorld y asignarlo al GameWorld
var weatherWorld = new WeatherWorld();
_world.WeatherWorld = weatherWorld;

// 2. (Opcional) Conectar subsistemas
weatherWorld.LightingWorld = _lightingWorld;   // actualiza luz ambiente
weatherWorld.AudioController = Core.Audio;
weatherWorld.AudioMixer = _audioMixer;

// 3. Cambiar el clima (con transición de 5 segundos)
weatherWorld.SetWeather(WeatherTypeId.Thunderstorm, transitionDuration: 5f);

// 4. Leer estado actual
float temp = weatherWorld.CurrentTemperature;          // ºC
string type = weatherWorld.CurrentWeather.Value;       // "thunderstorm"
float windSpeed = weatherWorld.CurrentWind.SpeedKmh;   // km/h
```

El `GameWorld` llama automáticamente a `WeatherWorld.Update()` en cada frame cuando está asignado.

---

## Ver también

- [WeatherProfile y catálogo →](profiles.md)
- [WeatherWorld (API completa) →](weather-world.md)
- [WeatherBehaviour (ECS) →](behaviour.md)
- [WeatherParticleLayer →](particles.md)
- [LightningController →](lightning.md)
- [WeatherAudioLayer →](audio.md)
