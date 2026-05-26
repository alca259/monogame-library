using Alca.MonoGame.Kernel.Graphics.ThreeD;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.ThreeD;

// PrimitiveBatch requires a live GraphicsDevice for instantiation.
// Integration tests for Begin/AddVertex/End belong in a project with headless GPU setup.
//
// MeshRenderer has no constructor parameters and defaults to a null model,
// so its pre-load state is safely testable.

public sealed class MeshRendererTests
{
    [Fact]
    public void BoundingSphere_BeforeLoad_IsDefaultZero()
    {
        MeshRenderer renderer = new();
        Assert.Equal(Vector3.Zero, renderer.BoundingSphere.Center);
        Assert.Equal(0f, renderer.BoundingSphere.Radius);
    }

    [Fact]
    public void SetTexture_BeforeLoad_DoesNotThrow()
    {
        MeshRenderer renderer = new();
        Exception? ex = Record.Exception(() => renderer.SetTexture(null!));
        Assert.Null(ex);
    }

    [Fact]
    public void Draw_WhenModelNotLoaded_DoesNotThrow()
    {
        MeshRenderer renderer = new();
        Exception? ex = Record.Exception(() => renderer.Draw(null!, Matrix.Identity));
        Assert.Null(ex);
    }
}
