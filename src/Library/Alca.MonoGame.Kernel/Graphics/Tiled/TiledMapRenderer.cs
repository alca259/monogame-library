using Alca.MonoGame.Kernel.Graphics.Camera;
using MonoGame.Extended;
using MonoGame.Extended.Tilemaps;
using MonoGame.Extended.Tilemaps.Rendering;
using MonoGame.Extended.ViewportAdapters;

namespace Alca.MonoGame.Kernel.Graphics.Tiled;

/// <summary>Wraps <see cref="TilemapSpriteBatchRenderer"/> to render Tiled maps using the project's <see cref="Camera2D"/>.</summary>
public sealed class TiledMapRenderer : IDisposable
{
    private readonly DefaultViewportAdapter? _viewportAdapter;
    private readonly OrthographicCamera? _extCamera;
    private readonly TilemapSpriteBatchRenderer? _renderer;
    private bool _disposed;

    /// <summary>Gets the loaded tilemap, or <c>null</c> if <see cref="Load"/> has not been called.</summary>
    public Tilemap? LoadedMap { get; private set; }

    /// <summary>Initialises the renderer. Call after <see cref="GraphicsDevice"/> is available.</summary>
    public TiledMapRenderer(GraphicsDevice graphicsDevice)
    {
        _viewportAdapter = new DefaultViewportAdapter(graphicsDevice);
        _extCamera = new OrthographicCamera(_viewportAdapter);
        _renderer = new TilemapSpriteBatchRenderer
        {
            BlendState = BlendState.AlphaBlend
        };
    }

    /// <summary>Initialises an empty renderer for unit testing (no hardware required).</summary>
    internal TiledMapRenderer() { }

    /// <summary>Loads a tilemap asset via the content pipeline.</summary>
    public void Load(ContentManager content, string assetName)
    {
        LoadedMap = content.Load<Tilemap>(assetName);
        _renderer!.LoadTilemap(LoadedMap);
    }

    /// <summary>Advances tile animations. Call once per frame in <c>Update</c>.</summary>
    public void Update(GameTime gameTime)
    {
        if (LoadedMap is null) return;
        _renderer!.Update(gameTime);
    }

    /// <summary>Renders all tile layers.</summary>
    public void Draw(Camera2D camera, SpriteBatch spriteBatch)
    {
        if (LoadedMap is null) return;
        SyncCamera(camera);
        _renderer!.Draw(spriteBatch, _extCamera!);
    }

    /// <summary>Renders a single named tile layer. Use for entity interleaving between layers.</summary>
    public void DrawLayer(Camera2D camera, SpriteBatch spriteBatch, string layerName)
    {
        if (LoadedMap is null) return;
        SyncCamera(camera);
        _renderer!.DrawLayer(spriteBatch, _extCamera!, layerName);
    }

    /// <summary>Renders the specified tile layers in order. Use for entity interleaving between layers.</summary>
    /// <remarks>Each call allocates a params array at the call site — prefer multiple <see cref="DrawLayer"/> calls on hot paths.</remarks>
    public void DrawLayers(Camera2D camera, SpriteBatch spriteBatch, params string[] layerNames)
    {
        if (LoadedMap is null) return;
        SyncCamera(camera);
        _renderer!.DrawLayers(spriteBatch, _extCamera!, layerNames);
    }

    /// <summary>Returns the named layer from the loaded map, or <c>null</c> if not found or map not loaded.</summary>
    public TilemapLayer? GetLayer(string name) => LoadedMap?.Layers[name];

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // TilemapSpriteBatchRenderer holds no GPU buffers and does not require disposal.
    }

    // Extended's OrthographicCamera applies CreateRotationZ(-Rotation) internally,
    // while Camera2D uses +Rotation. Negate here to keep visual parity.
    private void SyncCamera(Camera2D camera)
    {
        _extCamera!.Zoom = camera.Zoom;
        _extCamera.Rotation = -camera.Rotation;
        _extCamera.LookAt(camera.Position);
    }
}
