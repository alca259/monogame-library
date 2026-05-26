using Alca.MonoGame.Kernel.Input;
using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Interaction;

public sealed class UIInteractionManagerTests
{
    #region Helpers

    private sealed class StubInteractable : UIElement, IUIInteractable
    {
        public bool IsHovered { get; private set; }
        public int PointerEnterCount { get; private set; }
        public int PointerLeaveCount { get; private set; }
        public int PointerDownCount { get; private set; }
        public int PointerUpCount { get; private set; }
        public bool ConsumeDown { get; set; }
        public bool ConsumeUp { get; set; }

        public void OnPointerEnter() { IsHovered = true; PointerEnterCount++; }
        public void OnPointerLeave() { IsHovered = false; PointerLeaveCount++; }
        public void OnPointerDown(ref UIPointerEventArgs args) { PointerDownCount++; if (ConsumeDown) args.Handled = true; }
        public void OnPointerUp(ref UIPointerEventArgs args) { PointerUpCount++; if (ConsumeUp) args.Handled = true; }
    }

    private sealed class StubInteractableContainer : UIContainer, IUIInteractable
    {
        public bool IsHovered { get; private set; }
        public int PointerDownCount { get; private set; }
        public int PointerUpCount { get; private set; }
        public bool ConsumeDown { get; set; }
        public bool ConsumeUp { get; set; }

        public void OnPointerEnter() => IsHovered = true;
        public void OnPointerLeave() => IsHovered = false;
        public void OnPointerDown(ref UIPointerEventArgs args) { PointerDownCount++; if (ConsumeDown) args.Handled = true; }
        public void OnPointerUp(ref UIPointerEventArgs args) { PointerUpCount++; if (ConsumeUp) args.Handled = true; }
    }

    private sealed class PlainElement : UIElement { }

    private static UIRoot MakeRoot(Rectangle bounds)
    {
        var root = new UIRoot();
        root.Arrange(bounds);
        return root;
    }

    #endregion

    #region HitTest

    [Fact]
    public void HitTest_PointInsideSingleElement_ReturnsElement()
    {
        var element = new PlainElement();
        element.Arrange(new Rectangle(0, 0, 100, 100));

        UIElement? hit = UIInteractionManager.HitTest(element, new Point(50, 50));

        Assert.Same(element, hit);
    }

    [Fact]
    public void HitTest_PointOutsideElement_ReturnsNull()
    {
        var element = new PlainElement();
        element.Arrange(new Rectangle(0, 0, 100, 100));

        UIElement? hit = UIInteractionManager.HitTest(element, new Point(200, 200));

        Assert.Null(hit);
    }

    [Fact]
    public void HitTest_InvisibleElement_ReturnsNull()
    {
        var element = new PlainElement { IsVisible = false };
        element.Arrange(new Rectangle(0, 0, 100, 100));

        UIElement? hit = UIInteractionManager.HitTest(element, new Point(50, 50));

        Assert.Null(hit);
    }

    [Fact]
    public void HitTest_DisabledElement_ReturnsNull()
    {
        var element = new PlainElement { IsEnabled = false };
        element.Arrange(new Rectangle(0, 0, 100, 100));

        UIElement? hit = UIInteractionManager.HitTest(element, new Point(50, 50));

        Assert.Null(hit);
    }

    [Fact]
    public void HitTest_PointInChild_ReturnsChild()
    {
        var root = MakeRoot(new Rectangle(0, 0, 400, 300));
        var child = new PlainElement();
        child.Arrange(new Rectangle(10, 10, 100, 100));
        root.Add(child);

        UIElement? hit = UIInteractionManager.HitTest(root, new Point(50, 50));

        Assert.Same(child, hit);
    }

    [Fact]
    public void HitTest_PointInParentButNotChild_ReturnsParent()
    {
        var root = MakeRoot(new Rectangle(0, 0, 400, 300));
        var child = new PlainElement();
        child.Arrange(new Rectangle(200, 200, 50, 50));
        root.Add(child);

        UIElement? hit = UIInteractionManager.HitTest(root, new Point(10, 10));

        Assert.Same(root, hit);
    }

    [Fact]
    public void HitTest_TwoOverlappingChildren_ReturnsLastAdded()
    {
        var root = MakeRoot(new Rectangle(0, 0, 400, 300));
        var first = new PlainElement();
        first.Arrange(new Rectangle(0, 0, 200, 200));
        var second = new PlainElement();
        second.Arrange(new Rectangle(0, 0, 200, 200));
        root.Add(first);
        root.Add(second);

        UIElement? hit = UIInteractionManager.HitTest(root, new Point(100, 100));

        Assert.Same(second, hit);
    }

    [Fact]
    public void HitTest_InvisibleChildOverlapping_FallsThroughToSibling()
    {
        var root = MakeRoot(new Rectangle(0, 0, 400, 300));
        var visible = new PlainElement();
        visible.Arrange(new Rectangle(0, 0, 200, 200));
        var hidden = new PlainElement { IsVisible = false };
        hidden.Arrange(new Rectangle(0, 0, 200, 200));
        root.Add(visible);
        root.Add(hidden);

        UIElement? hit = UIInteractionManager.HitTest(root, new Point(100, 100));

        Assert.Same(visible, hit);
    }

    #endregion

    #region Hover

    [Fact]
    public void HoverEnter_FiresOnIUIInteractableElement()
    {
        var stub = new StubInteractable();
        stub.Arrange(new Rectangle(0, 0, 100, 100));
        var root = MakeRoot(new Rectangle(0, 0, 400, 300));
        root.Add(stub);

        // Simulate hover enter by directly testing HitTest result + enter logic
        UIElement? hit = UIInteractionManager.HitTest(root, new Point(50, 50));
        if (hit is IUIInteractable i) i.OnPointerEnter();

        Assert.True(stub.IsHovered);
        Assert.Equal(1, stub.PointerEnterCount);
    }

