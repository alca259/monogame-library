namespace Alca.MonoGame.Kernel.DebugHelpers;

/// <summary>
/// HUD overlay that displays a rolling FPS counter and user-registered watch values.
/// Toggle visibility at runtime with <c>F2</c>.
/// </summary>
public sealed class DebugOverlay
{
    private const int MaxWatches = 32;
    private const float FpsSmoothing = 0.1f;

    private readonly (string Label, Func<string> ValueFunc)?[] _watches = new (string, Func<string>)?[MaxWatches];
    private int _watchCount;

    /// <summary>Gets or sets a value indicating whether this overlay is drawn.</summary>
    public bool IsVisible { get; set; }

    /// <summary>Gets the rolling average frames-per-second.</summary>
    public float FPS { get; private set; }

    /// <summary>Gets the number of watches currently registered.</summary>
    internal int WatchCount => _watchCount;

    /// <summary>
    /// Registers a live watch value. The <paramref name="valueFunc"/> is called each frame
    /// while the overlay is visible. Avoid allocating strings inside the delegate on the hot path.
    /// </summary>
    public void AddWatch(string label, Func<string> valueFunc)
    {
        if (_watchCount >= MaxWatches) return;
        _watches[_watchCount++] = (label, valueFunc);
    }

    /// <summary>Removes the first watch with the given label.</summary>
    public void RemoveWatch(string label)
    {
        for (int i = 0; i < _watchCount; i++)
        {
            if (_watches[i]?.Label == label)
            {
                _watches[i] = _watches[_watchCount - 1];
                _watches[_watchCount - 1] = null;
                _watchCount--;
                return;
            }
        }
    }

    /// <summary>Updates the FPS counter and checks for the F2 toggle key.</summary>
    public void Update(GameTime gameTime)
    {
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (elapsed > 0f)
            FPS = FPS + FpsSmoothing * (1f / elapsed - FPS);

        if (Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(Keys.F2))
            IsVisible = !IsVisible;
    }

    /// <summary>Renders the FPS counter and all watch entries using <paramref name="font"/>.</summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (!IsVisible) return;

        spriteBatch.Begin();

        var pos = new Vector2(8f, 8f);
        spriteBatch.DrawString(font, $"FPS: {FPS:F1}", pos, Color.Yellow);
        pos.Y += font.LineSpacing;

        for (int i = 0; i < _watchCount; i++)
        {
            if (_watches[i] is not (string label, Func<string> func)) continue;
            spriteBatch.DrawString(font, $"{label}: {func()}", pos, Color.White);
            pos.Y += font.LineSpacing;
        }

        spriteBatch.End();
    }
}
