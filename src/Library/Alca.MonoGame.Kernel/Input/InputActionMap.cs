namespace Alca.MonoGame.Kernel.Input;

/// <summary>A named collection of <see cref="InputAction"/> instances that can be loaded into the <see cref="InputManager"/>.</summary>
public sealed class InputActionMap
{
    private readonly Dictionary<string, InputAction> _actions;
    private readonly List<InputAction> _actionList;

    /// <summary>Creates a new empty InputActionMap.</summary>
    public InputActionMap()
    {
        _actions = new Dictionary<string, InputAction>(16);
        _actionList = new List<InputAction>(16);
    }

    /// <summary>Registers an action in this map. Replaces any existing action with the same name.</summary>
    /// <param name="action">The action to register.</param>
    public void Register(InputAction action)
    {
        if (_actions.TryGetValue(action.Name, out InputAction? existing))
        {
            int idx = _actionList.IndexOf(existing);
            if (idx >= 0) _actionList[idx] = action;
        }
        else
        {
            _actionList.Add(action);
        }

        _actions[action.Name] = action;
    }

    /// <summary>Removes the action registered under the specified name.</summary>
    /// <param name="name">The name of the action to remove.</param>
    public void Unregister(string name)
    {
        if (_actions.TryGetValue(name, out InputAction? action))
        {
            _actionList.Remove(action);
            _actions.Remove(name);
        }
    }

    /// <summary>Returns the action with the specified name, or null if not found.</summary>
    /// <param name="name">The action name to look up.</param>
    public InputAction? Get(string name)
    {
        _actions.TryGetValue(name, out InputAction? action);
        return action;
    }

    /// <summary>Updates all registered actions from the provided input states. Uses indexed iteration — no heap allocations.</summary>
    internal void Update(
        KeyboardState currK, KeyboardState prevK,
        MouseState currM, MouseState prevM,
        GamePadState currP, GamePadState prevP)
    {
        for (int i = 0; i < _actionList.Count; i++)
        {
            _actionList[i].Update(currK, prevK, currM, prevM, currP, prevP);
        }
    }

    /// <summary>Returns a read-only ordered list of all registered actions.</summary>
    internal IReadOnlyList<InputAction> GetAllActions() => _actionList;
}
