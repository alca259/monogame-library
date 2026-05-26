namespace Alca.MonoGame.Kernel.Physics;

/// <summary>
/// Flags enum that defines named collision layers for 2D physics filtering.
/// Combine with <see cref="Collider2D.Layer"/> and <see cref="Collider2D.Mask"/> to control which
/// colliders can interact with each other.
/// </summary>
[Flags]
public enum CollisionCategory : ushort
{
    /// <summary>No category. Colliders with this layer will not participate in filtered collisions.</summary>
    None = 0x0000,

    /// <summary>Default category applied to all new colliders.</summary>
    Default = 0x0001,

    /// <summary>Player-controlled entities.</summary>
    Player = 0x0002,

    /// <summary>Enemy entities.</summary>
    Enemy = 0x0004,

    /// <summary>Projectile entities such as bullets and arrows.</summary>
    Projectile = 0x0008,

    /// <summary>Trigger volumes that detect overlap without physical response.</summary>
    Trigger = 0x0010,

    /// <summary>Static terrain such as walls, floors, and platforms.</summary>
    Terrain = 0x0020,

    /// <summary>All categories combined. A collider with this mask responds to every other layer.</summary>
    All = 0xFFFF,
}
