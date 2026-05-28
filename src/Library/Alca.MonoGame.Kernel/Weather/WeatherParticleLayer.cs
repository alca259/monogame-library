using MonoGame.Extended.Graphics;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Data;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Profiles;

namespace Alca.MonoGame.Kernel.Weather;

/// <summary>
/// Owns and drives up to five weather particle effects: rain, snow, hail, fog wisps, and wind sprites.
/// Wind direction is applied each frame by mutating cached <see cref="LinearGravityModifier"/> references,
/// yielding zero heap allocations in <see cref="Update"/>.
/// Call <see cref="LoadContent"/> once before the first <see cref="Update"/>.
/// Dispose when no longer needed to release particle effects.
/// </summary>
public sealed class WeatherParticleLayer : IDisposable
{
    private ParticleEffect? _rainEffect;
    private ParticleEffect? _snowEffect;
    private ParticleEffect? _hailEffect;
    private ParticleEffect? _fogEffect;
    private ParticleEffect? _windEffect;

    private LinearGravityModifier? _rainGravity;
    private LinearGravityModifier? _snowGravity;
    private LinearGravityModifier? _hailGravity;
    private LinearGravityModifier? _fogGravity;
    private LinearGravityModifier? _windGravity;

    private bool _disposed;

    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the center of the horizontal emitter band in world space, typically just above the camera viewport.
    /// Update this each frame to follow the camera so precipitation covers the visible screen area.
    /// </summary>
    public Vector2 EmitterPosition { get; set; }

    /// <summary>Gets or sets the width of the emitter band in world units. Default 1600.</summary>
    public float EmitterWidth { get; set; } = 1600f;

    // ── Content loading ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates all particle effects from the supplied textures and caches modifier references.
    /// Must be called once before the first <see cref="Update"/>.
    /// Passing <see langword="null"/> for any texture disables that effect permanently.
    /// </summary>
    public void LoadContent(
        Texture2D? rainTexture,
        Texture2D? snowTexture,
        Texture2D? hailTexture,
        Texture2D? fogTexture,
        Texture2D? windTexture)
    {
        if (rainTexture is not null)
            (_rainEffect, _rainGravity) = BuildRainEffect(rainTexture);

        if (snowTexture is not null)
            (_snowEffect, _snowGravity) = BuildSnowEffect(snowTexture);

        if (hailTexture is not null)
            (_hailEffect, _hailGravity) = BuildHailEffect(hailTexture);

        if (fogTexture is not null)
            (_fogEffect, _fogGravity) = BuildFogEffect(fogTexture);

        if (windTexture is not null)
            (_windEffect, _windGravity) = BuildWindEffect(windTexture);
    }

    // ── Game loop ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates active particle effects, applies wind direction to cached gravity modifiers,
    /// and enables or disables each effect based on <paramref name="profile"/>.
    /// No heap allocations.
    /// </summary>
    public void Update(GameTime gameTime, in WeatherProfile profile, in WindState wind)
    {
        float totalElapsed = (float)gameTime.TotalGameTime.TotalSeconds;
        float worldUnits = 1f; // caller should pass WorldUnitsPerKmh from WeatherWorld if needed

        Vector2 windForce = wind.ComputeEffectiveForce(totalElapsed, worldUnits);

        UpdateEffect(_rainEffect, _rainGravity, gameTime, windForce,
            profile.HasPrecipitation && IsRainProfile(profile));

        UpdateEffect(_snowEffect, _snowGravity, gameTime, windForce,
            profile.HasPrecipitation && IsSnowProfile(profile));

        UpdateEffect(_hailEffect, _hailGravity, gameTime, windForce,
            profile.HasPrecipitation && IsHailProfile(profile));

        UpdateEffect(_fogEffect, _fogGravity, gameTime, windForce,
            profile.HasPrecipitation && IsFogProfile(profile));

        UpdateEffect(_windEffect, _windGravity, gameTime, windForce,
            profile.HasPrecipitation && IsWindProfile(profile));
    }

