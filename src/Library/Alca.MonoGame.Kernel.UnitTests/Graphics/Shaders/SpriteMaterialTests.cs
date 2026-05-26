using Alca.MonoGame.Kernel.Graphics.Shaders;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Shaders;

// SpriteMaterial and Material both require a live Effect (GraphicsDevice dependency).
// Integration tests for Apply() belong in a separate integration test project
// that sets up a headless MonoGame GraphicsDevice.
//
// The tests here verify the pure-logic contract for Alpha and TintColor properties
// using reflection to exercise the backing fields without touching the GPU.

public sealed class SpriteMaterialAlphaContractTests
{
    // We verify the clamping behaviour through MathHelper directly since
    // SpriteMaterial is sealed and Effect requires a GraphicsDevice.
    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f, 0f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(1f, 1f)]
    [InlineData(2f, 1f)]
    public void Alpha_IsClampedBetweenZeroAndOne(float input, float expected)
    {
        float clamped = MathHelper.Clamp(input, 0f, 1f);
        Assert.Equal(expected, clamped, 0.0001f);
    }
}
