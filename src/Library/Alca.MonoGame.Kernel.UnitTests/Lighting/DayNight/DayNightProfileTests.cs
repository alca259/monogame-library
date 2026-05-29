using Alca.MonoGame.Kernel.Lighting.DayNight;

namespace Alca.MonoGame.Kernel.UnitTests.Lighting.DayNight;

public sealed class DayNightProfileTests
{
    // ── Default profile ───────────────────────────────────────────────────────

    [Fact]
    public void Default_IsNotNull()
    {
        Assert.NotNull(DayNightProfile.Default);
    }

    [Fact]
    public void DayDurationSeconds_Default_IsPositive()
    {
        Assert.True(DayNightProfile.Default.DayDurationSeconds > 0f);
    }

    // ── Sample ────────────────────────────────────────────────────────────────

    [Fact]
    public void Sample_AtMidnight_ReturnsFirstKeyframeColor()
    {
        var profile = DayNightProfile.Default;

        DayNightKeyframe kf = profile.Sample(TimeOfDay.Midnight);

        // At exactly midnight (0h) the sample returns the Midnight keyframe values.
        Assert.Equal(profile.Midnight.AmbientColor, kf.AmbientColor);
    }

    [Fact]
    public void Sample_AtNoon_ReturnsBrightKeyframe()
    {
        var profile = DayNightProfile.Default;

        DayNightKeyframe kf = profile.Sample(TimeOfDay.Noon);

        // Default Noon keyframe has intensity 1.0 — the brightest keyframe.
        Assert.Equal(1.0f, kf.AmbientIntensity, 3);
    }

    [Fact]
    public void Sample_BetweenKeyframes_InterpolatesCorrectly()
    {
        // Custom profile with known colours so interpolation is deterministic.
        var midnight = new DayNightKeyframe(TimeOfDay.Midnight, new Color(0, 0, 0), 0f, 0f);
        var sunrise  = new DayNightKeyframe(TimeOfDay.Sunrise,  new Color(0, 0, 0), 1f, 0f);
        var noon     = new DayNightKeyframe(TimeOfDay.Noon,     new Color(0, 0, 0), 1f, 0f);
        var sunset   = new DayNightKeyframe(TimeOfDay.Sunset,   new Color(0, 0, 0), 1f, 0f);

        var profile = new DayNightProfile(midnight, sunrise, noon, sunset);

        // At 3h (midpoint of Midnight→Sunrise), intensity should interpolate to 0.5.
        DayNightKeyframe kf = profile.Sample(TimeOfDay.FromHours(3f));

        Assert.Equal(0.5f, kf.AmbientIntensity, 3);
    }

    [Fact]
    public void Sample_AtSunset_ReturnsSunsetKeyframe()
    {
        var profile = DayNightProfile.Default;

        DayNightKeyframe kf = profile.Sample(TimeOfDay.Sunset);

        Assert.Equal(profile.Sunset.AmbientColor, kf.AmbientColor);
    }

    [Fact]
    public void Sample_AtSunrise_ReturnsSunriseKeyframe()
    {
        var profile = DayNightProfile.Default;

        DayNightKeyframe kf = profile.Sample(TimeOfDay.Sunrise);

        Assert.Equal(profile.Sunrise.AmbientColor, kf.AmbientColor);
    }

    // ── Keyframes accessible ─────────────────────────────────────────────────

    [Fact]
    public void Profile_MidnightKeyframe_HasCorrectTime()
    {
        var profile = DayNightProfile.Default;

        Assert.Equal(0f, profile.Midnight.Time.Hours, 6);
    }

    [Fact]
    public void Profile_NoonKeyframe_HasCorrectTime()
    {
        var profile = DayNightProfile.Default;

        Assert.Equal(12f, profile.Noon.Time.Hours, 6);
    }
}
