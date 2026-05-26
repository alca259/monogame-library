namespace Alca.MonoGame.Kernel.UI.Interaction;

/// <summary>Marks a UIElement as capable of receiving pointer interaction events.</summary>
public interface IUIInteractable
{
    /// <summary>Whether the pointer is currently over this element.</summary>
    bool IsHovered { get; }

    /// <summary>Fires when a pointer button is pressed while this element is the hit target.</summary>
    void OnPointerDown(ref UIPointerEventArgs args);

    /// <summary>Fires when a pressed pointer button is released while this element is the hit target.</summary>
    void OnPointerUp(ref UIPointerEventArgs args);

    /// <summary>Fires once when the pointer enters this element's bounds.</summary>
    void OnPointerEnter();

    /// <summary>Fires once when the pointer leaves this element's bounds.</summary>
    void OnPointerLeave();
}
