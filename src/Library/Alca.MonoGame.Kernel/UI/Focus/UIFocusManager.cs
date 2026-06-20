using Alca.MonoGame.Kernel.UI.Input;

namespace Alca.MonoGame.Kernel.UI.Focus;

/// <summary>Manages keyboard and gamepad focus for UI elements.
/// Maintains a tab-ordered list and a neighbor-ID registry for D-Pad navigation.</summary>
public sealed class UIFocusManager
{
    // Kept sorted by TabIndex; used for Tab/Shift+Tab navigation.
    private readonly List<IFocusable> _focusables = new(16);

    // O(1) lookup by TabIndex for D-Pad neighbor navigation.
    private readonly Dictionary<int, IFocusable> _registry = new(16);

    private IFocusable? _focused;

    /// <summary>Gets the currently focused element, or null if nothing is focused.</summary>
    public IFocusable? FocusedElement => _focused;

    /// <summary>Registers an element for Tab and D-Pad navigation.
    /// The element is inserted in TabIndex order. Duplicate TabIndex values are ignored.</summary>
    public void Register(IFocusable element)
    {
        if (_registry.ContainsKey(element.TabIndex))
            return;

        _registry[element.TabIndex] = element;

        int insertAt = _focusables.Count;
        for (int i = 0; i < _focusables.Count; i++)
        {
            if (element.TabIndex < _focusables[i].TabIndex)
            {
                insertAt = i;
                break;
            }
        }

        _focusables.Insert(insertAt, element);
    }

    /// <summary>Removes an element from the focus system.
    /// If the element currently has focus, focus is cleared without firing OnFocusLost.</summary>
    public void Unregister(IFocusable element)
    {
        _registry.Remove(element.TabIndex);
        _focusables.Remove(element);

        if (ReferenceEquals(_focused, element))
            _focused = null;
    }

    /// <summary>Clears all registered elements and removes focus. Fires OnFocusLost on the focused element.</summary>
    public void Clear()
    {
        SetFocus(null);
        _focusables.Clear();
        _registry.Clear();
    }

    /// <summary>Transfers focus to <paramref name="element"/>, firing OnFocusLost on the previous
    /// focused element and OnFocusGained on the new one. Pass null to clear focus.</summary>
    public void SetFocus(IFocusable? element)
    {
        if (ReferenceEquals(_focused, element)) return;

        _focused?.OnFocusLost();
        _focused = element;
        _focused?.OnFocusGained();
    }

    /// <summary>Moves focus to the next element in TabIndex order, wrapping around.</summary>
    public void FocusNext()
    {
        if (_focusables.Count == 0) return;

        if (_focused is null)
        {
            SetFocus(_focusables[0]);
            return;
        }

        int currentIndex = FindFocusedIndex();
        int nextIndex = (currentIndex + 1) % _focusables.Count;
        SetFocus(_focusables[nextIndex]);
    }

    /// <summary>Moves focus to the previous element in TabIndex order, wrapping around.</summary>
    public void FocusPrevious()
    {
        if (_focusables.Count == 0) return;

        if (_focused is null)
        {
            SetFocus(_focusables[_focusables.Count - 1]);
            return;
        }

        int currentIndex = FindFocusedIndex();
        int prevIndex = (currentIndex - 1 + _focusables.Count) % _focusables.Count;
        SetFocus(_focusables[prevIndex]);
    }

    /// <summary>Moves focus in the Up direction using the focused element's FocusNeighborUp ID.</summary>
    public void FocusUp() => MoveFocusToNeighbor(_focused?.FocusNeighborUp);

    /// <summary>Moves focus in the Down direction using the focused element's FocusNeighborDown ID.</summary>
    public void FocusDown() => MoveFocusToNeighbor(_focused?.FocusNeighborDown);

    /// <summary>Moves focus in the Left direction using the focused element's FocusNeighborLeft ID.</summary>
    public void FocusLeft() => MoveFocusToNeighbor(_focused?.FocusNeighborLeft);

    /// <summary>Moves focus in the Right direction using the focused element's FocusNeighborRight ID.</summary>
    public void FocusRight() => MoveFocusToNeighbor(_focused?.FocusNeighborRight);

    /// <summary>Processes UI input to drive focus changes.
    /// Must be called once per frame after input states are updated.</summary>
    public void Update(UIInputContext input)
    {
        if (input.TabNext?.IsPressed == true)
            FocusNext();
        else if (input.TabPrevious?.IsPressed == true)
            FocusPrevious();

        if (input.MoveUp?.IsPressed == true)    FocusUp();
        if (input.MoveDown?.IsPressed == true)  FocusDown();
        if (input.MoveLeft?.IsPressed == true)  FocusLeft();
        if (input.MoveRight?.IsPressed == true) FocusRight();
    }

    private int FindFocusedIndex()
    {
        for (int i = 0; i < _focusables.Count; i++)
        {
            if (ReferenceEquals(_focusables[i], _focused))
                return i;
        }

        return 0;
    }

    private void MoveFocusToNeighbor(int? neighborTabIndex)
    {
        if (neighborTabIndex is null) return;
        if (_registry.TryGetValue(neighborTabIndex.Value, out IFocusable? neighbor))
            SetFocus(neighbor);
    }
}
