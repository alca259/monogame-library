using Alca.MonoGame.Kernel.Scenes;
using Alca.MonoGame.Kernel.UI;
using Microsoft.Xna.Framework.Content;

namespace Alca.MonoGame.Kernel.UnitTests.Scenes;

public sealed class SceneUIWiringTests
{
    [Fact]
    public void EnableUI_CreatesUIRoot()
    {
        UIScene sut = new();
        sut.Initialize();

        Assert.NotNull(sut.ExposedUIRoot);
    }

    [Fact]
    public void EnableUI_IsIdempotent()
    {
        UIScene sut = new();
        sut.Initialize();
        UIRoot? firstRoot = sut.ExposedUIRoot;
        sut.CallEnableUIAgain();

        Assert.Same(firstRoot, sut.ExposedUIRoot);
    }

    [Fact]
    public void EnableUI_NotCalled_UIRootIsNull()
    {
        NoUIScene sut = new();
        sut.Initialize();

        Assert.Null(sut.ExposedUIRoot);
    }

    [Fact]
    public void SceneManager_ActiveUIRoot_ReturnsNullWhenNoScene()
    {
        SceneManager manager = new();

        Assert.Null(manager.ActiveUIRoot);
    }

    // ── test doubles ─────────────────────────────────────────────────────────

    private sealed class UIScene : Scene
    {
        public UIRoot? ExposedUIRoot => UIRoot;

        internal UIScene() : base(new ContentManager(new StubServiceProvider()) { RootDirectory = "Content" }) { }

        protected override void PreInitialize() => EnableUI();

        public void CallEnableUIAgain() => EnableUI();
    }

    private sealed class NoUIScene : Scene
    {
        public UIRoot? ExposedUIRoot => UIRoot;

        internal NoUIScene() : base(new ContentManager(new StubServiceProvider()) { RootDirectory = "Content" }) { }
    }

    private sealed class StubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
