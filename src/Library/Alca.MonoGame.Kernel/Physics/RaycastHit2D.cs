namespace Alca.MonoGame.Kernel.Physics;

/// <summary>Result of a 2D physics raycast or point query.</summary>
public readonly struct RaycastHit2D
{
    /// <summary>Gets the world-space intersection point.</summary>
    public Vector2 Point { get; init; }

    /// <summary>Gets the surface normal at the intersection point.</summary>
    public Vector2 Normal { get; init; }

    /// <summary>Gets the distance from the ray origin to the hit point.</summary>
    public float Distance { get; init; }

    /// <summary>Gets the collider that was hit, or <c>null</c> when no hit occurred.</summary>
    public Collider2D? Collider { get; init; }

    /// <summary>Gets a value indicating whether the raycast hit something.</summary>
    public bool IsHit => Collider is not null;
}
