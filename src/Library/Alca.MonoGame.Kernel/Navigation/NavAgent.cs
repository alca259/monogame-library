using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Navigation;

/// <summary>
/// Moves its <see cref="GameEntity"/> along a navigation path computed by <see cref="Pathfinder"/>.
/// Resolves <see cref="NavGrid"/> and <see cref="Pathfinder"/> from <see cref="ECS.GameWorld"/> in <see cref="Awake"/>.
/// </summary>
public sealed class NavAgent : GameBehaviour
{
    private readonly NavPath _path = new(512);
    private int _currentWaypointIdx;
    private NavGrid? _navGrid;
    private Pathfinder? _pathfinder;
    private TransformBehaviour? _transform;

    // ── Configuration ──────────────────────────────────────────────────────────

    /// <summary>Gets or sets movement speed in world units per second. Default is 100.</summary>
    public float Speed { get; set; } = 100f;

    /// <summary>Gets or sets the distance from a waypoint at which the agent advances to the next one. Default is 5.</summary>
    public float StoppingDistance { get; set; } = 5f;

    /// <summary>Gets or sets the navigation profile defining jump height and movement capabilities.</summary>
    public NavAgentProfile Profile { get; set; } = NavAgentProfile.Default;

    /// <summary>Gets or sets a value indicating whether the transform rotates toward the movement direction. Default is false.</summary>
    public bool RotateTowardMovement { get; set; } = false;

    /// <summary>Gets or sets the rotation speed in radians per second when <see cref="RotateTowardMovement"/> is true. Default is 2π.</summary>
    public float RotationSpeed { get; set; } = MathHelper.TwoPi;

    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets a value indicating whether a path is currently loaded.</summary>
    public bool HasPath => !_path.IsEmpty;

    /// <summary>Gets a value indicating whether the agent is currently moving.</summary>
    public bool IsMoving { get; private set; }

    /// <summary>Gets the last destination set via <see cref="SetDestination"/>.</summary>
    public Vector2 Destination { get; private set; }

    // ── Events ─────────────────────────────────────────────────────────────────

    /// <summary>Raised when the agent reaches its destination. No heap allocation — assign in Initialize/LoadContent, not in Update.</summary>
    public Action? OnDestinationReached { get; set; }

    /// <summary>Raised when <see cref="SetDestination"/> is called but no path could be found.</summary>
    public Action? OnPathNotFound { get; set; }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Awake()
    {
        _navGrid = Entity.World.NavGrid;
        _pathfinder = Entity.World.Pathfinder;
        _transform = Entity.GetComponent<TransformBehaviour>();
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!IsMoving || _path.IsEmpty || _transform is null) return;

        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 currentPos = _transform.Position2d;
        Vector2 target = _path.GetWaypoint(_currentWaypointIdx);
        Vector2 delta2d = target - currentPos;
        float dist = delta2d.Length();

        if (dist <= StoppingDistance)
        {
            _currentWaypointIdx++;
            if (_currentWaypointIdx >= _path.Count)
            {
                Stop();
                OnDestinationReached?.Invoke();
                return;
            }
            target = _path.GetWaypoint(_currentWaypointIdx);
            delta2d = target - currentPos;
            dist = delta2d.Length();
        }

        if (dist <= 0f) return;

        float invDist = 1f / dist;
        Vector2 dir = new(delta2d.X * invDist, delta2d.Y * invDist);
        float moveAmount = Speed * delta;

        _transform.Position2d = moveAmount >= dist
            ? target
            : new Vector2(currentPos.X + dir.X * moveAmount, currentPos.Y + dir.Y * moveAmount);

        if (RotateTowardMovement)
            ApplyRotation(dir, delta);
    }

    // ── API ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates a path to <paramref name="worldPosition"/> and begins movement.
    /// Requires <see cref="NavGrid"/> and <see cref="Pathfinder"/> to be set on the owning <see cref="ECS.GameWorld"/>.
    /// </summary>
    /// <returns><c>true</c> if a path was found and movement has started; <c>false</c> otherwise.</returns>
    public bool SetDestination(Vector2 worldPosition)
    {
        if (_navGrid is null || _pathfinder is null || _transform is null)
            return false;

        Destination = worldPosition;
        _path.Clear();
        _currentWaypointIdx = 0;

        bool found = _pathfinder.FindPath(_navGrid, _transform.Position2d, worldPosition, _path, Profile);
        if (!found)
        {
            IsMoving = false;
            OnPathNotFound?.Invoke();
            return false;
        }

        IsMoving = _path.Count > 0;
        return true;
    }

    /// <summary>Stops movement immediately and clears the current path.</summary>
    public void Stop()
    {
        _path.Clear();
        _currentWaypointIdx = 0;
        IsMoving = false;
    }

    /// <summary>Recalculates the path to the current destination. Useful when the grid changes at runtime.</summary>
    public void RecomputePath()
    {
        if (Destination != default)
            SetDestination(Destination);
    }

    // ── Internal ───────────────────────────────────────────────────────────────

    private void ApplyRotation(Vector2 dir, float delta)
    {
        float targetAngle = MathF.Atan2(dir.Y, dir.X);
        float current = _transform!.Rotation2d;
        float diff = WrapAngle(targetAngle - current);
        float maxDelta = RotationSpeed * delta;

        _transform.Rotation2d = MathF.Abs(diff) <= maxDelta
            ? targetAngle
            : current + MathF.Sign(diff) * maxDelta;
    }

    private static float WrapAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathHelper.TwoPi;
        while (angle < -MathF.PI) angle += MathHelper.TwoPi;
        return angle;
    }
}
