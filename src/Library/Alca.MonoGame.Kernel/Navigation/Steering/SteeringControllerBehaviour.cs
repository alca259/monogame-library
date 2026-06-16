using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Navigation.Steering;

/// <summary>
/// Combines multiple <see cref="ISteeringBehavior"/> contributions (weighted sum) and optionally
/// applies the result to the entity's <see cref="TransformBehaviour"/> each frame.
/// </summary>
public sealed class SteeringControllerBehaviour : GameBehaviour
{
    private const int MaxEntries = 8;

    private readonly SteeringEntry[] _entries = new SteeringEntry[MaxEntries];
    private int _entryCount;

    /// <summary>Gets or sets the maximum speed the final combined vector is clamped to.</summary>
    public float MaxResultSpeed { get; set; } = 300f;

    /// <summary>
    /// Gets or sets a value indicating whether the combined velocity is applied to
    /// <see cref="TransformBehaviour.Position2d"/> each frame.
    /// When <c>false</c>, callers read <see cref="ResultVelocity"/> and apply it themselves.
    /// </summary>
    public bool ApplyToTransform { get; set; } = true;

    /// <summary>Gets the combined steering velocity from the last <see cref="Update"/> call.</summary>
    public Vector2 ResultVelocity { get; private set; }

    /// <summary>Adds a behavior with the specified weight. Throws if the capacity of 8 is exceeded.</summary>
    public void Add(ISteeringBehavior behavior, float weight = 1f)
    {
        if (_entryCount >= MaxEntries)
            throw new InvalidOperationException(
                $"SteeringController supports at most {MaxEntries} concurrent behaviors.");

        _entries[_entryCount++] = new SteeringEntry(behavior, weight);
    }

    /// <summary>Removes the first entry matching <paramref name="behavior"/> (swap-and-pop).</summary>
    public void Remove(ISteeringBehavior behavior)
    {
        for (int i = 0; i < _entryCount; i++)
        {
            if (!ReferenceEquals(_entries[i]._behavior, behavior)) continue;
            _entries[i] = _entries[_entryCount - 1];
            _entries[_entryCount - 1] = default;
            _entryCount--;
            return;
        }
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (_entryCount == 0)
        {
            ResultVelocity = Vector2.Zero;
            return;
        }

        Vector2 pos = Entity.Transform.Position2d;
        Vector2 vel = ResultVelocity;
        Vector2 combined = Vector2.Zero;

        for (int i = 0; i < _entryCount; i++)
            combined += _entries[i]._behavior.CalculateSteering(pos, vel, gameTime) * _entries[i]._weight;

        float len = combined.Length();
        if (len > MaxResultSpeed && len > 0.001f)
            combined = (combined / len) * MaxResultSpeed;

        ResultVelocity = combined;

        if (ApplyToTransform)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Entity.Transform.Position2d += combined * dt;
        }
    }

    #region Internal types
    private readonly struct SteeringEntry
    {
        internal readonly ISteeringBehavior _behavior;
        internal readonly float _weight;

        internal SteeringEntry(ISteeringBehavior behavior, float weight)
        {
            _behavior = behavior;
            _weight = weight;
        }
    }
    #endregion
}
