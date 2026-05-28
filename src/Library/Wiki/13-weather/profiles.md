# WeatherProfile y catálogo

**Namespace:** `Alca.MonoGame.Kernel.Weather`

`WeatherProfile` es un `readonly struct` que describe completamente un estado atmosférico: temperatura, viento, luz, niebla, precipitación, rayos y volúmenes de audio. Todos los valores son linealmente interpolables, lo que permite transiciones suaves entre perfiles.

---

## Campos de WeatherProfile

### Temperatura

| Campo | Tipo | Descripción |
|---|---|---|
| `TemperatureMin` | `float` | ºC mínimos del rango de oscilación |
| `TemperatureMax` | `float` | ºC máximos del rango de oscilación |

### Viento

| Campo | Tipo | Descripción |
|---|---|---|
| `WindSpeedMinKmh` | `float` | Velocidad mínima de viento (km/h) |
| `WindSpeedMaxKmh` | `float` | Velocidad máxima de viento (km/h) |
| `WindDirection` | `Vector2` | Dirección normalizada del viento |
| `WindTurbulence` | `float` | Amplitud de turbulencia sinusoidal (0–1) |

### Iluminación

| Campo | Tipo | Descripción |
|---|---|---|
| `AmbientColor` | `Color` | Color de la luz ambiente |
| `AmbientIntensity` | `float` | Intensidad de la luz ambiente (0–1) |
| `SkyColor` | `Color` | Color de fondo del cielo |

### Niebla

| Campo | Tipo | Descripción |
|---|---|---|
| `FogColor` | `Color` | Color de las partículas de niebla |
| `FogDensity` | `float` | Densidad 0–1 (se fuerza a 0 si `WindSpeedMaxKmh > 0`) |

### Precipitación y rayos

| Campo | Tipo | Descripción |
|---|---|---|
| `HasPrecipitation` | `bool` | Activa los efectos de partículas |
| `PrecipitationLevel` | `PrecipitationIntensity` | Intensidad (`None`, `Low`, `Medium`, `High`, `VeryHigh`) |
| `HasLightning` | `bool` | Activa el temporizador de rayos |
| `LightningMinInterval` | `float` | Segundos mínimos entre rayos |
| `LightningMaxInterval` | `float` | Segundos máximos entre rayos |

### Audio

| Campo | Tipo | Descripción |
|---|---|---|
| `RainVolume` | `float` | Volumen del loop de lluvia (0–1) |
| `WindVolume` | `float` | Volumen del loop de viento (0–1) |
| `ThunderVolume` | `float` | Volumen del loop de trueno ambiente (0–1) |

### Datos de juego

| Campo | Tipo | Descripción |
|---|---|---|
| `CustomData` | `string?` | JSON arbitrario definido por el desarrollador. La librería no lo interpreta. |

---

## Interpolación (Lerp)

```csharp
WeatherProfile interpolated = WeatherProfile.Lerp(from, to, t);
```

- Todos los valores `float` y `Color` se interpolan linealmente.
- Los valores `bool` (`HasPrecipitation`, `HasLightning`) se conmutan cuando `t >= 0.5`.
- `PrecipitationLevel` se conmuta cuando `t >= 0.5`.
- `CustomData` siempre adopta el valor de `to`.

---

## WeatherProfiles — catálogo por defecto

`WeatherProfiles` es una clase estática con los 10 perfiles predefinidos listos para usar:

```csharp
WeatherProfile sunny = WeatherProfiles.Sunny;
WeatherProfile storm = WeatherProfiles.Storm;

// Obtener por WeatherTypeId
WeatherProfile? profile = WeatherProfiles.Get(WeatherTypeId.Thunderstorm);
```

### Perfiles disponibles

| Propiedad | WeatherTypeId |
|---|---|
| `WeatherProfiles.Sunny` | `WeatherTypeId.Sunny` |
| `WeatherProfiles.HeatWave` | `WeatherTypeId.HeatWave` |
| `WeatherProfiles.Cloudy` | `WeatherTypeId.Cloudy` |
| `WeatherProfiles.Fog` | `WeatherTypeId.Fog` |
| `WeatherProfiles.Storm` | `WeatherTypeId.Storm` |
| `WeatherProfiles.Thunderstorm` | `WeatherTypeId.Thunderstorm` |
| `WeatherProfiles.HailStorm` | `WeatherTypeId.HailStorm` |
| `WeatherProfiles.Blizzard` | `WeatherTypeId.Blizzard` |
| `WeatherProfiles.ColdSnap` | `WeatherTypeId.ColdSnap` |
| `WeatherProfiles.OrangeWind` | `WeatherTypeId.OrangeWind` |

---

## WeatherTypeId — identificador extensible

`WeatherTypeId` es un `readonly struct` que envuelve un `string`. Los 10 tipos predefinidos son constantes estáticas; el desarrollador puede crear tipos nuevos con cualquier `string`:

```csharp
// Predefinido
WeatherTypeId sunny = WeatherTypeId.Sunny;   // Value = "sunny"

// Custom
var radioactiveRain = new WeatherTypeId("radioactive_rain");

// Igualdad (case-insensitive)
bool same = new WeatherTypeId("SUNNY") == WeatherTypeId.Sunny;  // true
```

---

## CustomData — datos de juego en el perfil

El campo `CustomData` almacena JSON arbitrario que el juego puede deserializar según sus necesidades:

```csharp
var toxic = new WeatherProfile
{
    // ... campos estándar ...
    CustomData = """{"type":"radioactive","glowColor":"#00FF00","toxicLevel":9000}"""
};

weatherWorld.RegisterCustomWeather(new WeatherTypeId("toxic_storm"), toxic);

// En otro lugar del juego
if (weatherWorld.TryGetProfile(weatherWorld.CurrentWeather, out WeatherProfile p))
{
    if (p.CustomData is not null)
    {
        var data = JsonSerializer.Deserialize<ToxicData>(p.CustomData);
        // usar data.GlowColor, etc.
    }
}
```

---

## Ver también

- [Visión general →](overview.md)
- [WeatherWorld (API) →](weather-world.md)
