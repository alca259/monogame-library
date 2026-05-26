namespace Alca.MonoGame.Kernel.ECS;

/// <summary>Owns all entities and drives the ECS loop. Equivalent to Unity's Scene.</summary>
public sealed class GameWorld
{
    private readonly List<GameEntity> _entities = [];
    private readonly List<GameEntity> _toAdd = [];
    private readonly HashSet<GameEntity> _toDestroy = [];

    /// <summary>Gets or sets a value indicating whether this world processes updates. Draw always runs.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the 2D physics world. When set, <see cref="Update"/> automatically steps the
    /// simulation once per frame before processing entity updates.
    /// </summary>
    public Physics.Physics2DWorld? PhysicsWorld { get; set; }

    /// <summary>
    /// Gets or sets the lighting world. When set, <see cref="Lighting.LightBehaviour"/> components
    /// automatically register and unregister themselves on <see cref="GameBehaviour.Awake"/> and
    /// <see cref="GameBehaviour.OnDestroy"/>. Optional — omit for projects without dynamic lighting.
    /// </summary>
    public Lighting.LightingWorld? LightingWorld { get; set; }

    /// <summary>
    /// Gets or sets the navigation grid used by <see cref="Navigation.NavAgent"/> components in this world.
    /// Optional — omit for projects that do not use pathfinding.
    /// </summary>
    public Navigation.NavGrid? NavGrid { get; set; }

    /// <summary>
    /// Gets or sets the pathfinder service used by <see cref="Navigation.NavAgent"/> components in this world.
    /// Optional — omit for projects that do not use pathfinding.
    /// </summary>
    public Navigation.Pathfinder? Pathfinder { get; set; }

    /// <summary>
    /// Gets or sets the physics-to-navgrid sync helper. When set, <see cref="Update"/> automatically
    /// calls <see cref="Navigation.NavGridPhysicsSync.SyncAll"/> after each physics step.
    /// Optional — omit for projects that do not use combined physics + pathfinding.
    /// </summary>
    public Navigation.NavGridPhysicsSync? NavPhysicsSync { get; set; }

    /// <summary>
    /// Gets or sets the async pathfinder used by <see cref="Navigation.NavAgent.SetDestinationAsync"/>.
    /// When set, <see cref="Navigation.NavAgent"/> can offload path searches to a background thread.
    /// Optional — omit for projects that do not need async pathfinding.
    /// </summary>
    public Navigation.AsyncPathfinder? AsyncPathfinder { get; set; }

    /// <summary>
    /// Gets or sets the audio controller used by <see cref="Audio.SpatialAudioSource"/> and
    /// <see cref="Audio.SpatialAudioListener"/> components in this world.
    /// Optional — omit for projects that do not use 3D spatial audio.
    /// </summary>
    public Audio.AudioController? AudioController { get; set; }

    /// <summary>
    /// Gets or sets the audio mixer for channel-based volume routing used by audio components.
    /// Optional — omit for projects that do not use audio mixing.
    /// </summary>
    public Audio.AudioMixer? AudioMixer { get; set; }

    /// <summary>
    /// Gets or sets the network server for this world. Set by <see cref="Network.NetworkManagerBehaviour"/>
    /// on <c>StartServer</c> or <c>StartHost</c>. Optional — omit for projects without networking.
    /// </summary>
    public Network.NetworkServer? NetworkServer { get; set; }

    /// <summary>
    /// Gets or sets the network client for this world. Set by <see cref="Network.NetworkManagerBehaviour"/>
    /// on <c>StartClient</c> or <c>StartHost</c>. Optional — omit for projects without networking.
    /// </summary>
    public Network.NetworkClient? NetworkClient { get; set; }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    /// <summary>Flushes pending creation/destruction then updates all active entities.</summary>
    public void Update(GameTime gameTime)
    {
        FlushPending();

        if (!IsEnabled) return;

        PhysicsWorld?.Step(gameTime);
        if (NavPhysicsSync is not null && NavGrid is not null)
            NavPhysicsSync.SyncAll(NavGrid);

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

    /// <summary>Schedules an entity for removal. It is removed from the world at the start of the next
    /// Update (deferred) and <see cref="GameBehaviour.OnDestroy"/> is called on all its behaviours.
    /// </summary>
    public void Destroy(GameEntity entity) => _toDestroy.Add(entity);

    /// <summary>
    /// Immediately calls <see cref="GameBehaviour.OnDestroy"/> on all entities (including pending ones)
    /// and clears all entity lists. Safe to call multiple times.
    /// </summary>
    public void Destroy()
    {
        for (int i = 0; i < _toAdd.Count; i++)
            _toAdd[i].Destroy();
        _toAdd.Clear();

        for (int i = 0; i < _entities.Count; i++)
            _entities[i].Destroy();
        _entities.Clear();

        _toDestroy.Clear();
    }

    // ── Queries ────────────────────────────────────────────────────────────────

    /// <summary>Gets the number of active entities in this world.</summary>
    public int EntityCount => _entities.Count;

    /// <summary>Returns all entities that have a component of type T (concrete type or interface).</summary>
    [Obsolete("Allocates an enumerator. Use FindEntities<T>(List<GameEntity>) for zero-alloc hot paths.")]
    public IEnumerable<GameEntity> FindEntities<T>() where T : class
    {
        for (int i = 0; i < _entities.Count; i++)
            if (_entities[i].HasComponent<T>()) yield return _entities[i];
    }

    /// <summary>
    /// Fills <paramref name="results"/> with all entities that have a component of type T.
    /// No heap allocations — safe to call from the update loop.
    /// </summary>
    public void FindEntities<T>(List<GameEntity> results) where T : class
    {
        for (int i = 0; i < _entities.Count; i++)
            if (_entities[i].HasComponent<T>()) results.Add(_entities[i]);
    }

    /// <summary>Returns all components of type T (concrete type or interface) across all entities.</summary>
    [Obsolete("Allocates an enumerator. Use FindComponents<T>(List<T>) for zero-alloc hot paths.")]
    public IEnumerable<T> FindComponents<T>() where T : class
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            var c = _entities[i].GetComponent<T>();
            if (c is not null) yield return c;
        }
    }

    /// <summary>
    /// Fills <paramref name="results"/> with all components of type T across all entities.
    /// No heap allocations — safe to call from the update loop.
    /// </summary>
    public void FindComponents<T>(List<T> results) where T : class
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            T? c = _entities[i].GetComponent<T>();
            if (c is not null) results.Add(c);
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

        foreach (GameEntity entity in _toDestroy)
        {
            entity.Destroy();
            _entities.Remove(entity);
        }
        _toDestroy.Clear();
    }
}
