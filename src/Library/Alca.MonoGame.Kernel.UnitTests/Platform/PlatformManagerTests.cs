using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.Platform;

namespace Alca.MonoGame.Kernel.UnitTests.Platform;

public sealed class PlatformManagerTests
{
    [Fact]
    public void IsDesktop_WhenPlatformIsDesktop_ReturnsTrue()
    {
        var resolution = new ResolutionManager(1920, 1080);
        var manager = new PlatformManager(PlatformType.Desktop, resolution);

        Assert.True(manager.IsDesktop);
        Assert.False(manager.IsMobile);
        Assert.False(manager.IsConsole);
    }

    [Fact]
    public void IsMobile_WhenPlatformIsMobile_ReturnsTrue()
    {
        var resolution = new ResolutionManager(1920, 1080);
        var manager = new PlatformManager(PlatformType.Mobile, resolution);

        Assert.False(manager.IsDesktop);
        Assert.True(manager.IsMobile);
        Assert.False(manager.IsConsole);
    }

    [Fact]
    public void IsConsole_WhenPlatformIsConsole_ReturnsTrue()
    {
        var resolution = new ResolutionManager(1920, 1080);
        var manager = new PlatformManager(PlatformType.Console, resolution);

        Assert.False(manager.IsDesktop);
        Assert.False(manager.IsMobile);
        Assert.True(manager.IsConsole);
    }

    [Fact]
    public void VirtualWidth_DelegatesToResolutionManager()
    {
        var resolution = new ResolutionManager(1280, 720);
        var manager = new PlatformManager(PlatformType.Desktop, resolution);

        Assert.Equal(1280, manager.VirtualWidth);
    }

    [Fact]
    public void VirtualHeight_DelegatesToResolutionManager()
    {
        var resolution = new ResolutionManager(1280, 720);
        var manager = new PlatformManager(PlatformType.Desktop, resolution);

        Assert.Equal(720, manager.VirtualHeight);
    }

    [Fact]
    public void CurrentPlatform_MatchesConstructorArgument()
    {
        var resolution = new ResolutionManager(1920, 1080);

        Assert.Equal(PlatformType.Desktop, new PlatformManager(PlatformType.Desktop, resolution).CurrentPlatform);
        Assert.Equal(PlatformType.Mobile, new PlatformManager(PlatformType.Mobile, resolution).CurrentPlatform);
        Assert.Equal(PlatformType.Console, new PlatformManager(PlatformType.Console, resolution).CurrentPlatform);
    }

    [Fact]
    public void SupportedOrientations_DefaultIsDefault()
    {
        var resolution = new ResolutionManager(1920, 1080);
        var manager = new PlatformManager(PlatformType.Mobile, resolution);

        Assert.Equal(DisplayOrientation.Default, manager.SupportedOrientations);
    }

    [Fact]
    public void SupportedOrientations_CanBeChanged()
    {
        var resolution = new ResolutionManager(1920, 1080);
        var manager = new PlatformManager(PlatformType.Mobile, resolution)
        {
            SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight
        };

        Assert.Equal(DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight, manager.SupportedOrientations);
    }

    [Fact]
    public void AppPaused_EventCanBeSubscribed()
    {
        var resolution = new ResolutionManager(1920, 1080);
        var manager = new PlatformManager(PlatformType.Desktop, resolution);
        bool fired = false;
        manager.AppPaused += () => fired = true;

        // Internal constructor does not hook game events; verify subscription doesn't throw.
        Assert.False(fired);
    }

    [Fact]
    public void ScreenResized_EventCanBeSubscribed()
    {
        var resolution = new ResolutionManager(1920, 1080);
        var manager = new PlatformManager(PlatformType.Desktop, resolution);
        bool fired = false;
        manager.ScreenResized += () => fired = true;

        Assert.False(fired);
    }

    [Fact]
    public void Dispose_DoesNotThrowWhenGameIsNull()
    {
        var resolution = new ResolutionManager(1920, 1080);
        var manager = new PlatformManager(PlatformType.Desktop, resolution);

        var exception = Record.Exception(manager.Dispose);
        Assert.Null(exception);
    }
}
