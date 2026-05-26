using Alca.MonoGame.Kernel.Input;

namespace Alca.MonoGame.Kernel.UI.Interaction;

/// <summary>Pointer event data passed by ref to IUIInteractable callbacks; no heap allocation.</summary>
public struct UIPointerEventArgs
{
    /// <summary>Cursor position in screen-space at the time of the event.</summary>
    public Point Position;

    /// <summary>The mouse button that triggered this event.</summary>
    public MouseButton Button;

    /// <summary>Set to true inside a handler to stop event bubbling up the parent chain.</summary>
    public bool Handled;
}
