using Alca.MonoGame.Kernel.Graphics;

namespace Alca.MonoGame.Kernel.UI.Controls;

/// <summary>A container that renders a background fill and optional border before its children.</summary>
public sealed class Panel : UIContainer
{
    /// <summary>Solid fill color. Applied when <see cref="BackgroundTexture"/> is null.</summary>
    public Color BackgroundColor { get; set; } = Color.Transparent;

    /// <summary>Optional background texture; drawn scaled to <see cref="UIElement.Bounds"/>.</summary>
    public Texture2D? BackgroundTexture { get; set; }

    /// <summary>Border color. Ignored when <see cref="BorderThickness"/> is 0.</summary>
    public Color BorderColor { get; set; } = Color.White;

    /// <summary>Border width in pixels. 0 = no border.</summary>
    public int BorderThickness { get; set; }

    /// <summary>
    /// Optional nine-slice texture.
    /// When set, the texture is drawn as a 9-patch instead of the solid background.
    /// </summary>
    public Texture2D? NineSliceTexture { get; set; }

    /// <summary>
    /// Border insets used to divide <see cref="NineSliceTexture"/> into a 3×3 grid.
    /// Each edge can have an independent thickness. Set <see cref="NineSliceBorderData.TileEdges"/>
    /// or <see cref="NineSliceBorderData.TileCenter"/> to repeat instead of stretch those regions.
    /// </summary>
    public NineSliceBorderData NineSliceBorder { get; set; } = NineSliceBorderData.Uniform(8);

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;

        float opacity = EffectiveOpacity;

        if (NineSliceTexture is not null)
            DrawNineSlice(spriteBatch, NineSliceTexture, Bounds, NineSliceBorder, Color.White * opacity);
        else if (BackgroundTexture is not null)
            spriteBatch.Draw(BackgroundTexture, Bounds, BackgroundColor * opacity);
        else if (BackgroundColor != Color.Transparent)
            DrawHelper.DrawRect(BackgroundTexture!, spriteBatch, Bounds, BackgroundColor * opacity);

        if (BorderThickness > 0 && BackgroundTexture is not null)
            DrawHelper.DrawBorder(BackgroundTexture, spriteBatch, Bounds, BorderColor * opacity, BorderThickness);

        for (int i = 0; i < ChildrenReadOnly.Count; i++)
        {
            if (ChildrenReadOnly[i].IsVisible)
                ChildrenReadOnly[i].Draw(spriteBatch);
        }
    }

    private static void DrawNineSlice(SpriteBatch sb, Texture2D texture, Rectangle dest, NineSliceBorderData border, Color color)
    {
        int tw = texture.Width;
        int th = texture.Height;

        // Clamp destination borders to half the dest size
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

        // Source rectangles
        Rectangle srcTL = new(0,          0,          sL, sT);
        Rectangle srcTC = new(sL,         0,          centerSrcW, sT);
        Rectangle srcTR = new(tw - sR,    0,          sR, sT);
        Rectangle srcML = new(0,          sT,         sL, centerSrcH);
        Rectangle srcMC = new(sL,         sT,         centerSrcW, centerSrcH);
        Rectangle srcMR = new(tw - sR,    sT,         sR, centerSrcH);
        Rectangle srcBL = new(0,          th - sB,    sL, sB);
        Rectangle srcBC = new(sL,         th - sB,    centerSrcW, sB);
        Rectangle srcBR = new(tw - sR,    th - sB,    sR, sB);

        // Destination rectangles
        Rectangle dstTL = new(dest.X,          dest.Y,           dL, dT);
        Rectangle dstTC = new(dest.X + dL,     dest.Y,           centerDstW, dT);
        Rectangle dstTR = new(dest.Right - dR, dest.Y,           dR, dT);
        Rectangle dstML = new(dest.X,          dest.Y + dT,      dL, centerDstH);
        Rectangle dstMC = new(dest.X + dL,     dest.Y + dT,      centerDstW, centerDstH);
        Rectangle dstMR = new(dest.Right - dR, dest.Y + dT,      dR, centerDstH);
        Rectangle dstBL = new(dest.X,          dest.Bottom - dB, dL, dB);
        Rectangle dstBC = new(dest.X + dL,     dest.Bottom - dB, centerDstW, dB);
        Rectangle dstBR = new(dest.Right - dR, dest.Bottom - dB, dR, dB);

        // Corners always stretch
        sb.Draw(texture, dstTL, srcTL, color);
        sb.Draw(texture, dstTR, srcTR, color);
        sb.Draw(texture, dstBL, srcBL, color);
        sb.Draw(texture, dstBR, srcBR, color);

        // Edges
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

        // Center
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
            sb.Draw(texture, new Rectangle(tx, ty, w, h),
                new Rectangle(src.X, src.Y, w, h), color);
        }
    }
}
