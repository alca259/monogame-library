using Alca.MonoGame.Kernel.Navigation;

namespace Alca.MonoGame.Kernel.UnitTests.Navigation;

public sealed class NavPathTests
{
    [Fact]
    public void Clear_ResetsCount()
    {
        var path = new NavPath(10);
        path.AddWaypoint(new Vector2(1, 2));
        path.AddWaypoint(new Vector2(3, 4));

        path.Clear();

        Assert.Equal(0, path.Count);
        Assert.True(path.IsEmpty);
    }

    [Fact]
    public void GetWaypoint_ValidIndex_ReturnsCorrectPosition()
    {
        var path = new NavPath(10);
        var expected = new Vector2(5f, 10f);
        path.AddWaypoint(expected);

        Vector2 actual = path.GetWaypoint(0);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddWaypoint_ExceedsCapacity_DoesNotThrow()
    {
        var path = new NavPath(3);
        path.AddWaypoint(new Vector2(1, 0));
        path.AddWaypoint(new Vector2(2, 0));
        path.AddWaypoint(new Vector2(3, 0));

        // Should not throw; silently ignored
        path.AddWaypoint(new Vector2(4, 0));

        Assert.Equal(3, path.Count);
    }

    [Fact]
    public void GetWaypoint_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var path = new NavPath(10);
        path.AddWaypoint(new Vector2(1, 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => path.GetWaypoint(1));
    }

    [Fact]
    public void IsEmpty_AfterClear_ReturnsTrue()
    {
        var path = new NavPath(10);
        path.AddWaypoint(Vector2.Zero);
        path.Clear();

        Assert.True(path.IsEmpty);
    }
}
