using Alca.MonoGame.Kernel.Timers;

namespace Alca.MonoGame.Kernel.UnitTests.Timers;

public sealed class TimerManagerTests
{
    private static GameTime MakeTime(float seconds)
        => new(TimeSpan.Zero, TimeSpan.FromSeconds(seconds));

    [Fact]
    public void Schedule_FiresCallbackAfterDelay()
    {
        var tm = new TimerManager();
        int fired = 0;
        tm.Schedule(1f, () => fired++);

        tm.Update(MakeTime(0.5f));
        Assert.Equal(0, fired);

        tm.Update(MakeTime(0.5f));
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Schedule_DoesNotFireBeforeDelay()
    {
        var tm = new TimerManager();
        int fired = 0;
        tm.Schedule(2f, () => fired++);

        tm.Update(MakeTime(1.9f));

        Assert.Equal(0, fired);
    }

    [Fact]
    public void ScheduleRepeating_FiresMultipleTimes()
    {
        var tm = new TimerManager();
        int fired = 0;
        tm.ScheduleRepeating(1f, () => fired++);

        tm.Update(MakeTime(3.5f));

        Assert.Equal(3, fired);
    }

    [Fact]
    public void ScheduleRepeating_WithMaxFires_StopsAfterLimit()
    {
        var tm = new TimerManager();
        int fired = 0;
        tm.ScheduleRepeating(1f, () => fired++, maxFires: 2);

        tm.Update(MakeTime(10f));

        Assert.Equal(2, fired);
    }

    [Fact]
    public void Cancel_PreventsCallback()
    {
        var tm = new TimerManager();
        int fired = 0;
        var timer = tm.Schedule(1f, () => fired++);

        timer.Cancel();
        tm.Update(MakeTime(2f));

        Assert.Equal(0, fired);
    }

    [Fact]
    public void CancelAll_ClearsAllTimers()
    {
        var tm = new TimerManager();
        int fired = 0;
        tm.Schedule(1f, () => fired++);
        tm.Schedule(1f, () => fired++);
        tm.Schedule(1f, () => fired++);

        tm.CancelAll();
        tm.Update(MakeTime(2f));

        Assert.Equal(0, fired);
    }

    [Fact]
    public void Pause_FreezesElapsedTime()
    {
        var tm = new TimerManager();
        int fired = 0;
        var timer = tm.Schedule(1f, () => fired++);

        timer.Pause();
        tm.Update(MakeTime(2f));

        Assert.Equal(0, fired);
    }

    [Fact]
    public void Resume_ContinuesFromPausedTime()
    {
        var tm = new TimerManager();
        int fired = 0;
        var timer = tm.Schedule(1f, () => fired++);

        tm.Update(MakeTime(0.4f));
        timer.Pause();
        tm.Update(MakeTime(5f));
        timer.Resume();
        tm.Update(MakeTime(0.7f));

        Assert.Equal(1, fired);
    }

    [Fact]
    public void Update_WhenNoActiveTimers_DoesNotThrow()
    {
        var tm = new TimerManager();
        var ex = Record.Exception(() => tm.Update(MakeTime(1f)));
        Assert.Null(ex);
    }
}
