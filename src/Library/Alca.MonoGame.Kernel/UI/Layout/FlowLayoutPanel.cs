namespace Alca.MonoGame.Kernel.UI.Layout;

/// <summary>
/// Arranges children left-to-right and wraps to a new row when the available width is exceeded.
/// Equivalent to CSS <c>flex-wrap: wrap</c> with horizontal primary axis.
/// </summary>
public sealed class FlowLayoutPanel : UIContainer
{
    /// <summary>Horizontal pixel gap between items on the same row.</summary>
    public float Spacing { get; set; } = 0f;

    /// <inheritdoc />
    public override void Measure(Vector2 availableSize)
    {
        float cursorX = 0f;
        float rowHeight = 0f;
        float totalH = 0f;
        float maxW = 0f;
        float w = availableSize.X;

        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].Measure(availableSize);
            Vector2 ds = Children[i].DesiredSize;

            if (cursorX > 0f && cursorX + ds.X > w)
            {
                float rowW = cursorX - Spacing;
                if (rowW > maxW) maxW = rowW;
                totalH += rowHeight + Spacing;
                cursorX = 0f;
                rowHeight = 0f;
            }

            cursorX += ds.X + Spacing;
            if (ds.Y > rowHeight) rowHeight = ds.Y;
        }

        float lastRowW = cursorX > 0f ? cursorX - Spacing : 0f;
        if (lastRowW > maxW) maxW = lastRowW;
        totalH += rowHeight;

        DesiredSize = new Vector2(maxW > 0f ? maxW : availableSize.X, totalH);
    }

    /// <inheritdoc />
    public override void Arrange(Rectangle finalBounds)
    {
        Bounds = finalBounds;
        float cursorX = finalBounds.X;
        float cursorY = finalBounds.Y;
        float rowHeight = 0f;
        int rowStart = 0;

        for (int i = 0; i < Children.Count; i++)
        {
            Vector2 ds = Children[i].DesiredSize;

            if (cursorX > finalBounds.X && cursorX + ds.X > finalBounds.Right)
            {
                AlignRow(rowStart, i, cursorY, rowHeight, finalBounds.X);
                cursorY += rowHeight + Spacing;
                cursorX = finalBounds.X;
                rowHeight = 0f;
                rowStart = i;
            }

            // Temporary placement; AlignRow will reposition with vertical centering.
            Children[i].Arrange(new Rectangle((int)cursorX, (int)cursorY, (int)ds.X, (int)ds.Y));
            cursorX += ds.X + Spacing;
            if (ds.Y > rowHeight) rowHeight = ds.Y;
        }

        AlignRow(rowStart, Children.Count, cursorY, rowHeight, finalBounds.X);
    }

    // Vertically centers items within a completed row and commits their final Bounds.
    private void AlignRow(int from, int to, float rowY, float rowH, float startX)
    {
        float cx = startX;
        for (int i = from; i < to; i++)
        {
            Vector2 ds = Children[i].DesiredSize;
            float iy = rowY + (rowH - ds.Y) * 0.5f;
            Children[i].Arrange(new Rectangle((int)cx, (int)iy, (int)ds.X, (int)ds.Y));
            cx += ds.X + Spacing;
        }
    }
}
