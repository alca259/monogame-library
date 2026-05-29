namespace MonoGame.Editor.WinForms.Rendering;

/// <summary>
/// Renderiza una previsualización de esfera UV-3D de un material en un <see cref="RenderTarget2D"/>.
/// <para>
/// Todas las operaciones de GPU deben ejecutarse en el hilo de renderizado. Llamar a <see cref="Initialize"/> una vez desde
/// el hilo de renderizado y luego <see cref="Render"/> cada vez que la previsualización necesite actualizarse. Obtener el
/// resultado como <see cref="System.Drawing.Bitmap"/> mediante <see cref="GetPreviewBitmap"/>.
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

    /// <summary>Devuelve <see langword="true"/> tras llamar a <see cref="Initialize"/> desde el hilo de renderizado.</summary>
    public bool IsInitialized => _graphicsDevice is not null;

    #region Initialization (render thread)

    /// <summary>Crea recursos de GPU. Debe llamarse una vez desde el hilo de renderizado.</summary>
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
    /// Actualiza las propiedades del material usadas en la siguiente pasada de renderizado.
    /// Seguro de llamar desde el hilo de UI; la carga real a GPU se difiere al hilo de renderizado.
    /// </summary>
    /// <param name="texture">Textura difusa opcional para mostrar en la esfera.</param>
    /// <param name="color">Color de tinte (por defecto blanco si no se establece textura).</param>
    public void SetMaterial(Texture2D? texture, Microsoft.Xna.Framework.Color color)
    {
        _currentTexture = texture;
        _currentColor   = color;
        _dirty          = true;
    }

    #endregion

    #region Render (render thread)

    /// <summary>
    /// Renderiza la previsualización de la esfera en el <see cref="RenderTarget2D"/> interno.
    /// Llamar desde el hilo de renderizado. Devuelve <see langword="false"/> si aún no está inicializado.
    /// </summary>
    public bool Render()
    {
        if (_graphicsDevice is null || _renderTarget is null || _effect is null ||
            _vertexBuffer is null  || _indexBuffer  is null) return false;

        if (!_dirty) return true;
        _dirty = false;

        _graphicsDevice.SetRenderTarget(_renderTarget);
        _graphicsDevice.Clear(Microsoft.Xna.Framework.Color.DimGray);

        // Cámara posicionada en (0, 0, 1.5) apuntando al origen
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
    /// Lee el último fotograma renderizado como un <see cref="System.Drawing.Bitmap"/>.
    /// Llamar desde el hilo de renderizado tras <see cref="Render"/> y luego pasar el bitmap al hilo de UI.
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
            // MonoGame Color es RGBA; System.Drawing.Color es ARGB
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
