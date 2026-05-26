using Alca.MonoGame.Kernel.StateMachine;

namespace Alca.MonoGame.Kernel.UnitTests.StateMachine;

public sealed class StateMachineTests
{
    private enum TestState { Idle, Running, Dead }

    private sealed class SpyState : IState<TestState>
    {
        public TestState? EnteredFrom { get; private set; }
        public TestState? ExitedTo { get; private set; }
        public int UpdateCount { get; private set; }

        public void Enter(TestState previousState) => EnteredFrom = previousState;
        public void Update(GameTime gameTime) => UpdateCount++;
        public void Exit(TestState nextState) => ExitedTo = nextState;
    }

    private static StateMachine<TestState> CreateFsm(out SpyState idle, out SpyState running)
    {
        var fsm = new StateMachine<TestState>();
        idle = new SpyState();
        running = new SpyState();
        fsm.Register(TestState.Idle, idle);
        fsm.Register(TestState.Running, running);
        fsm.Transition(TestState.Idle);
        return fsm;
    }

    [Fact]
    public void Transition_CallsExitOnCurrentAndEnterOnNext()
    {
        var fsm = CreateFsm(out var idle, out var running);

        fsm.Transition(TestState.Running);

        Assert.Equal(TestState.Running, idle.ExitedTo);
        Assert.Equal(TestState.Idle, running.EnteredFrom);
    }

    [Fact]
    public void Transition_SameState_IsNoOp()
    {
        var fsm = CreateFsm(out var idle, out _);
        // Reset after initial transition
        var spy = new SpyState();
        var fsm2 = new StateMachine<TestState>();
        fsm2.Register(TestState.Idle, spy);
        fsm2.Transition(TestState.Idle);

        fsm2.Transition(TestState.Idle); // same state again

        Assert.Null(spy.ExitedTo);
        Assert.Equal(TestState.Idle, spy.EnteredFrom);
    }

    [Fact]
    public void Transition_UnknownState_ThrowsKeyNotFoundException()
    {
        var fsm = new StateMachine<TestState>();
        fsm.Register(TestState.Idle, new SpyState());
        fsm.Transition(TestState.Idle);

        Assert.Throws<KeyNotFoundException>(() => fsm.Transition(TestState.Dead));
    }

    [Fact]
    public void Register_DuplicateState_ThrowsArgumentException()
    {
        var fsm = new StateMachine<TestState>();
        fsm.Register(TestState.Idle, new SpyState());

        Assert.Throws<ArgumentException>(() => fsm.Register(TestState.Idle, new SpyState()));
    }

    [Fact]
    public void Update_CallsUpdateOnCurrentState()
    {
        var fsm = CreateFsm(out var idle, out _);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

        fsm.Update(gameTime);
        fsm.Update(gameTime);

        Assert.Equal(2, idle.UpdateCount);
    }

    [Fact]
    public void PreviousState_ReflectsLastTransition()
    {
        var fsm = CreateFsm(out _, out _);

        fsm.Transition(TestState.Running);

        Assert.Equal(TestState.Idle, fsm.PreviousState);
        Assert.Equal(TestState.Running, fsm.CurrentState);
    }
}
