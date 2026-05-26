namespace Alca.MonoGame.Kernel.Navigation;

/// <summary>Synchronizes physics collider AABBs into a <see cref="NavGrid"/> as walkable or non-walkable regions.</summary>
public sealed class NavGridPhysicsSync
{
    private struct SyncEntry
    {
        public Physics.Collider2D Collider;
        public bool Walkable;
    }

    private readonly List<SyncEntry> _registrations = new(32);

    /// <summary>Gets the number of registered colliders.</summary>
    internal int RegistrationCount => _registrations.Count;

    /// <summary>Registers a collider so its AABB is applied to the grid during <see cref="SyncAll"/>.</summary>
    /// <param name="collider">The collider to track.</param>
    /// <param name="walkable">Whether the covered cells should be walkable. Default is false (obstacle).</param>
    public void Register(Physics.Collider2D collider, bool walkable = false)
    {
        _registrations.Add(new SyncEntry { Collider = collider, Walkable = walkable });
    }

    /// <summary>Removes a previously registered collider. Does nothing if not found.</summary>
    public void Unregister(Physics.Collider2D collider)
    {
        for (int i = 0; i < _registrations.Count; i++)
        {
            if (ReferenceEquals(_registrations[i].Collider, collider))
            {
                _registrations.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>Applies all registered collider AABBs to the given grid. Call after physics has stepped.</summary>
    public void SyncAll(NavGrid grid)
    {
        for (int i = 0; i < _registrations.Count; i++)
            ApplyEntry(grid, _registrations[i]);
    }

    /// <summary>Applies a single registered collider's AABB to the grid immediately.</summary>
    public void SyncOne(Physics.Collider2D collider, NavGrid grid)
    {
        for (int i = 0; i < _registrations.Count; i++)
        {
            if (ReferenceEquals(_registrations[i].Collider, collider))
            {
                ApplyEntry(grid, _registrations[i]);
                return;
            }
        }
    }

    private static void ApplyEntry(NavGrid grid, SyncEntry entry)
    {
        entry.Collider.GetWorldBounds(out Vector2 lower, out Vector2 upper);

        grid.WorldToGrid(lower, out int minX, out int minY);
        grid.WorldToGrid(upper, out int maxX, out int maxY);

        if (minX > maxX) (minX, maxX) = (maxX, minX);
        if (minY > maxY) (minY, maxY) = (maxY, minY);

        for (int cy = minY; cy <= maxY; cy++)
            for (int cx = minX; cx <= maxX; cx++)
                grid.SetWalkable(cx, cy, entry.Walkable);
    }
}
