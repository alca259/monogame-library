namespace Alca.MonoGame.Kernel.ECS;

/// <summary>
/// Base class for cross-entity logic that runs once per frame over many components,
/// instead of being duplicated across individual <see cref="GameBehaviour"/> instances.
/// Register a system with <see cref="GameWorld.AddSystem{T}(T)"/>. Override only the lifecycle hooks you need.
/// </summary>
/// <remarks>
/// Systems run after the built-in subsystems (physics, weather, triggers, day/night) and before
/// entity behaviour updates. Use <see cref="Order"/> to control execution order among systems.
/// Call <see cref="GameWorld.AddSystem{T}(T)"/> and <see cref="GameWorld.RemoveSystem"/> outside the update loop.
/// Cache query buffers as <c>readonly</c> fields; use the zero-alloc <c>FindComponents&lt;T&gt;(List&lt;T&gt;)</c>
/// family from <see cref="World"/> and clear/refill each frame.
/// </remarks>
public abstract class GameSystem
{
    private GameWorld? _world;

    /// <summary>
    /// Gets the world this system belongs to.
    /// Guaranteed non-null from <see cref="Initialize"/> onward.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before the system is registered with a <see cref="GameWorld"/>.</exception>
    public GameWorld World => _world ?? throw new InvalidOperationException(
        "This system is not registered with a GameWorld. Access World only from Initialize() or later.");

    /// <summary>Gets the world this system belongs to, or null if not yet registered. For subclass use only.</summary>
    protected GameWorld? WorldOrNull => _world;

    /// <summary>Gets or sets a value indicating whether this system participates in Update and Draw.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the execution order relative to other systems. Lower values run first.
    /// Equal-order systems run in registration order. Must be set before calling <see cref="GameWorld.AddSystem{T}(T)"/>;
    /// changing it afterwards does not reorder the system.
    /// </summary>
    public int Order { get; set; }

    /// <summary>Called once when the system is registered with a world. Cache component buffers and cross-references here.</summary>
    public virtual void Initialize() { }

    /// <summary>Called every frame while the world and this system are enabled. Runs before entity behaviour updates.</summary>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>Called every draw frame while this system is enabled, regardless of <see cref="GameWorld.IsEnabled"/>.</summary>
    public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch) { }

    /// <summary>Called when the system is removed from the world or the world is destroyed.</summary>
    public virtual void OnRemoved() { }

    internal void SetWorldInternal(GameWorld world)
    {
        if (_world is not null)
            throw new InvalidOperationException("This system is already registered with a GameWorld and cannot be reassigned.");
        _world = world;
    }
}
