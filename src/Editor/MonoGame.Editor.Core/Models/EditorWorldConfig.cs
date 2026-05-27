namespace MonoGame.Editor.Core.Models;

/// <summary>Configures optional GameWorld subsystems for a scene.</summary>
public sealed class EditorWorldConfig
{
    // ── Physics 2D ────────────────────────────────────────────────────────────

    /// <summary>Gets or sets a value indicating whether Physics2DWorld is enabled for this scene.</summary>
    public bool UsePhysics2D  { get; set; }

    /// <summary>Gets or sets the X component of the gravity vector (pixels/s²). Default: 0.</summary>
    public float GravityX     { get; set; } = 0f;

    /// <summary>Gets or sets the Y component of the gravity vector (pixels/s²). Default: -9.8.</summary>
    public float GravityY     { get; set; } = -9.8f;

    // ── Lighting ──────────────────────────────────────────────────────────────

    /// <summary>Gets or sets a value indicating whether LightingWorld is enabled for this scene.</summary>
    public bool UseLighting   { get; set; }

    /// <summary>
    /// Gets or sets the ambient light color as RGBA bytes [R, G, B, A].
    /// Default: black (0, 0, 0, 255).
    /// </summary>
    public int[] AmbientColorRgba { get; set; } = [0, 0, 0, 255];

    // ── Navigation ────────────────────────────────────────────────────────────

    /// <summary>Gets or sets a value indicating whether NavGrid and Pathfinder are enabled for this scene.</summary>
    public bool UseNavigation { get; set; }

    /// <summary>Gets or sets the width of the navigation grid in cells.</summary>
    public int NavGridWidth   { get; set; } = 32;

    /// <summary>Gets or sets the height of the navigation grid in cells.</summary>
    public int NavGridHeight  { get; set; } = 32;

    /// <summary>Gets or sets the size of each navigation cell in pixels.</summary>
    public float NavGridCellSize { get; set; } = 32f;

    /// <summary>Gets or sets the X world-space origin of the navigation grid.</summary>
    public float NavGridOriginX { get; set; }

    /// <summary>Gets or sets the Y world-space origin of the navigation grid.</summary>
    public float NavGridOriginY { get; set; }

    // ── Audio ─────────────────────────────────────────────────────────────────

    /// <summary>Gets or sets a value indicating whether AudioController is enabled for this scene.</summary>
    public bool UseAudio      { get; set; }
}
