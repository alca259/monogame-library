# Librería de Shaders 2D

**Namespace:** `Alca.MonoGame.Kernel.Graphics.Shaders` / `Alca.MonoGame.Kernel.Graphics.Effects`

La librería de shaders 2D proporciona un conjunto de materiales y efectos de post-proceso listos para usar. Todos los materiales extienden la clase base `Material` y se aplican antes de cada llamada a `SpriteBatch.Draw`. Los efectos de post-proceso extienden `PostProcessEffect` y operan sobre un `RenderTarget2D` completo.

## Materiales disponibles

| Nombre | Clase | Efecto visual | Archivo .fx |
|---|---|---|---|
| Contorno sólido | `OutlineMaterial` | Dibuja un borde de color alrededor del sprite | `Content/Shaders/Outline.fx` |
| Hit flash | `FlashMaterial` | Sobreexpone el sprite con un color plano | `Content/Shaders/Flash.fx` |
| Disolución | `DissolveMaterial` | Aparece o desaparece mediante una textura de ruido | `Content/Shaders/Dissolve.fx` |
| Halo luminoso | `GlowMaterial` | Proyecta un halo suave alrededor del sprite | `Content/Shaders/Glow.fx` |
| Silueta | `SilhouetteMaterial` | Rellena el sprite con un color sólido | `Content/Shaders/Silhouette.fx` |
| Efecto CRT | `CRTPostEffect` | Post-proceso: scanlines, distorsión barril y viñeta | `Content/Shaders/CRT.fx` |

## Uso básico

El flujo estándar es: cargar el `Effect` desde el Content Pipeline, pasarlo al constructor del material y llamar a `material.Apply()` justo antes de `SpriteBatch.Draw`:

```csharp
// LoadContent
Effect outlineEffect = Content.Load<Effect>("Shaders/Outline");
_outlineMaterial = new OutlineMaterial(outlineEffect)
{
    OutlineColor     = Color.Red,
    OutlineThickness = 2f,
    AlphaThreshold   = 0.1f
};

// Draw
_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

_outlineMaterial.Apply();
_spriteBatch.Draw(_playerTexture, _playerPosition, Color.White);

_spriteBatch.End();
```

> Los materiales no son `SpriteMaterial` (que es `sealed`). Extienden directamente `Material` y no necesitan estar vinculados a un componente ECS para funcionar; pueden usarse en cualquier contexto de `SpriteBatch`.

## OutlineMaterial — contorno dinámico

Útil para resaltar entidades seleccionables, enemigos en rango o el jugador activo:

```csharp
// Campos de la entidad
private OutlineMaterial _outlineMaterial = null!;

// LoadContent
Effect fx = Content.Load<Effect>("Shaders/Outline");
_outlineMaterial = new OutlineMaterial(fx)
{
    OutlineColor     = Color.Yellow,
    OutlineThickness = 3f,
    AlphaThreshold   = 0.05f
};

// Update — cambio dinámico de color según estado
_outlineMaterial.OutlineColor = _entity.IsSelected ? Color.Cyan : Color.Yellow;

// Draw
_outlineMaterial.Apply();
_spriteBatch.Draw(_entityTexture, _entityPosition, Color.White);
```

## FlashMaterial — hit flash en combate

Simula el parpadeo blanco o de color cuando un personaje recibe daño. `FlashIntensity` se anima desde `1.0f` hasta `0.0f` en los frames siguientes al impacto:

```csharp
// Campo
private FlashMaterial _flashMaterial = null!;
private float _flashTimer = 0f;
private const float FlashDuration = 0.15f;

// LoadContent
_flashMaterial = new FlashMaterial(Content.Load<Effect>("Shaders/Flash"))
{
    FlashColor = Color.White
};

// Llamar al recibir daño
public void TriggerHitFlash() => _flashTimer = FlashDuration;

// Update
if (_flashTimer > 0f)
{
    _flashTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    _flashMaterial.FlashIntensity = MathHelper.Clamp(_flashTimer / FlashDuration, 0f, 1f);
}

// Draw
if (_flashTimer > 0f)
{
    _flashMaterial.Apply();
}
_spriteBatch.Draw(_texture, _position, Color.White);
```

## DissolveMaterial — aparición/desaparición

