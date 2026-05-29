# Soporte 2.5D

**Namespace:** `Alca.MonoGame.Kernel.Mathematics` / `Alca.MonoGame.Kernel.Graphics.Sprites` / `Alca.MonoGame.Kernel.Graphics.Camera`

El soporte 2.5D de la librería agrupa herramientas de proyección isométrica, ordenación por profundidad, sprites en modo billboard y materiales con mapas normales. Permite construir mundos con perspectiva visual sin abandonar el pipeline 2D de `SpriteBatch`.

## Modos de juego 2.5D soportados

| Modo | Descripción | Clases clave |
|---|---|---|
| Isométrico clásico | Proyección 2:1 (tile rectangular). Depth sorting por posición Y. | `IsometricHelper`, `YSortRendererBehaviour`, `IsometricCamera` |
| Vista superior (top-down) | Ordenación por Y sin transformación isométrica. | `YSortRendererBehaviour` |
| Billboard | Sprites que siempre se orientan hacia la cámara. | `BillboardSpriteBehaviour` |
| Iluminación superficial | Sprites con mapa normal reaccionan a luces 2D. | `NormalMapSpriteMaterial`, `LightingWorld` |

## IsometricHelper — proyección isométrica

`IsometricHelper` convierte entre coordenadas de mundo y coordenadas de pantalla usando una proyección isométrica 2:1. El singleton `Default` usa tiles de 64×32 píxeles; para otros tamaños, instancia la clase directamente.

```csharp
// Usar el singleton por defecto (tile 64x32)
IsometricHelper iso = IsometricHelper.Default;

// O crear una instancia con tile personalizado (p.ej. 128x64)
var isoLarge = new IsometricHelper(tileWidth: 128, tileHeight: 64);

// Proyectar posición de mundo a pantalla
Vector2 worldPos  = new Vector2(3f, 5f);
Vector2 screenPos = iso.WorldToScreen(worldPos);

// Proyección inversa: pantalla → mundo
Vector2 worldBack = iso.ScreenToWorld(screenPos);

// Roundtrip — worldBack ≈ worldPos
Console.WriteLine(Vector2.Distance(worldPos, worldBack) < 0.001f); // true

// Calcular profundidad de capa para SpriteBatch
float depth = iso.DepthFromWorldY(worldPos.Y);
// o desde posición completa (combina X e Y para mayor precisión)
float depth2 = iso.DepthFromPosition(worldPos);
```

> `IsometricAngle` es una constante de `26.565°` (≈ `atan(0.5)`), el ángulo de inclinación característico de la proyección 2:1.

## YSortRendererBehaviour — profundidad por posición Y

`YSortRendererBehaviour` es un comportamiento de renderizado que sustituye a `SpriteRendererBehaviour` cuando se necesita que las entidades con mayor posición Y (más abajo en pantalla) se dibujen encima de las que están más arriba. El `LayerDepth` se calcula como `1 - Clamp(posY / worldHeight, 0, 1)`.

```csharp
// En la entidad o escena, en LoadContent
float worldHeight = 2000f; // altura total del mundo en unidades de juego
Texture2D characterTexture = Content.Load<Texture2D>("Characters/Hero");

// Sustituir SpriteRendererBehaviour por YSortRendererBehaviour
var renderer = new YSortRendererBehaviour(characterTexture, worldHeight);
_heroEntity.AddBehaviour(renderer);

// Todas las entidades con YSortRendererBehaviour se ordenan automáticamente
// entre sí al usar SpriteSortMode.FrontToBack o BackToFront en SpriteBatch.Begin
_spriteBatch.Begin(
    SpriteSortMode.FrontToBack,
    BlendState.AlphaBlend);
// ... Draw de entidades ...
_spriteBatch.End();
```

> Para combinar `YSortRendererBehaviour` con `IsometricHelper`, calcula `worldHeight` en coordenadas de mundo isométrico, no en píxeles de pantalla.

## IsometricCamera — cámara isométrica

`IsometricCamera` envuelve una `Camera2D` y la combina con `IsometricHelper` para ofrecer conversiones de coordenadas que tienen en cuenta tanto la proyección isométrica como la transformación de la cámara (posición, zoom, rotación):

```csharp
// Crear la cámara isométrica con el helper por defecto
var isoCamera = new IsometricCamera(IsometricHelper.Default);

// Acceder a la Camera2D subyacente para configurarla
isoCamera.Camera.Zoom = 1.5f;

// Centrar en una entidad del mundo
isoCamera.CenterOn(_heroEntity.WorldPosition);

// Convertir un clic de ratón a posición de mundo (para selección de tiles, etc.)
Vector2 mouseScreen = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
Vector2 mouseWorld  = isoCamera.ScreenToWorld(mouseScreen);
```

Usa la matriz de la cámara al iniciar `SpriteBatch`:

```csharp
_spriteBatch.Begin(
    SpriteSortMode.FrontToBack,
    BlendState.AlphaBlend,
    transformMatrix: isoCamera.Camera.GetTransformMatrix());
```

## BillboardSpriteBehaviour

`BillboardSpriteBehaviour` cancela la rotación de la cámara para que el sprite siempre apunte al frente. Es útil para barras de vida, nombres de personajes o árboles en escenas 2.5D donde la cámara puede girar:

