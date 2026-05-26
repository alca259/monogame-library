namespace Alca.MonoGame.Kernel.Persistence;

/// <summary>Metadata for a single save slot. Serialized as a JSON sidecar alongside the binary save data.</summary>
public sealed class SaveSlot
{
    /// <summary>Gets or sets the slot identifier (file-system-safe name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the slot was last written.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Gets or sets cumulative in-game play time in seconds.</summary>
    public float PlayTimeSeconds { get; set; }

    /// <summary>Gets or sets the optional path to a screenshot thumbnail, or <c>null</c> if none was saved.</summary>
    public string? ThumbnailPath { get; set; }
}
