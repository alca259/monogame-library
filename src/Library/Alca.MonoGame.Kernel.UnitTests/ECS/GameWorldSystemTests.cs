using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.UnitTests.ECS;

public sealed class GameWorldSystemTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    // ── AddSystem ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddSystem_CallsInitializeOnce_WithWorldSet()
    {
        var world = new GameWorld();
        var system = new RecordingSystem();

        world.AddSystem(system);

        Assert.Equal(1, system.InitializeCallCount);
        Assert.Same(world, system.WorldAtInitialize);
    }

    [Fact]
    public void AddSystem_Generic_CreatesAndRegisters()
    {
        var world = new GameWorld();

        var system = world.AddSystem<RecordingSystem>();

        Assert.NotNull(system);
        Assert.Equal(1, world.SystemCount);
    }

    [Fact]
    public void AddSystem_SetsWorldReference()
    {
        var world = new GameWorld();
        var system = new RecordingSystem();

        world.AddSystem(system);

        Assert.Same(world, system.World);
    }

    // ── SystemCount ────────────────────────────────────────────────────────────

    [Fact]
    public void SystemCount_ReflectsAddAndRemove()
    {
        var world = new GameWorld();
        var s1 = new RecordingSystem();
        var s2 = new RecordingSystem();

        world.AddSystem(s1);
        world.AddSystem(s2);
        Assert.Equal(2, world.SystemCount);

        world.RemoveSystem(s1);
        Assert.Equal(1, world.SystemCount);
    }

    // ── Execution order ────────────────────────────────────────────────────────

    [Fact]
    public void Update_RunsSystemsInOrderAscending()
    {
        var world = new GameWorld();
        var log = new List<int>();

        var s30 = new OrderedSystem(30, log);
        var s10 = new OrderedSystem(10, log);
        var s20 = new OrderedSystem(20, log);
        world.AddSystem(s30);
        world.AddSystem(s10);
        world.AddSystem(s20);

        world.Update(AnyGameTime());

        Assert.Equal([10, 20, 30], log);
    }

    [Fact]
    public void Update_EqualOrder_PreservesInsertionOrder()
    {
        var world = new GameWorld();
        var log = new List<string>();

        var sA = new NamedSystem("A", 0, log);
        var sB = new NamedSystem("B", 0, log);
        var sC = new NamedSystem("C", 0, log);
        world.AddSystem(sA);
        world.AddSystem(sB);
        world.AddSystem(sC);

        world.Update(AnyGameTime());

        Assert.Equal(["A", "B", "C"], log);
    }

    // ── Enabled flag ───────────────────────────────────────────────────────────

    [Fact]
    public void Update_DisabledSystem_IsSkipped()
    {
        var world = new GameWorld();
        var system = new RecordingSystem { Enabled = false };
        world.AddSystem(system);

        world.Update(AnyGameTime());

        Assert.Equal(0, system.UpdateCallCount);
    }

    [Fact]
    public void Update_WhenWorldDisabled_SkipsSystems()
    {
        var world = new GameWorld();
        var system = new RecordingSystem();
        world.AddSystem(system);
        world.Update(AnyGameTime()); // flush entities; system runs once
        world.IsEnabled = false;

        world.Update(AnyGameTime());
        world.Update(AnyGameTime());

        Assert.Equal(1, system.UpdateCallCount);
    }

    // ── Systems before entities ────────────────────────────────────────────────

    [Fact]
    public void Update_RunsSystemsBeforeEntities()
    {
        var world = new GameWorld();
        var executionLog = new List<string>();

        var system = new LoggingSystem("sys", executionLog);
        world.AddSystem(system);

        var entity = world.CreateEntity("E");
        var behaviour = new LoggingBehaviour("ent", executionLog);
        entity.Add(behaviour);

        world.Update(AnyGameTime()); // flush: entity visible; system + entity run

        Assert.Equal(["sys", "ent"], executionLog);
    }

    // ── Flush interaction ──────────────────────────────────────────────────────

    [Fact]
    public void Update_AfterFlush_SeesNewlyCreatedEntities()
    {
        var world = new GameWorld();
        var system = new EntityCountingSystem();
        world.AddSystem(system);

        world.CreateEntity("E1");
        world.CreateEntity("E2");
        world.Update(AnyGameTime()); // FlushPending runs before system

        Assert.Equal(2, system.LastSeenEntityCount);
    }

    // ── RemoveSystem ───────────────────────────────────────────────────────────

    [Fact]
    public void RemoveSystem_CallsOnRemoved()
    {
        var world = new GameWorld();
        var system = new RecordingSystem();
        world.AddSystem(system);

        world.RemoveSystem(system);

        Assert.Equal(1, system.OnRemovedCallCount);
    }

    [Fact]
    public void RemoveSystem_RemovesFromUpdateLoop()
    {
        var world = new GameWorld();
        var system = new RecordingSystem();
        world.AddSystem(system);
        world.Update(AnyGameTime());

        world.RemoveSystem(system);
        world.Update(AnyGameTime());

        Assert.Equal(1, system.UpdateCallCount);
    }

    [Fact]
    public void RemoveSystem_NotRegistered_ReturnsFalse()
    {
        var world = new GameWorld();
        var system = new RecordingSystem();

        bool result = world.RemoveSystem(system);

        Assert.False(result);
    }

    // ── GetSystem ──────────────────────────────────────────────────────────────

    [Fact]
    public void GetSystem_ReturnsRegisteredInstance()
    {
        var world = new GameWorld();
        var system = world.AddSystem<RecordingSystem>();

        var found = world.GetSystem<RecordingSystem>();

        Assert.Same(system, found);
    }

    [Fact]
    public void GetSystem_ByBaseType_Matches()
    {
        var world = new GameWorld();
        world.AddSystem<RecordingSystem>();

        var found = world.GetSystem<GameSystem>();

        Assert.NotNull(found);
    }

    [Fact]
    public void GetSystem_WhenAbsent_ReturnsNull()
    {
        var world = new GameWorld();

        var found = world.GetSystem<RecordingSystem>();

        Assert.Null(found);
    }

    // ── Destroy ────────────────────────────────────────────────────────────────

    [Fact]
    public void Destroy_CallsOnRemovedForAllSystems_AndClears()
    {
        var world = new GameWorld();
        var s1 = new RecordingSystem();
        var s2 = new RecordingSystem();
        world.AddSystem(s1);
        world.AddSystem(s2);

        world.Destroy();

        Assert.Equal(1, s1.OnRemovedCallCount);
        Assert.Equal(1, s2.OnRemovedCallCount);
        Assert.Equal(0, world.SystemCount);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private sealed class RecordingSystem : GameSystem
    {
        public int InitializeCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int OnRemovedCallCount { get; private set; }
        public GameWorld? WorldAtInitialize { get; private set; }

        public override void Initialize()
        {
            InitializeCallCount++;
            WorldAtInitialize = World;
        }

        public override void Update(GameTime gameTime) => UpdateCallCount++;
        public override void OnRemoved() => OnRemovedCallCount++;
    }

    private sealed class OrderedSystem : GameSystem
    {
        private readonly List<int> _log;

        public OrderedSystem(int order, List<int> log)
        {
            Order = order;
            _log = log;
        }

        public override void Update(GameTime gameTime) => _log.Add(Order);
    }

    private sealed class NamedSystem : GameSystem
    {
        private readonly string _name;
        private readonly List<string> _log;

        public NamedSystem(string name, int order, List<string> log)
        {
            _name = name;
            Order = order;
            _log = log;
        }

        public override void Update(GameTime gameTime) => _log.Add(_name);
    }

    private sealed class LoggingSystem : GameSystem
    {
        private readonly string _label;
        private readonly List<string> _log;

        public LoggingSystem(string label, List<string> log)
        {
            _label = label;
            _log = log;
        }

        public override void Update(GameTime gameTime) => _log.Add(_label);
    }

    private sealed class LoggingBehaviour : GameBehaviour
    {
        private readonly string _label;
        private readonly List<string> _log;

        public LoggingBehaviour(string label, List<string> log)
        {
            _label = label;
            _log = log;
        }

        public override void Update(GameTime gameTime) => _log.Add(_label);
    }

    private sealed class EntityCountingSystem : GameSystem
    {
        public int LastSeenEntityCount { get; private set; }

        public override void Update(GameTime gameTime)
        {
            LastSeenEntityCount = World.EntityCount;
        }
    }
}
