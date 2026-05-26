# Post-procesado y RenderTargets

**Namespace:** `Alca.MonoGame.Kernel.Graphics.Effects`

El sistema de post-procesado permite aplicar efectos de pantalla completa (blur, vignette, color grading, etc.) usando un esquema de render targets ping-pong.

---

## RenderTargetManager

Gestiona dos `RenderTarget2D` que alternan el rol de origen y destino para cadenas de efectos.

### Constructor

```csharp
new RenderTargetManager(GraphicsDevice graphicsDevice, int width, int height)
```

### Métodos

| Método | Descripción |
|---|---|
| `BeginCapture()` | Redirige el render al target A (antes de dibujar la escena) |
| `EndCapture()` | Restaura el backbuffer (después de dibujar la escena) |
| `Apply(Effect, SpriteBatch)` | Aplica un efecto único y dibuja al backbuffer |
| `ApplyChain(Effect[], SpriteBatch)` | Aplica una cadena de efectos en ping-pong |
| `Dispose()` | Libera los render targets |

---

## PostProcessEffect

Clase base abstracta para efectos personalizados.

### Constructor

```csharp
protected PostProcessEffect(Effect effect)
```

### Métodos

| Método | Descripción |
|---|---|
| `SetParameters()` | **Abstracto** — empuja los uniformes del shader al GPU |
| `Apply(RenderTargetManager, SpriteBatch)` | Llama a `SetParameters()` y aplica el efecto |

---

## Ejemplo: efecto vignette

### 1. Shader HLSL (`Content/Effects/vignette.fx`)

```hlsl
sampler2D ScreenTexture : register(s0);
float Intensity = 0.8;
float Radius    = 0.75;

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(ScreenTexture, uv);
    float2 centered = uv - 0.5;
    float dist = length(centered);
    float vignette = smoothstep(Radius, Radius - 0.3, dist * Intensity);
    return color * vignette;
}

technique VignetteTechnique
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
```

### 2. Clase C#

```csharp
using Alca.MonoGame.Kernel.Graphics.Effects;
using Microsoft.Xna.Framework.Graphics;

public sealed class VignetteEffect : PostProcessEffect
{
    private readonly EffectParameter _intensityParam;
    private readonly EffectParameter _radiusParam;

    public float Intensity { get; set; } = 0.8f;
    public float Radius    { get; set; } = 0.75f;

    public VignetteEffect(Effect effect) : base(effect)
    {
        _intensityParam = effect.Parameters["Intensity"];
        _radiusParam    = effect.Parameters["Radius"];
    }

    public override void SetParameters()
    {
        _intensityParam.SetValue(Intensity);
        _radiusParam.SetValue(Radius);
    }
}
```

### 3. Uso en la Scene

```csharp
private RenderTargetManager _rtm = null!;
private VignetteEffect _vignette = null!;

public override void LoadContent()
{
    var effectAsset = Content.Load<Effect>("Effects/vignette");
    _vignette = new VignetteEffect(effectAsset);

    var vp = Core.GraphicsDevice.Viewport;
    _rtm = new RenderTargetManager(Core.GraphicsDevice, vp.Width, vp.Height);
}

public override void Draw(GameTime gameTime)
{
    // 1. Capturar la escena
    _rtm.BeginCapture();
    Core.GraphicsDevice.Clear(Color.CornflowerBlue);
    base.Draw(gameTime); // dibuja mundo y UI
    _rtm.EndCapture();

    // 2. Aplicar el efecto al backbuffer
    _vignette.Apply(_rtm, Core.SpriteBatch);
}

public override void UnloadContent()
{
    base.UnloadContent();
    _rtm.Dispose();
}
```

---

## Cadena de múltiples efectos

```csharp
private RenderTargetManager _rtm = null!;
private Effect _blur   = null!;
private Effect _grading = null!;

public override void Draw(GameTime gameTime)
{
    _rtm.BeginCapture();
    Core.GraphicsDevice.Clear(Color.Black);
    base.Draw(gameTime);
    _rtm.EndCapture();

    // Aplica blur → color grading → backbuffer
    _rtm.ApplyChain(new[] { _blur, _grading }, Core.SpriteBatch);
}
```

---

## Notas

- Crea el `RenderTargetManager` en `LoadContent` y llama a `Dispose()` en `UnloadContent`.
- El tamaño de los render targets debe coincidir con la resolución de pantalla (o con la resolución virtual). Si la ventana cambia de tamaño, recrea el manager.
- `ApplyChain` es zero-alloc si el array de efectos se pre-asigna como campo.

---

## Ver también

- [Shaders y Materiales →](shaders.md)
- [ResolutionManager →](resolution.md)
