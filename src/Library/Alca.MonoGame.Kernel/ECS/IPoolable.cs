namespace Alca.MonoGame.Kernel.ECS;

/// <summary>Marks a behaviour as recyclable by a <see cref="GameEntityPool{T}"/>.</summary>
public interface IPoolable
{
    /// <summary>Resets the behaviour to its initial state when retrieved from the pool.</summary>
    void Reset();
}
