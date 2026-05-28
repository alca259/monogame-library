using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Weather;

/// <summary>
/// Manages the full lifecycle of lightning strikes: random timing, a pre-allocated
/// <see cref="Lighting.PointLight2D"/> flash entity, spatial thunder audio, and physics impulse dispatch.
/// Pre-allocates all resources in the constructor; zero heap allocations in <see cref="Update"/>.
/// Dispose to remove the flash entity and release thunder instances.
/// </summary>
public sealed class LightningController : IDisposable
{
    private readonly WeatherWorld _weatherWorld;
    private readonly GameWorld _gameWorld;
    private readonly GameEntity _flashEntity;
    private readonly Lighting.PointLight2D _flashLight;

    private float _strikeTimer;
    private float _nextStrikeInterval;
    private float _flashTimer;
    private bool _isFlashing;
    private bool _disposed;

    // ── Strike geometry ───────────────────────────────────────────────────────

    /// <summary>Gets or sets the minimum world-space X coordinate at which a strike can occur. Default 0.</summary>
    public float MinX { get; set; } = 0f;

    /// <summary>Gets or sets the maximum world-space X coordinate at which a strike can occur. Default 1280.</summary>
    public float MaxX { get; set; } = 1280f;

    /// <summary>Gets or sets the world-space Y coordinate where the flash appears (above the screen). Default -80.</summary>
    public float StrikeY { get; set; } = -80f;

    // ── Flash light ───────────────────────────────────────────────────────────

    /// <summary>Gets or sets the peak intensity of the flash <see cref="Lighting.PointLight2D"/>. Default 8.</summary>
    public float FlashIntensity { get; set; } = 8f;

    /// <summary>Gets or sets the range of the flash light in world units. Default 600.</summary>
    public float FlashRange { get; set; } = 600f;

    /// <summary>Gets or sets the duration of the full-intensity flash in seconds. Default 0.15.</summary>
    public float FlashDuration { get; set; } = 0.15f;

    /// <summary>Gets or sets the color of the flash light. Default is cool blue-white.</summary>
    public Color FlashColor { get; set; } = new Color(200, 220, 255);

    // ── Physics impulse ───────────────────────────────────────────────────────

    /// <summary>Gets or sets the world-unit radius within which nearby <see cref="WeatherBehaviour"/> entities receive an impulse. Default 200.</summary>
    public float ImpulseRadius { get; set; } = 200f;

    /// <summary>Gets or sets the peak impulse magnitude at the strike center, falling off linearly to <see cref="ImpulseRadius"/>. Default 350.</summary>
    public float ImpulseStrength { get; set; } = 350f;

    // ── Audio ─────────────────────────────────────────────────────────────────

    /// <summary>Gets or sets the audio controller used for spatial thunder playback. Optional.</summary>
    public Audio.AudioController? AudioController { get; set; }

    /// <summary>Gets or sets the weather audio layer whose strike pool is used for thunder playback. Optional.</summary>
    public WeatherAudioLayer? AudioLayer { get; set; }

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>Gets whether a flash is currently active.</summary>
    public bool IsFlashing => _isFlashing;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the controller, a hidden flash entity with a <see cref="Lighting.PointLight2D"/>,
    /// and seeds the first strike timer.
    /// </summary>
    /// <param name="weatherWorld">Required — used to raise <see cref="WeatherWorld.LightningStruck"/>.</param>
    /// <param name="gameWorld">Required — flash entity is created inside this world.</param>
    public LightningController(WeatherWorld weatherWorld, GameWorld gameWorld)
    {
        _weatherWorld = weatherWorld;
        _gameWorld    = gameWorld;

        _flashEntity = gameWorld.CreateEntity("_WeatherLightningFlash", Vector2.Zero);
        _flashEntity.Active = false;

        _flashLight = new Lighting.PointLight2D
        {
            Color     = FlashColor,
            Intensity = 0f,
            Range     = FlashRange
        };
        _flashEntity.Add(_flashLight);

        _nextStrikeInterval = 10f; // will be updated on first Update with profile data
    }

    // ── Game loop ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Advances the lightning timer, fires a strike when ready, and manages flash decay.
    /// Dispatches impulses to all registered <paramref name="behaviours"/>.
    /// No heap allocations.
    /// </summary>
    public void Update(GameTime gameTime, in WeatherProfile profile, List<WeatherBehaviour> behaviours)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_isFlashing)
        {
            _flashTimer += dt;
            if (_flashTimer >= FlashDuration)
                EndFlash();
        }

        if (!profile.HasLightning) return;

        _strikeTimer += dt;
        if (_strikeTimer >= _nextStrikeInterval)
        {
            float x = MathHelper.Lerp(MinX, MaxX, Random.Shared.NextSingle());
            TriggerStrikeAt(new Vector2(x, StrikeY), behaviours);
            _strikeTimer = 0f;
            _nextStrikeInterval = MathHelper.Lerp(
                profile.LightningMinInterval,
                profile.LightningMaxInterval,
                Random.Shared.NextSingle());
        }
    }

    /// <summary>Immediately triggers a strike at <paramref name="worldPosition"/>, bypassing the timer.</summary>
    public void TriggerStrikeAt(Vector2 worldPosition)
    {
        // Resolve profile from WeatherWorld for impulse data
        WeatherWorld.TryGetProfile(WeatherWorld.CurrentWeather, out _);
        TriggerStrikeAt(worldPosition, null);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _gameWorld.Destroy(_flashEntity);
        _disposed = true;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void TriggerStrikeAt(Vector2 pos, List<WeatherBehaviour>? behaviours)
    {
        // Flash light
        _flashEntity.Transform.Position2d = pos;
        _flashLight.Color     = FlashColor;
        _flashLight.Intensity = FlashIntensity;
        _flashLight.Range     = FlashRange;
        _flashEntity.Active   = true;
        _isFlashing           = true;
        _flashTimer           = 0f;

        // Physics impulse
        if (behaviours is not null)
        {
            for (int i = 0; i < behaviours.Count; i++)
                behaviours[i].ApplyLightningImpulse(pos, ImpulseRadius, ImpulseStrength);
        }

        // Spatial audio
        if (AudioController is not null && AudioLayer is not null)
            AudioLayer.PlayThunderStrike(pos, AudioController);

        // Raise event
        _weatherWorld.RaiseLightningStruck(new LightningStrikeEvent
        {
            Position       = pos,
            Intensity      = FlashIntensity,
            ImpulseRadius  = ImpulseRadius,
            ImpulseStrength = ImpulseStrength
        });
    }

    private void EndFlash()
    {
        _flashEntity.Active   = false;
        _flashLight.Intensity = 0f;
        _isFlashing           = false;
    }

    private WeatherWorld WeatherWorld => _weatherWorld;
}
