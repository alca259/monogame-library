using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.UnitTests.ECS;

public sealed class GameBehaviourTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    // ── Lifecycle order ────────────────────────────────────────────────────────

    [Fact]
    public void Awake_IsCalledImmediately_WhenAddedToEntity()
    {
        var world = new GameWorld();
        var spy = new LifecycleSpy();
        world.CreateEntity("E").Add(spy);

        Assert.True(spy.AwakeCalled);
        Assert.False(spy.StartCalled);
    }

    [Fact]
    public void Start_IsCalledOnFirstUpdate_BeforeUpdate()
    {
        var world = new GameWorld();
        var spy = new LifecycleSpy();
        world.CreateEntity("E").Add(spy);

        world.Update(AnyGameTime());

        Assert.True(spy.StartCalled);
        Assert.True(spy.StartCalledBeforeUpdate);
    }

    [Fact]
    public void Start_IsCalledOnlyOnce_AcrossMultipleUpdates()
    {
        var world = new GameWorld();
        var spy = new LifecycleSpy();
        world.CreateEntity("E").Add(spy);

        world.Update(AnyGameTime());
        world.Update(AnyGameTime());
        world.Update(AnyGameTime());

        Assert.Equal(1, spy.StartCallCount);
    }

    [Fact]
    public void OnDestroy_IsCalledWhenWorldDestroys()
    {
        var world = new GameWorld();
        var spy = new LifecycleSpy();
        var entity = world.CreateEntity("E");
        entity.Add(spy);

        world.Update(AnyGameTime());
        world.Destroy(entity);
        world.Update(AnyGameTime());

        Assert.True(spy.OnDestroyCalled);
    }

    // ── Enabled flag ──────────────────────────────────────────────────────────

    [Fact]
    public void Update_IsSkipped_WhenBehaviourIsDisabled()
    {
        var world = new GameWorld();
        var spy = new LifecycleSpy();
        var entity = world.CreateEntity("E");
        entity.Add(spy);
        spy.Enabled = false;

        world.Update(AnyGameTime());
        world.Update(AnyGameTime());

        Assert.Equal(0, spy.UpdateCallCount);
    }

    [Fact]
    public void Update_Resumes_WhenBehaviourIsReEnabled()
    {
        var world = new GameWorld();
        var spy = new LifecycleSpy();
        var entity = world.CreateEntity("E");
        entity.Add(spy);
        spy.Enabled = false;

        world.Update(AnyGameTime());
        spy.Enabled = true;
        world.Update(AnyGameTime());

        Assert.Equal(1, spy.UpdateCallCount);
    }

    // ── Entity reference ───────────────────────────────────────────────────────

    [Fact]
    public void Entity_IsSet_WhenAddedToEntity()
    {
        var world = new GameWorld();
        var spy = new LifecycleSpy();
        var entity = world.CreateEntity("E");
        entity.Add(spy);

        Assert.Same(entity, spy.Entity);
    }

    [Fact]
    public void Entity_ThrowsInvalidOperationException_WhenAccessedBeforeAttaching()
    {
        var spy = new LifecycleSpy();

        Assert.Throws<InvalidOperationException>(() => _ = spy.Entity);
    }

    [Fact]
    public void Entity_ThrowsInvalidOperationException_WhenAttachedToSecondEntity()
    {
        var world = new GameWorld();
        var spy = new LifecycleSpy();
        var entity = world.CreateEntity("E");
        entity.Add(spy);

        var entity2 = world.CreateEntity("E2");
        Assert.Throws<InvalidOperationException>(() => entity2.Add(spy));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private sealed class LifecycleSpy : GameBehaviour
    {
        public bool AwakeCalled { get; private set; }
        public bool StartCalled { get; private set; }
        public bool OnDestroyCalled { get; private set; }
        public int StartCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public bool StartCalledBeforeUpdate { get; private set; }

        private bool _startCompleted;

        public override void Awake() => AwakeCalled = true;

        public override void Start()
        {
            StartCalled = true;
            StartCallCount++;
            _startCompleted = true;
        }

        public override void Update(GameTime gameTime)
        {
            if (UpdateCallCount == 0)
                StartCalledBeforeUpdate = _startCompleted;
            UpdateCallCount++;
        }

        public override void OnDestroy() => OnDestroyCalled = true;
    }
}
