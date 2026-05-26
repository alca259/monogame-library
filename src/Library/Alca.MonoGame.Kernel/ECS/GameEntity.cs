using System.Reflection;

namespace Alca.MonoGame.Kernel.ECS;

/// <summary>Named container of GameBehaviours. Equivalent to Unity's GameObject.</summary>
public sealed class GameEntity
{
    private static readonly Type[] _updateParamTypes = [typeof(GameTime)];
    private static readonly Type[] _drawParamTypes = [typeof(GameTime), typeof(SpriteBatch)];

    private readonly Dictionary<Type, GameBehaviour> _behaviours = [];
    private readonly List<GameBehaviour> _allBehavioursList = new(8);
    private readonly List<GameBehaviour> _updatables = [];
    private readonly List<GameBehaviour> _drawables = [];
    private readonly List<GameEntity> _children = new(4);
    private readonly HashSet<string> _tags = new(8);
    private GameEntity? _parent;
    private bool _started;

    #region Identity

    /// <summary>Gets the unique identifier for this entity.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Gets the display name of this entity.</summary>
    public string Name { get; }

    /// <summary>Gets or sets a value indicating whether this entity participates in Update and Draw.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Gets the world that owns this entity.</summary>
    public GameWorld World { get; internal set; } = null!;

    /// <summary>
    /// Gets the always-present spatial component. Equivalent to Unity's <c>gameObject.transform</c>.
    /// Set automatically when a <see cref="TransformBehaviour"/> is added.
    /// </summary>
    public TransformBehaviour Transform { get; private set; } = null!;

    #endregion

    #region Hierarchy

    /// <summary>Gets the parent of this entity, or null if it is a root entity.</summary>
    public GameEntity? Parent => _parent;

    /// <summary>Gets the number of direct children.</summary>
    public int ChildCount => _children.Count;

    /// <summary>Gets the direct children of this entity.</summary>
    public IReadOnlyList<GameEntity> Children => _children;

    /// <summary>Gets the topmost entity in the hierarchy.</summary>
    public GameEntity Root
    {
        get { var e = this; while (e._parent is not null) e = e._parent; return e; }
    }

    /// <summary>Sets or clears the parent of this entity, updating both parent and child references.</summary>
    public void SetParent(GameEntity? newParent)
    {
        if (_parent == newParent) return;
        _parent?.RemoveChildInternal(this);
        _parent = newParent;
        newParent?.AddChildInternal(this);
    }

    internal void AddChildInternal(GameEntity child) => _children.Add(child);

    internal void RemoveChildInternal(GameEntity child) => _children.Remove(child);

    /// <summary>Returns true if this entity is a direct or indirect child of <paramref name="other"/>.</summary>
    public bool IsChildOf(GameEntity other)
    {
        var p = _parent;
        while (p is not null)
        {
            if (p == other) return true;
            p = p._parent;
        }
        return false;
    }

    /// <summary>Returns the index of this entity in its parent's children list, or 0 if it has no parent.</summary>
    public int GetSiblingIndex() => _parent?._children.IndexOf(this) ?? 0;

    /// <summary>Moves this entity to the first position in its parent's children list.</summary>
    public void SetAsFirstSibling()
    {
        if (_parent is null) return;
        var list = _parent._children;
        list.Remove(this);
        list.Insert(0, this);
    }

    /// <summary>Moves this entity to the last position in its parent's children list.</summary>
    public void SetAsLastSibling()
    {
        if (_parent is null) return;
        var list = _parent._children;
        list.Remove(this);
        list.Add(this);
    }

