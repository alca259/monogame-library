namespace Alca.MonoGame.Kernel.Physics.Triggers;

/// <summary>
/// Service that manages all registered <see cref="TriggerZone2D"/> volumes and detects pairwise
/// overlaps each frame. Assign to <see cref="ECS.GameWorld.TriggerWorld"/> to enable automatic
/// registration of trigger zones.
/// </summary>
public sealed class TriggerWorld
{
    private readonly List<TriggerZone2D> _zones = new(32);

    // Tracks which (i,j) pairs were overlapping in the previous frame.
    // Key encodes two 32-bit zone indices: (long)i << 32 | (uint)j  where i < j.
    private readonly HashSet<long> _activeOverlaps = new(64);
    private readonly HashSet<long> _newOverlaps = new(64);

    #region Registration
    /// <summary>Registers <paramref name="zone"/> for overlap testing. No-op if already registered.</summary>
    public void Register(TriggerZone2D zone)
    {
        if (!_zones.Contains(zone))
            _zones.Add(zone);
    }

    /// <summary>
    /// Unregisters <paramref name="zone"/> from overlap testing.
    /// Also clears the active-overlap cache to avoid stale index-based keys after list shift.
    /// </summary>
    public void Unregister(TriggerZone2D zone)
    {
        if (_zones.Remove(zone))
        {
            // Indices have shifted; reset tracking to avoid incorrect Enter/Exit events.
            _activeOverlaps.Clear();
            _newOverlaps.Clear();
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// Tests all zone pairs and dispatches Enter/Stay/Exit events.
    /// O(n²) — intended for small to moderate numbers of trigger zones (≤ 200).
    /// Zero heap allocations.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _newOverlaps.Clear();

        int count = _zones.Count;

        for (int i = 0; i < count - 1; i++)
        {
            TriggerZone2D zoneA = _zones[i];
            if (!zoneA.Enabled) continue;

            for (int j = i + 1; j < count; j++)
            {
                TriggerZone2D zoneB = _zones[j];
                if (!zoneB.Enabled) continue;

                // Layer mask test — both must share at least one bit with the other's mask
                if ((zoneA.LayerMask & zoneB.LayerMask) == 0) continue;

                if (!AreOverlapping(zoneA, zoneB)) continue;

                long key = BuildKey(i, j);
                _newOverlaps.Add(key);

                if (_activeOverlaps.Contains(key))
                {
                    // Stay
                    zoneA.RaiseStay(zoneB);
                    zoneB.RaiseStay(zoneA);
                }
                else
                {
                    // Enter
                    zoneA.RaiseEnter(zoneB);
                    zoneB.RaiseEnter(zoneA);
                }
            }
        }

        // Exit — pairs that were active last frame but are no longer overlapping
        foreach (long key in _activeOverlaps)
        {
            if (!_newOverlaps.Contains(key))
            {
                DecodeKey(key, out int idxA, out int idxB);

                if (idxA < count && idxB < count)
                {
                    TriggerZone2D zoneA = _zones[idxA];
                    TriggerZone2D zoneB = _zones[idxB];
                    zoneA.RaiseExit(zoneB);
                    zoneB.RaiseExit(zoneA);
                }
            }
        }

        // Swap sets (copy new into active for next frame)
        _activeOverlaps.Clear();
        foreach (long key in _newOverlaps)
            _activeOverlaps.Add(key);
    }
    #endregion

    #region Private helpers
    private static bool AreOverlapping(TriggerZone2D a, TriggerZone2D b)
    {
        // Both AABB
        if (a.Shape == TriggerShapeType.AABB && b.Shape == TriggerShapeType.AABB)
        {
            Rectangle ra = a.WorldBounds;
            Rectangle rb = b.WorldBounds;
            return ra.Intersects(rb);
        }

        // Both Circle
        if (a.Shape == TriggerShapeType.Circle && b.Shape == TriggerShapeType.Circle)
        {
            float distSq = Vector2.DistanceSquared(a.WorldCenter, b.WorldCenter);
            float combinedRadius = a.Radius + b.Radius;
            return distSq <= combinedRadius * combinedRadius;
        }

        // Mixed: AABB vs Circle — treat the AABB as the bounds, circle as point + radius
        TriggerZone2D aabb = a.Shape == TriggerShapeType.AABB ? a : b;
        TriggerZone2D circle = a.Shape == TriggerShapeType.Circle ? a : b;

        Rectangle rect = aabb.WorldBounds;
        Vector2 center = circle.WorldCenter;

        float nearestX = MathHelper.Clamp(center.X, rect.Left, rect.Right);
        float nearestY = MathHelper.Clamp(center.Y, rect.Top, rect.Bottom);

        float dx = center.X - nearestX;
        float dy = center.Y - nearestY;
        float distSqMixed = dx * dx + dy * dy;

        return distSqMixed <= circle.Radius * circle.Radius;
    }

    private static long BuildKey(int i, int j) => ((long)i << 32) | (uint)j;

    private static void DecodeKey(long key, out int i, out int j)
    {
        i = (int)(key >> 32);
        j = (int)(key & 0xFFFFFFFFL);
    }
    #endregion
}
