namespace Alca.MonoGame.Kernel.Physics.Triggers;

/// <summary>Describes an overlap event between two <see cref="TriggerZone2D"/> volumes.</summary>
public readonly struct TriggerOverlapInfo
{
    /// <summary>Gets the trigger zone that received the event.</summary>
    public TriggerZone2D Self { get; }

    /// <summary>Gets the other trigger zone that caused the event.</summary>
    public TriggerZone2D Other { get; }

    /// <summary>Initializes a new <see cref="TriggerOverlapInfo"/> with the two participating zones.</summary>
    public TriggerOverlapInfo(TriggerZone2D self, TriggerZone2D other)
    {
        Self = self;
        Other = other;
    }
}
