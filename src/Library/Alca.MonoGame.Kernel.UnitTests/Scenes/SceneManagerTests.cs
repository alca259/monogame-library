using Alca.MonoGame.Kernel.Scenes;
using Microsoft.Xna.Framework.Content;

namespace Alca.MonoGame.Kernel.UnitTests.Scenes;

public sealed class SceneManagerTests
{
    private static GameTime MakeGameTime(float elapsedSeconds) =>
        new GameTime(TimeSpan.FromSeconds(elapsedSeconds), TimeSpan.FromSeconds(elapsedSeconds));

    #region Existing fade tests

    [Fact]
    public void CurrentScene_IsNull_WhenFirstCreated()
    {
        SceneManager sut = new();

        Assert.Null(sut.CurrentScene);
    }

    [Fact]
    public void FadeAlpha_IsZero_WhenNoTransitionIsActive()
    {
        SceneManager sut = new();

        Assert.Equal(0f, sut.FadeAlpha);
    }

    [Fact]
    public void RequestChange_WhenNoFadeActive_StartsFadingOut()
    {
        SceneManager sut = new();
        MockScene scene = new();

        sut.RequestChange(scene);
        sut.Update(MakeGameTime(0.001f));

        Assert.True(sut.FadeAlpha > 0f);
    }

    [Fact]
    public void Update_FadingOut_IncreasesAlpha_Proportionally()
    {
        SceneManager sut = new();
        MockScene scene = new();

        sut.RequestChange(scene);
        sut.Update(MakeGameTime(0.15f));

        Assert.True(Math.Abs(sut.FadeAlpha - 0.5f) < 0.01f);
    }

    [Fact]
    public void Update_FadingOut_DoesNotExceedAlphaOne()
    {
        SceneManager sut = new();
        MockScene scene = new();

        sut.RequestChange(scene);
        sut.Update(MakeGameTime(0.29f));

        Assert.True(sut.FadeAlpha <= 1.0f);
    }

    [Fact]
    public void RequestChange_WhenAlreadyFading_DoesNotResetFadeTimer()
    {
        SceneManager sut = new();
        MockScene sceneA = new();
        MockScene sceneB = new();

        sut.RequestChange(sceneA);
        sut.Update(MakeGameTime(0.1f));
        float alphaAfterFirstUpdate = sut.FadeAlpha;

        sut.RequestChange(sceneB);
        sut.Update(MakeGameTime(0.001f));

        Assert.True(sut.FadeAlpha > alphaAfterFirstUpdate);
    }

    #endregion

    #region PushScene / PopScene

    [Fact]
    public void PushScene_InitializesOverlay()
    {
        SceneManager sut = new();
        MockScene overlay = new();

        sut.PushScene(overlay);

        Assert.True(overlay.WasInitialized);
    }

    [Fact]
    public void PushScene_IncreasesOverlayCount()
    {
        SceneManager sut = new();

        sut.PushScene(new MockScene());

        Assert.Equal(1, sut.OverlayCount);
    }

    [Fact]
    public void PopScene_DisposesTopOverlay()
    {
        SceneManager sut = new();
        MockScene overlay = new();
        sut.PushScene(overlay);

        sut.PopScene();

        Assert.True(overlay.IsDisposed);
    }

    [Fact]
    public void PopScene_DecreasesOverlayCount()
    {
        SceneManager sut = new();
        sut.PushScene(new MockScene());
        sut.PushScene(new MockScene());

        sut.PopScene();

        Assert.Equal(1, sut.OverlayCount);
    }

    [Fact]
    public void PopScene_OnEmptyStack_DoesNotThrow()
    {
        SceneManager sut = new();

        Exception? ex = Record.Exception(() => sut.PopScene());

        Assert.Null(ex);
    }

    [Fact]
    public void PushScene_MultipleLayers_StacksCorrectly()
    {
        SceneManager sut = new();

        sut.PushScene(new MockScene());
        sut.PushScene(new MockScene());
        sut.PushScene(new MockScene());

        Assert.Equal(3, sut.OverlayCount);
    }

    #endregion

    #region Update routing

    [Fact]
    public void Update_WithNoStack_UpdatesCurrentScene()
    {
        SceneManager sut = new();
        MockScene base_ = ActivateScene(sut);

        sut.Update(MakeGameTime(0.016f));

        Assert.Equal(1, base_.UpdateCount);
    }

    [Fact]
    public void Update_WithOverlayOnStack_OnlyUpdatesTopOverlay()
    {
        SceneManager sut = new();
        MockScene base_ = ActivateScene(sut);
        MockScene overlay = new();
        sut.PushScene(overlay);

        sut.Update(MakeGameTime(0.016f));

        Assert.Equal(0, base_.UpdateCount);
        Assert.Equal(1, overlay.UpdateCount);
    }

