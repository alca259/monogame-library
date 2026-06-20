using Alca.MonoGame.Kernel.UI.Controls.Utilities;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Kernel.UI.Controls.Display;

/// <summary>Displays a Texture2D inside its Bounds using the specified draw mode.</summary>
public sealed class UISprite : UIElement
{
    private Texture2D? _texture;

    /// <summary>The texture to display. Setting to null causes the element to render nothing.</summary>
    public Texture2D? Texture
    {
        get => _texture;
        set
        {
            if (ReferenceEquals(_texture, value)) return;
            _texture = value;
            Invalidate();
        }
    }

    /// <summary>Source region within the texture. Null means the full texture.</summary>
    public Rectangle? SourceRect { get; set; }

    /// <summary>Tint color multiplied on top of the texture.</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>Determines how the texture is scaled or tiled to fill Bounds.</summary>
    public SpriteDrawMode DrawMode { get; set; } = SpriteDrawMode.Stretch;

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        if (_texture is null)
        {
            DesiredSize = Vector2.Zero;
            return;
        }

        Rectangle src = SourceRect ?? _texture.Bounds;
        DesiredSize = new Vector2(src.Width, src.Height);
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible || _texture is null) return;

        Rectangle src = SourceRect ?? _texture.Bounds;
        Color tint = Color * EffectiveOpacity;

        switch (DrawMode)
        {
            case SpriteDrawMode.Stretch:
                spriteBatch.Draw(_texture, Bounds, src, tint);
                break;

            case SpriteDrawMode.Fit:
                DrawFit(spriteBatch, src, tint);
                break;

            case SpriteDrawMode.Crop:
                spriteBatch.Draw(_texture, new Vector2(Bounds.X, Bounds.Y), src, tint);
                break;

            case SpriteDrawMode.Tile:
                // Caller must have started the SpriteBatch with SamplerState.LinearWrap.
                spriteBatch.Draw(_texture, Bounds, src, tint);
                break;
        }
    }

    private void DrawFit(SpriteBatch spriteBatch, Rectangle src, Color tint)
    {
        float scaleX = Bounds.Width / (float)src.Width;
        float scaleY = Bounds.Height / (float)src.Height;
        float scale = MathF.Min(scaleX, scaleY);

        int w = (int)(src.Width * scale);
        int h = (int)(src.Height * scale);

        var dest = new Rectangle(
            Bounds.X + (Bounds.Width - w) / 2,
            Bounds.Y + (Bounds.Height - h) / 2,
            w, h);

        spriteBatch.Draw(_texture!, dest, src, tint);
    }
}
