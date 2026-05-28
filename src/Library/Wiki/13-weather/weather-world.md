# WeatherWorld

**Namespace:** `Alca.MonoGame.Kernel.Weather`

`WeatherWorld` es el servicio central que dirige la simulación meteorológica: gestiona el catálogo de perfiles, interpola entre estados, mantiene la temperatura actual, y delega en los subsistemas opcionales (partículas, rayos, audio).

Asígnalo a `GameWorld.WeatherWorld` para que se actualice automáticamente en cada frame.

---

## Estado actual

| Propiedad | Tipo | Descripción |
|---|---|---|
| `CurrentWeather` | `WeatherTypeId` | Tipo activo (o el origen durante una transición) |
| `CurrentTemperature` | `float` | ºC interpolado en el rango del perfil activo |
| `IsTransitioning` | `bool` | `true` durante una transición |
| `TransitionProgress` | `float` | Progreso de la transición en `[0, 1]` |
| `ActiveProfile` | `WeatherProfile` | Perfil completamente interpolado en este frame |
| `CurrentWind` | `WindState` | Estado de viento derivado del perfil activo |

---

## Subsistemas de integración

Asigna antes del primer `Update`:

| Propiedad | Tipo | Descripción |
|---|---|---|
| `LightingWorld` | `LightingWorld?` | Actualiza `AmbientColor` cada frame |
| `AudioController` | `AudioController?` | Necesario para audio espacial de truenos |
| `AudioMixer` | `AudioMixer?` | Routing de volumen por canal |
| `WorldUnitsPerKmh` | `float` | Escala km/h → unidades mundo (default 1) |

---

## Subsistemas gestionados

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Lightning` | `LightningController?` | Controlador de rayos (actívalo con `EnableLightning`) |
| `Particles` | `WeatherParticleLayer?` | Capa de partículas (actívala con `EnableParticles`) |
| `Audio` | `WeatherAudioLayer?` | Audio ambiente (actívalo con `EnableAudio`) |

---

## Habilitación de subsistemas

```csharp
var ww = new WeatherWorld();

// Partículas
var particles = new WeatherParticleLayer();
particles.LoadContent(rainTex, snowTex, hailTex, fogTex, windTex);
ww.EnableParticles(particles);

// Rayos (requiere GameWorld para crear la entidad flash)
var lightning = new LightningController(ww, _world);
ww.EnableLightning(lightning);

// Audio
var audio = new WeatherAudioLayer();
audio.LoadSounds(rainSfx, windSfx, thunderSfx, thunderPool);
ww.EnableAudio(audio);
```

---

## Control del clima

### Transición suave

```csharp
// Transición de 5 segundos (default 3 s)
ww.SetWeather(WeatherTypeId.Thunderstorm, transitionDuration: 5f);
```

Si ya hay una transición en curso, se completa instantáneamente antes de iniciar la nueva.

### Cambio inmediato

```csharp
ww.SetWeatherImmediate(WeatherTypeId.Sunny);
```

### Registrar tipos custom

```csharp
var customId = new WeatherTypeId("acid_rain");
ww.RegisterCustomWeather(customId, new WeatherProfile
{
    TemperatureMin = 18f, TemperatureMax = 25f,
    WindSpeedMaxKmh = 12f,
    HasPrecipitation = true,
    CustomData = """{"corrosion":true}"""
    // ... resto de campos
});

ww.SetWeather(customId);
```

### Modificar un perfil en runtime

```csharp
// Aumentar el viento del clima activo durante el juego
if (ww.TryGetProfile(ww.CurrentWeather, out WeatherProfile current))
{
    ww.ModifyProfile(ww.CurrentWeather, current with
    {
        WindSpeedMinKmh = current.WindSpeedMinKmh + 5f,
        WindSpeedMaxKmh = current.WindSpeedMaxKmh + 5f,
    });
}
```

El cambio se aplica en el siguiente frame. Si el tipo está siendo la destino de una transición en curso, también se actualiza.

---

## Gestión del catálogo

```csharp
// Verificar si un tipo está registrado
if (ww.TryGetProfile(WeatherTypeId.Blizzard, out WeatherProfile p))
    Console.WriteLine(p.TemperatureMin);

// TryGetProfile devuelve false para tipos no registrados
bool found = ww.TryGetProfile(new WeatherTypeId("unknown"), out _);  // false
```

---

## Evento de rayo

```csharp
ww.LightningStruck += evt =>
{
    Console.WriteLine($"Rayo en {evt.Position}, intensidad {evt.Intensity}");
    // evt: Position, Intensity, ImpulseRadius, ImpulseStrength
};
```

Los handlers se invocan sincrónicamente desde `Update`. No asignes dentro de ellos.

---

## WindState

El estado de viento se calcula cada frame a partir del perfil activo:

```csharp
WindState wind = ww.CurrentWind;
wind.Direction     // Vector2 normalizado
wind.SpeedKmh      // float, oscila entre Min y Max
wind.Turbulence    // float (0–1)
wind.IsCalm        // true si SpeedKmh ≈ 0

// Fuerza efectiva (incluye turbulencia sinusoidal)
Vector2 force = wind.ComputeEffectiveForce(totalElapsed, worldUnitsPerKmh);
```

---

## Temperatura

La temperatura oscila suavemente dentro del rango `[TemperatureMin, TemperatureMax]` del perfil activo usando una sinusoide de baja frecuencia:

```csharp
float temp = ww.CurrentTemperature;  // ºC, nunca fuera del rango del perfil
```

---

## Restricción viento ↔ niebla

Si registras o modificas un perfil con `WindSpeedMaxKmh > 0` y `FogDensity > 0` simultáneamente, `WeatherWorld` emite una advertencia de depuración y fuerza `FogDensity = 0` en el perfil aplicado.

```
[WeatherWorld] Profile 'my_weather' has both wind (max 10 km/h) and fog density (0.5).
Wind and fog are mutually exclusive — FogDensity forced to 0.
```

---

## Ver también

- [Visión general →](overview.md)
- [WeatherProfile y catálogo →](profiles.md)
- [WeatherBehaviour (ECS) →](behaviour.md)
- [WeatherParticleLayer →](particles.md)
- [LightningController →](lightning.md)
- [WeatherAudioLayer →](audio.md)
