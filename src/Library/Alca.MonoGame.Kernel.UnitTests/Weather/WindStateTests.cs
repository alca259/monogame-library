using Alca.MonoGame.Kernel.Weather;

namespace Alca.MonoGame.Kernel.UnitTests.Weather;

public sealed class WindStateTests
{
    [Fact]
    public void IsCalm_WhenSpeedIsZero_ReturnsTrue()
    {
        var wind = new WindState { SpeedKmh = 0f };
        Assert.True(wind.IsCalm);
    }

    [Fact]
    public void IsCalm_WhenSpeedIsAtThreshold_ReturnsTrue()
    {
        var wind = new WindState { SpeedKmh = 0.01f };
        Assert.True(wind.IsCalm);
    }

    [Fact]
    public void IsCalm_WhenSpeedIsAboveThreshold_ReturnsFalse()
    {
        var wind = new WindState { SpeedKmh = 5f };
        Assert.False(wind.IsCalm);
    }

    [Fact]
    public void ComputeEffectiveForce_WhenCalm_ReturnsZero()
    {
        var wind = new WindState
        {
            Direction = Vector2.UnitX,
            SpeedKmh  = 0f,
            Turbulence = 0.5f
        };

        Vector2 force = wind.ComputeEffectiveForce(10f);

        Assert.Equal(Vector2.Zero, force);
    }

    [Fact]
    public void ComputeEffectiveForce_NoTurbulence_ApproximatesBaseSpeed()
    {
        var wind = new WindState
        {
            Direction  = Vector2.UnitX,
            SpeedKmh   = 10f,
            Turbulence = 0f
        };

        Vector2 force = wind.ComputeEffectiveForce(0f, worldUnitsPerKmh: 1f);

        // With turbulence=0, effectiveSpeed = baseSpeed * 1 = 10 * 1 = 10
        Assert.Equal(10f, force.X, 3);
        Assert.Equal(0f,  force.Y, 3);
    }

    [Fact]
    public void ComputeEffectiveForce_WorldUnitsPerKmh_ScalesResult()
    {
        var wind = new WindState
        {
            Direction  = Vector2.UnitX,
            SpeedKmh   = 10f,
            Turbulence = 0f
        };

        Vector2 forceAt1  = wind.ComputeEffectiveForce(0f, worldUnitsPerKmh: 1f);
        Vector2 forceAt2  = wind.ComputeEffectiveForce(0f, worldUnitsPerKmh: 2f);

        Assert.Equal(forceAt1.X * 2f, forceAt2.X, 3);
    }

    [Fact]
    public void ComputeEffectiveForce_DirectionMaintained()
    {
        var wind = new WindState
        {
            Direction  = Vector2.UnitY,
            SpeedKmh   = 5f,
            Turbulence = 0f
        };

        Vector2 force = wind.ComputeEffectiveForce(0f);

        Assert.Equal(0f, force.X, 3);
        Assert.True(force.Y > 0f);
    }
}
