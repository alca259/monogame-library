namespace Alca.MonoGame.Kernel.Navigation;

/// <summary>
/// Defines the navigation capabilities of an agent, passed to <see cref="Pathfinder"/> to
/// compute traversal costs. Value type — no heap allocation.
/// </summary>
public readonly struct NavAgentProfile
{
    /// <summary>Gets the maximum obstacle height this agent can jump over. 0 = cannot jump.</summary>
    public float JumpHeight { get; init; }

    /// <summary>Gets the cost multiplier applied when traversing a jumpable obstacle cell. Default is 2.0.</summary>
    public float JumpCostMultiplier { get; init; }

    /// <summary>
    /// Gets the cost multiplier for upward movement in <see cref="NavigationMode.SideScroll"/> mode.
    /// Simulates the effort of fighting gravity. Default is 1.5.
    /// </summary>
    public float VerticalAscentCostMultiplier { get; init; }

    /// <summary>Gets a value indicating whether diagonal movement is allowed. Default is true.</summary>
    public bool AllowDiagonal { get; init; }

    /// <summary>Gets the default profile: no jump, diagonal movement enabled.</summary>
    public static NavAgentProfile Default => new()
    {
        JumpHeight = 0f,
        JumpCostMultiplier = 2.0f,
        VerticalAscentCostMultiplier = 1.5f,
        AllowDiagonal = true
    };
}
