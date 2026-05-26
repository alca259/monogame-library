namespace Alca.MonoGame.Kernel.UI.Focus;

/// <summary>Contract for UI elements that can receive keyboard and gamepad focus.</summary>
public interface IFocusable
{
    /// <summary>Navigation order when Tab/Shift+Tab is pressed. Lower values are focused first.</summary>
    int TabIndex { get; }

    /// <summary>TabIndex of the neighbor reached by pressing D-Pad Up; null = no neighbor.</summary>
    int? FocusNeighborUp { get; }

    /// <summary>TabIndex of the neighbor reached by pressing D-Pad Down; null = no neighbor.</summary>
    int? FocusNeighborDown { get; }

    /// <summary>TabIndex of the neighbor reached by pressing D-Pad Left; null = no neighbor.</summary>
    int? FocusNeighborLeft { get; }

    /// <summary>TabIndex of the neighbor reached by pressing D-Pad Right; null = no neighbor.</summary>
    int? FocusNeighborRight { get; }

    /// <summary>True when this element currently has focus.</summary>
    bool IsFocused { get; }

    /// <summary>Called by UIFocusManager when this element gains keyboard/gamepad focus.</summary>
    void OnFocusGained();

    /// <summary>Called by UIFocusManager when this element loses keyboard/gamepad focus.</summary>
    void OnFocusLost();
}
