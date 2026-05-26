using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Data;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using MonoGame.Extended.Particles.Profiles;

namespace Alca.MonoGame.Kernel.Graphics.Particles;

/// <summary>Fluent builder for constructing ParticleEffect instances entirely in code.</summary>
public sealed class ParticleBuilder
{
    private const int DefaultCapacity = 500;

    private Profile _profile = Profile.Point();
    private float _lifetimeMin = 1f;
    private float _lifetimeMax = 2f;
    private float _speed = 50f;
    private int _capacity = DefaultCapacity;
    private Texture2DRegion? _textureRegion;
    private readonly List<Modifier> _modifiers = [];
    private HslColor? _startColor;
    private HslColor? _endColor;

    /// <summary>Sets the texture region used to render individual particles.</summary>
    public ParticleBuilder WithTextureRegion(Texture2DRegion textureRegion)
    {
        _textureRegion = textureRegion;
        return this;
    }

    /// <summary>Sets the maximum number of simultaneously active particles.</summary>
    public ParticleBuilder WithCapacity(int capacity)
    {
        _capacity = capacity;
        return this;
    }

    /// <summary>Configures a directional spray emission profile. Direction defaults to upward.</summary>
    public ParticleBuilder WithSprayProfile(float spread, float speed)
    {
        _profile = Profile.Spray(new Vector2(0f, -1f), spread);
        _speed = speed;
        return this;
    }

    /// <summary>Configures a circle emission profile with outward radiation.</summary>
    public ParticleBuilder WithCircleProfile(float radius)
    {
        _profile = Profile.Circle(radius, CircleRadiation.Out);
        return this;
    }

    /// <summary>Adds a downward linear gravity modifier with the given strength.</summary>
    public ParticleBuilder WithGravity(float gravityY)
    {
        _modifiers.Add(new LinearGravityModifier
        {
            Direction = Vector2.UnitY,
            Strength = gravityY
        });
        return this;
    }

    /// <summary>Sets the minimum and maximum particle lifetime in seconds.</summary>
    public ParticleBuilder WithLifetime(float min, float max)
    {
        _lifetimeMin = min;
        _lifetimeMax = max;
        return this;
    }

    /// <summary>Adds an age-based color interpolation from start to end over the particle's lifetime.</summary>
    public ParticleBuilder WithColorRange(Color start, Color end)
    {
        _startColor = HslColor.FromRgb(start);
        _endColor = HslColor.FromRgb(end);
        return this;
    }

    /// <summary>Builds and returns the configured ParticleEffect. Call after all With* methods.</summary>
    public ParticleEffect Build()
    {
        float avgLifetime = (_lifetimeMin + _lifetimeMax) * 0.5f;

        ParticleEmitter emitter = new(_capacity)
        {
            Profile = _profile,
            TextureRegion = _textureRegion!,
            LifeSpan = avgLifetime,
            Parameters = new ParticleReleaseParameters
            {
                Speed = new ParticleFloatParameter(_speed)
            }
        };

        for (int i = 0; i < _modifiers.Count; i++)
        {
            emitter.Modifiers.Add(_modifiers[i]);
        }

        if (_startColor.HasValue && _endColor.HasValue)
        {
            emitter.Modifiers.Add(new AgeModifier
            {
                Interpolators =
                [
                    new ColorInterpolator
                    {
                        StartValue = _startColor.Value,
                        EndValue = _endColor.Value
                    }
                ]
            });
        }

        return new ParticleEffect("built")
        {
            Emitters = [emitter]
        };
    }
}
