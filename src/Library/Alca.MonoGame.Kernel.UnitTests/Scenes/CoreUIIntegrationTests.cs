using Alca.MonoGame.Kernel.Input;
using Alca.MonoGame.Kernel.Scenes;
using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Focus;
using Alca.MonoGame.Kernel.UI.Interaction;
using Microsoft.Xna.Framework.Content;

namespace Alca.MonoGame.Kernel.UnitTests.Scenes;

public sealed class CoreUIIntegrationTests
{
    [Fact]
    public void UIInteractionManager_Update_WithEmptyUIRoot_DoesNotThrow()
    {
        UIInteractionManager interaction = new();
        UIRoot root = new();
        root.Bounds = new Rectangle(0, 0, 800, 600);
        MouseInfo mouse = new();
        UIFocusManager focus = new();

        Exception? ex = Record.Exception(() => interaction.Update(root, mouse, focus));

        Assert.Null(ex);
    }

    [Fact]
    public void SceneManager_ActiveUIRoot_IsNotNullAfterSceneWithUIInitializes()
    {
        UIScene scene = new();
        SceneManager manager = new();

        manager.RequestChange(scene);
        manager.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));  // start fade-out
        manager.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.5f))); // complete fade

        Assert.NotNull(manager.ActiveUIRoot);
    }

    // ── test doubles ─────────────────────────────────────────────────────────

    private sealed class UIScene : Scene
    {
        internal UIScene() : base(new ContentManager(new StubServiceProvider()) { RootDirectory = "Content" }) { }

        protected override void PreInitialize() => EnableUI();
    }

    private sealed class StubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
