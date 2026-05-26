namespace Alca.MonoGame.Kernel.Graphics.Effects;

/// <summary>Manages two ping-pong render targets for post-processing effect chains.</summary>
public sealed class RenderTargetManager : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private RenderTarget2D _targetA;
    private RenderTarget2D _targetB;
    private bool _disposed;

    /// <summary>Initializes the manager and pre-allocates both render targets at the given resolution.</summary>
    public RenderTargetManager(GraphicsDevice graphicsDevice, int width, int height)
    {
        _graphicsDevice = graphicsDevice;
        _targetA = new RenderTarget2D(graphicsDevice, width, height);
        _targetB = new RenderTarget2D(graphicsDevice, width, height);
    }

    /// <summary>Sets <c>_targetA</c> as the active render target. Call before drawing the scene.</summary>
    public void BeginCapture()
    {
        _graphicsDevice.SetRenderTarget(_targetA);
    }

    /// <summary>Restores the back buffer as the active render target.</summary>
    public void EndCapture()
    {
        _graphicsDevice.SetRenderTarget(null);
    }

    /// <summary>Applies <paramref name="effect"/> to the captured scene and draws the result to the back buffer.</summary>
    public void Apply(Effect effect, SpriteBatch spriteBatch)
    {
        _graphicsDevice.SetRenderTarget(_targetB);
        _graphicsDevice.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, effect: effect);
        spriteBatch.Draw(_targetA, Vector2.Zero, Color.White);
        spriteBatch.End();

        _graphicsDevice.SetRenderTarget(null);

        spriteBatch.Begin();
        spriteBatch.Draw(_targetB, Vector2.Zero, Color.White);
        spriteBatch.End();
    }

    /// <summary>Applies a chain of effects sequentially using ping-pong between the two targets.</summary>
    public void ApplyChain(Effect[] effects, SpriteBatch spriteBatch)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            Effect effect = effects[i];
            bool isLast = i == effects.Length - 1;

            RenderTarget2D source = (i % 2 == 0) ? _targetA : _targetB;
            RenderTarget2D dest   = (i % 2 == 0) ? _targetB : _targetA;

            _graphicsDevice.SetRenderTarget(isLast ? null : dest);
            _graphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, effect: effect);
            spriteBatch.Draw(source, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;
        _targetA.Dispose();
        _targetB.Dispose();
        _disposed = true;
    }
}
