using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Scenes;
using Microsoft.Xna.Framework.Content;

namespace Alca.MonoGame.Kernel.UnitTests.Scenes;

public sealed class SceneWorldBindingTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    [Fact]
    public void CreateWorld_WhenOverridden_WorldIsNotNull()
    {
        WorldScene sut = new();
        sut.Initialize();

        Assert.NotNull(sut.ExposedWorld);
    }

    [Fact]
    public void Initialize_CallsInitializeWorld()
    {
        WorldScene sut = new();
        sut.Initialize();

        Assert.True(sut.WasInitializeWorldCalled);
    }

    [Fact]
    public void Update_WhenWorldSet_CallsWorldUpdate()
    {
        WorldScene sut = new();
        sut.Initialize();
        GameEntity entity = sut.ExposedWorld!.CreateEntity("E");
        UpdateTrackerBehaviour tracker = new();
        entity.Add(tracker);

        sut.Update(AnyGameTime()); // base.Update flushes entity + updates
        sut.Update(AnyGameTime()); // second tick to verify update was routed

        Assert.True(tracker.UpdateCount > 0);
    }

    [Fact]
    public void Draw_WhenWorldSet_CallsWorldDraw()
    {
        WorldScene sut = new();
        sut.Initialize();
        GameEntity entity = sut.ExposedWorld!.CreateEntity("E");
        DrawTrackerBehaviour tracker = new();
        entity.Add(tracker);

        sut.Update(AnyGameTime()); // flush entity into the active entity list
        sut.Draw(AnyGameTime());   // should route to World.Draw

        Assert.Equal(1, tracker.DrawCount);
    }

    [Fact]
    public void UnloadContent_WhenWorldSet_CallsWorldDestroy()
    {
        WorldScene sut = new();
        sut.Initialize();
        GameEntity entity = sut.ExposedWorld!.CreateEntity("E");
        DestroyTrackerBehaviour tracker = new();
        entity.Add(tracker);
        sut.Update(AnyGameTime()); // flush entity

        sut.UnloadContent();

        Assert.True(tracker.WasDestroyed);
    }

    // ── test doubles ─────────────────────────────────────────────────────────

    private sealed class WorldScene : Scene
    {
        public bool WasInitializeWorldCalled { get; private set; }
        public GameWorld? ExposedWorld => World;

        internal WorldScene() : base(new ContentManager(new StubServiceProvider()) { RootDirectory = "Content" }) { }

        protected override GameWorld? CreateWorld() => new GameWorld();

        protected override void InitializeWorld() => WasInitializeWorldCalled = true;

        public override void Update(GameTime gameTime) => base.Update(gameTime);

        public override void Draw(GameTime gameTime) => base.Draw(gameTime);
    }

    private sealed class UpdateTrackerBehaviour : GameBehaviour
    {
        public int UpdateCount { get; private set; }
        public override void Update(GameTime gameTime) => UpdateCount++;
    }

    private sealed class DrawTrackerBehaviour : GameBehaviour
    {
        public int DrawCount { get; private set; }
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) => DrawCount++;
    }

    private sealed class DestroyTrackerBehaviour : GameBehaviour
    {
        public bool WasDestroyed { get; private set; }
        public override void OnDestroy() => WasDestroyed = true;
    }

    private sealed class StubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
