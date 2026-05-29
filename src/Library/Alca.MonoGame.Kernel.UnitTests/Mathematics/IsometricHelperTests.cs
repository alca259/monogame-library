namespace Alca.MonoGame.Kernel.UnitTests.Mathematics;

/// <summary>Unit tests for <see cref="IsometricHelper"/> coordinate projection.</summary>
public sealed class IsometricHelperTests
{
    private static readonly IsometricHelper Sut64x32 = new IsometricHelper(64f, 32f);

    // ── WorldToScreen ────────────────────────────────────────────────────────

    [Fact]
    public void WorldToScreen_Origin_ReturnsZero()
    {
        Vector2 result = Sut64x32.WorldToScreen(Vector2.Zero);

        Assert.Equal(0f, result.X, 0.0001f);
        Assert.Equal(0f, result.Y, 0.0001f);
    }

    [Fact]
    public void WorldToScreen_UnitX_ReturnsPositiveXPositiveY()
    {
        Vector2 result = Sut64x32.WorldToScreen(new Vector2(1f, 0f));

        // screen.X = (1 - 0) * 32 = 32; screen.Y = (1 + 0) * 16 = 16
        Assert.Equal(32f, result.X, 0.0001f);
        Assert.Equal(16f, result.Y, 0.0001f);
    }

    [Fact]
    public void WorldToScreen_UnitY_ReturnsNegativeXPositiveY()
    {
        Vector2 result = Sut64x32.WorldToScreen(new Vector2(0f, 1f));

        // screen.X = (0 - 1) * 32 = -32; screen.Y = (0 + 1) * 16 = 16
        Assert.Equal(-32f, result.X, 0.0001f);
        Assert.Equal(16f, result.Y, 0.0001f);
    }

    // ── ScreenToWorld ────────────────────────────────────────────────────────

    [Fact]
    public void ScreenToWorld_Origin_ReturnsZero()
    {
        Vector2 result = Sut64x32.ScreenToWorld(Vector2.Zero);

        Assert.Equal(0f, result.X, 0.0001f);
        Assert.Equal(0f, result.Y, 0.0001f);
    }

    [Fact]
    public void ScreenToWorld_IsInverseOfWorldToScreen_AtArbitraryPoint()
    {
        Vector2 world = new Vector2(3f, 5f);
        Vector2 screen = Sut64x32.WorldToScreen(world);
        Vector2 back = Sut64x32.ScreenToWorld(screen);

        Assert.Equal(world.X, back.X, 0.001f);
        Assert.Equal(world.Y, back.Y, 0.001f);
    }

    // ── DepthFromWorldY ──────────────────────────────────────────────────────

    [Fact]
    public void DepthFromWorldY_Zero_ReturnsOne()
    {
        float depth = Sut64x32.DepthFromWorldY(0f, 1000f);
        Assert.Equal(1f, depth, 0.0001f);
    }

    [Fact]
    public void DepthFromWorldY_Max_ReturnsZero()
    {
        float depth = Sut64x32.DepthFromWorldY(1000f, 1000f);
        Assert.Equal(0f, depth, 0.0001f);
    }

    [Fact]
    public void DepthFromWorldY_Half_ReturnsPointFive()
    {
        float depth = Sut64x32.DepthFromWorldY(500f, 1000f);
        Assert.Equal(0.5f, depth, 0.0001f);
    }

    [Fact]
    public void DepthFromWorldY_AboveMax_ClampedToZero()
    {
        float depth = Sut64x32.DepthFromWorldY(2000f, 1000f);
        Assert.Equal(0f, depth, 0.0001f);
    }

    // ── Default static instance ──────────────────────────────────────────────

    [Fact]
    public void Default_IsNotNull()
    {
        Assert.NotNull(IsometricHelper.Default);
    }

    [Fact]
    public void Default_UsesDefaultTileDimensions()
    {
        Vector2 result = IsometricHelper.Default.WorldToScreen(new Vector2(1f, 0f));
        float expectedX = IsometricHelper.DefaultTileWidth / 2f;
        Assert.Equal(expectedX, result.X, 0.0001f);
    }

    // ── DepthFromPosition ────────────────────────────────────────────────────

    [Fact]
    public void DepthFromPosition_UsesYCoordinate()
    {
        Vector2 worldSize = new Vector2(100f, 100f);
        float depthAt50Y = Sut64x32.DepthFromPosition(new Vector2(0f, 50f), worldSize);
        Assert.Equal(0.5f, depthAt50Y, 0.0001f);
    }
}
