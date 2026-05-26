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
    /// Source border inset used to divide <see cref="NineSliceTexture"/> into a 3×3 grid.
    /// Defines how many pixels from each edge are treated as corners/edges.
    /// </summary>
    public int NineSliceBorder { get; set; } = 8;

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

    private static void DrawNineSlice(SpriteBatch sb, Texture2D texture, Rectangle dest, int border, Color color)
    {
        int tw = texture.Width;
        int th = texture.Height;

        // Source grid corners
        int srcB = border;

        // Destination grid corners
        int db = border;

        // Clamp border to half of dest size so it doesn't flip
        if (db > dest.Width / 2) db = dest.Width / 2;
        if (db > dest.Height / 2) db = dest.Height / 2;

        int centerSrcW = tw - srcB * 2;
        int centerSrcH = th - srcB * 2;
        int centerDstW = dest.Width - db * 2;
        int centerDstH = dest.Height - db * 2;

        // Top-left
        sb.Draw(texture, new Rectangle(dest.X, dest.Y, db, db),
            new Rectangle(0, 0, srcB, srcB), color);
        // Top-center
        sb.Draw(texture, new Rectangle(dest.X + db, dest.Y, centerDstW, db),
            new Rectangle(srcB, 0, centerSrcW, srcB), color);
        // Top-right
        sb.Draw(texture, new Rectangle(dest.Right - db, dest.Y, db, db),
            new Rectangle(tw - srcB, 0, srcB, srcB), color);

        // Middle-left
        sb.Draw(texture, new Rectangle(dest.X, dest.Y + db, db, centerDstH),
            new Rectangle(0, srcB, srcB, centerSrcH), color);
        // Center
        sb.Draw(texture, new Rectangle(dest.X + db, dest.Y + db, centerDstW, centerDstH),
            new Rectangle(srcB, srcB, centerSrcW, centerSrcH), color);
        // Middle-right
        sb.Draw(texture, new Rectangle(dest.Right - db, dest.Y + db, db, centerDstH),
            new Rectangle(tw - srcB, srcB, srcB, centerSrcH), color);

        // Bottom-left
        sb.Draw(texture, new Rectangle(dest.X, dest.Bottom - db, db, db),
            new Rectangle(0, th - srcB, srcB, srcB), color);
        // Bottom-center
        sb.Draw(texture, new Rectangle(dest.X + db, dest.Bottom - db, centerDstW, db),
            new Rectangle(srcB, th - srcB, centerSrcW, srcB), color);
        // Bottom-right
        sb.Draw(texture, new Rectangle(dest.Right - db, dest.Bottom - db, db, db),
            new Rectangle(tw - srcB, th - srcB, srcB, srcB), color);
    }
}
