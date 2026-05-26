# Rendering 3D

**Namespace:** `Alca.MonoGame.Kernel.Graphics.ThreeD`

El framework incluye dos utilitarios para renderizado 3D: `MeshRenderer` para modelos cargados y `PrimitiveBatch` para primitivos de debug.

---

## MeshRenderer

Renderiza un `Model` de MonoGame con transformación world aplicada.

### Uso básico

```csharp
// En LoadContent:
_model = Content.Load<Model>("Models/cube");
_meshRenderer = new MeshRenderer(_model);

// En Draw:
_meshRenderer.Draw(worldMatrix, _camera.View, _camera.Projection);
```

---

## PrimitiveBatch

Renderiza primitivos 3D (líneas, cajas de debug, esferas, etc.) útiles para visualizar física, bounds y puntos de navegación.

### Uso básico

```csharp
_primitiveBatch = new PrimitiveBatch(Core.GraphicsDevice);

// En Draw:
_primitiveBatch.Begin(_camera.View, _camera.Projection);
_primitiveBatch.DrawBox(position, size, Color.Green);
_primitiveBatch.DrawLine(start, end, Color.Red);
_primitiveBatch.End();
```

---

## Ejemplo completo: cubo 3D en escena

```csharp
public sealed class Scene3D : Scene
{
    private Camera3D _camera = null!;
    private Model _cubeModel = null!;
    private MeshRenderer _mesh = null!;
    private PrimitiveBatch _primitives = null!;

    protected override void PreInitialize()
    {
        _camera = new FixedCamera3D
        {
            Position = new Vector3(0, 5, 10),
            Target   = Vector3.Zero
        };
    }

    public override void LoadContent()
    {
        _cubeModel = Content.Load<Model>("Models/cube");
        _mesh      = new MeshRenderer(_cubeModel);
        _primitives = new PrimitiveBatch(Core.GraphicsDevice);
    }

    private float _rotation;

    public override void Update(GameTime gameTime)
    {
        _rotation += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.SkyBlue);

        // Matriz de mundo del cubo (rotación continua)
        var worldMatrix = Matrix.CreateRotationY(_rotation);

        // Renderizar modelo
        _mesh.Draw(worldMatrix, _camera.View, _camera.Projection);

        // Debug: dibuja el eje Y en verde
        _primitives.Begin(_camera.View, _camera.Projection);
        _primitives.DrawLine(Vector3.Zero, Vector3.Up * 3f, Color.Green);
        _primitives.End();
    }

    public override void UnloadContent()
    {
        base.UnloadContent();
        _primitives.Dispose();
    }
}
```

---

## Notas

- Los modelos 3D deben compilarse con el Content Pipeline de MonoGame (`.fbx`, `.obj` → `.xnb`).
- `PrimitiveBatch` es principalmente una herramienta de debug. Para rendering 3D de producción, usa `MeshRenderer` con materiales personalizados (`LitMaterial`).
- El renderizado 3D utiliza el `GraphicsDevice` directamente, sin `SpriteBatch`.

---

## Ver también

- [Cámaras 3D →](camera-3d.md)
- [Shaders y Materiales →](shaders.md)
