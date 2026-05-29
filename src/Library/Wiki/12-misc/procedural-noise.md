# Ruido Procedural

**Namespace:** `Alca.MonoGame.Kernel.Mathematics.Noise`

Colección de generadores de ruido deterministas para uso en juegos: generación de terreno, texturas de disolución, variación orgánica de colores y cualquier necesidad de valores pseudoaleatorios suaves. Todas las clases son instanciables con una semilla y no realizan asignaciones de heap por frame tras su construcción.

---

## Cuándo usar Perlin vs Simplex

| Característica | `PerlinNoise` | `SimplexNoise` |
|---|---|---|
| Dimensiones | 1D, 2D | 2D, 3D |
| Artefactos visuales | Leve patrón de cuadrícula a escala alta | Sin artefactos de cuadrícula |
| Coste computacional | Bajo | Bajo (ligeramente menor que Perlin en 3D) |
| fBm integrado | Sí (`Fractal()`) | No (calcular manualmente) |
| Rango de salida | [-1, 1] / [0, 1] | [-1, 1] / [0, 1] |
| Uso recomendado | Heightmaps, texturas, efectos dissolve | Animación, 3D, variación continua en tiempo |

---

## Uso básico de PerlinNoise

```csharp
using Alca.MonoGame.Kernel.Mathematics.Noise;

// Semilla fija → resultados deterministas
var perlin = new PerlinNoise(seed: 42);

// 1D: útil para oscilación de parámetros a lo largo del tiempo
float wave = perlin.Get(gameTime.TotalGameTime.TotalSeconds * 0.5f);

// 2D: coordenadas de mundo escaladas
float height = perlin.Get(x * 0.01f, y * 0.01f);

// 2D normalizado al rango [0, 1]
float alpha = perlin.Get01(x * 0.02f, y * 0.02f);
```

### API de `PerlinNoise`

| Método | Rango | Descripción |
|---|---|---|
| `Get(float x)` | [-1, 1] | Ruido 1D |
| `Get(float x, float y)` | [-1, 1] | Ruido 2D |
| `Get01(float x, float y)` | [0, 1] | Ruido 2D remapeado |
| `Fractal(x, y, octaves, persistence, lacunarity)` | [-1, 1] | fBm normalizado |

---

## fBm (fractal Brownian motion) con `Fractal()`

El método `Fractal` acumula varias octavas de ruido (frecuencias crecientes, amplitudes decrecientes) para producir un resultado con detalle a múltiples escalas, similar a la topografía natural.

```csharp
var perlin = new PerlinNoise(seed: 7);

// Parámetros típicos para terreno 2D
int   octaves     = 6;
float persistence = 0.5f;   // amplitud se reduce a la mitad por octava
float lacunarity  = 2.0f;   // frecuencia se duplica por octava

float heightValue = perlin.Fractal(
    x * 0.005f, y * 0.005f,
    octaves, persistence, lacunarity);

// heightValue en [-1, 1] → convertir a altura de mapa
float worldHeight = (heightValue + 1f) * 0.5f * MaxTerrainHeight;
```

| Parámetro | Efecto al aumentar |
|---|---|
| `octaves` | Más detalle fino; coste lineal |
| `persistence` | Amplitud de las octavas altas (más rugosidad si > 0.5) |
| `lacunarity` | Frecuencia de las octavas altas (más detalle pequeño si > 2) |

---

## Uso básico de SimplexNoise

```csharp
using Alca.MonoGame.Kernel.Mathematics.Noise;

var simplex = new SimplexNoise(seed: 123);

// 2D
float n2 = simplex.Get(x * 0.03f, y * 0.03f);

// 3D: z puede ser el tiempo para animar el ruido
float n3 = simplex.Get(x * 0.02f, y * 0.02f, time * 0.1f);

// Normalizado [0, 1]
float alpha = simplex.Get01(x * 0.05f, y * 0.05f);
```

### API de `SimplexNoise`

| Método | Rango | Descripción |
|---|---|---|
| `Get(float x, float y)` | [-1, 1] | Ruido 2D |
| `Get(float x, float y, float z)` | [-1, 1] | Ruido 3D |
| `Get01(float x, float y)` | [0, 1] | Ruido 2D remapeado |