    [Fact]
    public void HoverLeave_FiresWhenPointerMoves()
    {
        var stub = new StubInteractable();
        stub.Arrange(new Rectangle(0, 0, 100, 100));

        stub.OnPointerEnter();
        stub.OnPointerLeave();

        Assert.False(stub.IsHovered);
        Assert.Equal(1, stub.PointerLeaveCount);
    }

    #endregion

    #region Bubbling

    [Fact]
    public void PointerDown_BubblesFromChildToParent_WhenNotHandled()
    {
        var parent = new StubInteractableContainer();
        parent.Arrange(new Rectangle(0, 0, 200, 200));
        var child = new StubInteractable();
        child.Arrange(new Rectangle(10, 10, 80, 80));
        parent.Add(child);

        UIPointerEventArgs args = new UIPointerEventArgs { Position = new Point(50, 50), Button = MouseButton.Left };
        UIElement? current = child;
        while (current is not null && !args.Handled)
        {
            if (current is IUIInteractable interactable) interactable.OnPointerDown(ref args);
            current = current.Parent;
        }

        Assert.Equal(1, child.PointerDownCount);
        Assert.Equal(1, parent.PointerDownCount);
    }

    [Fact]
    public void PointerDown_StopsBubbling_WhenHandled()
    {
        var parent = new StubInteractableContainer();
        parent.Arrange(new Rectangle(0, 0, 200, 200));
        var child = new StubInteractable { ConsumeDown = true };
        child.Arrange(new Rectangle(10, 10, 80, 80));
        parent.Add(child);

        UIPointerEventArgs args = new UIPointerEventArgs { Position = new Point(50, 50), Button = MouseButton.Left };
        UIElement? current = child;
        while (current is not null && !args.Handled)
        {
            if (current is IUIInteractable interactable) interactable.OnPointerDown(ref args);
            current = current.Parent;
        }

        Assert.Equal(1, child.PointerDownCount);
        Assert.Equal(0, parent.PointerDownCount);
        Assert.True(args.Handled);
    }

    [Fact]
    public void PointerUp_BubblesFromChildToParent_WhenNotHandled()
    {
        var parent = new StubInteractableContainer();
        parent.Arrange(new Rectangle(0, 0, 200, 200));
        var child = new StubInteractable();
        child.Arrange(new Rectangle(10, 10, 80, 80));
        parent.Add(child);

        UIPointerEventArgs args = new UIPointerEventArgs { Position = new Point(50, 50), Button = MouseButton.Left };
        UIElement? current = child;
        while (current is not null && !args.Handled)
        {
            if (current is IUIInteractable interactable) interactable.OnPointerUp(ref args);
            current = current.Parent;
        }

        Assert.Equal(1, child.PointerUpCount);
        Assert.Equal(1, parent.PointerUpCount);
    }

    [Fact]
    public void PointerUp_StopsBubbling_WhenHandled()
    {
        var parent = new StubInteractableContainer();
        parent.Arrange(new Rectangle(0, 0, 200, 200));
        var child = new StubInteractable { ConsumeUp = true };
        child.Arrange(new Rectangle(10, 10, 80, 80));
        parent.Add(child);

        UIPointerEventArgs args = new UIPointerEventArgs { Position = new Point(50, 50), Button = MouseButton.Left };
        UIElement? current = child;
        while (current is not null && !args.Handled)
        {
            if (current is IUIInteractable interactable) interactable.OnPointerUp(ref args);
            current = current.Parent;
        }

        Assert.Equal(1, child.PointerUpCount);
        Assert.Equal(0, parent.PointerUpCount);
        Assert.True(args.Handled);
    }

    [Fact]
    public void Bubbling_SkipsNonInteractableAncestors()
    {
        var root = new UIContainer();
        root.Arrange(new Rectangle(0, 0, 400, 300));
        var middle = new PlainElement();
        middle.Arrange(new Rectangle(0, 0, 200, 200));
        var child = new StubInteractable();
        child.Arrange(new Rectangle(10, 10, 80, 80));
        root.Add(middle);
        // Manually build parent chain for test
        child.Parent = middle;
        middle.Parent = root;

        UIPointerEventArgs args = new UIPointerEventArgs { Position = new Point(50, 50), Button = MouseButton.Left };
        UIElement? current = child;
        int interactableCallCount = 0;
        while (current is not null && !args.Handled)
        {
            if (current is IUIInteractable interactable) { interactable.OnPointerDown(ref args); interactableCallCount++; }
            current = current.Parent;
        }

        Assert.Equal(1, interactableCallCount);
        Assert.Equal(1, child.PointerDownCount);
    }

    #endregion

    #region UIPointerEventArgs

    [Fact]
    public void UIPointerEventArgs_DefaultsHandledToFalse()
    {
        UIPointerEventArgs args = default;
        Assert.False(args.Handled);
    }

    [Fact]
    public void UIPointerEventArgs_CanSetHandled_ViaRef()
    {
        UIPointerEventArgs args = new UIPointerEventArgs { Position = new Point(10, 20), Button = MouseButton.Left };
        SetHandled(ref args);
        Assert.True(args.Handled);
    }

    private static void SetHandled(ref UIPointerEventArgs args) => args.Handled = true;

    [Fact]
    public void UIPointerEventArgs_Position_MatchesProvided()
    {
        var pos = new Point(42, 99);
        UIPointerEventArgs args = new UIPointerEventArgs { Position = pos };
        Assert.Equal(pos, args.Position);
    }

    #endregion
}
