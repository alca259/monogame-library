using Alca.MonoGame.Kernel.Lighting;
using Alca.MonoGame.Kernel.Lighting.DayNight;

namespace Alca.MonoGame.Kernel.UnitTests.Lighting.DayNight;

public sealed class DayNightCycleTests
{
    private static GameTime ElapsedTime(double seconds) =>
        new(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_DefaultTime_IsMidnight()
    {
        var cycle = new DayNightCycle(DayNightProfile.Default);

        Assert.Equal(0f, cycle.CurrentTime.Hours, 6);
    }

    // ── SetTime ───────────────────────────────────────────────────────────────

    [Fact]
    public void SetTime_ChangesCurrentTime()
    {
        var cycle = new DayNightCycle(DayNightProfile.Default);

        cycle.SetTime(TimeOfDay.Noon);

        Assert.Equal(12f, cycle.CurrentTime.Hours, 3);
    }

    // ── Paused ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_Paused_DoesNotAdvanceTime()
    {
        var cycle = new DayNightCycle(DayNightProfile.Default)
        {
            Paused = true
        };
        cycle.SetTime(TimeOfDay.FromHours(5f));

        cycle.Update(ElapsedTime(60.0));

        Assert.Equal(5f, cycle.CurrentTime.Hours, 3);
    }

    [Fact]
    public void TimeScale_Zero_BehavesLikePaused()
    {
        var cycle = new DayNightCycle(DayNightProfile.Default)
        {
            TimeScale = 0f
        };
        cycle.SetTime(TimeOfDay.FromHours(3f));

        cycle.Update(ElapsedTime(60.0));

        Assert.Equal(3f, cycle.CurrentTime.Hours, 3);
    }

    // ── Time advancement ──────────────────────────────────────────────────────

    [Fact]
    public void Update_WithTimeScale_AdvancesProportionally()
    {
        // 24-second day → 1 h/s of game time at TimeScale=1.
        var profile = new DayNightProfile(dayDurationSeconds: 24f);
        var cycle = new DayNightCycle(profile) { TimeScale = 2f };

        cycle.SetTime(TimeOfDay.Midnight);

        // Elapsed 1 real second, TimeScale=2 → 2 game-hours advanced.
        cycle.Update(ElapsedTime(1.0));

        Assert.Equal(2f, cycle.CurrentTime.Hours, 3);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    [Fact]
    public void OnSunrise_CrossingSunriseThreshold_Fires()
    {
        // Use a short day so we can advance in fractional seconds.
        // dayDurationSeconds=24 → 1 h/s of game time at TimeScale=1.
        var profile = new DayNightProfile(dayDurationSeconds: 24f);
        var cycle = new DayNightCycle(profile);

        // Position just before sunrise (5.9h).
        cycle.SetTime(TimeOfDay.FromHours(5.9f));

        bool sunriseFired = false;
        cycle.OnSunrise = () => sunriseFired = true;

        // Advance 0.2 real seconds → +0.2 game-hours → crosses 6h.
        cycle.Update(ElapsedTime(0.2));

        Assert.True(sunriseFired);
    }

    [Fact]
    public void OnSunrise_NotFiredAgainBeforeReset()
    {
        var profile = new DayNightProfile(dayDurationSeconds: 24f);
        var cycle = new DayNightCycle(profile);

        cycle.SetTime(TimeOfDay.FromHours(5.9f));

        int callCount = 0;
        cycle.OnSunrise = () => callCount++;

        // Cross sunrise once.
        cycle.Update(ElapsedTime(0.2));
        // Advance further still in daytime — should NOT fire again.
        cycle.Update(ElapsedTime(1.0));

        Assert.Equal(1, callCount);
    }

    // ── LightingWorld integration ─────────────────────────────────────────────

    [Fact]
    public void Update_WithLightingWorld_UpdatesAmbientColor()
    {
        var lightingWorld = new LightingWorld
        {
            AmbientColor = Color.Black
        };
        var cycle = new DayNightCycle(DayNightProfile.Default, lightingWorld);

        // SetTime triggers an immediate lighting update.
        cycle.SetTime(TimeOfDay.Noon);

        // At noon the default profile has a bright ambient color — not pure black.
        Assert.NotEqual(Color.Black, lightingWorld.AmbientColor);
    }
}
