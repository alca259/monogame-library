using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;

namespace Alca.MonoGame.Kernel.Graphics.Fonts;

/// <summary>Wraps <see cref="BitmapFont"/> from MonoGame.Extended to provide consistent text rendering and measurement helpers.</summary>
public sealed class BitmapFontRenderer
{
    private BitmapFont? _font;

    /// <summary>Gets the loaded font, or <c>null</c> if <see cref="Load"/> has not been called.</summary>
    public BitmapFont? Font => _font;

    /// <summary>Loads a <see cref="BitmapFont"/> asset via the content pipeline.</summary>
    /// <param name="content">The content manager used to load the asset.</param>
    /// <param name="assetName">Asset path relative to the content root, without extension.</param>
    public void Load(ContentManager content, string assetName)
    {
        _font = content.Load<BitmapFont>(assetName);
    }

    /// <summary>Draws <paramref name="text"/> at <paramref name="pos"/>. No-op if the font is not loaded.</summary>
    public void DrawString(SpriteBatch sb, string text, Vector2 pos, Color color)
    {
        if (_font is null) return;
        sb.DrawString(_font, text, pos, color);
    }

    /// <summary>Draws <paramref name="text"/> with uniform <paramref name="scale"/> and <paramref name="rotation"/> (radians) applied around the text origin. No-op if the font is not loaded.</summary>
    public void DrawString(SpriteBatch sb, string text, Vector2 pos, Color color, float scale, float rotation)
    {
        if (_font is null) return;
        sb.DrawString(_font, text, pos, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f, null);
    }

    /// <summary>Returns the rendered size of <paramref name="text"/> in pixels, or <see cref="Vector2.Zero"/> if the font is not loaded.</summary>
    public Vector2 MeasureString(string text)
    {
        if (_font is null) return Vector2.Zero;
        SizeF size = _font.MeasureString(text);
        return new Vector2(size.Width, size.Height);
    }

    /// <summary>Draws <paramref name="text"/> centred within <paramref name="bounds"/>. No-op if the font is not loaded.</summary>
    public void DrawCentered(SpriteBatch sb, string text, Rectangle bounds, Color color)
    {
        if (_font is null) return;
        SizeF size = _font.MeasureString(text);
        Vector2 pos = new(
            bounds.X + (bounds.Width  - size.Width)  * 0.5f,
            bounds.Y + (bounds.Height - size.Height) * 0.5f
        );
        sb.DrawString(_font, text, pos, color);
    }
}
