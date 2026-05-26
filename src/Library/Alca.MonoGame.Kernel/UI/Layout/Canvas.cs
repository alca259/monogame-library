namespace Alca.MonoGame.Kernel.UI.Layout;

/// <summary>
/// Positions children at explicit pixel offsets relative to the canvas top-left corner.
/// Each child's offset is set via <see cref="SetOffset"/>; children without an offset default to (0, 0).
/// </summary>
public sealed class Canvas : UIContainer
{
    private readonly Dictionary<UIElement, Vector2> _offsets = new(8);

    /// <summary>Sets the position of <paramref name="child"/> relative to this canvas's top-left corner.</summary>
    /// <param name="child">The child element to position.</param>
    /// <param name="offset">Pixel offset from the canvas origin.</param>
    public void SetOffset(UIElement child, Vector2 offset)
    {
        _offsets[child] = offset;
        Invalidate();
    }

    /// <summary>Returns the offset registered for <paramref name="child"/>, or <see cref="Vector2.Zero"/> if none.</summary>
    public Vector2 GetOffset(UIElement child) =>
        _offsets.TryGetValue(child, out Vector2 off) ? off : Vector2.Zero;

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
            Vector2 ds = child.DesiredSize;
            _offsets.TryGetValue(child, out Vector2 offset);

            int x = finalBounds.X + (int)offset.X;
            int y = finalBounds.Y + (int)offset.Y;

            child.Arrange(new Rectangle(x, y, (int)ds.X, (int)ds.Y));
        }
    }

    /// <inheritdoc />
    protected override void OnChildRemoved(UIElement child)
    {
        _offsets.Remove(child);
    }
}
