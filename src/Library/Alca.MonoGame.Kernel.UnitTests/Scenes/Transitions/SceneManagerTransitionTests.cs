using Alca.MonoGame.Kernel.Scenes;
using Alca.MonoGame.Kernel.Scenes.Transitions;
using Microsoft.Xna.Framework.Content;

namespace Alca.MonoGame.Kernel.UnitTests.Scenes.Transitions;

/// <summary>
/// Integration tests verifying that <see cref="SceneManager"/> correctly routes through
/// a custom <see cref="ISceneTransition"/> while remaining backward-compatible when none is provided.
/// </summary>
public sealed class SceneManagerTransitionTests
{
    private static GameTime MakeGameTime(float elapsedSeconds) =>
        new GameTime(TimeSpan.FromSeconds(elapsedSeconds), TimeSpan.FromSeconds(elapsedSeconds));

    // ── Backward-compat: no transition ──────────────────────────────────────

    [Fact]
    public void RequestChange_WithNullTransition_UseBuiltInFade()
    {
        SceneManager sut = new();
        MockScene scene = new();

        sut.RequestChange(scene, null);
        sut.Update(MakeGameTime(0.001f));

        Assert.True(sut.FadeAlpha > 0f);
    }

    [Fact]
    public void RequestChange_NoTransitionArg_WorksIdenticalToNoOverload()
    {
        SceneManager sutA = new();
        SceneManager sutB = new();
        MockScene sceneA = new();
        MockScene sceneB = new();

        sutA.RequestChange(sceneA);
        sutB.RequestChange(sceneB, null);

        sutA.Update(MakeGameTime(0.15f));
        sutB.Update(MakeGameTime(0.15f));

        Assert.Equal(sutA.FadeAlpha, sutB.FadeAlpha, 3);
    }

    // ── Custom transition ────────────────────────────────────────────────────

    [Fact]
    public void RequestChange_WithCustomTransition_CustomTransitionIsCalledToUpdate()
    {
        SceneManager sut = new();
        MockScene scene = new();
        TrackingTransition transition = new();

        sut.RequestChange(scene, transition);
        sut.Update(MakeGameTime(0.016f));

        Assert.True(transition.UpdateCalled);
    }

    [Fact]
    public void RequestChange_WithCustomTransition_BeginTransitionOutCalled()
    {
        SceneManager sut = new();
        MockScene scene = new();
        TrackingTransition transition = new();

        sut.RequestChange(scene, transition);

        Assert.True(transition.BeginOutCalled);
    }

    [Fact]
    public void RequestChange_WithCustomTransition_SceneIsAppliedAfterOutCompletes()
    {
        SceneManager sut = new();
        MockScene scene = new();
        // Transition completes instantly on first Update
        InstantTransition transition = new();

        sut.RequestChange(scene, transition);
        sut.Update(MakeGameTime(0.001f)); // out completes → apply → in phase
        sut.Update(MakeGameTime(0.001f)); // in completes → None

        Assert.Equal(scene, sut.CurrentScene);
    }

    [Fact]
    public void DefaultTransitionDuration_CanBeChanged()
    {
        SceneManager sut = new();
        sut.DefaultTransitionDuration = 2f;

        Assert.Equal(2f, sut.DefaultTransitionDuration);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private sealed class MockScene : Scene
    {
        public override bool IsOverlay => false;

        internal MockScene() : base(new ContentManager(new StubServiceProvider()) { RootDirectory = "Content" }) { }

        public override void Update(GameTime gameTime) { }
        public override void Draw(GameTime gameTime) { }

        private sealed class StubServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }
    }

    private sealed class TrackingTransition : ISceneTransition
    {
        public bool BeginOutCalled { get; private set; }
        public bool BeginInCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool ResetCalled { get; private set; }

        public bool IsTransitionOutComplete { get; private set; } = false;
        public bool IsTransitionInComplete { get; private set; } = false;

        public void BeginTransitionOut(float durationSeconds) => BeginOutCalled = true;
        public void BeginTransitionIn(float durationSeconds) => BeginInCalled = true;
        public void Update(float deltaTime) => UpdateCalled = true;
        public void Draw(SpriteBatch spriteBatch, Viewport viewport) { }
        public void Reset() => ResetCalled = true;
    }

    /// <summary>Transition that reports completion on the very first Update call.</summary>
    private sealed class InstantTransition : ISceneTransition
    {
        private bool _outPhase = false;

        public bool IsTransitionOutComplete { get; private set; }
        public bool IsTransitionInComplete { get; private set; }

        public void BeginTransitionOut(float d) { _outPhase = true; IsTransitionOutComplete = false; IsTransitionInComplete = false; }
        public void BeginTransitionIn(float d) { _outPhase = false; IsTransitionOutComplete = false; IsTransitionInComplete = false; }

        public void Update(float dt)
        {
            if (_outPhase)
                IsTransitionOutComplete = true;
            else
                IsTransitionInComplete = true;
        }

        public void Draw(SpriteBatch spriteBatch, Viewport viewport) { }
        public void Reset() { IsTransitionOutComplete = false; IsTransitionInComplete = false; }
    }
}
