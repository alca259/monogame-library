namespace Alca.MonoGame.Kernel.Lighting.DayNight;

/// <summary>
/// Standalone service that advances in-game time and updates <see cref="LightingWorld"/> ambient color.
/// Fires events at sunrise, noon, sunset, and midnight crossings.
/// </summary>
public sealed class DayNightCycle
{
    private readonly DayNightProfile _profile;
    private readonly LightingWorld? _lightingWorld;

    private float _prevHour;

    // Threshold-crossing guards — reset when time passes the opposite threshold
    private bool _sunriseTriggered;
    private bool _noonTriggered;
    private bool _sunsetTriggered;
    private bool _midnightTriggered;

    #region Public state
    /// <summary>Gets the current in-game time of day.</summary>
    public TimeOfDay CurrentTime { get; private set; }

    /// <summary>Gets or sets the time multiplier. 0 = paused, 1 = normal, 2 = double speed.</summary>
    public float TimeScale { get; set; } = 1f;

    /// <summary>Gets or sets a value that pauses the cycle when true, ignoring <see cref="TimeScale"/>.</summary>
    public bool Paused { get; set; }
    #endregion

    #region Events
    /// <summary>Raised once when the time crosses 06:00 in a forward direction.</summary>
    public Action? OnSunrise;

    /// <summary>Raised once when the time crosses 12:00 in a forward direction.</summary>
    public Action? OnNoon;

    /// <summary>Raised once when the time crosses 20:00 in a forward direction.</summary>
    public Action? OnSunset;

    /// <summary>Raised once when the time crosses 00:00 (midnight) in a forward direction.</summary>
    public Action? OnMidnight;
    #endregion

    #region Constructor
    /// <summary>
    /// Initializes a new <see cref="DayNightCycle"/> using the given profile.
    /// </summary>
    /// <param name="profile">The lighting profile to use. Defaults to <see cref="DayNightProfile.Default"/>.</param>
    /// <param name="lightingWorld">Optional lighting world whose ambient color will be updated each frame.</param>
    public DayNightCycle(DayNightProfile profile, LightingWorld? lightingWorld = null)
    {
        _profile = profile;
        _lightingWorld = lightingWorld;
        CurrentTime = TimeOfDay.Midnight;
        _prevHour = 0f;
    }
    #endregion

    #region Public API
    /// <summary>Instantly moves the clock to <paramref name="time"/> and updates the lighting world.</summary>
    public void SetTime(TimeOfDay time)
    {
        _prevHour = time.Hours;
        CurrentTime = time;
        UpdateLighting();
    }

    /// <summary>Advances the clock by the elapsed real-time delta, optionally scaled, and dispatches events.</summary>
    public void Update(GameTime gameTime)
    {
        _prevHour = CurrentTime.Hours;

        if (!Paused)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float hoursPerSecond = 24f / _profile.DayDurationSeconds;
            float elapsed = dt * TimeScale * hoursPerSecond;
            CurrentTime = TimeOfDay.FromHours(CurrentTime.Hours + elapsed);
        }

        UpdateLighting();
        CheckThresholds();
    }
    #endregion

    #region Private helpers
    private void UpdateLighting()
    {
        if (_lightingWorld is null) return;

        DayNightKeyframe kf = _profile.Sample(CurrentTime);

        // Multiply color components by intensity and clamp to [0, 255]
        float ri = kf.AmbientColor.R * kf.AmbientIntensity;
        float gi = kf.AmbientColor.G * kf.AmbientIntensity;
        float bi = kf.AmbientColor.B * kf.AmbientIntensity;

        byte r = (byte)MathHelper.Clamp(ri, 0f, 255f);
        byte g = (byte)MathHelper.Clamp(gi, 0f, 255f);
        byte b = (byte)MathHelper.Clamp(bi, 0f, 255f);

        _lightingWorld.AmbientColor = new Color(r, g, b);
    }

    private void CheckThresholds()
    {
        float cur = CurrentTime.Hours;

        // Detect forward crossing of each threshold: the hour wrapped from just-below to at-or-above.
        // "Crossed" = prev < threshold <= cur, OR a midnight wrap occurred and the threshold is past.

        bool wrappedMidnight = _prevHour > cur; // clock wrapped 24 → 0

        // Sunrise (06:00)
        if (!_sunriseTriggered && CrossedThreshold(6f, _prevHour, cur, wrappedMidnight))
        {
            _sunriseTriggered = true;
            OnSunrise?.Invoke();
        }
        if (_sunriseTriggered && CrossedThreshold(18f, _prevHour, cur, wrappedMidnight))
            _sunriseTriggered = false;

        // Noon (12:00)
        if (!_noonTriggered && CrossedThreshold(12f, _prevHour, cur, wrappedMidnight))
        {
            _noonTriggered = true;
            OnNoon?.Invoke();
        }
        if (_noonTriggered && CrossedThreshold(0f, _prevHour, cur, wrappedMidnight))
            _noonTriggered = false;

        // Sunset (20:00)
        if (!_sunsetTriggered && CrossedThreshold(20f, _prevHour, cur, wrappedMidnight))
        {
            _sunsetTriggered = true;
            OnSunset?.Invoke();
        }
        if (_sunsetTriggered && CrossedThreshold(8f, _prevHour, cur, wrappedMidnight))
            _sunsetTriggered = false;

        // Midnight (00:00) — only fires on a wrap
        if (!_midnightTriggered && wrappedMidnight)
        {
            _midnightTriggered = true;
            OnMidnight?.Invoke();
        }
        if (_midnightTriggered && CrossedThreshold(12f, _prevHour, cur, wrappedMidnight))
            _midnightTriggered = false;
    }

    /// <summary>
    /// Returns true if the clock moved forward over <paramref name="threshold"/> between
    /// <paramref name="prev"/> and <paramref name="cur"/>, accounting for midnight wrap.
    /// </summary>
    private static bool CrossedThreshold(float threshold, float prev, float cur, bool wrapped)
    {
        if (!wrapped)
            return prev < threshold && cur >= threshold;

        // Wrap: [prev → 24) ∪ [0 → cur)
        return prev < threshold || cur >= threshold;
    }
    #endregion
}
