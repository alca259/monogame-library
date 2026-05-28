namespace Alca.MonoGame.Kernel.Weather;

/// <summary>
/// World service that owns and drives the weather simulation. Assign to
/// <see cref="ECS.GameWorld.WeatherWorld"/> to enable automatic per-frame stepping.
/// <para>
/// Manages weather type transitions, interpolates <see cref="WeatherProfile"/> each frame,
/// maintains current temperature, and delegates to optional subsystems:
/// <see cref="WeatherParticleLayer"/>, <see cref="LightningController"/>, and <see cref="WeatherAudioLayer"/>.
/// </para>
/// </summary>
public sealed class WeatherWorld
{
    private readonly Dictionary<string, WeatherProfile> _catalog = new(16, StringComparer.OrdinalIgnoreCase);
    private readonly List<WeatherBehaviour> _registeredBehaviours = new(32);

    private WeatherTypeId _currentWeather;
    private WeatherTypeId _targetWeather;
    private WeatherProfile _fromProfile;
    private WeatherProfile _toProfile;
    private float _transitionTimer;
    private float _transitionDuration;
    private float _currentTemperature;
    private float _totalElapsed;

    // ── Public state ──────────────────────────────────────────────────────────

    /// <summary>Gets the weather type currently active (or the origin type during a transition).</summary>
    public WeatherTypeId CurrentWeather => _currentWeather;

    /// <summary>Gets the current temperature in degrees Celsius, oscillating within the active profile's range.</summary>
    public float CurrentTemperature => _currentTemperature;

    /// <summary>Gets whether a weather transition is currently in progress.</summary>
    public bool IsTransitioning { get; private set; }

    /// <summary>Gets the transition progress in [0, 1]; 1 when not transitioning.</summary>
    public float TransitionProgress { get; private set; }

    /// <summary>Gets the fully interpolated weather profile applied this frame.</summary>
    public WeatherProfile ActiveProfile { get; private set; }

    /// <summary>Gets the wind state derived from <see cref="ActiveProfile"/> this frame.</summary>
    public WindState CurrentWind { get; private set; }

    // ── Subsystem wiring (set before first Update) ────────────────────────────

    /// <summary>Gets or sets the lighting world used to drive ambient color each frame.</summary>
    public Lighting.LightingWorld? LightingWorld { get; set; }

    /// <summary>Gets or sets the audio controller used for spatial thunder playback.</summary>
    public Audio.AudioController? AudioController { get; set; }

    /// <summary>Gets or sets the audio mixer used for channel-based volume routing.</summary>
    public Audio.AudioMixer? AudioMixer { get; set; }

    /// <summary>
    /// Gets or sets the scale factor converting km/h to world units per second.
    /// Default 1 means 1 km/h = 1 world unit/s. Adjust to match your world scale.
    /// </summary>
    public float WorldUnitsPerKmh { get; set; } = 1f;

    // ── Optional subsystems ───────────────────────────────────────────────────

    /// <summary>Gets the lightning controller, or <see langword="null"/> if not enabled.</summary>
    public LightningController? Lightning { get; private set; }

    /// <summary>Gets the particle layer, or <see langword="null"/> if not enabled.</summary>
    public WeatherParticleLayer? Particles { get; private set; }