---

## NoiseMap — generación de heightmaps

`NoiseMap` preasigna un `float[,]` de las dimensiones indicadas y lo rellena en una sola llamada. La generación ocurre fuera del bucle de juego (en `LoadContent` o `InitializeWorld`).

```csharp
using Alca.MonoGame.Kernel.Mathematics.Noise;

// Crear el mapa una sola vez
var noiseMap = new NoiseMap(width: 256, height: 256);
var perlin   = new PerlinNoise(seed: 99);

// Generar con Perlin fBm
float[,] heights = noiseMap.Generate(
    perlin,
    scale:       0.004f,
    octaves:     5,
    persistence: 0.5f,
    lacunarity:  2.0f);

// Generar con Simplex (sin fBm integrado)
var simplex = new SimplexNoise(seed: 99);
float[,] simpleHeights = noiseMap.Generate(simplex, scale: 0.008f);

// Acceso mediante indexer
float h = noiseMap[128, 64];
```

### API de `NoiseMap`

| Miembro | Descripción |
|---|---|
| `NoiseMap(int width, int height)` | Preasigna el buffer interno |
| `Generate(PerlinNoise, scale, octaves, persistence, lacunarity)` | Rellena y devuelve `float[,]` en [-1, 1] |
| `Generate(SimplexNoise, scale)` | Rellena y devuelve `float[,]` en [-1, 1] |
| `this[int x, int y]` | Acceso directo al valor generado |
| `ToTexture(GraphicsDevice)` | Crea una `Texture2D` en escala de grises |

---

## Convertir a textura

`ToTexture` genera una `Texture2D` en escala de grises (una asignación de heap única). Útil para previsualización en editor, dissolve masks o texturas de ruido para shaders.

```csharp
// En LoadContent o InitializeWorld — nunca en Update/Draw
var noiseMap = new NoiseMap(128, 128);
noiseMap.Generate(new PerlinNoise(seed: 5), scale: 0.01f, octaves: 4,
                  persistence: 0.5f, lacunarity: 2.0f);

Texture2D noiseTex = noiseMap.ToTexture(GraphicsDevice);

// Usar como máscara de dissolve en un shader
_dissolveEffect.Parameters["NoiseMask"].SetValue(noiseTex);

// O dibujar directamente para depuración
spriteBatch.Draw(noiseTex, Vector2.Zero, Color.White);
```

> `ToTexture` asigna memoria en el heap (textura GPU). Llámalo una sola vez durante la carga, no en el bucle de juego.

---

## Casos de uso

| Caso de uso | Clase recomendada | Notas |
|---|---|---|
| Heightmap de terreno 2D/3D | `NoiseMap` + `PerlinNoise.Fractal` | Múltiples octavas para detalle natural |
| Dissolve transition mask | `NoiseMap.ToTexture` | Textura de ruido estática; pasar a shader |
| Variación de color por tile | `PerlinNoise.Get01` | Escalar UV por posición del tile |
| Animación orgánica (ondas, nubes) | `SimplexNoise.Get` (3D con tiempo en z) | Sin artefactos a frecuencias altas |
| Generación de cuevas (cellular) | `PerlinNoise.Get01` + umbral | Valores < 0.45 = sólido, > 0.45 = vacío |
| Desplazamiento de viento / partículas | `PerlinNoise.Get` (1D temporal) | Muy bajo coste; sin GC |

---

## Notas de rendimiento

- `PerlinNoise` y `SimplexNoise` no realizan ninguna asignación de heap después de la construcción — seguros en `Update` y `Draw`.
- `NoiseMap.Generate` reutiliza el `float[,]` preasignado; no crea basura.
- `NoiseMap.ToTexture` sí asigna una textura GPU — úsalo solo en inicialización.
- Para variación por frame (animaciones), pasa el tiempo escalado como coordenada z de `SimplexNoise.Get(x, y, t)` en lugar de regenerar el mapa completo.

---

## Ver también

- [Matemáticas y Geometría →](mathematics.md)
