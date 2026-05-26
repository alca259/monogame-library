namespace Alca.MonoGame.Kernel.UI.Controls;

/// <summary>Manages mutual exclusion for a set of <see cref="RadioButton"/> instances.
/// This is a non-visual coordinator — it does not render anything itself.</summary>
public sealed class RadioGroup
{
    #region Fields

    private readonly List<RadioButton> _buttons = new(8);
    private RadioButton? _selectedButton;

    #endregion

    #region Properties

    /// <summary>The currently selected button, or null if none are selected.</summary>
    public RadioButton? SelectedButton => _selectedButton;

    /// <summary>Zero-based index of the selected button within the registration order. -1 if none selected.</summary>
    public int SelectedIndex
    {
        get
        {
            if (_selectedButton is null) return -1;
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (ReferenceEquals(_buttons[i], _selectedButton))
                    return i;
            }
            return -1;
        }
    }

    /// <summary>Number of buttons registered in this group.</summary>
    public int Count => _buttons.Count;

    /// <summary>Fired when the selected button changes. Passes the new selected index.</summary>
    public event Action<int>? SelectionChanged;

    #endregion

    #region Registration

    /// <summary>Registers a button with this group. No-op if already registered.</summary>
    public void Register(RadioButton button)
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (ReferenceEquals(_buttons[i], button)) return;
        }

        _buttons.Add(button);
    }

    /// <summary>Removes a button from this group. If the button was selected, selection is cleared.</summary>
    public void Unregister(RadioButton button)
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (ReferenceEquals(_buttons[i], button))
            {
                _buttons.RemoveAt(i);
                if (ReferenceEquals(_selectedButton, button))
                {
                    _selectedButton.IsSelected = false;
                    _selectedButton = null;
                    SelectionChanged?.Invoke(-1);
                }

                return;
            }
        }
    }

    #endregion

    #region Selection

    /// <summary>Selects the given button and deselects all others. Fires <see cref="SelectionChanged"/>.</summary>
    public void Select(RadioButton button)
    {
        if (ReferenceEquals(_selectedButton, button)) return;

        // Deselect previous
        if (_selectedButton is not null)
            _selectedButton.IsSelected = false;

        _selectedButton = button;
        _selectedButton.IsSelected = true;

        SelectionChanged?.Invoke(SelectedIndex);
    }

    /// <summary>Selects the button at the given zero-based index. No-op if index is out of range.</summary>
    public void SelectAt(int index)
    {
        if (index < 0 || index >= _buttons.Count) return;
        Select(_buttons[index]);
    }

    /// <summary>Clears the current selection without selecting another button.</summary>
    public void ClearSelection()
    {
        if (_selectedButton is null) return;
        _selectedButton.IsSelected = false;
        _selectedButton = null;
        SelectionChanged?.Invoke(-1);
    }

    #endregion

    #region Navigation Helpers

    /// <summary>Returns the button immediately after <paramref name="button"/> in registration order, or null if it is the last.</summary>
    public RadioButton? NextAfter(RadioButton button)
    {
        for (int i = 0; i < _buttons.Count - 1; i++)
        {
            if (ReferenceEquals(_buttons[i], button))
                return _buttons[i + 1];
        }

        return null;
    }

    /// <summary>Returns the button immediately before <paramref name="button"/> in registration order, or null if it is the first.</summary>
    public RadioButton? PrevBefore(RadioButton button)
    {
        for (int i = 1; i < _buttons.Count; i++)
        {
            if (ReferenceEquals(_buttons[i], button))
                return _buttons[i - 1];
        }

        return null;
    }

    /// <summary>Returns the button at the given index, or null if out of range.</summary>
    public RadioButton? GetAt(int index)
    {
        if (index < 0 || index >= _buttons.Count) return null;
        return _buttons[index];
    }

    #endregion
}
