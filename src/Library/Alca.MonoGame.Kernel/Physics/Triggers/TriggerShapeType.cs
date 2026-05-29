namespace Alca.MonoGame.Kernel.Physics.Triggers;

/// <summary>Defines the collision shape used by a <see cref="TriggerZone2D"/> for overlap tests.</summary>
public enum TriggerShapeType
{
    /// <summary>Axis-aligned bounding box (rectangle) overlap test.</summary>
    AABB,

    /// <summary>Circular radius-based overlap test.</summary>
    Circle
}