    [Fact]
    public void Update_WithTwoOverlays_OnlyTopOverlayUpdates()
    {
        SceneManager sut = new();
        ActivateScene(sut);
        MockScene lower = new();
        MockScene upper = new();
        sut.PushScene(lower);
        sut.PushScene(upper);

        sut.Update(MakeGameTime(0.016f));

        Assert.Equal(0, lower.UpdateCount);
        Assert.Equal(1, upper.UpdateCount);
    }

    [Fact]
    public void Update_AfterPopScene_ResumesBaseSceneUpdate()
    {
        SceneManager sut = new();
        MockScene base_ = ActivateScene(sut);
        sut.PushScene(new MockScene());
        sut.PopScene();

        sut.Update(MakeGameTime(0.016f));

        Assert.Equal(1, base_.UpdateCount);
    }

    #endregion

    #region Draw routing

    [Fact]
    public void Draw_WithNoStack_DrawsCurrentScene()
    {
        SceneManager sut = new();
        MockScene base_ = ActivateScene(sut);

        sut.Draw(MakeGameTime(0.016f));

        Assert.Equal(1, base_.DrawCount);
    }

    [Fact]
    public void Draw_WithOverlayIsOverlayTrue_DrawsBaseAndOverlay()
    {
        SceneManager sut = new();
        MockScene base_ = ActivateScene(sut);
        MockScene overlay = new(isOverlay: true);
        sut.PushScene(overlay);

        sut.Draw(MakeGameTime(0.016f));

        Assert.Equal(1, base_.DrawCount);
        Assert.Equal(1, overlay.DrawCount);
    }

    [Fact]
    public void Draw_WithOverlayIsOverlayFalse_DrawsOnlyOverlay()
    {
        SceneManager sut = new();
        MockScene base_ = ActivateScene(sut);
        MockScene overlay = new(isOverlay: false);
        sut.PushScene(overlay);

        sut.Draw(MakeGameTime(0.016f));

        Assert.Equal(0, base_.DrawCount);
        Assert.Equal(1, overlay.DrawCount);
    }

    [Fact]
    public void Draw_WithTwoOverlaysAndTopIsOverlay_DrawsAllThreeLayers()
    {
        SceneManager sut = new();
        MockScene base_ = ActivateScene(sut);
        MockScene lower = new(isOverlay: true);
        MockScene upper = new(isOverlay: true);
        sut.PushScene(lower);
        sut.PushScene(upper);

        sut.Draw(MakeGameTime(0.016f));

        Assert.Equal(1, base_.DrawCount);
        Assert.Equal(1, lower.DrawCount);
        Assert.Equal(1, upper.DrawCount);
    }

    #endregion

    #region RequestChange clears stack

    [Fact]
    public void RequestChange_WithOverlaysOnStack_DisposesAllOverlays()
    {
        SceneManager sut = new();
        MockScene overlayA = new();
        MockScene overlayB = new();
        sut.PushScene(overlayA);
        sut.PushScene(overlayB);

        sut.RequestChange(new MockScene());

        Assert.True(overlayA.IsDisposed);
        Assert.True(overlayB.IsDisposed);
    }

    [Fact]
    public void RequestChange_WithOverlaysOnStack_ClearsStackCount()
    {
        SceneManager sut = new();
        sut.PushScene(new MockScene());
        sut.PushScene(new MockScene());

        sut.RequestChange(new MockScene());

        Assert.Equal(0, sut.OverlayCount);
    }

    #endregion

    #region IsOverlay property

    [Fact]
    public void Scene_IsOverlay_DefaultIsFalse()
    {
        MockScene scene = new();

        Assert.False(scene.IsOverlay);
    }

    [Fact]
    public void Scene_IsOverlay_TrueWhenConstructedAsOverlay()
    {
        MockScene scene = new(isOverlay: true);

        Assert.True(scene.IsOverlay);
    }

    #endregion

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Bypasses the fade by forcibly setting CurrentScene via RequestChange + instant skip.</summary>
    private static MockScene ActivateScene(SceneManager sm)
    {
        MockScene scene = new();
        // Drive the fade machine to completion so _currentScene is set
        sm.RequestChange(scene);
        sm.Update(MakeGameTime(0.3f)); // FadingOut completes → ApplyPendingChange
        sm.Update(MakeGameTime(0.3f)); // FadingIn completes → FadeState.None
        scene.ResetCounters();
        return scene;
    }

    private sealed class MockScene : Scene
    {
        private readonly bool _isOverlay;

        public bool WasInitialized { get; private set; }
        public int UpdateCount { get; private set; }
        public int DrawCount { get; private set; }

        public override bool IsOverlay => _isOverlay;

        internal MockScene(bool isOverlay = false) : base(new ContentManager(new StubServiceProvider()) { RootDirectory = "Content" })
        {
            _isOverlay = isOverlay;
        }

        public override void Initialize()
        {
            base.Initialize();
            WasInitialized = true;
        }

        public override void Update(GameTime gameTime) => UpdateCount++;

        public override void Draw(GameTime gameTime) => DrawCount++;

        public void ResetCounters()
        {
            UpdateCount = 0;
            DrawCount = 0;
        }

        private sealed class StubServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }
    }
}
