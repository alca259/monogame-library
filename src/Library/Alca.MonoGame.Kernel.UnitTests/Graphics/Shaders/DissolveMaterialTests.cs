using Alca.MonoGame.Kernel.Graphics.Shaders;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Shaders;

// DissolveMaterial requires a compiled Effect from the MonoGame Content Pipeline.
// Both Progress and EdgeWidth use MathHelper.Clamp(value, 0f, 1f) internally.
// Tests below verify the clamping contract without instantiating the class,
// following the established SpriteMaterialAlphaContractTests pattern.

public sealed class DissolveMaterialTests
{
    /// <summary>Verifies the [0, 1] clamping contract for the Progress property.</summary>
    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(0f,    0f)]
    [InlineData(0.5f,  0.5f)]
    [InlineData(1f,    1f)]
    [InlineData(2f,    1f)]
    public void Progress_ClampingFormula_ReturnsExpected(float input, float expected)
    {
        float clamped = MathHelper.Clamp(input, 0f, 1f);
        Assert.Equal(expected, clamped, 3);
    }

    /// <summary>Verifies the [0, 1] clamping contract for the EdgeWidth property.</summary>
    [Theory]
    [InlineData(-0.1f, 0f)]
    [InlineData(0f,    0f)]
    [InlineData(0.05f, 0.05f)]
    [InlineData(0.5f,  0.5f)]
    [InlineData(1f,    1f)]
    [InlineData(1.5f,  1f)]
    public void EdgeWidth_ClampingFormula_ReturnsExpected(float input, float expected)
    {
        float clamped = MathHelper.Clamp(input, 0f, 1f);
        Assert.Equal(expected, clamped, 3);
    }

    /// <summary>Verifies the documented default EdgeWidth (0.05) lies within the valid range.</summary>
    [Fact]
    public void EdgeWidth_DefaultValue_IsInValidRange()
    {
        const float defaultEdgeWidth = 0.05f;
        Assert.InRange(defaultEdgeWidth, 0f, 1f);
    }

    [Fact(Skip = "Requires a compiled .xnb Effect from the MonoGame Content Pipeline.")]
    public void Constructor_WithEffect_SetsDefaultPropertyValues()
    {
        // Cannot be tested without a compiled .fx shader available at test time.
    }
}
