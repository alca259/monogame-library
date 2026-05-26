namespace Alca.MonoGame.Kernel.UnitTests.Mathematics;

public sealed class MathUtilsTests
{
    [Fact]
    public void DistanceSquared_ReturnsSqrtlessDistance()
    {
        var a = new Vector2(0f, 0f);
        var b = new Vector2(3f, 4f);
        Assert.Equal(25f, MathUtils.DistanceSquared(a, b));
    }

    [Fact]
    public void AngleBetween_SamePoint_ReturnsZero()
    {
        var a = new Vector2(0f, 0f);
        Assert.Equal(0f, MathUtils.AngleBetween(a, a));
    }

    [Fact]
    public void AngleBetween_PointRight_ReturnsZero()
    {
        float angle = MathUtils.AngleBetween(Vector2.Zero, new Vector2(1f, 0f));
        Assert.Equal(0f, angle, 0.0001f);
    }

    [Fact]
    public void AngleBetween_PointDown_ReturnsPiOver2()
    {
        float angle = MathUtils.AngleBetween(Vector2.Zero, new Vector2(0f, 1f));
        Assert.Equal(MathF.PI / 2f, angle, 0.0001f);
    }

    [Fact]
    public void AngleToVector2_ZeroRadians_ReturnsRight()
    {
        Vector2 v = MathUtils.AngleToVector2(0f);
        Assert.Equal(1f, v.X, 0.0001f);
        Assert.Equal(0f, v.Y, 0.0001f);
    }

    [Fact]
    public void AngleToVector2_PiOver2_ReturnsDown()
    {
        Vector2 v = MathUtils.AngleToVector2(MathF.PI / 2f);
        Assert.Equal(0f, v.X, 0.0001f);
        Assert.Equal(1f, v.Y, 0.0001f);
    }

    [Theory]
    [InlineData(MathF.PI + 0.1f)]
    [InlineData(-MathF.PI - 0.1f)]
    public void WrapAngle_OutOfRange_WrapsToMinusPiToPi(float angle)
    {
        float wrapped = MathUtils.WrapAngle(angle);
        Assert.True(wrapped >= -MathF.PI && wrapped <= MathF.PI);
    }

    [Fact]
    public void Clamp_BelowMin_ReturnsMin()
    {
        Assert.Equal(0f, MathUtils.Clamp(-5f, 0f, 10f));
    }

    [Fact]
    public void Clamp_AboveMax_ReturnsMax()
    {
        Assert.Equal(10f, MathUtils.Clamp(15f, 0f, 10f));
    }

    [Fact]
    public void Clamp_InRange_ReturnsSameValue()
    {
        Assert.Equal(5f, MathUtils.Clamp(5f, 0f, 10f));
    }

    [Fact]
    public void LerpFloat_T0_ReturnsA()
    {
        Assert.Equal(3f, MathUtils.Lerp(3f, 9f, 0f));
    }

    [Fact]
    public void LerpFloat_T1_ReturnsB()
    {
        Assert.Equal(9f, MathUtils.Lerp(3f, 9f, 1f));
    }

    [Fact]
    public void LerpFloat_THalf_ReturnsMidpoint()
    {
        Assert.Equal(6f, MathUtils.Lerp(3f, 9f, 0.5f));
    }

    [Fact]
    public void LerpVector2_THalf_ReturnsMidpoint()
    {
        Vector2 a = new(0f, 0f);
        Vector2 b = new(10f, 20f);
        Vector2 result = MathUtils.Lerp(a, b, 0.5f);
        Assert.Equal(5f, result.X, 0.0001f);
        Assert.Equal(10f, result.Y, 0.0001f);
    }

    [Fact]
    public void LerpColor_T0_ReturnsA()
    {
        Color result = MathUtils.Lerp(Color.Black, Color.White, 0f);
        Assert.Equal(Color.Black.R, result.R);
    }

    [Fact]
    public void LerpColor_T1_ReturnsB()
    {
        Color result = MathUtils.Lerp(Color.Black, Color.White, 1f);
        Assert.Equal(Color.White.R, result.R);
    }

    [Fact]
    public void SmoothStep_T0_ReturnsA()
    {
        Assert.Equal(2f, MathUtils.SmoothStep(2f, 8f, 0f));
    }

    [Fact]
    public void SmoothStep_T1_ReturnsB()
    {
        Assert.Equal(8f, MathUtils.SmoothStep(2f, 8f, 1f));
    }

    [Fact]
    public void SmoothStep_THalf_ReturnsMidpoint()
    {
        Assert.Equal(5f, MathUtils.SmoothStep(2f, 8f, 0.5f));
    }

    [Fact]
    public void MapRange_MidInput_ReturnsMidOutput()
    {
        float result = MathUtils.MapRange(5f, 0f, 10f, 100f, 200f);
        Assert.Equal(150f, result, 0.0001f);
    }

    [Fact]
    public void MapRange_MinInput_ReturnsMinOutput()
    {
        float result = MathUtils.MapRange(0f, 0f, 10f, 100f, 200f);
        Assert.Equal(100f, result, 0.0001f);
    }

    [Fact]
    public void MapRange_MaxInput_ReturnsMaxOutput()
    {
        float result = MathUtils.MapRange(10f, 0f, 10f, 100f, 200f);
        Assert.Equal(200f, result, 0.0001f);
    }
}
