using Alca.MonoGame.Kernel.Lighting.DayNight;

namespace Alca.MonoGame.Kernel.UnitTests.Lighting.DayNight;

public sealed class TimeOfDayTests
{
    // ── FromHours factory ─────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidHours_StoresCorrectly()
    {
        TimeOfDay t = TimeOfDay.FromHours(12f);

        Assert.Equal(12f, t.Hours, 6);
    }

    [Fact]
    public void FromHours_NegativeValue_Wraps()
    {
        // -1h should wrap to 23h
        TimeOfDay t = TimeOfDay.FromHours(-1f);

        Assert.Equal(23f, t.Hours, 6);
    }

    [Fact]
    public void FromHours_GreaterThan24_Wraps()
    {
        // 25h should wrap to 1h
        TimeOfDay t = TimeOfDay.FromHours(25f);

        Assert.Equal(1f, t.Hours, 6);
    }

    [Fact]
    public void FromHours_Exactly24_WrapsToZero()
    {
        TimeOfDay t = TimeOfDay.FromHours(24f);

        Assert.Equal(0f, t.Hours, 6);
    }

    // ── IsDaytime / IsNighttime ───────────────────────────────────────────────

    [Fact]
    public void IsDaytime_At12_ReturnsTrue()
    {
        Assert.True(TimeOfDay.Noon.IsDaytime);
    }

    [Fact]
    public void IsDaytime_AtMidnight_ReturnsFalse()
    {
        Assert.False(TimeOfDay.Midnight.IsDaytime);
    }

    [Fact]
    public void IsNighttime_AtMidnight_ReturnsTrue()
    {
        Assert.True(TimeOfDay.Midnight.IsNighttime);
    }

    [Theory]
    [InlineData(6f,  true)]   // exactly sunrise boundary → daytime
    [InlineData(19.99f, true)]
    [InlineData(5.99f, false)]
    [InlineData(20f, false)]  // sunset threshold is exclusive
    public void IsDaytime_Boundaries_CorrectResult(float hours, bool expectedDaytime)
    {
        TimeOfDay t = TimeOfDay.FromHours(hours);

        Assert.Equal(expectedDaytime, t.IsDaytime);
    }

    // ── Lerp ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Lerp_MidpointBetweenTwoTimes_ReturnsMidpoint()
    {
        TimeOfDay a = TimeOfDay.FromHours(8f);
        TimeOfDay b = TimeOfDay.FromHours(12f);

        TimeOfDay mid = TimeOfDay.Lerp(a, b, 0.5f);

        // Midpoint of 8h and 12h on forward arc = 10h
        Assert.Equal(10f, mid.Hours, 3);
    }

    [Fact]
    public void Lerp_AtZero_ReturnsFirstTime()
    {
        TimeOfDay a = TimeOfDay.FromHours(4f);
        TimeOfDay b = TimeOfDay.FromHours(16f);

        TimeOfDay result = TimeOfDay.Lerp(a, b, 0f);

        Assert.Equal(4f, result.Hours, 6);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsSecondTime()
    {
        TimeOfDay a = TimeOfDay.FromHours(4f);
        TimeOfDay b = TimeOfDay.FromHours(16f);

        TimeOfDay result = TimeOfDay.Lerp(a, b, 1f);

        Assert.Equal(16f, result.Hours, 6);
    }

    // ── Static factories ──────────────────────────────────────────────────────

    [Fact]
    public void StaticMidnight_HoursIsZero()
    {
        Assert.Equal(0f, TimeOfDay.Midnight.Hours, 6);
    }

    [Fact]
    public void StaticSunrise_HoursIsSix()
    {
        Assert.Equal(6f, TimeOfDay.Sunrise.Hours, 6);
    }

    [Fact]
    public void StaticNoon_HoursIsTwelve()
    {
        Assert.Equal(12f, TimeOfDay.Noon.Hours, 6);
    }

    [Fact]
    public void StaticSunset_HoursIsTwenty()
    {
        Assert.Equal(20f, TimeOfDay.Sunset.Hours, 6);
    }

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_Noon_Returns1200Format()
    {
        Assert.Equal("12:00", TimeOfDay.Noon.ToString());
    }

    [Fact]
    public void ToString_Midnight_Returns0000Format()
    {
        Assert.Equal("00:00", TimeOfDay.Midnight.ToString());
    }
}
