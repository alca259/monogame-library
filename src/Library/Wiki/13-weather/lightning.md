# LightningController

**Namespace:** `Alca.MonoGame.Kernel.Weather`

`LightningController` gestiona el ciclo de vida completo de un rayo: temporización aleatoria, un flash luminoso usando `PointLight2D`, audio espacial de trueno, e impulso físico a entidades cercanas. Todos los recursos se pre-asignan en el constructor — cero asignaciones en `Update`.

---

## Constructor

```csharp
var lightning = new LightningController(weatherWorld, gameWorld);
weatherWorld.EnableLightning(lightning);
```

Crea una entidad `_WeatherLightningFlash` oculta (inactiva por defecto) con un `PointLight2D` adjunto. El `GameWorld` debe tener un `LightingWorld` asignado para que el flash sea visible.

---

## Propiedades de configuración

### Geometría del rayo

| Propiedad | Tipo | Default | Descripción |
|---|---|---|---|
| `MinX` | `float` | `0` | Coordenada X mínima en espacio mundo donde puede impactar un rayo |
| `MaxX` | `float` | `1280` | Coordenada X máxima |
| `StrikeY` | `float` | `-80` | Coordenada Y del flash (normalmente por encima de la pantalla) |

### Flash luminoso

| Propiedad | Tipo | Default | Descripción |
|---|---|---|---|
| `FlashIntensity` | `float` | `8` | Intensidad pico del `PointLight2D` |
| `FlashRange` | `float` | `600` | Radio del flash en unidades mundo |
| `FlashDuration` | `float` | `0.15` | Duración del flash a plena intensidad (segundos) |
| `FlashColor` | `Color` | Azul-blanco frio | Color de la luz del flash |

### Impulso físico

| Propiedad | Tipo | Default | Descripción |
|---|---|---|---|
| `ImpulseRadius` | `float` | `200` | Radio en unidades mundo dentro del cual las entidades reciben impulso |
| `ImpulseStrength` | `float` | `350` | Impulso máximo en el centro, con caída lineal hasta el borde del radio |

### Audio

| Propiedad | Tipo | Descripción |
|---|---|---|
| `AudioController` | `AudioController?` | Controlador de audio para reproducir truenos espaciales |
| `AudioLayer` | `WeatherAudioLayer?` | Capa de audio que provee el pool de truenos |

---

## Estado

```csharp
bool isFlashing = lightning.IsFlashing;  // true mientras el flash está activo
```

---

## Disparo manual

```csharp
// Forzar un rayo en una posición concreta, sin esperar al temporizador
lightning.TriggerStrikeAt(new Vector2(640f, -80f));
```

---

## Flujo de un rayo

```
1. Temporizador alcanza _nextStrikeInterval
2. TriggerStrikeAt(pos)
   ├── PointLight2D.Intensity = FlashIntensity → entidad flash activa
   ├── WeatherBehaviour[i].ApplyLightningImpulse(pos, ImpulseRadius, ImpulseStrength)
   ├── WeatherAudioLayer.PlayThunderStrike(pos, audioController)
   └── WeatherWorld.RaiseLightningStruck(LightningStrikeEvent)
3. _flashTimer >= FlashDuration → flash inactivo
4. _nextStrikeInterval = Random[LightningMinInterval, LightningMaxInterval]
```

---

## Evento LightningStrikeEvent

```csharp
weatherWorld.LightningStruck += evt =>
{
    // evt.Position        — Vector2, posición del impacto
    // evt.Intensity       — float, intensidad del flash
    // evt.ImpulseRadius   — float, radio del impulso
    // evt.ImpulseStrength — float, magnitud del impulso
};
```

---

## Ejemplo de integración completa

```csharp
// Setup en PostInitialize / LoadContent
var weatherWorld = new WeatherWorld();
var lightningCtrl = new LightningController(weatherWorld, _world)
{
    MinX           = 200f,
    MaxX           = 1080f,
    FlashIntensity = 10f,
    ImpulseRadius  = 300f,
    ImpulseStrength = 500f,
};
weatherWorld.EnableLightning(lightningCtrl);
weatherWorld.SetWeather(WeatherTypeId.Thunderstorm);

// Reaccionar a cada rayo
weatherWorld.LightningStruck += evt =>
    _screenShake.Trigger(intensity: evt.Intensity * 0.5f, duration: 0.3f);
```

---

## Limpieza

```csharp
lightningCtrl.Dispose();  // destruye la entidad flash del GameWorld
```

---

## Ver también

- [Visión general →](overview.md)
- [WeatherWorld (API) →](weather-world.md)
- [WeatherBehaviour →](behaviour.md)
- [Iluminación 2D →](../09-lighting/overview.md)
