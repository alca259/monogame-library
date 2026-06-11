using Alca.MonoGame.Kernel.Audio.Mixer;

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

    private static readonly List<WeatherBehaviour> _emptyBehaviourList = new(0);

    private WeatherTypeId _currentWeather;
    private WeatherTypeId _targetWeather;
    private WeatherProfile _fromProfile;
    private WeatherProfile _toProfile;
    private float _transitionTimer;
    private float _transitionDuration;
    private float _currentTemperature;
    private float _totalElapsed;

    // Gust state machine
    private float _windGustSpeed;
    private float _windGustTarget;
    private float _windGustTimer;
    private float _windGustDuration;
    private bool _windGustActive;

    // Temperature random walk
    private float _tempWalkTarget;
    private float _tempWalkTimer;
    private float _tempWalkDuration;
    private bool _tempWalkActive;

    #region Public state
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
    #endregion

    #region Subsystem wiring (set before first Update) 
    /// <summary>Gets or sets the lighting world used to drive ambient color each frame.</summary>
    public Lighting.LightingWorld? LightingWorld { get; set; }

    /// <summary>Gets or sets the audio controller used for spatial thunder playback.</summary>
    public Audio.AudioController? AudioController { get; set; }

    /// <summary>Gets or sets the audio mixer used for channel-based volume routing.</summary>
    public AudioMixer? AudioMixer { get; set; }

    /// <summary>
    /// Gets or sets the scale factor converting km/h to world units per second.
    /// Default 1 means 1 km/h = 1 world unit/s. Adjust to match your world scale.
    /// </summary>
    public float WorldUnitsPerKmh { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the speed multiplier for temperature field interpolation during weather transitions.
    /// Values below 1 slow down temperature changes (e.g. 0.3 = temperature takes ~3× longer to transition).
    /// Values above 1 make temperature transition faster than the overall duration.
    /// Does NOT affect the in-state random walk. Default 0.3.
    /// </summary>
    public float TemperatureTransitionSpeed { get; set; } = 0.3f;

    /// <summary>
    /// Gets or sets the speed multiplier for wind field interpolation during weather transitions.
    /// Mirrors <see cref="TemperatureTransitionSpeed"/> but applies to wind speed, direction, and turbulence.
    /// Default 1.0 (wind transitions at the same rate as the overall profile).
    /// </summary>
    public float WindTransitionSpeed { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets whether the weather world is currently in an interior context.
    /// When <see langword="true"/>: no precipitation particles are rendered, wind is not dispatched to
    /// <see cref="WeatherBehaviour"/> entities, ambient lighting is not updated, and temperature
    /// stabilizes at <see cref="IndoorTemperature"/>. Lightning flash and audio remain active.
    /// Lightning impulses are NOT dispatched to entities indoors.
    /// </summary>
    public bool IsInterior { get; set; } = false;

    /// <summary>Gets or sets the stable temperature used when <see cref="IsInterior"/> is <see langword="true"/>. Default 22 °C.</summary>
    public float IndoorTemperature { get; set; } = 22f;

    /// <summary>
    /// Gets or sets the audio volume multiplier applied to all weather audio when <see cref="IsInterior"/> is <see langword="true"/>.
    /// Default 0.2 (muffled outdoor sounds heard through walls).
    /// </summary>
    public float IndoorAudioMultiplier { get; set; } = 0.2f;
    #endregion

    #region Optional subsystems
    /// <summary>Gets the lightning controller, or <see langword="null"/> if not enabled.</summary>
    public LightningController? Lightning { get; private set; }

    /// <summary>Gets the particle layer, or <see langword="null"/> if not enabled.</summary>
    public WeatherParticleLayer? Particles { get; private set; }

    /// <summary>Gets the audio layer, or <see langword="null"/> if not enabled.</summary>
    public WeatherAudioLayer? Audio { get; private set; }
    #endregion

    #region Events
    /// <summary>
    /// Raised once per lightning strike, carrying position, intensity, and impulse data.
    /// Handlers are invoked synchronously from <see cref="Update"/>; do not allocate inside them.
    /// </summary>
    public event Action<LightningStrikeEvent>? LightningStruck;
    #endregion

    #region Construction
    /// <summary>Initializes a new <see cref="WeatherWorld"/> starting in <see cref="WeatherTypeId.Sunny"/>.</summary>
    public WeatherWorld()
    {
        BuildDefaultCatalog();
        ApplyImmediate(WeatherTypeId.Sunny);
    }
    #endregion

    #region Subsystem registration
    /// <summary>Registers a particle layer that will be driven each frame. Call once before the first <see cref="Update"/>.</summary>
    public void EnableParticles(WeatherParticleLayer layer) => Particles = layer;

    /// <summary>Registers a lightning controller. Call once before the first <see cref="Update"/>.</summary>
    public void EnableLightning(LightningController controller) => Lightning = controller;

    /// <summary>Registers an audio layer. Call once before the first <see cref="Update"/>.</summary>
    public void EnableAudio(WeatherAudioLayer layer) => Audio = layer;
    #endregion

    #region Catalog management
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
    #endregion

    #region Weather control
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
    #endregion

    #region Game loop
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
        UpdateWind(dt);

        if (Particles is not null)
            Particles.IsInterior = IsInterior;

        if (!IsInterior)
        {
            DispatchLighting();
            DispatchWind();
            Particles?.Update(gameTime, ActiveProfile, CurrentWind);
        }

        WeatherProfile audioProfile = IsInterior
            ? ActiveProfile with
              {
                  RainVolume    = ActiveProfile.RainVolume    * IndoorAudioMultiplier,
                  WindVolume    = ActiveProfile.WindVolume    * IndoorAudioMultiplier,
                  ThunderVolume = ActiveProfile.ThunderVolume * IndoorAudioMultiplier,
              }
            : ActiveProfile;

        Audio?.Update(gameTime, audioProfile);
        Lightning?.Update(gameTime, ActiveProfile,
            IsInterior ? _emptyBehaviourList : _registeredBehaviours);
    }
    #endregion

    #region ECS registration (called by WeatherBehaviour, not user code)
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
    #endregion

    #region Private helpers
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

        // Reset random-walk state so gusts start fresh for the new profile
        _windGustSpeed  = MathHelper.Lerp(profile.WindSpeedMinKmh, profile.WindSpeedMaxKmh, 0.5f);
        _windGustActive = false;
        _tempWalkActive = false;
    }

    private void AdvanceTransition(float dt)
    {
        _transitionTimer += dt;
        float t = _transitionDuration > 0f
            ? Math.Clamp(_transitionTimer / _transitionDuration, 0f, 1f)
            : 1f;

        TransitionProgress = t;

        // Independent progress values let temperature and wind interpolate at their own rates
        float tTemp = Math.Clamp(t * TemperatureTransitionSpeed, 0f, 1f);
        float tWind = Math.Clamp(t * WindTransitionSpeed, 0f, 1f);

        WeatherProfile lerped = WeatherProfile.Lerp(_fromProfile, _toProfile, t);
        ActiveProfile = lerped with
        {
            TemperatureMin  = MathHelper.Lerp(_fromProfile.TemperatureMin,  _toProfile.TemperatureMin,  tTemp),
            TemperatureMax  = MathHelper.Lerp(_fromProfile.TemperatureMax,  _toProfile.TemperatureMax,  tTemp),
            WindSpeedMinKmh = MathHelper.Lerp(_fromProfile.WindSpeedMinKmh, _toProfile.WindSpeedMinKmh, tWind),
            WindSpeedMaxKmh = MathHelper.Lerp(_fromProfile.WindSpeedMaxKmh, _toProfile.WindSpeedMaxKmh, tWind),
            WindDirection   = Vector2.Lerp(_fromProfile.WindDirection,       _toProfile.WindDirection,   tWind),
            WindTurbulence  = MathHelper.Lerp(_fromProfile.WindTurbulence,   _toProfile.WindTurbulence,  tWind),
        };

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

        // Clamp gust speed to new profile range and let the gust machine pick a new target
        _windGustSpeed  = Math.Clamp(_windGustSpeed, _toProfile.WindSpeedMinKmh, _toProfile.WindSpeedMaxKmh);
        _windGustActive = false;
        _tempWalkActive = false;
    }

    private void UpdateTemperature(float dt)
    {
        if (IsInterior)
        {
            _currentTemperature = MathHelper.Lerp(_currentTemperature, IndoorTemperature, dt * 0.3f);
            return;
        }

        float min   = ActiveProfile.TemperatureMin;
        float max   = ActiveProfile.TemperatureMax;
        float range = max - min;

        _tempWalkTimer += dt;
        if (!_tempWalkActive || _tempWalkTimer >= _tempWalkDuration)
        {
            _tempWalkTarget   = min + range * Random.Shared.NextSingle();
            _tempWalkDuration = MathHelper.Lerp(60f, 180f, Random.Shared.NextSingle());
            _tempWalkTimer    = 0f;
            _tempWalkActive   = true;
        }

        float lerpSpeed = range > 0.01f ? 0.02f : 1f;
        _currentTemperature = MathHelper.Lerp(_currentTemperature, _tempWalkTarget, dt * lerpSpeed);
        _currentTemperature = Math.Clamp(_currentTemperature, min, max);
    }

    private void UpdateWind(float dt)
    {
        WeatherProfile p = ActiveProfile;

        _windGustTimer += dt;
        if (!_windGustActive || _windGustTimer >= _windGustDuration)
        {
            _windGustTarget   = MathHelper.Lerp(p.WindSpeedMinKmh, p.WindSpeedMaxKmh, Random.Shared.NextSingle());
            _windGustDuration = MathHelper.Lerp(2f, 15f, Random.Shared.NextSingle());
            _windGustTimer    = 0f;
            _windGustActive   = true;
        }

        _windGustSpeed = MathHelper.Lerp(_windGustSpeed, _windGustTarget, dt * 1.5f);
        _windGustSpeed = Math.Clamp(_windGustSpeed, p.WindSpeedMinKmh, p.WindSpeedMaxKmh);

        Vector2 dir = p.WindDirection.LengthSquared() > 0.001f
            ? Vector2.Normalize(p.WindDirection)
            : Vector2.UnitX;

        CurrentWind = new WindState
        {
            Direction  = dir,
            SpeedKmh   = _windGustSpeed,
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
            System.Diagnostics.Debug.WriteLine(
                $"[WeatherWorld] Profile '{id.Value}' has both wind (max {profile.WindSpeedMaxKmh} km/h) " +
                $"and fog density ({profile.FogDensity}). Wind and fog are mutually exclusive — FogDensity forced to 0.");

            return profile with { FogDensity = 0f };
        }

        return profile;
    }
    #endregion
}
