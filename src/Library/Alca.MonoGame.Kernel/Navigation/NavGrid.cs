namespace Alca.MonoGame.Kernel.Navigation;

/// <summary>
/// A 2D navigation grid that stores walkability, movement cost, and obstacle height per cell.
/// Supports top-down and side-scrolling coordinate mappings.
/// </summary>
public sealed class NavGrid
{
    private readonly NavCell[] _cells;

    /// <summary>Gets the grid width in cells.</summary>
    public int Width { get; }

    /// <summary>Gets the grid height in cells.</summary>
    public int Height { get; }

    /// <summary>Gets the world-space size of each cell edge.</summary>
    public float CellSize { get; }

    /// <summary>Gets the world-space position of the (0, 0) cell corner.</summary>
    public Vector2 Origin { get; }

    /// <summary>Gets the navigation mode that determines world axis semantics.</summary>
    public NavigationMode Mode { get; }

    /// <summary>Creates a new NavGrid. All cells start walkable with movement cost 1 and no obstacle.</summary>
    public NavGrid(int width, int height, float cellSize, Vector2 origin,
                   NavigationMode mode = NavigationMode.TopDown)
    {
        Width = width;
        Height = height;
        CellSize = cellSize;
        Origin = origin;
        Mode = mode;
        _cells = new NavCell[width * height];
        InitializeCells();
    }

    #region Write
    /// <summary>Sets whether the cell at (x, y) is walkable at ground level.</summary>
    public void SetWalkable(int x, int y, bool walkable)
    {
        if (!IsInBounds(x, y)) return;
        int i = Index(x, y);
        _cells[i] = _cells[i] with { IsWalkable = walkable };
    }

    /// <summary>Sets the terrain movement cost multiplier for the cell at (x, y). Must be &gt; 0.</summary>
    public void SetMovementCost(int x, int y, float cost)
    {
        if (!IsInBounds(x, y)) return;
        int i = Index(x, y);
        _cells[i] = _cells[i] with { MovementCost = cost };
    }

    /// <summary>
    /// Sets the obstacle height for the cell at (x, y). 0 removes the obstacle.
    /// Cells with ObstacleHeight &gt; 0 are only traversable if the agent's JumpHeight &gt;= ObstacleHeight.
    /// </summary>
    public void SetObstacleHeight(int x, int y, float height)
    {
        if (!IsInBounds(x, y)) return;
        int i = Index(x, y);
        _cells[i] = _cells[i] with { ObstacleHeight = height };
    }

    /// <summary>Sets all cells to the given walkability state, preserving movement cost and obstacle height.</summary>
    public void SetAll(bool walkable)
    {
        for (int i = 0; i < _cells.Length; i++)
            _cells[i] = _cells[i] with { IsWalkable = walkable };
    }
    #endregion

    #region Query
    /// <summary>Returns the cell at the given grid coordinates.</summary>
    public NavCell GetCell(int x, int y) => _cells[Index(x, y)];

    /// <summary>Returns true if (x, y) is within the grid bounds.</summary>
    public bool IsInBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

    /// <summary>Returns true if the cell at (x, y) is walkable at ground level.</summary>
    public bool IsWalkable(int x, int y) => IsInBounds(x, y) && _cells[Index(x, y)].IsWalkable;
    #endregion

    #region Coordinate conversion
    /// <summary>
    /// Converts a world-space position to grid coordinates.
    /// Coordinates may be out of bounds; call <see cref="IsInBounds"/> to validate.
    /// </summary>
    public void WorldToGrid(Vector2 worldPos, out int x, out int y)
    {
        x = (int)MathF.Floor((worldPos.X - Origin.X) / CellSize);
        y = (int)MathF.Floor((worldPos.Y - Origin.Y) / CellSize);
    }

    /// <summary>Returns the world-space center position of the cell at (x, y).</summary>
    public Vector2 GridToWorld(int x, int y) =>
        new(Origin.X + x * CellSize + CellSize * 0.5f,
            Origin.Y + y * CellSize + CellSize * 0.5f);
    #endregion

    #region Internal
    private int Index(int x, int y) => y * Width + x;

    private void InitializeCells()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _cells[y * Width + x] = new NavCell
                {
                    GridX = x,
                    GridY = y,
                    IsWalkable = true,
                    MovementCost = 1f,
                    ObstacleHeight = 0f
                };
            }
        }
    }
    #endregion
}
