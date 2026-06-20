using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Kernel.Graphics;

public static class DrawHelper
{
    public static void DrawLine(Texture2D? texture, SpriteBatch sb, Vector2 from, Vector2 to, Color color, float thickness = 2f)
    {
        Texture2D tex = texture ?? DefaultPixelTexture;
        var delta = to - from;
        var angle = MathF.Atan2(delta.Y, delta.X);
        var length = delta.Length();

        sb.Draw(
            tex,
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

    public static void DrawRect(Texture2D? texture, SpriteBatch sb, Rectangle rect, Color color)
        => sb.Draw(texture ?? DefaultPixelTexture, rect, color);

    public static void DrawBorder(Texture2D? texture, SpriteBatch sb, Rectangle rect, Color color, int thickness = 2)
    {
        Texture2D tex = texture ?? DefaultPixelTexture;
        sb.Draw(tex, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        sb.Draw(tex, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        sb.Draw(tex, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        sb.Draw(tex, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
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

    /// <summary>Draws a 9-slice (nine-patch) texture scaled into <paramref name="dest"/>.</summary>
    public static void DrawNineSlice(SpriteBatch sb, Texture2D texture, Rectangle dest, NineSliceBorderData border, Color color)
    {
        if (dest.Width <= 0 || dest.Height <= 0) return;

        int tw = texture.Width;
        int th = texture.Height;

        int dL = Math.Min(border.Left,   dest.Width  / 2);
        int dR = Math.Min(border.Right,  dest.Width  / 2);
        int dT = Math.Min(border.Top,    dest.Height / 2);
        int dB = Math.Min(border.Bottom, dest.Height / 2);

        int sL = border.Left;
        int sR = border.Right;
        int sT = border.Top;
        int sB = border.Bottom;

        int centerSrcW = tw - sL - sR;
        int centerSrcH = th - sT - sB;
        int centerDstW = dest.Width  - dL - dR;
        int centerDstH = dest.Height - dT - dB;

        Rectangle srcTL = new(0,          0,          sL,         sT);
        Rectangle srcTC = new(sL,         0,          centerSrcW, sT);
        Rectangle srcTR = new(tw - sR,    0,          sR,         sT);
        Rectangle srcML = new(0,          sT,         sL,         centerSrcH);
        Rectangle srcMC = new(sL,         sT,         centerSrcW, centerSrcH);
        Rectangle srcMR = new(tw - sR,    sT,         sR,         centerSrcH);
        Rectangle srcBL = new(0,          th - sB,    sL,         sB);
        Rectangle srcBC = new(sL,         th - sB,    centerSrcW, sB);
        Rectangle srcBR = new(tw - sR,    th - sB,    sR,         sB);

        Rectangle dstTL = new(dest.X,          dest.Y,           dL,         dT);
        Rectangle dstTC = new(dest.X + dL,     dest.Y,           centerDstW, dT);
        Rectangle dstTR = new(dest.Right - dR, dest.Y,           dR,         dT);
        Rectangle dstML = new(dest.X,          dest.Y + dT,      dL,         centerDstH);
        Rectangle dstMC = new(dest.X + dL,     dest.Y + dT,      centerDstW, centerDstH);
        Rectangle dstMR = new(dest.Right - dR, dest.Y + dT,      dR,         centerDstH);
        Rectangle dstBL = new(dest.X,          dest.Bottom - dB, dL,         dB);
        Rectangle dstBC = new(dest.X + dL,     dest.Bottom - dB, centerDstW, dB);
        Rectangle dstBR = new(dest.Right - dR, dest.Bottom - dB, dR,         dB);

        sb.Draw(texture, dstTL, srcTL, color);
        sb.Draw(texture, dstTR, srcTR, color);
        sb.Draw(texture, dstBL, srcBL, color);
        sb.Draw(texture, dstBR, srcBR, color);

        if (border.TileEdges)
        {
            DrawTiled(sb, texture, dstTC, srcTC, color);
            DrawTiled(sb, texture, dstML, srcML, color);
            DrawTiled(sb, texture, dstMR, srcMR, color);
            DrawTiled(sb, texture, dstBC, srcBC, color);
        }
        else
        {
            sb.Draw(texture, dstTC, srcTC, color);
            sb.Draw(texture, dstML, srcML, color);
            sb.Draw(texture, dstMR, srcMR, color);
            sb.Draw(texture, dstBC, srcBC, color);
        }

        if (border.TileCenter)
            DrawTiled(sb, texture, dstMC, srcMC, color);
        else
            sb.Draw(texture, dstMC, srcMC, color);
    }

    private static void DrawTiled(SpriteBatch sb, Texture2D texture, Rectangle dest, Rectangle src, Color color)
    {
        if (src.Width <= 0 || src.Height <= 0 || dest.Width <= 0 || dest.Height <= 0) return;

        for (int ty = dest.Y; ty < dest.Bottom; ty += src.Height)
        for (int tx = dest.X; tx < dest.Right;  tx += src.Width)
        {
            int w = Math.Min(src.Width,  dest.Right  - tx);
            int h = Math.Min(src.Height, dest.Bottom - ty);
            sb.Draw(texture, new Rectangle(tx, ty, w, h), new Rectangle(src.X, src.Y, w, h), color);
        }
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
