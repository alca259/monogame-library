namespace Alca.MonoGame.Kernel.ECS;

/// <summary>Owns all entities and drives the ECS loop. Equivalent to Unity's Scene.</summary>
public sealed class GameWorld
{
    private readonly List<GameEntity> _entities = [];
    private readonly List<GameEntity> _toAdd = [];
    private readonly List<GameEntity> _toDestroy = [];

    /// <summary>Gets or sets a value indicating whether this world processes updates. Draw always runs.</summary>
    public bool IsEnabled { get; set; } = true;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    /// <summary>Flushes pending creation/destruction then updates all active entities.</summary>
    public void Update(GameTime gameTime)
    {
        FlushPending();

        if (!IsEnabled) return;

        for (int i = 0; i < _entities.Count; i++)
            _entities[i].Update(gameTime);
    }

    /// <summary>Draws all active entities. Always runs regardless of <see cref="IsEnabled"/>.</summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _entities.Count; i++)
            _entities[i].Draw(gameTime, spriteBatch);
    }

    // ── Entity management ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates an entity with a pre-attached <see cref="TransformBehaviour"/> at the given 2D position (Z = 0).
    /// The entity is added to the world at the start of the next Update (deferred).
    /// </summary>
    public GameEntity CreateEntity(string name = "", Vector2 position = default)
    {
        var entity = new GameEntity(name) { World = this };
        entity.Add(new TransformBehaviour(position));
        _toAdd.Add(entity);
        return entity;
    }

    /// <summary>
    /// Creates an entity with a pre-attached <see cref="TransformBehaviour"/> at the given 3D position.
    /// The entity is added to the world at the start of the next Update (deferred).
    /// </summary>
    public GameEntity CreateEntity(string name, Vector3 position)
    {
        var entity = new GameEntity(name) { World = this };
        entity.Add(new TransformBehaviour(position));
        _toAdd.Add(entity);
        return entity;
    }

    /// <summary>
    /// Schedules an entity for removal. It is removed from the world at the start of the next
    /// Update (deferred) and <see cref="GameBehaviour.OnDestroy"/> is called on all its behaviours.
    /// </summary>
    public void Destroy(GameEntity entity)
    {
        if (!_toDestroy.Contains(entity))
            _toDestroy.Add(entity);
    }

    // ── Queries ────────────────────────────────────────────────────────────────

    /// <summary>Returns all entities that have a component of type T (concrete type or interface).</summary>
    public IEnumerable<GameEntity> FindEntities<T>() where T : class
    {
        for (int i = 0; i < _entities.Count; i++)
            if (_entities[i].HasComponent<T>()) yield return _entities[i];
    }

    /// <summary>Returns all components of type T (concrete type or interface) across all entities.</summary>
    public IEnumerable<T> FindComponents<T>() where T : class
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            var c = _entities[i].GetComponent<T>();
            if (c is not null) yield return c;
        }
    }

    /// <summary>Returns the first entity with the given name, or null if none exists.</summary>
    public GameEntity? FindByName(string name)
    {
        for (int i = 0; i < _entities.Count; i++)
            if (_entities[i].Name == name) return _entities[i];
        return null;
    }

    /// <summary>Fills <paramref name="results"/> with all entities that have the given tag.</summary>
    public void GetEntitiesByTag(string tag, List<GameEntity> results)
    {
        for (int i = 0; i < _entities.Count; i++)
            if (_entities[i].HasTag(tag)) results.Add(_entities[i]);
    }

    /// <summary>Fills <paramref name="results"/> with all behaviours in the world assignable to <typeparamref name="T"/>.</summary>
    public void GetBehavioursWithInterface<T>(List<T> results) where T : class
    {
        for (int i = 0; i < _entities.Count; i++)
            _entities[i].GetComponents<T>(results);
    }

    // ── Internal ───────────────────────────────────────────────────────────────

    private void FlushPending()
    {
        for (int i = 0; i < _toAdd.Count; i++)
            _entities.Add(_toAdd[i]);
        _toAdd.Clear();

        for (int i = 0; i < _toDestroy.Count; i++)
        {
            _toDestroy[i].Destroy();
            _entities.Remove(_toDestroy[i]);
        }
        _toDestroy.Clear();
    }
}
