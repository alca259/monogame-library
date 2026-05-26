using Alca.MonoGame.Kernel.Graphics.Effects;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Effects;

// PostProcessEffect is abstract and wraps a live Effect (GraphicsDevice dependency).
// Apply() delegates to RenderTargetManager, so GPU tests belong in an integration project.
//
// The tests here verify the pure-logic contract: the class is abstract, it exposes
// the correct public API, and SetParameters() is correctly declared abstract.

public sealed class PostProcessEffectApiSurfaceTests
{
    [Fact]
    public void PostProcessEffect_IsAbstract()
    {
        Assert.True(typeof(PostProcessEffect).IsAbstract);
    }

    [Fact]
    public void PostProcessEffect_HasExpectedPublicMembers()
    {
        Type t = typeof(PostProcessEffect);

        Assert.NotNull(t.GetProperty("Effect"));
        Assert.NotNull(t.GetMethod("SetParameters"));
        Assert.NotNull(t.GetMethod("Apply"));
    }

    [Fact]
    public void PostProcessEffect_SetParameters_IsAbstract()
    {
        System.Reflection.MethodInfo? method = typeof(PostProcessEffect).GetMethod("SetParameters");

        Assert.NotNull(method);
        Assert.True(method!.IsAbstract);
    }
}