```csharp
// LoadContent
Texture2D treeTexture = Content.Load<Texture2D>("Environment/Tree");

// La Camera2D se pasa para que el behaviour pueda leer su rotación actual
var billboard = new BillboardSpriteBehaviour(treeTexture, _isoCamera.Camera);
_treeEntity.AddBehaviour(billboard);
```

El behaviour aplica la rotación inversa de la cámara en cada frame dentro de `Draw`, sin ninguna asignación en el heap.

## NormalMapSpriteMaterial

`NormalMapSpriteMaterial` extiende `Material` y permite que un sprite 2D reaccione a luces dinámicas mediante un mapa de normales. El método `SyncLights` lee el color ambiental de `LightingWorld` y lo pasa automáticamente al shader:

```csharp
// LoadContent
Effect normalFx      = Content.Load<Effect>("Shaders/NormalMap");
Texture2D normalMap  = Content.Load<Texture2D>("Characters/Hero_Normal");

_normalMaterial = new NormalMapSpriteMaterial(normalFx)
{
    NormalMap      = normalMap,
    NormalStrength = 1.0f,
    AmbientColor   = Color.White
};

// Update — sincronizar el color ambiental con LightingWorld
// (llamar una vez por frame antes de Draw)
_normalMaterial.SyncLights(_lightingWorld);

// Draw
_normalMaterial.Apply();
_spriteBatch.Draw(_heroTexture, _heroPosition, Color.White);
```

> `SyncLights` es una lectura barata; no realiza asignaciones en el heap y es seguro llamarlo dentro de `Update` o `Draw`.

## Quickstart completo: escena isométrica básica

El siguiente ejemplo muestra la configuración mínima para una escena isométrica con tres entidades ordenadas por Y:

```csharp
// Campos
private IsometricHelper _iso = null!;
private IsometricCamera _isoCamera = null!;
private YSortRendererBehaviour _heroRenderer   = null!;
private YSortRendererBehaviour _enemyRenderer  = null!;
private YSortRendererBehaviour _chestRenderer  = null!;
private Vector2 _heroWorld   = new Vector2(2f, 3f);
private Vector2 _enemyWorld  = new Vector2(4f, 1f);
private Vector2 _chestWorld  = new Vector2(3f, 5f);
private const float WorldHeight = 1000f;

// LoadContent
protected override void LoadContent()
{
    _iso       = IsometricHelper.Default;
    _isoCamera = new IsometricCamera(_iso);
    _isoCamera.CenterOn(_iso.WorldToScreen(_heroWorld));

    Texture2D heroTex  = Content.Load<Texture2D>("Characters/Hero");
    Texture2D enemyTex = Content.Load<Texture2D>("Characters/Enemy");
    Texture2D chestTex = Content.Load<Texture2D>("Environment/Chest");

    _heroRenderer  = new YSortRendererBehaviour(heroTex,  WorldHeight);
    _enemyRenderer = new YSortRendererBehaviour(enemyTex, WorldHeight);
    _chestRenderer = new YSortRendererBehaviour(chestTex, WorldHeight);
}

// Update
protected override void Update(GameTime gameTime)
{
    // Mover el héroe con teclado (ejemplo simplificado)
    KeyboardState kb = Keyboard.GetState();
    float speed = 2f * (float)gameTime.ElapsedGameTime.TotalSeconds;
    if (kb.IsKeyDown(Keys.Right)) _heroWorld.X += speed;
    if (kb.IsKeyDown(Keys.Left))  _heroWorld.X -= speed;
    if (kb.IsKeyDown(Keys.Down))  _heroWorld.Y += speed;
    if (kb.IsKeyDown(Keys.Up))    _heroWorld.Y -= speed;

    _isoCamera.CenterOn(_iso.WorldToScreen(_heroWorld));

    base.Update(gameTime);
}

// Draw
protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.DarkGreen);

    _spriteBatch.Begin(
        SpriteSortMode.FrontToBack,
        BlendState.AlphaBlend,
        transformMatrix: _isoCamera.Camera.GetTransformMatrix());

    // Calcular posiciones de pantalla
    Vector2 heroScreen  = _iso.WorldToScreen(_heroWorld);
    Vector2 enemyScreen = _iso.WorldToScreen(_enemyWorld);
    Vector2 chestScreen = _iso.WorldToScreen(_chestWorld);

    // Dibujar; YSortRendererBehaviour calcula LayerDepth automáticamente
    _heroRenderer.Draw(_spriteBatch, heroScreen);
    _enemyRenderer.Draw(_spriteBatch, enemyScreen);
    _chestRenderer.Draw(_spriteBatch, chestScreen);

    _spriteBatch.End();

    base.Draw(gameTime);
}
```

El `SpriteSortMode.FrontToBack` junto con los valores de `LayerDepth` calculados por `YSortRendererBehaviour` garantizan que el cofre (`Y=5`) se dibuje siempre delante del enemigo (`Y=1`) y detrás o delante del héroe según su posición Y relativa.

## Ver también

- [Camera2D](camera-2d.md)
- [Sprites y SpriteRendererBehaviour](sprites.md)
- [Librería de shaders 2D](shader-library.md)
- [Visión general del sistema de iluminación](../09-lighting/overview.md)
