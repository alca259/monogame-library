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
            DrawHelper.DrawNineSlice(spriteBatch, NineSliceTexture, Bounds, NineSliceBorder, Color.White * opacity);
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

}
