namespace Alca.MonoGame.Kernel.Physics;

/// <summary>
/// Optional helper that manages collision filtering across a set of registered colliders.
/// Use <see cref="SetCanCollide"/> to allow or prevent interactions between two categories
/// across all colliders in the matrix without having to update masks individually.
/// </summary>
public sealed class CollisionMatrix
{
    private readonly List<Collider2D> _colliders = new(16);

    /// <summary>Gets the number of registered colliders.</summary>
    public int Count => _colliders.Count;

    /// <summary>Registers a collider so it is included in future <see cref="SetCanCollide"/> calls.</summary>
    public void Register(Collider2D collider)
    {
        if (!_colliders.Contains(collider))
            _colliders.Add(collider);
    }

    /// <summary>Removes a previously registered collider.</summary>
    public void Unregister(Collider2D collider) => _colliders.Remove(collider);

    /// <summary>
    /// Allows or prevents collisions between two categories for all registered colliders.
    /// Updates the <see cref="Collider2D.Mask"/> of every registered collider that belongs
    /// to category <paramref name="a"/> (adds or removes <paramref name="b"/> from its mask)
    /// and vice versa.
    /// </summary>
    /// <param name="a">First category.</param>
    /// <param name="b">Second category.</param>
    /// <param name="value"><c>true</c> to allow collisions; <c>false</c> to prevent them.</param>
    public void SetCanCollide(CollisionCategory a, CollisionCategory b, bool value)
    {
        for (int i = 0; i < _colliders.Count; i++)
        {
            var c = _colliders[i];

            if ((c.Layer & a) != 0)
            {
                if (value)
                    c.Mask |= b;
                else
                    c.Mask &= ~b;
            }

            if ((c.Layer & b) != 0)
            {
                if (value)
                    c.Mask |= a;
                else
                    c.Mask &= ~a;
            }
        }
    }
}
