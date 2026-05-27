using Alca.MonoGame.Kernel.Input;
using Alca.MonoGame.Kernel.UI.Focus;

namespace Alca.MonoGame.Kernel.UI.Interaction;

/// <summary>Runs pointer hit testing and dispatches enter/leave/down/up events each frame.
/// Attach a single instance to the application's UIRoot and call Update from the game loop.</summary>
public sealed class UIInteractionManager
{
    private UIElement? _hoveredElement;

    // Reused every frame — no heap allocation in Update.
    private UIPointerEventArgs _eventArgs;

    /// <summary>True if the pointer is currently over an interactive UI control (<see cref="IUIInteractable"/>).
    /// Layout containers (AnchorLayout, StackPanel, etc.) are transparent to this check.
    /// Call after <see cref="Update"/> to suppress game-world input.</summary>
    public bool IsPointerOverUI => _hoveredElement is IUIInteractable;

    /// <summary>Fired when the hovered element changes. Carries the new element (null = none).</summary>
    public event Action<UIElement?>? HoverChanged;

    /// <summary>Processes pointer input against the UI tree.
    /// Must be called after the layout Arrange pass so Bounds are current.</summary>
    /// <param name="root">Root of the UI tree to test against.</param>
    /// <param name="mouse">Current-frame mouse snapshot from InputManager.</param>
    /// <param name="focusManager">Optional focus manager; when provided, clicking an <see cref="IFocusable"/> element transfers focus to it.</param>
    public void Update(UIRoot root, MouseInfo mouse, UIFocusManager? focusManager = null)
    {
        Point mousePos = mouse.Position;

        UIElement? newHover = HitTest(root, mousePos);

        if (!ReferenceEquals(newHover, _hoveredElement))
        {
            if (_hoveredElement is IUIInteractable oldInteractable)
                oldInteractable.OnPointerLeave();

            if (newHover is IUIInteractable newInteractable)
                newInteractable.OnPointerEnter();

            _hoveredElement = newHover;
            HoverChanged?.Invoke(_hoveredElement);
        }

        bool justPressed = mouse.WasButtonJustPressed(MouseButton.Left);
        bool justReleased = mouse.WasButtonJustReleased(MouseButton.Left);

        if (justPressed && newHover is not null)
        {
            if (focusManager is not null)
            {
                UIElement? cur = newHover;
                IFocusable? focusTarget = null;
                while (cur is not null)
                {
                    if (cur is IFocusable f) { focusTarget = f; break; }
                    cur = cur.Parent;
                }
                focusManager.SetFocus(focusTarget);
            }

            _eventArgs = new UIPointerEventArgs { Position = mousePos, Button = MouseButton.Left };
            BubblePointerDown(newHover, ref _eventArgs);
        }

        if (justReleased && newHover is not null)
        {
            _eventArgs = new UIPointerEventArgs { Position = mousePos, Button = MouseButton.Left };
            BubblePointerUp(newHover, ref _eventArgs);
        }
    }

    /// <summary>Returns the deepest visible, enabled element whose Bounds contains point,
    /// or null if no element is hit. Children are tested last-to-first (topmost drawn = first tested).</summary>
    internal static UIElement? HitTest(UIElement element, Point point)
    {
        if (!element.IsVisible || !element.IsEnabled) return null;
        if (!element.Bounds.Contains(point)) return null;

        if (element is UIContainer container)
        {
            IReadOnlyList<UIElement> children = container.ChildrenReadOnly;
            for (int i = children.Count - 1; i >= 0; i--)
            {
                UIElement? hit = HitTest(children[i], point);
                if (hit is not null) return hit;
            }
        }

        return element;
    }

    private static void BubblePointerDown(UIElement target, ref UIPointerEventArgs args)
    {
        UIElement? current = target;
        while (current is not null && !args.Handled)
        {
            if (current is IUIInteractable interactable)
                interactable.OnPointerDown(ref args);
            current = current.Parent;
        }
    }

    private static void BubblePointerUp(UIElement target, ref UIPointerEventArgs args)
    {
        UIElement? current = target;
        while (current is not null && !args.Handled)
        {
            if (current is IUIInteractable interactable)
                interactable.OnPointerUp(ref args);
            current = current.Parent;
        }
    }
}
