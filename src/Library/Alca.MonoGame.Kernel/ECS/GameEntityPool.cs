namespace Alca.MonoGame.Kernel.ECS;

/// <summary>
/// Reusable pool of entities backed by a single <typeparamref name="T"/> behaviour.
/// Use instead of spawning/destroying frequently created entities such as bullets or particles.
/// </summary>
public sealed class GameEntityPool<T> where T : GameBehaviour, IPoolable, new()
{
    private readonly Stack<GameEntity> _pool = [];
    private readonly GameWorld _world;
    private readonly string _name;

    /// <summary>Gets the number of entities currently available in the pool.</summary>
    public int AvailableCount => _pool.Count;

    /// <summary>Creates the pool and optionally pre-warms it with <paramref name="prewarm"/> inactive entities.</summary>
    public GameEntityPool(GameWorld world, string name, int prewarm = 0)
    {
        _world = world;
        _name = name;

        for (int i = 0; i < prewarm; i++)
            _pool.Push(CreateNew());
    }

    /// <summary>
    /// Retrieves an entity from the pool (or creates one if the pool is empty),
    /// calls <see cref="IPoolable.Reset"/>, activates it, and optionally configures it.
    /// </summary>
    public GameEntity Get(Action<GameEntity>? configure = null)
    {
        var entity = _pool.Count > 0 ? _pool.Pop() : CreateNew();
        entity.GetComponent<T>()!.Reset();
        entity.Active = true;
        configure?.Invoke(entity);
        return entity;
    }

    /// <summary>Returns an entity to the pool. The entity is deactivated but not destroyed.</summary>
    public void Return(GameEntity entity)
    {
        entity.Active = false;
        _pool.Push(entity);
    }

    private GameEntity CreateNew()
    {
        var entity = _world.CreateEntity(_name);
        entity.Add(new T());
        entity.Active = false;
        return entity;
    }
}
