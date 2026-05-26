# Sprites y TextureAtlas

**Namespace:** `Alca.MonoGame.Kernel.Graphics.Sprites`

Estos tipos forman la base del sistema visual 2D: `TextureRegion` recorta una zona de una textura, `Sprite` añade color, escala y rotación, y `TextureAtlas` agrupa regiones por nombre.

---

## TextureRegion

Representa una región rectangular dentro de una textura. Ideal para sprites atlaseados (sprite sheets).

### Constructor

```csharp
new TextureRegion(Texture2D texture, int x, int y, int width, int height)
```

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Texture` | `Texture2D` | Textura fuente |
| `SourceRectangle` | `Rectangle` | Rectángulo de recorte |
| `Width` / `Height` | `int` | Dimensiones en píxeles |
| `TopTextureCoordinate` | `float` | Coordenada UV superior (0-1) |
| `BottomTextureCoordinate` | `float` | Coordenada UV inferior (0-1) |
| `LeftTextureCoordinate` | `float` | Coordenada UV izquierda (0-1) |
| `RightTextureCoordinate` | `float` | Coordenada UV derecha (0-1) |

### Dibujar

```csharp
// Simple
region.Draw(spriteBatch, position, Color.White);

// Con todos los parámetros
region.Draw(spriteBatch, position, Color.White,
    rotation: 0f,
    origin: new Vector2(16, 16), // centro del sprite
    scale: Vector2.One,
    effects: SpriteEffects.None,
    layerDepth: 0.5f);
```

---

## Sprite

Envuelve una `TextureRegion` añadiendo estado visual: color, escala, rotación, origin y efectos de flip.

### Propiedades

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Region` | `TextureRegion` | Región fuente (required) |
| `Color` | `Color` | Tinte multiplicativo (defecto: `Color.White`) |
| `Rotation` | `float` | Rotación en radianes |
| `Scale` | `Vector2` | Escala XY (defecto: `Vector2.One`) |
| `Origin` | `Vector2` | Punto de rotación/escala relativo a top-left del sprite |
| `Effects` | `SpriteEffects` | Flip horizontal/vertical |
| `LayerDepth` | `float` | Profundidad de capa [0,1] |
| `Width` / `Height` | `float` | Dimensiones en píxeles (Region × Scale) |

### Métodos

```csharp
sprite.CenterOrigin(); // ajusta Origin al centro del sprite
sprite.Draw(spriteBatch, position);
```

### Ejemplo: sprite con flip

```csharp
var sprite = new Sprite
{
    Region = new TextureRegion(texture, 0, 0, 32, 32),
    Color  = Color.White,
    Scale  = new Vector2(2f, 2f)
};
sprite.CenterOrigin();

// Flip horizontal cuando va a la izquierda
sprite.Effects = movingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

spriteBatch.Begin();
sprite.Draw(spriteBatch, entity.Transform.Position2d);
spriteBatch.End();
```

---

## TextureAtlas

Colección de `TextureRegion` accesibles por nombre. Útil para sprite sheets donde los frames tienen nombres.

### Uso típico

```csharp
var atlas = new TextureAtlas();
atlas.Add("player_idle_0", new TextureRegion(sheet, 0,   0, 32, 32));
atlas.Add("player_idle_1", new TextureRegion(sheet, 32,  0, 32, 32));
atlas.Add("player_run_0",  new TextureRegion(sheet, 64,  0, 32, 32));
atlas.Add("player_run_1",  new TextureRegion(sheet, 96,  0, 32, 32));

// Acceso por nombre
TextureRegion? frame = atlas.Get("player_idle_0");
```

---

## Ejemplo completo: sprite sheet con 4 columnas × 4 filas

```csharp
// Crear atlas desde una textura de 128×128 con frames de 32×32
var sheet   = Content.Load<Texture2D>("Sprites/character_sheet");
var atlas   = new TextureAtlas();
string[] animNames = { "idle", "run", "jump", "death" };

for (int row = 0; row < 4; row++)
for (int col = 0; col < 4; col++)
{
    string name = $"{animNames[row]}_{col}";
    atlas.Add(name, new TextureRegion(sheet, col * 32, row * 32, 32, 32));
}

// Construir animación de idle (fila 0)
var idleFrames = new List<TextureRegion>();
for (int i = 0; i < 4; i++)
    idleFrames.Add(atlas.Get($"idle_{i}")!);

var idleAnim = new Animation(idleFrames, TimeSpan.FromMilliseconds(120));
```

---

## Ver también

- [Animation →](animation.md)
- [SpriteRendererBehaviour →](../02-ecs/game-entity.md)
