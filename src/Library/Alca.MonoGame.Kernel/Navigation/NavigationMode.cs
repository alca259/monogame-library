namespace Alca.MonoGame.Kernel.Navigation;

/// <summary>Defines which world axes map to the navigation grid.</summary>
public enum NavigationMode
{
    /// <summary>Top-down view. Grid maps world X and Y axes (horizontal plane). World height is on the Z axis.</summary>
    TopDown,
    /// <summary>Side-scrolling view. Grid maps world X and Y axes. World Y represents screen height.</summary>
    SideScroll
}
