# Pipeline GPU de Iluminación

**Namespace:** `Alca.MonoGame.Kernel.Lighting.GPU`

`LightingRenderPipeline` delega el cálculo de iluminación en la GPU mediante un shader HLSL que recibe los datos de todas las luces activas como buffer uniforme.

---

## LightingRenderPipeline

### Constructor

```csharp
new LightingRenderPipeline(GraphicsDevice graphicsDevice,
                           LightingWorld lightingWorld,
                           int maxLights = 64)
```

### Métodos

| Método | Descripción |
|---|---|
| `LoadEffect(content, assetPath)` | Carga el shader de iluminación desde el Content Pipeline |
| `BeginSceneCapture()` | Redirige el render al render target interno (antes de dibujar la escena) |
| `EndSceneCapture()` | Restaura el backbuffer |
| `ApplyLighting(layer, spriteBatch)` | Empuja los datos de luz al shader y dibuja el render target resultante |
| `Resize(width, height)` | Recrea los render targets al cambiar la resolución |
| `Dispose()` | Libera todos los recursos |

---

## LightShaderData

Struct que serializa una luz para el shader. Cada luz ocupa `FloatCount = 13` floats en el buffer.

```csharp
public readonly struct LightShaderData
{
    public Vector2 Position  { get; }
    public float   Range     { get; }
    public float   Intensity { get; }
    public Vector4 Color     { get; }
    public int     Type      { get; }   // 0=Ambient, 1=Directional, 2=Point, 3=Spot
    public float   InnerAngle { get; }
    public float   OuterAngle { get; }
    public Vector2 Direction  { get; }
    public void PackInto(float[] buffer, int offset);
}
```

---

## Ejemplo: habilitación del pipeline GPU

```csharp
public sealed class NightScene : Scene
{
    private LightingRenderPipeline _lightPipeline = null!;

    protected override GameWorld? CreateWorld() =>
        new GameWorld
        {
            LightingWorld = new LightingWorld { AmbientColor = new Color(10, 10, 30) }
        };

    public override void LoadContent()
    {
        var vp = Core.GraphicsDevice.Viewport;
        _lightPipeline = new LightingRenderPipeline(
            Core.GraphicsDevice,
            World!.LightingWorld!,
            maxLights: 32);

        _lightPipeline.LoadEffect(Content, "Effects/lighting");
    }

    public override void Draw(GameTime gameTime)
    {
        // 1. Capturar la escena sin iluminación
        _lightPipeline.BeginSceneCapture();
        Core.GraphicsDevice.Clear(Color.Black);
        base.Draw(gameTime);  // dibuja sprites, tiles, etc.
        _lightPipeline.EndSceneCapture();

        // 2. Aplicar la iluminación en GPU sobre la capa World
        _lightPipeline.ApplyLighting(LightingLayer.World, Core.SpriteBatch);
    }

    public override void UnloadContent()
    {
        base.UnloadContent();
        _lightPipeline.Dispose();
    }
}
```

---

## Shader HLSL de referencia (fragmento)

```hlsl
// Estructura de datos de una luz (13 floats)
struct LightData
{
    float2 Position;
    float  Range;
    float  Intensity;
    float4 Color;
    int    Type;
    float  InnerAngle;
    float  OuterAngle;
    float2 Direction;
};

sampler2D SceneTexture : register(s0);
float4    AmbientColor;
int       LightCount;
LightData Lights[64];

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
    float4 scene = tex2D(SceneTexture, uv);
    float4 accumulated = AmbientColor;

    for (int i = 0; i < LightCount; i++)
    {
        // ... lógica de contribución por tipo de luz ...
    }

    return scene * accumulated;
}
```

---

## Notas

- El shader de iluminación debe estar compilado con el Content Pipeline de MonoGame (`ps_3_0` o superior).
- `maxLights` limita el tamaño del buffer; asegúrate de que no hay más luces activas de esa cantidad en la capa.
- Si la ventana cambia de tamaño, llama a `_lightPipeline.Resize(newWidth, newHeight)` para recrear los render targets.
- El método CPU (`LightingWorld.Resolve`) y el pipeline GPU son independientes; puedes mezclarlos por capa.

---

## Ver también

- [Tipos de luz →](light-types.md)
- [Post-procesado →](../04-graphics/post-processing.md)
