namespace Alca.MonoGame.Kernel.ECS;

/// <summary>Base class for all component logic. Override only the lifecycle hooks you need.</summary>
public abstract class GameBehaviour
{
    private GameEntity? _entity;

    /// <summary>
    /// Gets the entity this behaviour is attached to.
    /// Guaranteed non-null from <see cref="Awake"/> onward.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before the behaviour is attached to a <see cref="GameEntity"/>.</exception>
    public GameEntity Entity => _entity ?? throw new InvalidOperationException(
        "This behaviour is not attached to a GameEntity. Access Entity only from Awake() or later lifecycle methods.");

    /// <summary>Gets the entity this behaviour is attached to, or null if not yet attached. For subclass use only.</summary>
    protected GameEntity? EntityOrNull => _entity;

    /// <summary>Gets or sets a value indicating whether this behaviour participates in Update and Draw.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Called immediately when added to an entity. Cache sibling component references here.</summary>
    public virtual void Awake() { }

    /// <summary>Called before the first Update. Use for logic that depends on all entities being initialized.</summary>
    public virtual void Start() { }

    /// <summary>Called every frame. Only invoked if this class overrides it.</summary>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>Called every draw frame. Only invoked if this class overrides it.</summary>
    public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch) { }

    /// <summary>Called when the entity is destroyed.</summary>
    public virtual void OnDestroy() { }

    internal void SetEntityInternal(GameEntity entity)
    {
        if (_entity is not null)
            throw new InvalidOperationException("This behaviour is already attached to a GameEntity and cannot be reassigned.");
        _entity = entity;
    }
}
