# Ciclo Día/Noche

**Namespace:** `Alca.MonoGame.Kernel.Lighting.DayNight`

El sistema de ciclo día/noche permite simular el paso del tiempo en el mundo del juego, interpolando automáticamente el color ambiental, la intensidad y el ángulo del sol entre keyframes configurables. Se integra con `LightingWorld` para actualizar la iluminación en cada frame.

## Arquitectura

El sistema está compuesto por cuatro elementos principales organizados en capas:

```
DayNightCycle
    │
    ├─► DayNightProfile
    │       │
    │       └─► DayNightKeyframe[]
    │               (Midnight, Sunrise, Noon, Sunset)
    │
    └─► LightingWorld  (opcional)
            │
            └─► AmbientColor = color * intensity
```

- `DayNightCycle` es el servicio principal. Avanza el tiempo y dispara eventos en cada cruce horario.
- `DayNightProfile` contiene los keyframes y expone el método `Sample(TimeOfDay)` que devuelve un keyframe interpolado.
- `DayNightKeyframe` es una `readonly struct` que agrupa el color, la intensidad y el ángulo del sol en un instante concreto.
- `LightingWorld` recibe el color ambiental resultante. Su presencia es opcional; sin ella el ciclo funciona en modo standalone.

## Integración con GameWorld

La forma más habitual de usar el ciclo es asignarlo a `GameWorld.DayNightCycle`. El mundo se encarga de llamar a `Update` en cada frame:

```csharp
// En la escena o en Initialize del mundo
var profile = DayNightProfile.Default;
var cycle = new DayNightCycle(profile, _lightingWorld);

// Arranca al mediodía
cycle.SetTime(TimeOfDay.Noon);

// Asignar al mundo — Update se llama automáticamente
_gameWorld.DayNightCycle = cycle;
```

Si necesitas reaccionar a los eventos desde la escena, suscríbete antes de asignar:

```csharp
cycle.OnSunrise += () => _backgroundMusic.CrossFadeTo(_morningTrack);
cycle.OnSunset  += () => _backgroundMusic.CrossFadeTo(_eveningTrack);
_gameWorld.DayNightCycle = cycle;
```

## TimeOfDay — struct de tiempo circular

`TimeOfDay` es una `readonly struct` que representa una hora del día en el rango `[0, 24)`. Los valores se envuelven automáticamente: `25.0f` se convierte en `1.0f`.

| Miembro | Tipo | Descripción |
|---|---|---|
| `Hours` | `float` | Hora en formato decimal, rango `[0, 24)` |
| `IsDaytime` | `bool` | `true` si `6 ≤ Hours < 20` |
| `IsNighttime` | `bool` | Opuesto a `IsDaytime` |
| `Midnight` | `static TimeOfDay` | `Hours = 0` |
| `Sunrise` | `static TimeOfDay` | `Hours = 6` |
| `Noon` | `static TimeOfDay` | `Hours = 12` |
| `Sunset` | `static TimeOfDay` | `Hours = 20` |
| `FromHours(float)` | `static TimeOfDay` | Crea instancia con wrapping automático |
| `Lerp(a, b, t)` | `static TimeOfDay` | Interpola por el arco más corto |
| `ToString()` | `string` | Formato `"HH:MM"` |

```csharp
TimeOfDay morning = TimeOfDay.FromHours(8.5f);  // "08:30"
TimeOfDay mid = TimeOfDay.Lerp(TimeOfDay.Sunrise, TimeOfDay.Noon, 0.5f); // "09:00"

Console.WriteLine(morning.IsDaytime);   // true
Console.WriteLine(morning.ToString());  // "08:30"
```

## DayNightProfile — definir keyframes personalizados

`DayNightProfile` acepta exactamente cuatro keyframes: Midnight, Sunrise, Noon y Sunset. Usa `DayNightProfile.Default` para valores predefinidos o crea un perfil propio:

