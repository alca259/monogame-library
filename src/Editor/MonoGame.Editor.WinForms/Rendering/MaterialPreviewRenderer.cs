namespace MonoGame.Editor.WinForms.Rendering;

/// <summary>
/// Renders a 3D UV-sphere preview of a material to a <see cref="RenderTarget2D"/>.
/// <para>
/// All GPU operations must run on the render thread. Call <see cref="Initialize"/> once from the
/// render thread, then <see cref="Render"/> each time the preview needs updating. Retrieve the
/// result as a <see cref="System.Drawing.Bitmap"/> via <see cref="GetPreviewBitmap"/>.
/// </para>
/// </summary>
public sealed class MaterialPreviewRenderer : IDisposable
{
    #region Constants

    private const int PreviewSize   = 256;
    private const int LatDivisions  = 24;
    private const int LonDivisions  = 32;
    private const float SphereRadius = 0.5f;

    #endregion

    #region Fields

    private GraphicsDevice?  _graphicsDevice;
    private RenderTarget2D?  _renderTarget;
    private VertexBuffer?    _vertexBuffer;
    private IndexBuffer?     _indexBuffer;
    private BasicEffect?     _effect;
    private int              _indexCount;
    private Texture2D?       _currentTexture;
    private Microsoft.Xna.Framework.Color _currentColor = Microsoft.Xna.Framework.Color.White;
    private bool             _dirty = true;

    #endregion

    /// <summary>Returns <see langword="true"/> after <see cref="Initialize"/> has been called from the render thread.</summary>
    public bool IsInitialized => _graphicsDevice is not null;

    #region Initialization (render thread)

    /// <summary>Creates GPU resources. Must be called once from the render thread.</summary>
    public void Initialize(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _renderTarget   = new RenderTarget2D(graphicsDevice, PreviewSize, PreviewSize,
            false, SurfaceFormat.Color, DepthFormat.Depth24);

        BuildSphereMesh(graphicsDevice);

        _effect = new BasicEffect(graphicsDevice)
        {
            LightingEnabled    = true,
            PreferPerPixelLighting = true,
            TextureEnabled     = false,
        };
        _effect.EnableDefaultLighting();
    }

    #endregion

    #region Material update (any thread)

    /// <summary>
    /// Updates the material properties used for the next render pass.
    /// Safe to call from the UI thread; actual GPU upload deferred to the render thread.
    /// </summary>
    /// <param name="texture">Optional diffuse texture to show on the sphere.</param>
    /// <param name="color">Tint color (defaults to white if no texture is set).</param>
    public void SetMaterial(Texture2D? texture, Microsoft.Xna.Framework.Color color)
    {
        _currentTexture = texture;
        _currentColor   = color;
        _dirty          = true;
    }

    #endregion

    #region Render (render thread)

    /// <summary>
    /// Renders the sphere preview to the internal <see cref="RenderTarget2D"/>.
    /// Call from the render thread. Returns <see langword="false"/> if not yet initialized.
    /// </summary>
    public bool Render()
    {
        if (_graphicsDevice is null || _renderTarget is null || _effect is null ||
            _vertexBuffer is null  || _indexBuffer  is null) return false;

        if (!_dirty) return true;
        _dirty = false;

        _graphicsDevice.SetRenderTarget(_renderTarget);
        _graphicsDevice.Clear(Microsoft.Xna.Framework.Color.DimGray);

        // Camera positioned at (0, 0, 1.5) looking at origin
        _effect.View       = Matrix.CreateLookAt(new Vector3(0f, 0.3f, 1.5f), Vector3.Zero, Vector3.Up);
        _effect.Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(40f), 1f, 0.1f, 100f);
        _effect.World      = Matrix.Identity;

        if (_currentTexture is not null)
        {
            _effect.TextureEnabled = true;
            _effect.Texture        = _currentTexture;
            _effect.DiffuseColor   = _currentColor.ToVector3();
        }
        else
        {
            _effect.TextureEnabled = false;
            _effect.DiffuseColor   = _currentColor.ToVector3();
        }

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
        }

        _graphicsDevice.SetRenderTarget(null);
        return true;
    }

    /// <summary>
    /// Reads back the last rendered frame as a <see cref="System.Drawing.Bitmap"/>.
    /// Call from the render thread after <see cref="Render"/>, then pass the bitmap to the UI thread.
    /// </summary>
    public System.Drawing.Bitmap? GetPreviewBitmap()
    {
        if (_renderTarget is null) return null;

        var pixels = new Microsoft.Xna.Framework.Color[PreviewSize * PreviewSize];
        _renderTarget.GetData(pixels);

        var bmp = new System.Drawing.Bitmap(PreviewSize, PreviewSize,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % PreviewSize;
            int y = i / PreviewSize;
            Microsoft.Xna.Framework.Color c = pixels[i];
            // MonoGame Color is RGBA; System.Drawing.Color is ARGB
            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B));
        }

        return bmp;
    }

    #endregion

    #region Sphere geometry

    private void BuildSphereMesh(GraphicsDevice gd)
    {
        var vertices = new List<VertexPositionNormalTexture>();
        var indices  = new List<short>();

        for (int lat = 0; lat <= LatDivisions; lat++)
        {
            float theta    = lat * MathF.PI / LatDivisions;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int lon = 0; lon <= LonDivisions; lon++)
            {
                float phi    = lon * 2f * MathF.PI / LonDivisions;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                float x = cosPhi * sinTheta;
                float y = cosTheta;
                float z = sinPhi * sinTheta;

                var normal = new Vector3(x, y, z);
                var uv     = new Vector2((float)lon / LonDivisions, (float)lat / LatDivisions);
                vertices.Add(new VertexPositionNormalTexture(
                    normal * SphereRadius, normal, uv));
            }
        }

        for (int lat = 0; lat < LatDivisions; lat++)
        for (int lon = 0; lon < LonDivisions; lon++)
        {
            short a = (short)( lat      * (LonDivisions + 1) + lon);
            short b = (short)( lat      * (LonDivisions + 1) + lon + 1);
            short c = (short)((lat + 1) * (LonDivisions + 1) + lon);
            short d = (short)((lat + 1) * (LonDivisions + 1) + lon + 1);

            indices.AddRange([a, b, c, b, d, c]);
        }

        VertexPositionNormalTexture[] vArray = [.. vertices];
        short[]                       iArray = [.. indices];

        _vertexBuffer = new VertexBuffer(gd, VertexPositionNormalTexture.VertexDeclaration,
            vArray.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vArray);

        _indexBuffer = new IndexBuffer(gd, IndexElementSize.SixteenBits,
            iArray.Length, BufferUsage.WriteOnly);
        _indexBuffer.SetData(iArray);

        _indexCount = iArray.Length;
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public void Dispose()
    {
        _renderTarget?.Dispose();
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _effect?.Dispose();
    }

    #endregion
}