    /// <summary>
    /// Draws all active particle effects using <see cref="BlendState.Additive"/> blending.
    /// Manages its own <see cref="SpriteBatch.Begin"/>/<see cref="SpriteBatch.End"/> calls per effect.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        DrawEffect(spriteBatch, _rainEffect);
        DrawEffect(spriteBatch, _snowEffect);
        DrawEffect(spriteBatch, _hailEffect);
        DrawEffect(spriteBatch, _fogEffect);
        DrawEffect(spriteBatch, _windEffect);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _rainEffect?.Dispose();
        _snowEffect?.Dispose();
        _hailEffect?.Dispose();
        _fogEffect?.Dispose();
        _windEffect?.Dispose();
        _disposed = true;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void UpdateEffect(
        ParticleEffect? effect,
        LinearGravityModifier? gravity,
        GameTime gameTime,
        Vector2 windForce,
        bool active)
    {
        if (effect is null) return;

        if (active)
        {
            // Update wind direction on cached modifier
            if (gravity is not null)
            {
                gravity.Direction = Vector2.UnitY + windForce * 0.01f; // blend down + sideways
                if (gravity.Direction.LengthSquared() > 0.001f)
                    gravity.Direction = Vector2.Normalize(gravity.Direction);
            }

            effect.Position = EmitterPosition;
            effect.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    private static void DrawEffect(SpriteBatch spriteBatch, ParticleEffect? effect)
    {
        if (effect is null) return;

        spriteBatch.Begin(blendState: BlendState.Additive);
        spriteBatch.Draw(effect);
        spriteBatch.End();
    }

    // ── Effect factories ──────────────────────────────────────────────────────

    private static (ParticleEffect effect, LinearGravityModifier gravity) BuildRainEffect(Texture2D texture)
    {
        var gravity = new LinearGravityModifier { Direction = Vector2.UnitY, Strength = 400f };
        var emitter = new ParticleEmitter(600)
        {
            Profile = Profile.BoxFill(0f, 0f),
            TextureRegion = new Texture2DRegion(texture),
            LifeSpan = 2.5f,
            Parameters = new ParticleReleaseParameters
            {
                Speed = new ParticleFloatParameter(300f)
            }
        };
        emitter.Modifiers.Add(gravity);
        var effect = new ParticleEffect("rain") { Emitters = [emitter] };
        return (effect, gravity);
    }

    private static (ParticleEffect effect, LinearGravityModifier gravity) BuildSnowEffect(Texture2D texture)
    {
        var gravity = new LinearGravityModifier { Direction = Vector2.UnitY, Strength = 60f };
        var emitter = new ParticleEmitter(400)
        {
            Profile = Profile.BoxFill(0f, 0f),
            TextureRegion = new Texture2DRegion(texture),
            LifeSpan = 5f,
            Parameters = new ParticleReleaseParameters
            {
                Speed = new ParticleFloatParameter(40f)
            }
        };
        emitter.Modifiers.Add(gravity);
        var effect = new ParticleEffect("snow") { Emitters = [emitter] };
        return (effect, gravity);
    }

    private static (ParticleEffect effect, LinearGravityModifier gravity) BuildHailEffect(Texture2D texture)
    {
        var gravity = new LinearGravityModifier { Direction = Vector2.UnitY, Strength = 600f };
        var emitter = new ParticleEmitter(300)
        {
            Profile = Profile.BoxFill(0f, 0f),
            TextureRegion = new Texture2DRegion(texture),
            LifeSpan = 1.5f,
            Parameters = new ParticleReleaseParameters
            {
                Speed = new ParticleFloatParameter(500f)
            }
        };
        emitter.Modifiers.Add(gravity);
        var effect = new ParticleEffect("hail") { Emitters = [emitter] };
        return (effect, gravity);
    }

    private static (ParticleEffect effect, LinearGravityModifier gravity) BuildFogEffect(Texture2D texture)
    {
        var gravity = new LinearGravityModifier { Direction = Vector2.UnitX, Strength = 5f };
        var emitter = new ParticleEmitter(150)
        {
            Profile = Profile.BoxFill(0f, 0f),
            TextureRegion = new Texture2DRegion(texture),
            LifeSpan = 8f,
            Parameters = new ParticleReleaseParameters
            {
                Speed = new ParticleFloatParameter(10f)
            }
        };
        emitter.Modifiers.Add(gravity);
        var effect = new ParticleEffect("fog") { Emitters = [emitter] };
        return (effect, gravity);
    }

    private static (ParticleEffect effect, LinearGravityModifier gravity) BuildWindEffect(Texture2D texture)
    {
        var gravity = new LinearGravityModifier { Direction = Vector2.UnitX, Strength = 350f };
        var emitter = new ParticleEmitter(500)
        {
            Profile = Profile.BoxFill(0f, 0f),
            TextureRegion = new Texture2DRegion(texture),
            LifeSpan = 2f,
            Parameters = new ParticleReleaseParameters
            {
                Speed = new ParticleFloatParameter(250f)
            }
        };
        emitter.Modifiers.Add(gravity);
        var effect = new ParticleEffect("wind") { Emitters = [emitter] };
        return (effect, gravity);
    }

    // ── Profile type detection helpers (zero-alloc, no LINQ) ─────────────────

    // These helpers determine which precipitation effect to activate based on the profile.
    // When the developer registers a custom weather, the profile's precipitation flags govern
    // which effect renders; the mapping is: fog profiles → fog effect, snow profiles → snow, etc.
    // For custom profiles we use a heuristic: temperature and wind speed guide the effect selection.

    private static bool IsRainProfile(in WeatherProfile p) =>
        p.TemperatureMin >= 5f && p.WindSpeedMaxKmh < 20f && p.FogDensity < 0.1f;

    private static bool IsSnowProfile(in WeatherProfile p) =>
        p.TemperatureMax <= 2f;

    private static bool IsHailProfile(in WeatherProfile p) =>
        p.TemperatureMin >= 15f && p.WindSpeedMinKmh >= 3f && p.TemperatureMax <= 40f && !IsSnowProfile(p);

    private static bool IsFogProfile(in WeatherProfile p) =>
        p.FogDensity >= 0.3f && p.WindSpeedMaxKmh <= 0.01f;

    private static bool IsWindProfile(in WeatherProfile p) =>
        p.WindSpeedMaxKmh >= 20f && p.FogDensity < 0.1f && !IsRainProfile(p) && !IsSnowProfile(p);
}
