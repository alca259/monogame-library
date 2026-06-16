namespace Alca.MonoGame.Kernel.Navigation;

/// <summary>
/// Computes navigation paths using the A* algorithm with 8-directional movement.
/// All internal data structures are pre-allocated at construction; no heap allocations
/// occur during pathfinding. Register as a singleton in DI.
/// Not thread-safe — synchronize externally when used from multiple threads.
/// </summary>
public sealed class Pathfinder
{
    private const float SqrtTwo = 1.41421356f;
    private const float DescentMultiplier = 0.8f;

    // 8 movement directions: 0-3 orthogonal, 4-7 diagonal
    private static readonly int[] _dirX = [1, -1, 0, 0, 1, -1, 1, -1];
    private static readonly int[] _dirY = [0, 0, 1, -1, 1, 1, -1, -1];

    // Per-cell arrays indexed by cellIndex = y * width + x
    private readonly float[] _gCost;
    private readonly float[] _hCost;
    private readonly int[] _parent;
    private readonly bool[] _inClosed;

    // Min-heap: parallel arrays storing (fCost, cellIndex) per heap slot
    private readonly float[] _heapF;
    private readonly int[] _heapIdx;
    private int _heapSize;

    private readonly int _capacity;

    /// <summary>
    /// Creates a Pathfinder with pre-allocated capacity for grids up to <paramref name="gridCapacity"/> cells.
    /// </summary>
    /// <param name="gridCapacity">Maximum Width × Height supported. Default is 65536 (256×256).</param>
    public Pathfinder(int gridCapacity = 65536)
    {
        _capacity = gridCapacity;
        _gCost = new float[gridCapacity];
        _hCost = new float[gridCapacity];
        _parent = new int[gridCapacity];
        _inClosed = new bool[gridCapacity];
        _heapF = new float[gridCapacity];
        _heapIdx = new int[gridCapacity];
    }

    /// <summary>
    /// Computes the shortest path from <paramref name="startWorld"/> to <paramref name="endWorld"/>.
    /// </summary>
    /// <param name="grid">The navigation grid.</param>
    /// <param name="startWorld">World-space start position.</param>
    /// <param name="endWorld">World-space destination position.</param>
    /// <param name="result">Pre-allocated path that will be cleared and filled with waypoints (excluding start).</param>
    /// <param name="profile">Agent navigation capabilities. Defaults to <see cref="NavAgentProfile.Default"/>.</param>
    /// <returns><c>true</c> if a path was found; <c>false</c> if the destination is unreachable.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the grid size exceeds the pre-allocated capacity.</exception>
    public bool FindPath(NavGrid grid, Vector2 startWorld, Vector2 endWorld,
                         NavPath result, NavAgentProfile profile = default)
    {
        result.Clear();

        int cellCount = grid.Width * grid.Height;
        if (cellCount > _capacity)
            throw new InvalidOperationException(
                $"Grid size {cellCount} exceeds Pathfinder capacity {_capacity}. Increase gridCapacity in the constructor.");

        grid.WorldToGrid(startWorld, out int startX, out int startY);
        grid.WorldToGrid(endWorld, out int endX, out int endY);

        if (!grid.IsInBounds(startX, startY) || !grid.IsInBounds(endX, endY))
            return false;

        int startIdx = startY * grid.Width + startX;
        int endIdx = endY * grid.Width + endX;

        if (startIdx == endIdx)
            return true;

        // Reset per-search state only over the used portion of the arrays
        Array.Fill(_gCost, float.MaxValue, 0, cellCount);
        Array.Clear(_hCost, 0, cellCount);
        Array.Fill(_parent, -1, 0, cellCount);
        Array.Clear(_inClosed, 0, cellCount);
        _heapSize = 0;

        _gCost[startIdx] = 0f;
        _hCost[startIdx] = Heuristic(startX, startY, endX, endY, profile.AllowDiagonal);
        HeapPush(startIdx, _hCost[startIdx]);

        while (_heapSize > 0)
        {
            int current = HeapPop();

            if (_inClosed[current]) continue;
            _inClosed[current] = true;

            if (current == endIdx)
            {
                ReconstructPath(grid, current, startIdx, result);
                return true;
            }

            int cx = current % grid.Width;
            int cy = current / grid.Width;
            int dirCount = profile.AllowDiagonal ? 8 : 4;

            for (int d = 0; d < dirCount; d++)
            {
                int dx = _dirX[d];
                int dy = _dirY[d];
                int nx = cx + dx;
                int ny = cy + dy;

                if (!grid.IsInBounds(nx, ny)) continue;

                int neighborIdx = ny * grid.Width + nx;
                if (_inClosed[neighborIdx]) continue;

                // Prevent corner-cutting for diagonal moves
                if (dx != 0 && dy != 0)
                {
                    if (!IsCellTraversable(grid, cx + dx, cy, profile) ||
                        !IsCellTraversable(grid, cx, cy + dy, profile))
                        continue;
                }

                float moveCost = GetMoveCost(grid, nx, ny, dy, dx != 0 && dy != 0, profile);
                if (moveCost < 0f) continue;

                float newG = _gCost[current] + moveCost;
                if (newG < _gCost[neighborIdx])
                {
                    _gCost[neighborIdx] = newG;
                    if (_hCost[neighborIdx] == 0f)
                        _hCost[neighborIdx] = Heuristic(nx, ny, endX, endY, profile.AllowDiagonal);
                    _parent[neighborIdx] = current;
                    HeapPush(neighborIdx, newG + _hCost[neighborIdx]);
                }
            }
        }

        return false;
    }