```csharp
var profile = new DayNightProfile(
    dayDurationSeconds: 300f,  // 5 minutos de juego = 1 día completo
    midnight: new DayNightKeyframe(
        time: TimeOfDay.Midnight,
        ambientColor: new Color(10, 10, 40),
        ambientIntensity: 0.08f,
        sunAngleDegrees: -90f
    ),
    sunrise: new DayNightKeyframe(
        time: TimeOfDay.Sunrise,
        ambientColor: new Color(255, 160, 80),
        ambientIntensity: 0.55f,
        sunAngleDegrees: 0f
    ),
    noon: new DayNightKeyframe(
        time: TimeOfDay.Noon,
        ambientColor: new Color(255, 250, 230),
        ambientIntensity: 1.0f,
        sunAngleDegrees: 90f
    ),
    sunset: new DayNightKeyframe(
        time: TimeOfDay.Sunset,
        ambientColor: new Color(220, 90, 50),
        ambientIntensity: 0.45f,
        sunAngleDegrees: 180f
    )
);

// Sample devuelve un keyframe interpolado para cualquier hora
DayNightKeyframe sample = profile.Sample(TimeOfDay.FromHours(9f));
Console.WriteLine(sample.AmbientIntensity); // valor entre Sunrise y Noon
```

## Eventos del ciclo

`DayNightCycle` expone cuatro eventos que se disparan exactamente una vez por cada cruce de umbral horario, sin importar la velocidad de `TimeScale`:

```csharp
var cycle = new DayNightCycle(DayNightProfile.Default, _lightingWorld);

cycle.OnSunrise += () =>
{
    _ambientSounds.Play("birds");
    _uiHud.ShowNotification("Amanece");
};

cycle.OnNoon += () =>
{
    _enemySpawner.SpawnWave(EnemyType.Desert);
};

cycle.OnSunset += () =>
{
    _ambientSounds.Play("crickets");
    _torchEntities.EnableAll();
};

cycle.OnMidnight += () =>
{
    _saveSystem.AutoSave();
    _enemySpawner.SpawnWave(EnemyType.Undead);
};
```

> Los eventos se comprueban en `Update`. Si `TimeScale` es tan alto que el ciclo salta varios umbrales en un mismo frame, cada evento se dispara una sola vez por ciclo completo.

## TimeScale — acelerar/pausar el tiempo

`TimeScale` multiplica la velocidad a la que avanza el tiempo. Un valor de `1.0f` hace que el ciclo dure exactamente `DayNightProfile.DayDurationSeconds`. `Paused` detiene por completo el avance sin perder la referencia de tiempo actual:

```csharp
// Avance 10× más rápido (útil en modo debug o time-lapse)
cycle.TimeScale = 10f;

// Congelar el tiempo (p.ej. durante un diálogo)
cycle.Paused = true;
// ... fin del diálogo ...
cycle.Paused = false;

// Velocidad normal
cycle.TimeScale = 1f;
```

Leer `CurrentTime` en cualquier momento:

```csharp
TimeOfDay now = cycle.CurrentTime;
_uiHud.UpdateClock(now.ToString()); // "14:32"
```

## Uso sin LightingWorld

`DayNightCycle` puede funcionar de forma completamente autónoma, sin ningún `LightingWorld`. En ese modo el servicio sigue interpolando keyframes y disparando eventos, pero no aplica ningún cambio de iluminación de forma automática. El juego puede leer `CurrentTime` o suscribirse a los eventos para gestionar la iluminación manualmente:

```csharp
// Sin LightingWorld
var cycle = new DayNightCycle(DayNightProfile.Default);
cycle.TimeScale = 2f;

// Modo standalone: leer el keyframe interpolado y aplicarlo a mano
DayNightKeyframe kf = DayNightProfile.Default.Sample(cycle.CurrentTime);
_customRenderer.SetAmbient(kf.AmbientColor * kf.AmbientIntensity);
_sunEntity.Rotation = MathHelper.ToRadians(kf.SunAngleDegrees);
```

Para llamar a `Update` manualmente cuando no se asigna a `GameWorld`:

```csharp
// En la escena, override de Update
protected override void Update(GameTime gameTime)
{
    _standaloneycle.Update(gameTime);
    base.Update(gameTime);
}
```

## Ver también

- [Visión general del sistema de iluminación](overview.md)
- [Tipos de luz](light-types.md)
- [WeatherWorld — sistema de clima](../13-weather/weather-world.md)
