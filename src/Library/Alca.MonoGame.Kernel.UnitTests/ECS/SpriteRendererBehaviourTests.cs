using System.Reflection;
using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.UnitTests.ECS;

// SpriteRendererBehaviour.Draw requires a live Texture2D and SpriteBatch (GPU dependency).
// Tests here verify the public API surface, type hierarchy, and draw-path detection.
// Rendering correctness belongs in integration tests with a headless MonoGame device.
public sealed class SpriteRendererBehaviourTests
{
    private static readonly Type[] _drawParams = [typeof(GameTime), typeof(SpriteBatch)];

    // ── Type contract ──────────────────────────────────────────────────────────

    [Fact]
    public void Class_IsSealed()
        => Assert.True(typeof(SpriteRendererBehaviour).IsSealed);

    [Fact]
    public void Class_InheritsGameBehaviour()
        => Assert.True(typeof(SpriteRendererBehaviour).IsSubclassOf(typeof(GameBehaviour)));

    [Fact]
    public void Constructor_AcceptsTexture2D()
    {
        var ctor = typeof(SpriteRendererBehaviour).GetConstructor([typeof(Texture2D)]);
        Assert.NotNull(ctor);
    }

    // ── Draw override detection ────────────────────────────────────────────────

    [Fact]
    public void Draw_IsOverridden_SoGameEntityRegistersItAsDrawable()
    {
        // GameEntity.OverridesMethod returns true when DeclaringType != typeof(GameBehaviour).
        // Mirrors TransformBehaviour_IsNotInUpdateList_ZeroCostPerFrame but inverted:
        // SpriteRendererBehaviour MUST override Draw so GameEntity puts it in _drawables.
        var method = typeof(SpriteRendererBehaviour).GetMethod(
            "Draw",
            BindingFlags.Instance | BindingFlags.Public,
            null, _drawParams, null);

        Assert.NotNull(method);
        Assert.NotEqual(typeof(GameBehaviour), method!.DeclaringType);
    }

    [Fact]
    public void DrawableSpy_AddedToEntity_IsCalledDuringWorldDraw()
    {
        // Verifies the draw hot-path via a concrete spy to confirm the same
        // registration mechanism SpriteRendererBehaviour relies on works end-to-end.
        var world = new GameWorld();
        var spy = new DrawSpy();
        world.CreateEntity("E").Add(spy);

        world.Update(new GameTime()); // flushes _toAdd into _entities (deferred creation)
        world.Draw(new GameTime(), null!);

        Assert.True(spy.DrawCalled);
    }

    // ── Property contract ──────────────────────────────────────────────────────

    [Fact]
    public void Color_Property_IsPublicReadWrite_TypeColor()
    {
        var prop = typeof(SpriteRendererBehaviour)
            .GetProperty("Color", BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(prop);
        Assert.Equal(typeof(Color), prop!.PropertyType);
        Assert.True(prop.CanRead);
        Assert.True(prop.CanWrite);
    }

    [Fact]
    public void LayerDepth_Property_IsPublicReadWrite_TypeFloat()
    {
        var prop = typeof(SpriteRendererBehaviour)
            .GetProperty("LayerDepth", BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(prop);
        Assert.Equal(typeof(float), prop!.PropertyType);
        Assert.True(prop.CanRead);
        Assert.True(prop.CanWrite);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private sealed class DrawSpy : GameBehaviour
    {
        public bool DrawCalled { get; private set; }
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) => DrawCalled = true;
    }
}