Controla `Progress` de `0f` (sprite completamente visible) a `1f` (completamente disuelto). El canal de ruido de `NoiseTexture` determina el patrón de disolución:

```csharp
// LoadContent
Texture2D noiseTexture = Content.Load<Texture2D>("Textures/Noise");
_dissolveMaterial = new DissolveMaterial(Content.Load<Effect>("Shaders/Dissolve"))
{
    NoiseTexture = noiseTexture,
    EdgeColor    = new Color(255, 120, 0),
    EdgeWidth    = 0.05f,
    Progress     = 0f
};

// Update — aparición progresiva al spawnear
_dissolveMaterial.Progress = MathHelper.Clamp(
    _dissolveMaterial.Progress + (float)gameTime.ElapsedGameTime.TotalSeconds * 0.8f,
    0f, 1f);

// Draw
_dissolveMaterial.Apply();
_spriteBatch.Draw(_texture, _position, Color.White);
```

## GlowMaterial y SilhouetteMaterial

Ambos materiales siguen el mismo patrón de uso: construir en `LoadContent` y llamar a `Apply()` antes de dibujar.

```csharp
// GlowMaterial — halo alrededor de objetos mágicos o interactuables
_glowMaterial = new GlowMaterial(Content.Load<Effect>("Shaders/Glow"))
{
    GlowColor     = Color.Aqua,
    GlowIntensity = 1.5f,
    GlowRadius    = 8   // píxeles; se pasa internamente como float al shader
};

// SilhouetteMaterial — mostrar enemigos detrás de obstáculos
_silhouetteMaterial = new SilhouetteMaterial(Content.Load<Effect>("Shaders/Silhouette"))
{
    SilhouetteColor = new Color(255, 0, 0, 120),
    AlphaThreshold  = 0.1f
};

// Draw (silhoueta primero, luego sprite normal)
_silhouetteMaterial.Apply();
_spriteBatch.Draw(_enemyTexture, _enemyPosition, Color.White);
```

## CRTPostEffect — post-proceso

`CRTPostEffect` extiende `PostProcessEffect` y opera sobre un `RenderTarget2D` proporcionado por el `RenderTargetManager`. El efecto aplica scanlines, distorsión barril y viñeta al frame completo:

```csharp
// LoadContent
Effect crtFx = Content.Load<Effect>("Shaders/CRT");
_crtEffect = new CRTPostEffect(crtFx)
{
    ScanlineIntensity  = 0.25f,
    BarrelDistortion   = 0.08f,
    VignetteRadius     = 0.75f,
    Resolution         = new Vector2(
        GraphicsDevice.Viewport.Width,
        GraphicsDevice.Viewport.Height)
};

// Registrar en el gestor de post-proceso
_renderTargetManager.AddPostEffect(_crtEffect);
```

En el ciclo de Draw, el `RenderTargetManager` aplica automáticamente el efecto al resolver el render target principal hacia la pantalla. Si lo gestionas manualmente:

```csharp
// Draw manual
GraphicsDevice.SetRenderTarget(_sceneTarget);
DrawScene();

GraphicsDevice.SetRenderTarget(null);
_spriteBatch.Begin(SpriteSortMode.Immediate);
_crtEffect.Apply();
_spriteBatch.Draw(_sceneTarget, Vector2.Zero, Color.White);
_spriteBatch.End();
```

## Notas importantes

- **Los materiales no extienden `SpriteMaterial`** (`SpriteMaterial` es `sealed`). Todos extienden `Material` directamente y pueden usarse en cualquier contexto de `SpriteBatch`.
- **Llamar a `Apply()` antes de cada `Draw`** — el estado del shader se aplica en el momento de la llamada. Si dibujas múltiples sprites con el mismo material en el mismo `SpriteBatch.Begin/End`, llama a `Apply()` una vez antes del primer `Draw`; el estado se mantiene mientras no cambie el `Effect` activo.
- **`SpriteBatch.Begin` con `SpriteSortMode.Immediate`** — es necesario cuando el shader requiere control total del estado de GPU (como `CRTPostEffect`). Para materiales por sprite (`OutlineMaterial`, `FlashMaterial`, etc.), `SpriteSortMode.Deferred` es suficiente.

## Ver también

- [Shaders — fundamentos y pipeline](shaders.md)
- [Post-procesado](post-processing.md)
