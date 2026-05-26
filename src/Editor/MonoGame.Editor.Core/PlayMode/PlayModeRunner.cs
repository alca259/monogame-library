using Alca.MonoGame.Kernel.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Editor.Core.Models;
using MonoGame.Editor.Core.Registry;

namespace MonoGame.Editor.Core.PlayMode;

/// <summary>Manages the runtime <see cref="GameWorld"/> for Play/Pause mode in the editor viewport.</summary>
public sealed class PlayModeRunner : IDisposable
{
    private readonly GameWorld _world;
    private SpriteBatch? _spriteBatch;
    private TimeSpan _totalTime;
    private bool _disposed;

    /// <summary>Whether the SpriteBatch has been initialized on the render thread.</summary>
    public bool IsInitialized => _spriteBatch is not null;

    /// <summary>Builds the runtime world from the editor scene. Call from the UI thread.</summary>
    public PlayModeRunner(EditorScene scene, GameObjectRegistry registry)
        => _world = SceneToWorldConverter.Convert(scene, registry);

    /// <summary>
    /// Creates the SpriteBatch using the given GraphicsDevice.
    /// Must be called from the render thread before the first <see cref="Update"/> or <see cref="Draw"/>.
    /// </summary>
    public void EnsureInitialized(GraphicsDevice gd)
    {
        if (_spriteBatch is not null) return;
        _spriteBatch = new SpriteBatch(gd);
    }

    /// <summary>Advances game logic by one frame. Skip when Paused.</summary>
    public void Update(TimeSpan elapsed)
    {
        if (_disposed) return;
        _totalTime += elapsed;
        _world.Update(new GameTime(_totalTime, elapsed));
    }

    /// <summary>Renders the world. Called every frame regardless of pause state.</summary>
    public void Draw(TimeSpan elapsed)
    {
        if (_disposed || _spriteBatch is null) return;
        var gt = new GameTime(_totalTime, elapsed);
        try
        {
            _spriteBatch.Begin();
            try
            {
                _world.Draw(gt, _spriteBatch);
            }
            catch
            {
                // Behaviours that rely on game content (textures, fonts) will fail here;
                // silently ignored — play mode still runs game logic correctly.
            }
            _spriteBatch.End();
        }
        catch
        {
            // Device lost or SpriteBatch in bad state — recover on next frame.
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _spriteBatch?.Dispose();
        _spriteBatch = null;
    }
}