    #region Traversal helpers
    private static bool IsCellTraversable(NavGrid grid, int x, int y, NavAgentProfile profile)
    {
        if (!grid.IsInBounds(x, y)) return false;
        NavCell cell = grid.GetCell(x, y);
        if (cell.IsWalkable) return true;
        return cell.ObstacleHeight > 0f && profile.JumpHeight >= cell.ObstacleHeight;
    }

    private static float GetMoveCost(NavGrid grid, int nx, int ny, int dy,
                                     bool isDiagonal, NavAgentProfile profile)
    {
        NavCell cell = grid.GetCell(nx, ny);
        float baseCost;

        if (cell.IsWalkable)
        {
            baseCost = (isDiagonal ? SqrtTwo : 1f) * cell.MovementCost;
        }
        else if (cell.ObstacleHeight > 0f && profile.JumpHeight >= cell.ObstacleHeight)
        {
            baseCost = (isDiagonal ? SqrtTwo : 1f) * cell.MovementCost * profile.JumpCostMultiplier;
        }
        else
        {
            return -1f;
        }

        if (grid.Mode == NavigationMode.SideScroll)
        {
            if (dy > 0) baseCost *= profile.VerticalAscentCostMultiplier;
            else if (dy < 0) baseCost *= DescentMultiplier;
        }

        return baseCost;
    }

    private static float Heuristic(int x1, int y1, int x2, int y2, bool allowDiagonal)
    {
        int dx = Math.Abs(x1 - x2);
        int dy = Math.Abs(y1 - y2);
        if (!allowDiagonal) return dx + dy;
        return (dx + dy) + (SqrtTwo - 2f) * Math.Min(dx, dy);
    }

    private void ReconstructPath(NavGrid grid, int endIdx, int startIdx, NavPath path)
    {
        int current = endIdx;
        while (current != startIdx)
        {
            path.AddWaypoint(grid.GridToWorld(current % grid.Width, current / grid.Width));
            int next = _parent[current];
            if (next < 0) break;
            current = next;
        }
        path.Reverse();
    }
    #endregion

    #region Min-heap
    private void HeapPush(int cellIdx, float f)
    {
        int i = _heapSize;
        _heapIdx[_heapSize] = cellIdx;
        _heapF[_heapSize] = f;
        _heapSize++;
        BubbleUp(i);
    }

    private int HeapPop()
    {
        int result = _heapIdx[0];
        _heapSize--;
        if (_heapSize > 0)
        {
            _heapIdx[0] = _heapIdx[_heapSize];
            _heapF[0] = _heapF[_heapSize];
            SinkDown(0);
        }
        return result;
    }

    private void BubbleUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (_heapF[i] < _heapF[parent])
            {
                Swap(i, parent);
                i = parent;
            }
            else break;
        }
    }

    private void SinkDown(int i)
    {
        while (true)
        {
            int smallest = i;
            int left = (i << 1) | 1;
            int right = left + 1;

            if (left < _heapSize && _heapF[left] < _heapF[smallest]) smallest = left;
            if (right < _heapSize && _heapF[right] < _heapF[smallest]) smallest = right;

            if (smallest == i) break;
            Swap(i, smallest);
            i = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (_heapIdx[a], _heapIdx[b]) = (_heapIdx[b], _heapIdx[a]);
        (_heapF[a], _heapF[b]) = (_heapF[b], _heapF[a]);
    }
    #endregion
}