    /// <summary>
    /// Finds a direct or indirect child by name using BFS. Returns null if not found.
    /// Not suitable for use inside Update or Draw — use caching instead.
    /// </summary>
    public GameEntity? Find(string name)
    {
        var queue = new Queue<GameEntity>();
        for (int i = 0; i < _children.Count; i++)
            queue.Enqueue(_children[i]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Name == name) return current;
            for (int i = 0; i < current._children.Count; i++)
                queue.Enqueue(current._children[i]);
        }
        return null;
    }

    /// <summary>Detaches all direct children by setting their parent to null.</summary>
    public void DetachChildren()
    {
        for (int i = _children.Count - 1; i >= 0; i--)
            _children[i].SetParent(null);
    }

    /// <summary>Calls <paramref name="action"/> on this entity and then recursively on all descendants (DFS).</summary>
    public void TraverseDown(Action<GameEntity> action)
    {
        action(this);
        for (int i = 0; i < _children.Count; i++)
            _children[i].TraverseDown(action);
    }

    /// <summary>Calls <paramref name="action"/> on this entity and then on each ancestor up to the root.</summary>
    public void TraverseUp(Action<GameEntity> action)
    {
        action(this);
        _parent?.TraverseUp(action);
    }

    #endregion

    #region Tags

    /// <summary>Adds the given tag to this entity. No-op if the tag is already present.</summary>
    public void AddTag(string tag) => _tags.Add(tag);

    /// <summary>Removes the given tag from this entity. No-op if not present.</summary>
    public void RemoveTag(string tag) => _tags.Remove(tag);

    /// <summary>Returns true if this entity has the given tag.</summary>
    public bool HasTag(string tag) => _tags.Contains(tag);

    /// <summary>Returns the set of all tags attached to this entity.</summary>
    public IReadOnlySet<string> GetTags() => _tags;

    #endregion

    internal GameEntity(string name) => Name = name;

    #region Fluent Builder

    /// <summary>
    /// Adds a behaviour to this entity, calls <see cref="GameBehaviour.Awake"/> immediately,
    /// and returns this entity for fluent chaining.
    /// </summary>
    public GameEntity Add<T>(T behaviour) where T : GameBehaviour
    {
        _behaviours[typeof(T)] = behaviour;
        _allBehavioursList.Add(behaviour);
        behaviour.SetEntityInternal(this);

        if (behaviour is TransformBehaviour t)
            Transform = t;

        // One-time reflection check — never repeated per frame.
        var type = behaviour.GetType();
        if (OverridesMethod(type, nameof(GameBehaviour.Update), _updateParamTypes))
            _updatables.Add(behaviour);
        if (OverridesMethod(type, nameof(GameBehaviour.Draw), _drawParamTypes))
            _drawables.Add(behaviour);

        behaviour.Awake();
        return this;
    }

    /// <summary>
    /// Creates and adds a behaviour of type <typeparamref name="T"/> in a single atomic step,
    /// guaranteeing that <see cref="GameBehaviour.Entity"/> is set before <see cref="GameBehaviour.Awake"/> is called.
    /// </summary>
    public T AddComponent<T>() where T : GameBehaviour, new()
    {
        var behaviour = new T();
        Add(behaviour);
        return behaviour;
    }

    #endregion

    #region Component Access

    /// <summary>Returns the component by concrete type or interface. Returns null if not found.</summary>
    public T? GetComponent<T>() where T : class
    {
        // Fast path: exact concrete type match
        if (_behaviours.TryGetValue(typeof(T), out var exact))
            return (T)(object)exact;

        // Slow path: search by interface or base type
        foreach (var b in _behaviours.Values)
            if (b is T match) return match;

        return null;
    }

    /// <summary>Attempts to get a component. Returns true and sets <paramref name="component"/> if found.</summary>
    public bool TryGetComponent<T>(out T? component) where T : class
    {
        component = GetComponent<T>();
        return component is not null;
    }

    /// <summary>Returns true if this entity has a component of type T (concrete or interface).</summary>
    public bool HasComponent<T>() where T : class => GetComponent<T>() is not null;

    /// <summary>Returns all behaviours attached to this entity.</summary>
    public IEnumerable<GameBehaviour> GetAllComponents() => _behaviours.Values;

    /// <summary>Gets the total number of behaviours attached to this entity.</summary>
    public int GetComponentCount() => _allBehavioursList.Count;

    /// <summary>Gets the behaviour at the specified index in the order they were added.</summary>
    public GameBehaviour GetComponentAtIndex(int index) => _allBehavioursList[index];

    /// <summary>Gets the index of the specified behaviour, or -1 if not found.</summary>
    public int GetComponentIndex(GameBehaviour behaviour) => _allBehavioursList.IndexOf(behaviour);

    /// <summary>Fills <paramref name="results"/> with all behaviours on this entity that are assignable to <typeparamref name="T"/>.</summary>
    public void GetComponents<T>(List<T> results) where T : class
    {
        for (int i = 0; i < _allBehavioursList.Count; i++)
            if (_allBehavioursList[i] is T match) results.Add(match);
    }

    /// <summary>Returns the first component of type <typeparamref name="T"/> found in any direct or indirect child.</summary>
    public T? GetComponentInChildren<T>(bool includeInactive = false) where T : class
    {
        for (int i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (!includeInactive && !child.Active) continue;
            var c = child.GetComponent<T>() ?? child.GetComponentInChildren<T>(includeInactive);
            if (c is not null) return c;
        }
        return null;
    }

    /// <summary>Fills <paramref name="results"/> with all components of type <typeparamref name="T"/> in any direct or indirect child.</summary>
    public void GetComponentsInChildren<T>(List<T> results, bool includeInactive = false) where T : class
    {
        for (int i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (!includeInactive && !child.Active) continue;
            child.GetComponents<T>(results);
            child.GetComponentsInChildren<T>(results, includeInactive);
        }
    }

    /// <summary>Returns the first component of type <typeparamref name="T"/> found walking up the parent chain.</summary>
    public T? GetComponentInParent<T>(bool includeInactive = false) where T : class
    {
        var p = _parent;
        while (p is not null)
        {
            if (!includeInactive && !p.Active) { p = p._parent; continue; }
            var c = p.GetComponent<T>();
            if (c is not null) return c;
            p = p._parent;
        }
        return null;
    }

    /// <summary>Fills <paramref name="results"/> with all components of type <typeparamref name="T"/> found walking up the parent chain.</summary>
    public void GetComponentsInParent<T>(List<T> results, bool includeInactive = false) where T : class
    {
        var p = _parent;
        while (p is not null)
        {
            if (!includeInactive && !p.Active) { p = p._parent; continue; }
            p.GetComponents<T>(results);
            p = p._parent;
        }
    }

    /// <summary>Returns true if the specified tag is attached to this entity. Equivalent to <see cref="HasTag"/>.</summary>
    public bool CompareTag(string tag) => HasTag(tag);

    /// <summary>Activates or deactivates this entity.</summary>
    public void SetActive(bool active) => Active = active;

    #endregion

    #region Internal Loop

    internal void Update(GameTime gameTime)
    {
        if (!Active) return;

        if (!_started)
        {
            StartAll();
            _started = true;
        }

        for (int i = 0; i < _updatables.Count; i++)
            if (_updatables[i].Enabled) _updatables[i].Update(gameTime);
    }

    internal void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!Active) return;

        for (int i = 0; i < _drawables.Count; i++)
            if (_drawables[i].Enabled) _drawables[i].Draw(gameTime, spriteBatch);
    }

    internal void Destroy()
    {
        foreach (var b in _behaviours.Values)
            b.OnDestroy();
    }

    #endregion

    #region Helpers

    private void StartAll()
    {
        foreach (var b in _behaviours.Values)
            b.Start();
    }

    private static bool OverridesMethod(Type type, string methodName, Type[] paramTypes)
    {
        var method = type.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public,
            null, paramTypes, null);
        return method?.DeclaringType != typeof(GameBehaviour);
    }

    #endregion
}