    /// <summary>Gets the audio layer, or <see langword="null"/> if not enabled.</summary>
    public WeatherAudioLayer? Audio { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised once per lightning strike, carrying position, intensity, and impulse data.
    /// Handlers are invoked synchronously from <see cref="Update"/>; do not allocate inside them.
    /// </summary>
    public event Action<LightningStrikeEvent>? LightningStruck;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Initializes a new <see cref="WeatherWorld"/> starting in <see cref="WeatherTypeId.Sunny"/>.</summary>
    public WeatherWorld()
    {
        BuildDefaultCatalog();
        ApplyImmediate(WeatherTypeId.Sunny);
    }

    // ── Subsystem registration ────────────────────────────────────────────────

    /// <summary>Registers a particle layer that will be driven each frame. Call once before the first <see cref="Update"/>.</summary>
    public void EnableParticles(WeatherParticleLayer layer) => Particles = layer;

    /// <summary>Registers a lightning controller. Call once before the first <see cref="Update"/>.</summary>
    public void EnableLightning(LightningController controller) => Lightning = controller;

    /// <summary>Registers an audio layer. Call once before the first <see cref="Update"/>.</summary>
    public void EnableAudio(WeatherAudioLayer layer) => Audio = layer;

    // ── Catalog management ────────────────────────────────────────────────────

    /// <summary>
    /// Registers a custom weather type or replaces an existing profile (built-in or previously registered).
    /// The wind/fog constraint is enforced: fog density is forced to 0 when wind speed max > 0.
    /// </summary>
    public void RegisterCustomWeather(WeatherTypeId id, in WeatherProfile profile)
    {
        WeatherProfile validated = ValidateConstraints(id, profile);
        _catalog[id.Value] = validated;
    }

    /// <summary>
    /// Replaces the profile for an already-registered weather type. Valid before, during, or after a transition.
    /// If the type is the current active weather, the change takes effect on the next frame.
    /// Throws <see cref="KeyNotFoundException"/> when the type is not registered.
    /// </summary>
    public void ModifyProfile(WeatherTypeId id, in WeatherProfile profile)
    {
        if (!_catalog.ContainsKey(id.Value))
            throw new KeyNotFoundException($"Weather type '{id.Value}' is not registered. Call RegisterCustomWeather first.");

        WeatherProfile validated = ValidateConstraints(id, profile);
        _catalog[id.Value] = validated;

        // If this is the current weather and not transitioning, refresh active profile immediately
        if (_currentWeather == id && !IsTransitioning)
        {
            _toProfile = validated;
            ActiveProfile = validated;
        }
        // If it's the target during a transition, update the destination
        else if (_targetWeather == id && IsTransitioning)
        {
            _toProfile = validated;
        }
    }

    /// <summary>Attempts to retrieve the profile for the given weather type. Returns false if not registered.</summary>
    public bool TryGetProfile(WeatherTypeId id, out WeatherProfile profile) =>
        _catalog.TryGetValue(id.Value, out profile);

    // ── Weather control ───────────────────────────────────────────────────────

    /// <summary>
    /// Begins a smooth transition to <paramref name="type"/> over <paramref name="transitionDuration"/> seconds.
    /// If a transition is already in progress it completes instantly before the new one starts.
    /// Throws <see cref="KeyNotFoundException"/> when the type is not registered.
    /// </summary>
    public void SetWeather(WeatherTypeId type, float transitionDuration = 3f)
    {
        if (!_catalog.TryGetValue(type.Value, out WeatherProfile target))
            throw new KeyNotFoundException($"Weather type '{type.Value}' is not registered.");

        // Complete ongoing transition instantly
        if (IsTransitioning)
            CompleteTransition();

        if (transitionDuration <= 0f)
        {
            ApplyImmediate(type);
            return;
        }

        _fromProfile       = ActiveProfile;
        _toProfile         = target;
        _targetWeather     = type;
        _transitionTimer   = 0f;
        _transitionDuration = transitionDuration;
        IsTransitioning    = true;
        TransitionProgress = 0f;
    }

    /// <summary>Switches immediately to <paramref name="type"/> with no interpolation.</summary>
    public void SetWeatherImmediate(WeatherTypeId type)
    {
        if (!_catalog.TryGetValue(type.Value, out _))
            throw new KeyNotFoundException($"Weather type '{type.Value}' is not registered.");

        if (IsTransitioning)
            CompleteTransition();

        ApplyImmediate(type);
    }

    // ── Game loop ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Advances the weather simulation by one frame. Called automatically by
    /// <see cref="ECS.GameWorld.Update"/> when assigned to <see cref="ECS.GameWorld.WeatherWorld"/>.
    /// No heap allocations.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _totalElapsed += dt;

        if (IsTransitioning)
            AdvanceTransition(dt);

        UpdateTemperature(dt);
        UpdateWind();
        DispatchLighting();
        DispatchWind();

        Particles?.Update(gameTime, ActiveProfile, CurrentWind);
        Audio?.Update(gameTime, ActiveProfile);
        Lightning?.Update(gameTime, ActiveProfile, _registeredBehaviours);
    }

    // ── ECS registration (called by WeatherBehaviour, not user code) ──────────

    /// <summary>Registers a <see cref="WeatherBehaviour"/> to receive wind and lightning impulses.</summary>
    internal void Register(WeatherBehaviour behaviour)
    {
        if (!_registeredBehaviours.Contains(behaviour))
            _registeredBehaviours.Add(behaviour);
    }

    /// <summary>Unregisters a previously registered <see cref="WeatherBehaviour"/>.</summary>
    internal void Unregister(WeatherBehaviour behaviour) =>
        _registeredBehaviours.Remove(behaviour);

    /// <summary>Raises the <see cref="LightningStruck"/> event. Called by <see cref="LightningController"/>.</summary>
    internal void RaiseLightningStruck(in LightningStrikeEvent evt) =>
        LightningStruck?.Invoke(evt);

    // ── Private helpers ───────────────────────────────────────────────────────

