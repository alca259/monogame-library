using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Kernel.UI.Layout;

/// <summary>
/// Positions children according to an <see cref="Anchor"/> value relative to this container's bounds.
/// Each child's anchor and optional pixel offset are set via <see cref="SetAnchor"/>.
/// </summary>
public sealed class AnchorLayout : UIContainer
{
    private readonly record struct AnchorEntry(Anchor Anchor, Vector2 Offset);

    private readonly Dictionary<UIElement, AnchorEntry> _anchors = new(8);

    /// <summary>Registers an anchor and optional pixel offset for <paramref name="child"/>.</summary>
    /// <param name="child">Child element to anchor.</param>
    /// <param name="anchor">Which edge or corner of the container to attach to.</param>
    /// <param name="offset">Additional pixel offset applied after anchor resolution.</param>
    public void SetAnchor(UIElement child, Anchor anchor, Vector2 offset = default)
    {
        Add(child);
        _anchors[child] = new AnchorEntry(anchor, offset);
        Invalidate();
    }

    /// <inheritdoc />
    public override void Measure(Vector2 availableSize)
    {
        for (int i = 0; i < Children.Count; i++)
            Children[i].Measure(availableSize);

        DesiredSize = availableSize;
    }

    /// <inheritdoc />
    public override void Arrange(Rectangle finalBounds)
    {
        Bounds = finalBounds;

        for (int i = 0; i < Children.Count; i++)
        {
            UIElement child = Children[i];
            _anchors.TryGetValue(child, out AnchorEntry entry);
            Rectangle childRect = ComputeAnchoredRect(finalBounds, child.DesiredSize, entry.Anchor, entry.Offset);
            child.Arrange(childRect);
        }
    }

    /// <inheritdoc />
    protected override void OnChildRemoved(UIElement child)
    {
        _anchors.Remove(child);
    }

    private static Rectangle ComputeAnchoredRect(Rectangle parent, Vector2 childSize, Anchor anchor, Vector2 offset)
    {
        int w = (int)childSize.X;
        int h = (int)childSize.Y;
        int ox = (int)offset.X;
        int oy = (int)offset.Y;

        int x, y, rw, rh;

        switch (anchor)
        {
            case Anchor.Fill:
                return new Rectangle(parent.X + ox, parent.Y + oy,
                    parent.Width - ox * 2, parent.Height - oy * 2);

            case Anchor.TopLeft:
                x = parent.X + ox; y = parent.Y + oy; rw = w; rh = h;
                break;
            case Anchor.TopCenter:
                x = parent.X + (parent.Width - w) / 2 + ox; y = parent.Y + oy; rw = w; rh = h;
                break;
            case Anchor.TopRight:
                x = parent.Right - w - ox; y = parent.Y + oy; rw = w; rh = h;
                break;
            case Anchor.MiddleLeft:
                x = parent.X + ox; y = parent.Y + (parent.Height - h) / 2 + oy; rw = w; rh = h;
                break;
            case Anchor.Center:
                x = parent.X + (parent.Width - w) / 2 + ox; y = parent.Y + (parent.Height - h) / 2 + oy; rw = w; rh = h;
                break;
            case Anchor.MiddleRight:
                x = parent.Right - w - ox; y = parent.Y + (parent.Height - h) / 2 + oy; rw = w; rh = h;
                break;
            case Anchor.BottomLeft:
                x = parent.X + ox; y = parent.Bottom - h - oy; rw = w; rh = h;
                break;
            case Anchor.BottomCenter:
                x = parent.X + (parent.Width - w) / 2 + ox; y = parent.Bottom - h - oy; rw = w; rh = h;
                break;
            case Anchor.BottomRight:
                x = parent.Right - w - ox; y = parent.Bottom - h - oy; rw = w; rh = h;
                break;
            default:
                x = parent.X + ox; y = parent.Y + oy; rw = w; rh = h;
                break;
        }

        return new Rectangle(x, y, rw, rh);
    }
}
