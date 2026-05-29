using Alca.MonoGame.Kernel.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Editor.Core.Models;
using MonoGame.Editor.Core.Registry;

namespace MonoGame.Editor.Core.PlayMode;

/// <summary>Gestiona el <see cref="GameWorld"/> en tiempo de ejecución para el modo Play/Pausa en el viewport del editor.</summary>
public sealed class PlayModeRunner : IDisposable
{
    private readonly GameWorld _world;
    private SpriteBatch? _spriteBatch;
    private TimeSpan _totalTime;
    private bool _disposed;

    /// <summary>Indica si el SpriteBatch ha sido inicializado en el hilo de renderizado.</summary>
    public bool IsInitialized => _spriteBatch is not null;

    /// <summary>Construye el mundo en tiempo de ejecución a partir de la escena del editor. Llamar desde el hilo de UI.</summary>
    public PlayModeRunner(EditorScene scene, GameObjectRegistry registry)
        => _world = SceneToWorldConverter.Convert(scene, registry);

    /// <summary>
    /// Crea el SpriteBatch usando el GraphicsDevice proporcionado.
    /// Debe llamarse desde el hilo de renderizado antes del primer <see cref="Update"/> o <see cref="Draw"/>.
    /// </summary>
    public void EnsureInitialized(GraphicsDevice gd)
    {
        if (_spriteBatch is not null) return;
        _spriteBatch = new SpriteBatch(gd);
    }

    /// <summary>Avanza la lógica del juego un fotograma. Omitir cuando está en pausa.</summary>
    public void Update(TimeSpan elapsed)
    {
        if (_disposed) return;
        _totalTime += elapsed;
        _world.Update(new GameTime(_totalTime, elapsed));
    }

    /// <summary>Renderiza el mundo. Se llama cada fotograma independientemente del estado de pausa.</summary>
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
                // Los GameBehaviour que dependen de contenido del juego (texturas, fuentes) fallarán aquí;
                // se ignoran silenciosamente — el modo play sigue ejecutando la lógica del juego correctamente.
            }
            _spriteBatch.End();
        }
        catch
        {
            // Dispositivo perdido o SpriteBatch en mal estado — recuperar en el siguiente fotograma.
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