    private void BuildDefaultCatalog()
    {
        _catalog[WeatherTypeId.Sunny.Value]        = WeatherProfiles.Sunny;
        _catalog[WeatherTypeId.HeatWave.Value]     = WeatherProfiles.HeatWave;
        _catalog[WeatherTypeId.Cloudy.Value]       = WeatherProfiles.Cloudy;
        _catalog[WeatherTypeId.Fog.Value]          = WeatherProfiles.Fog;
        _catalog[WeatherTypeId.Storm.Value]        = WeatherProfiles.Storm;
        _catalog[WeatherTypeId.Thunderstorm.Value] = WeatherProfiles.Thunderstorm;
        _catalog[WeatherTypeId.HailStorm.Value]    = WeatherProfiles.HailStorm;
        _catalog[WeatherTypeId.Blizzard.Value]     = WeatherProfiles.Blizzard;
        _catalog[WeatherTypeId.ColdSnap.Value]     = WeatherProfiles.ColdSnap;
        _catalog[WeatherTypeId.OrangeWind.Value]   = WeatherProfiles.OrangeWind;
    }

    private void ApplyImmediate(WeatherTypeId type)
    {
        _currentWeather    = type;
        _targetWeather     = type;
        _catalog.TryGetValue(type.Value, out WeatherProfile profile);
        _toProfile         = profile;
        _fromProfile       = profile;
        ActiveProfile      = profile;
        IsTransitioning    = false;
        TransitionProgress = 1f;
        _currentTemperature = MathHelper.Lerp(profile.TemperatureMin, profile.TemperatureMax, 0.5f);
    }

    private void AdvanceTransition(float dt)
    {
        _transitionTimer += dt;
        float t = _transitionDuration > 0f
            ? Math.Clamp(_transitionTimer / _transitionDuration, 0f, 1f)
            : 1f;

        TransitionProgress = t;
        ActiveProfile = WeatherProfile.Lerp(_fromProfile, _toProfile, t);

        if (t >= 1f)
            CompleteTransition();
    }

    private void CompleteTransition()
    {
        _currentWeather    = _targetWeather;
        ActiveProfile      = _toProfile;
        _fromProfile       = _toProfile;
        IsTransitioning    = false;
        TransitionProgress = 1f;
        _transitionTimer   = 0f;
    }

    private void UpdateTemperature(float dt)
    {
        float min = ActiveProfile.TemperatureMin;
        float max = ActiveProfile.TemperatureMax;
        // Gentle sinusoidal oscillation within the range
        float range = max - min;
        float target = min + range * (0.5f + 0.5f * MathF.Sin(_totalElapsed * 0.05f));
        float speed = range > 0.01f ? dt * 0.5f : 1f;
        _currentTemperature = MathHelper.Lerp(_currentTemperature, target, speed);
    }

    private void UpdateWind()
    {
        WeatherProfile p = ActiveProfile;
        float speed = MathHelper.Lerp(p.WindSpeedMinKmh, p.WindSpeedMaxKmh,
            0.5f + 0.5f * MathF.Sin(_totalElapsed * 0.1f));

        Vector2 dir = p.WindDirection.LengthSquared() > 0.001f
            ? Vector2.Normalize(p.WindDirection)
            : Vector2.UnitX;

        CurrentWind = new WindState
        {
            Direction  = dir,
            SpeedKmh   = speed,
            Turbulence = p.WindTurbulence
        };
    }

    private void DispatchLighting()
    {
        if (LightingWorld is null) return;
        WeatherProfile p = ActiveProfile;
        Color adjusted = new(
            (byte)(p.AmbientColor.R * p.AmbientIntensity),
            (byte)(p.AmbientColor.G * p.AmbientIntensity),
            (byte)(p.AmbientColor.B * p.AmbientIntensity));
        LightingWorld.AmbientColor = adjusted;
    }

    private void DispatchWind()
    {
        if (CurrentWind.IsCalm) return;
        Vector2 force = CurrentWind.ComputeEffectiveForce(_totalElapsed, WorldUnitsPerKmh);
        for (int i = 0; i < _registeredBehaviours.Count; i++)
            _registeredBehaviours[i].ApplyWindForce(force);
    }

    private static WeatherProfile ValidateConstraints(WeatherTypeId id, in WeatherProfile profile)
    {
        if (profile.WindSpeedMaxKmh > 0f && profile.FogDensity > 0f)
        {
            global::System.Diagnostics.Debug.WriteLine(
                $"[WeatherWorld] Profile '{id.Value}' has both wind (max {profile.WindSpeedMaxKmh} km/h) " +
                $"and fog density ({profile.FogDensity}). Wind and fog are mutually exclusive — FogDensity forced to 0.");

            return profile with { FogDensity = 0f };
        }

        return profile;
    }
}
