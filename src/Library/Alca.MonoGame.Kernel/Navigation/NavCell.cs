namespace Alca.MonoGame.Kernel.Navigation;

/// <summary>Represents a single cell in a <see cref="NavGrid"/>. Immutable value type.</summary>
public readonly record struct NavCell
{
    /// <summary>Gets the grid X coordinate.</summary>
    public int GridX { get; init; }

    /// <summary>Gets the grid Y coordinate.</summary>
    public int GridY { get; init; }

    /// <summary>Gets a value indicating whether this cell is walkable at ground level.</summary>
    public bool IsWalkable { get; init; }

    /// <summary>Gets the terrain movement cost multiplier. 1.0 = normal cost.</summary>
    public float MovementCost { get; init; }

    /// <summary>
    /// Gets the height of any obstacle occupying this cell. 0 = no obstacle.
    /// An agent whose <see cref="NavAgentProfile.JumpHeight"/> is &gt;= this value can traverse the cell.
    /// </summary>
    public float ObstacleHeight { get; init; }
}
