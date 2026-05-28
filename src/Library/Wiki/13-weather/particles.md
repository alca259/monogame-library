# WeatherParticleLayer

**Namespace:** `Alca.MonoGame.Kernel.Weather`

`WeatherParticleLayer` gestiona hasta cinco efectos de partículas independientes: lluvia, nieve, granizo, niebla y sprites de viento. El viento se aplica cada frame mutando directamente los modificadores de gravedad cacheados — cero asignaciones de heap en `Update`.

---

## Ciclo de vida

```csharp
// 1. Crear e inicializar
var particles = new WeatherParticleLayer();
particles.LoadContent(rainTex, snowTex, hailTex, fogTex, windTex);

// 2. Registrar en WeatherWorld
weatherWorld.EnableParticles(particles);

// 3. Seguimiento de cámara (actualizar cada frame antes de Update)
particles.EmitterPosition = cameraCenter - new Vector2(0, 100f);

// 4. Actualización y dibujado (los llama WeatherWorld automáticamente)
// particles.Update(gameTime, profile, wind);  ← automático
// particles.Draw(spriteBatch);                ← llamar manualmente en Draw()

// 5. Limpieza
particles.Dispose();
```

---

## Propiedades de configuración

| Propiedad | Tipo | Default | Descripción |
|---|---|---|---|
| `EmitterPosition` | `Vector2` | `(0, 0)` | Centro de la banda emisora en espacio mundo. Actualiza cada frame para seguir la cámara. |
| `EmitterWidth` | `float` | `1600` | Ancho de la banda emisora en unidades mundo. |

---

## LoadContent

```csharp
void LoadContent(
    Texture2D? rainTexture,
    Texture2D? snowTexture,
    Texture2D? hailTexture,
    Texture2D? fogTexture,
    Texture2D? windTexture)
```

Pasar `null` para cualquier textura deshabilita permanentemente ese efecto. La creación de los efectos y el cacheo de los modificadores de gravedad ocurren aquí — cero asignaciones en `Update`.

---

## Selección de efecto por perfil

`WeatherParticleLayer` activa el efecto apropiado según los datos del `WeatherProfile` activo:

| Perfil (heurística) | Efecto activado |
|---|---|
| `TemperatureMin >= 5` y `WindSpeedMaxKmh < 20` y `FogDensity < 0.1` | Lluvia |
| `TemperatureMax <= 2` | Nieve |
| `TemperatureMin >= 15` y `WindSpeedMinKmh >= 3` (y no nieve) | Granizo |
| `FogDensity >= 0.3` y `WindSpeedMaxKmh <= 0.01` | Niebla |
| `WindSpeedMaxKmh >= 20` y `FogDensity < 0.1` (y no lluvia, no nieve) | Viento sprites |

---

## Parámetros de los efectos

| Efecto | Partículas | Gravedad | Vida |
|---|---|---|---|
| Lluvia | 600 | 400 (↓) | 2.5 s |
| Nieve | 400 | 60 (↓) | 5 s |
| Granizo | 300 | 600 (↓) | 1.5 s |
| Niebla | 150 | 5 (→) | 8 s |
| Viento | 500 | 350 (→) | 2 s |

---

## Dibujado

```csharp
public override void Draw(GameTime gameTime)
{
    Core.SpriteBatch.Begin(/* tu configuración de cámara */);
    _world.Draw(gameTime, Core.SpriteBatch);
    Core.SpriteBatch.End();

    // Dibuja las partículas por separado (gestiona su propio Begin/End con BlendState.Additive)
    _weatherWorld.Particles?.Draw(Core.SpriteBatch);
}
```

Cada efecto activo abre y cierra su propio `SpriteBatch.Begin`/`End` con `BlendState.Additive` para evitar mezclas incorrectas.

---

## Seguimiento de cámara

Para que la precipitación cubra siempre la pantalla visible, actualiza `EmitterPosition` cada frame:

```csharp
public override void Update(GameTime gameTime)
{
    // Antes de la actualización del mundo
    if (_weatherWorld.Particles is not null)
        _weatherWorld.Particles.EmitterPosition = _camera.Center - new Vector2(0, 80f);

    _world.Update(gameTime);
}
```

---

## Ver también

- [Visión general →](overview.md)
- [WeatherWorld (API) →](weather-world.md)
- [Particles (MonoGame.Extended) →](../04-graphics/particles.md)
