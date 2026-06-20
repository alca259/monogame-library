using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Kernel.UI.Layout;

/// <summary>Arranges children in a single horizontal or vertical line with optional spacing between them.</summary>
public sealed class StackPanel : UIContainer
{
    /// <summary>Direction in which children are laid out.</summary>
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>Gap in pixels between consecutive children.</summary>
    public float Spacing { get; set; } = 0f;

    /// <inheritdoc />
    public override void Measure(Vector2 availableSize)
    {
        float totalMain = 0f;
        float maxCross = 0f;
        int count = Children.Count;

        for (int i = 0; i < count; i++)
        {
            Children[i].Measure(availableSize);
            Vector2 ds = Children[i].DesiredSize;

            if (Orientation == Orientation.Vertical)
            {
                totalMain += ds.Y;
                if (ds.X > maxCross) maxCross = ds.X;
            }
            else
            {
                totalMain += ds.X;
                if (ds.Y > maxCross) maxCross = ds.Y;
            }

            if (i < count - 1)
                totalMain += Spacing;
        }

        DesiredSize = Orientation == Orientation.Vertical
            ? new Vector2(maxCross, totalMain)
            : new Vector2(totalMain, maxCross);
    }

    /// <inheritdoc />
    public override void Arrange(Rectangle finalBounds)
    {
        Bounds = finalBounds;
        float cursor = Orientation == Orientation.Vertical ? finalBounds.Y : finalBounds.X;

        for (int i = 0; i < Children.Count; i++)
        {
            Vector2 ds = Children[i].DesiredSize;
            Rectangle childRect;

            if (Orientation == Orientation.Vertical)
            {
                childRect = new Rectangle(finalBounds.X, (int)cursor, finalBounds.Width, (int)ds.Y);
                cursor += ds.Y + Spacing;
            }
            else
            {
                childRect = new Rectangle((int)cursor, finalBounds.Y, (int)ds.X, finalBounds.Height);
                cursor += ds.X + Spacing;
            }

            Children[i].Arrange(childRect);
        }
    }
}
