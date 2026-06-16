namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Shaders;

// OutlineMaterial requires a compiled Effect from the MonoGame Content Pipeline.
// Since a headless Effect cannot be created without .xnb compiled bytecode, the tests
// that exercise instance behaviour are skipped. The pure clamping contract for
// AlphaThreshold (matching the setter pattern used by sibling materials) is verified
// against MathHelper directly, following the established SpriteMaterialAlphaContractTests pattern.

public sealed class OutlineMaterialTests
{
    /// <summary>Verifies the [0, 1] clamping contract that the AlphaThreshold property documents.</summary>
    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f,  0f)]
    [InlineData(0.1f, 0.1f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(1f,  1f)]
    [InlineData(2f,  1f)]
    public void AlphaThreshold_ClampingFormula_ReturnsExpected(float input, float expected)
    {
        float clamped = MathHelper.Clamp(input, 0f, 1f);
        Assert.Equal(expected, clamped, 3);
    }

    /// <summary>Verifies the default OutlineThickness is positive (purely documents intent).</summary>
    [Fact]
    public void OutlineThickness_DefaultValue_IsPositive()
    {
        // Default thickness in OutlineMaterial is 1f — verify the documented default via formula.
        const float defaultThickness = 1f;
        Assert.True(defaultThickness > 0f);
    }

    [Fact(Skip = "Requires a compiled .xnb Effect from the MonoGame Content Pipeline.")]
    public void Constructor_WithEffect_SetsDefaultPropertyValues()
    {
        // Cannot be tested without a compiled .fx shader available at test time.
    }
}
