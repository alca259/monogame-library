# Shaders y Materiales

**Namespace:** `Alca.MonoGame.Kernel.Graphics.Shaders`

`Material` y `SpriteMaterial` envuelven un `Effect` de MonoGame y centralizan la configuración de sus parámetros antes de renderizar.

---

## Material (clase base)

Encapsula un `Effect` y define el contrato para aplicar sus parámetros al GPU.

### Propiedad

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Effect` | `Effect` | El effect de MonoGame subyacente |

### Método abstracto

```csharp
public abstract void Apply();
```

Implementa `Apply()` para empujar todos los uniformes al shader antes de renderizar.

---

## SpriteMaterial

Especialización de `Material` para sprites 2D. Permite aplicar efectos a `SpriteBatch` mediante el parámetro `effect` de `SpriteBatch.Begin`.

---

## Ejemplo: material de outline

```csharp
public sealed class OutlineMaterial : Material
{
    private readonly EffectParameter _colorParam;
    private readonly EffectParameter _thicknessParam;

    public Color OutlineColor { get; set; } = Color.Black;
    public float Thickness    { get; set; } = 1f;

    public OutlineMaterial(Effect effect) : base(effect)
    {
        _colorParam     = effect.Parameters["OutlineColor"];
        _thicknessParam = effect.Parameters["Thickness"];
    }

    public override void Apply()
    {
        _colorParam.SetValue(OutlineColor.ToVector4());
        _thicknessParam.SetValue(Thickness);
    }
}
```

Uso con `SpriteBatch`:

```csharp
_outlineMaterial.Apply();
spriteBatch.Begin(SpriteSortMode.Immediate, effect: _outlineMaterial.Effect);
spriteBatch.Draw(texture, position, Color.White);
spriteBatch.End();
```

---

## Ejemplo: material 3D (BasicEffect personalizado)

```csharp
public sealed class LitMaterial : Material
{
    private readonly BasicEffect _basic;

    public Matrix World      { get; set; } = Matrix.Identity;
    public Matrix View       { get; set; }
    public Matrix Projection { get; set; }

    public LitMaterial(GraphicsDevice gd) : base(new BasicEffect(gd))
    {
        _basic = (BasicEffect)Effect;
        _basic.LightingEnabled = true;
        _basic.EnableDefaultLighting();
    }

    public override void Apply()
    {
        _basic.World      = World;
        _basic.View       = View;
        _basic.Projection = Projection;
    }
}
```

---

## Notas

- `Material` es `abstract` — siempre crea una subclase.
- Los parámetros del shader deben cachearse en el constructor (como `EffectParameter`), nunca obtenerse por nombre en `Apply()`.
- Para efectos de post-proceso, usa `PostProcessEffect` en lugar de `Material`.

---

## Ver también

- [Post-procesado →](post-processing.md)
- [Rendering 3D →](rendering-3d.md)
