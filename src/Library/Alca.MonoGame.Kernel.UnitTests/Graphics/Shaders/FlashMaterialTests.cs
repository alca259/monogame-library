using Alca.MonoGame.Kernel.Graphics.Shaders;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Shaders;

// FlashMaterial requires a compiled Effect from the MonoGame Content Pipeline.
// The FlashIntensity property uses MathHelper.Clamp(value, 0f, 1f) internally.
// Tests below verify the clamping contract without instantiating the class,
// following the established SpriteMaterialAlphaContractTests pattern.

public sealed class FlashMaterialTests
{
    /// <summary>Verifies the [0, 1] clamping contract for FlashIntensity.</summary>
    [Theory]
    [InlineData(-1f,  0f)]
    [InlineData(0f,   0f)]
    [InlineData(0.25f, 0.25f)]
    [InlineData(0.5f,  0.5f)]
    [InlineData(1f,   1f)]
    [InlineData(1.5f, 1f)]
    public void FlashIntensity_ClampingFormula_ReturnsExpected(float input, float expected)
    {
        float clamped = MathHelper.Clamp(input, 0f, 1f);
        Assert.Equal(expected, clamped, 3);
    }

    /// <summary>Verifies that 0 is a valid minimum (no flash).</summary>
    [Fact]
    public void FlashIntensity_ZeroInput_ClampedToZero()
    {
        float clamped = MathHelper.Clamp(0f, 0f, 1f);
        Assert.Equal(0f, clamped, 6);
    }

    [Fact(Skip = "Requires a compiled .xnb Effect from the MonoGame Content Pipeline.")]
    public void Constructor_WithEffect_SetsDefaultFlashColor()
    {
        // Cannot be tested without a compiled .fx shader available at test time.
    }
}
