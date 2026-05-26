namespace Alca.MonoGame.Kernel.Navigation;

/// <summary>
/// A pre-allocated, reusable container for navigation waypoints.
/// Avoids heap allocations when used repeatedly across frames.
/// </summary>
public sealed class NavPath
{
    private readonly Vector2[] _waypoints;
    private int _count;

    /// <summary>Gets the number of waypoints currently stored.</summary>
    public int Count => _count;

    /// <summary>Gets a value indicating whether this path contains no waypoints.</summary>
    public bool IsEmpty => _count == 0;

    /// <summary>Creates a new NavPath with the given maximum capacity.</summary>
    /// <param name="maxCapacity">Maximum number of waypoints. Overflow is silently ignored. Default is 512.</param>
    public NavPath(int maxCapacity = 512)
    {
        _waypoints = new Vector2[maxCapacity];
        _count = 0;
    }

    /// <summary>Returns the waypoint at the given index.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
    public Vector2 GetWaypoint(int index)
    {
        if ((uint)index >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _waypoints[index];
    }

    /// <summary>Removes all waypoints without reallocating memory.</summary>
    public void Clear() => _count = 0;

    internal void AddWaypoint(Vector2 point)
    {
        if (_count >= _waypoints.Length) return;
        _waypoints[_count++] = point;
    }

    internal void Reverse()
    {
        int left = 0;
        int right = _count - 1;
        while (left < right)
        {
            (_waypoints[left], _waypoints[right]) = (_waypoints[right], _waypoints[left]);
            left++;
            right--;
        }
    }
}
