namespace Alca.MonoGame.Kernel.Lighting.GPU;

/// <summary>
/// GPU-accelerated lighting renderer. Captures the scene into a <see cref="RenderTarget2D"/>,
/// then composites it with lighting data via a custom <see cref="Effect"/> in a single pass.
/// The CPU path (<see cref="LightingWorld.Resolve"/>) continues to work alongside this pipeline.
/// </summary>
/// <remarks>
/// Usage per frame:
/// <code>
/// pipeline.BeginSceneCapture();
/// // ... game sprite draws ...
/// pipeline.EndSceneCapture();
/// pipeline.ApplyLighting(LightingLayer.World, spriteBatch);
/// </code>
/// Call <see cref="LoadEffect"/> once after construction before the first frame.
/// Call <see cref="Resize"/> when the viewport changes.
/// </remarks>
public sealed class LightingRenderPipeline : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly LightingWorld _lightingWorld;
    private readonly int _maxLights;
    private readonly LightShaderData[] _lightDataBuffer;
    private readonly float[] _packedBuffer;

    private RenderTarget2D _sceneTarget;
    private RenderTarget2D _lightTarget;
    private Effect? _lightingEffect;
    private bool _disposed;

    /// <summary>
    /// Initializes the pipeline with pre-allocated GPU buffers. No heap allocations occur
    /// after construction.
    /// </summary>
    /// <param name="graphicsDevice">The active graphics device.</param>
    /// <param name="lightingWorld">The lighting world whose lights are uploaded each frame.</param>
    /// <param name="maxLights">Maximum number of lights sent to the shader. Default 64.</param>
    public LightingRenderPipeline(GraphicsDevice graphicsDevice, LightingWorld lightingWorld, int maxLights = 64)
    {
        _graphicsDevice = graphicsDevice;
        _lightingWorld = lightingWorld;
        _maxLights = maxLights;
        _lightDataBuffer = new LightShaderData[maxLights];
        _packedBuffer = new float[maxLights * LightShaderData.FloatCount];

        Viewport vp = graphicsDevice.Viewport;
        _sceneTarget = new RenderTarget2D(graphicsDevice, vp.Width, vp.Height);
        _lightTarget = new RenderTarget2D(graphicsDevice, vp.Width, vp.Height);
    }

    /// <summary>Loads the lighting <see cref="Effect"/> from the content pipeline.</summary>
    /// <param name="content">The active <see cref="ContentManager"/>.</param>
    /// <param name="assetPath">Content asset path (e.g., <c>"Effects/LightingEffect"</c>).</param>
    public void LoadEffect(ContentManager content, string assetPath)
        => _lightingEffect = content.Load<Effect>(assetPath);

    /// <summary>
    /// Sets the scene render target as active. The game must render all its sprites between
    /// this call and the matching <see cref="EndSceneCapture"/>.
    /// </summary>
    public void BeginSceneCapture()
        => _graphicsDevice.SetRenderTarget(_sceneTarget);

    /// <summary>Restores the back buffer as active render target.</summary>
    public void EndSceneCapture()
        => _graphicsDevice.SetRenderTarget(null);

    /// <summary>
    /// Uploads light data to the shader and draws the captured scene texture with lighting applied.
    /// Must be called after <see cref="EndSceneCapture"/>. No heap allocations.
    /// </summary>
    /// <param name="layer">Layer whose lights are uploaded to the shader.</param>
    /// <param name="spriteBatch">SpriteBatch used to submit the full-screen quad.</param>
    public void ApplyLighting(LightingLayer layer, SpriteBatch spriteBatch)
    {
        if (_lightingEffect is null) return;

        _lightingWorld.FillShaderBuffer(_lightDataBuffer, _maxLights, layer, out int count);

        for (int i = 0; i < count; i++)
            _lightDataBuffer[i].PackInto(_packedBuffer, i * LightShaderData.FloatCount);

        _lightingEffect.Parameters["_AmbientColor"]?.SetValue(_lightingWorld.AmbientColor.ToVector4());
        _lightingEffect.Parameters["_LightCount"]?.SetValue(count);
        if (count > 0)
            _lightingEffect.Parameters["_LightData"]?.SetValue(_packedBuffer);
        _lightingEffect.Parameters["_SceneTexture"]?.SetValue(_sceneTarget);

        spriteBatch.Begin(effect: _lightingEffect);
        spriteBatch.Draw(_sceneTarget, Vector2.Zero, Color.White);
        spriteBatch.End();
    }

    /// <summary>
    /// Recreates the internal render targets for the new resolution.
    /// Call from a <see cref="ResolutionManager"/> viewport-changed event.
    /// </summary>
    public void Resize(int width, int height)
    {
        _sceneTarget.Dispose();
        _lightTarget.Dispose();
        _sceneTarget = new RenderTarget2D(_graphicsDevice, width, height);
        _lightTarget = new RenderTarget2D(_graphicsDevice, width, height);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _sceneTarget.Dispose();
        _lightTarget.Dispose();
        _lightingEffect = null;
    }
}
