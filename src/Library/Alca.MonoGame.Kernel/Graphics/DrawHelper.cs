namespace Alca.MonoGame.Kernel.Graphics;

public static class DrawHelper
{
    public static void DrawLine(Texture2D texture, SpriteBatch sb, Vector2 from, Vector2 to, Color color, float thickness = 2f)
    {
        var delta = to - from;
        var angle = MathF.Atan2(delta.Y, delta.X);
        var length = delta.Length();

        sb.Draw(
            texture,
            from,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f
        );
    }

    public static void DrawRect(Texture2D texture, SpriteBatch sb, Rectangle rect, Color color)
        => sb.Draw(texture, rect, color);

    public static void DrawBorder(Texture2D texture, SpriteBatch sb, Rectangle rect, Color color, int thickness = 2)
    {
        sb.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        sb.Draw(texture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        sb.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        sb.Draw(texture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public static void DrawCenteredString(SpriteBatch sb, SpriteFont font, string text,
        Rectangle area, Color color, float scale = 1f)
    {
        var size = font.MeasureString(text) * scale;
        var pos = new Vector2(
            area.X + (area.Width - size.X) / 2f,
            area.Y + (area.Height - size.Y) / 2f
        );
        sb.DrawString(font, text, pos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        sb.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private static Texture2D? _defaultPixelTexture;
    /// <summary>Gets a default 1x1 white pixel texture for drawing primitives.</summary>
    public static Texture2D DefaultPixelTexture
    {
        get
        {
            if (_defaultPixelTexture == null)
            {
                _defaultPixelTexture = new Texture2D(Core.GraphicsDevice, 1, 1);
                _defaultPixelTexture.SetData([Color.White]);
            }
            return _defaultPixelTexture;
        }
    }
}
