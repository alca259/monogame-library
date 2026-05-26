using nkast.Aether.Physics2D.Dynamics;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>
/// 2D physics simulation world wrapping Aether.Physics2D.
/// Call <see cref="Step"/> each frame from your game's Update or let
/// <see cref="ECS.GameWorld"/> call it automatically when this is assigned to its
/// <see cref="ECS.GameWorld.PhysicsWorld"/> property.
/// </summary>
public sealed class Physics2DWorld
{
    private readonly World _aetherWorld;
    private Physics2DQuery? _query;

    /// <summary>Gets the underlying Aether world. Available after construction.</summary>
    internal World AetherWorld => _aetherWorld;

    /// <summary>Gets the physics query interface for raycasts and overlap tests. Created lazily on first access.</summary>
    public Physics2DQuery Query => _query ??= new Physics2DQuery(this);

    /// <summary>Gets or sets the global gravity vector. Default is (0, -9.8).</summary>
    public Vector2 Gravity
    {
        get => _aetherWorld.Gravity;
        set => _aetherWorld.Gravity = value;
    }

    /// <summary>Gets or sets the number of velocity constraint solver iterations. Higher values are more accurate. Default 8.</summary>
    public int VelocityIterations { get; set; } = 8;

    /// <summary>Gets or sets the number of position correction solver iterations. Higher values are more accurate. Default 3.</summary>
    public int PositionIterations { get; set; } = 3;

    /// <summary>Initializes a new physics world with the given gravity. Defaults to (0, -9.8) if <paramref name="gravity"/> is zero.</summary>
    public Physics2DWorld(Vector2 gravity = default)
    {
        var g = gravity == default ? new Vector2(0f, -9.8f) : gravity;
        _aetherWorld = new World(g);
    }

    /// <summary>Advances the physics simulation by one time step. Call from <c>Game.Update()</c> or rely on <see cref="ECS.GameWorld"/> integration.</summary>
    public void Step(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (dt <= 0f) return;
        var si = new SolverIterations { VelocityIterations = VelocityIterations, PositionIterations = PositionIterations };
        _aetherWorld.Step(dt, ref si);
    }
}

