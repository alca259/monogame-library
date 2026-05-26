namespace Alca.MonoGame.Kernel.UI;

/// <summary>A UIElement that owns and propagates Update/Draw to a child collection.</summary>
public class UIContainer : UIElement
{
    /// <summary>Direct children of this container.</summary>
    protected readonly List<UIElement> Children = [];

    /// <summary>Read-only view of direct children; used by UIInteractionManager for DFS traversal.</summary>
    public IReadOnlyList<UIElement> ChildrenReadOnly => Children;

    /// <summary>Adds a child, sets its Parent, and invalidates layout.</summary>
    public void Add(UIElement child)
    {
        if (child.Parent is UIContainer previous)
            previous.Remove(child);

        child.Parent = this;

        if (Children.Exists(c => c.Id == child.Id))
            return;

        Children.Add(child);
        Invalidate();
        OnChildAdded(child);
    }

    /// <summary>Removes a child, clears its Parent, and invalidates layout.</summary>
    public void Remove(UIElement child)
    {
        if (Children.Remove(child))
        {
            child.Parent = null;
            Invalidate();
            OnChildRemoved(child);
        }
    }

    /// <summary>Measures children and sets DesiredSize to their bounding box.</summary>
    public override void Measure(Vector2 availableSize)
    {
        float maxW = 0f;
        float maxH = 0f;
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].Measure(availableSize);
            if (Children[i].DesiredSize.X > maxW) maxW = Children[i].DesiredSize.X;
            if (Children[i].DesiredSize.Y > maxH) maxH = Children[i].DesiredSize.Y;
        }
        DesiredSize = new Vector2(maxW, maxH);
    }

    /// <summary>Sets Bounds and arranges all children within finalBounds.</summary>
    public override void Arrange(Rectangle finalBounds)
    {
        base.Arrange(finalBounds);
        for (int i = 0; i < Children.Count; i++)
            Children[i].Arrange(finalBounds);
    }

    /// <summary>Called when a child is added; override to hook layout invalidation.</summary>
    protected virtual void OnChildAdded(UIElement child) { }

    /// <summary>Called when a child is removed; override to hook layout invalidation.</summary>
    protected virtual void OnChildRemoved(UIElement child) { }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!IsEnabled) return;
        base.Update(gameTime);
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i].IsEnabled)
                Children[i].Update(gameTime);
        }
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        base.Draw(spriteBatch);
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i].IsVisible)
                Children[i].Draw(spriteBatch);
        }
    }
}
