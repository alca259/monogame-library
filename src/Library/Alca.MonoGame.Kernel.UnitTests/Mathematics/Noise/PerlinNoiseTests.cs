using Alca.MonoGame.Kernel.Mathematics.Noise;

namespace Alca.MonoGame.Kernel.UnitTests.Mathematics.Noise;

public sealed class PerlinNoiseTests
{
    // ── Determinism ───────────────────────────────────────────────────────────

    [Fact]
    public void Get_1D_SameSeedSameInput_ReturnsSameValue()
    {
        var noiseA = new PerlinNoise(42);
        var noiseB = new PerlinNoise(42);

        float a = noiseA.Get(1.5f);
        float b = noiseB.Get(1.5f);

        Assert.Equal(a, b, 6);
    }

    [Fact]
    public void Constructor_SameSeed_DeterministicOutput()
    {
        var n1 = new PerlinNoise(99);
        var n2 = new PerlinNoise(99);

        // Sample a grid of values and verify they match.
        for (int xi = 0; xi < 5; xi++)
        {
            for (int yi = 0; yi < 5; yi++)
            {
                float v1 = n1.Get(xi * 0.3f, yi * 0.3f);
                float v2 = n2.Get(xi * 0.3f, yi * 0.3f);
                Assert.Equal(v1, v2, 6);
            }
        }
    }

    [Fact]
    public void Get_DifferentSeeds_ProduceDifferentValues()
    {
        var nA = new PerlinNoise(0);
        var nB = new PerlinNoise(12345);

        // At least one sampled coordinate should differ.
        bool anyDifference = false;
        for (int i = 1; i <= 10; i++)
        {
            if (MathF.Abs(nA.Get(i * 0.7f, i * 0.3f) - nB.Get(i * 0.7f, i * 0.3f)) > 1e-5f)
            {
                anyDifference = true;
                break;
            }
        }

        Assert.True(anyDifference);
    }

    // ── Output range ─────────────────────────────────────────────────────────

    [Fact]
    public void Get_2D_OutputInMinusOneToOne()
    {
        var noise = new PerlinNoise(7);

        for (int xi = 0; xi < 20; xi++)
        {
            for (int yi = 0; yi < 20; yi++)
            {
                float v = noise.Get(xi * 0.25f, yi * 0.25f);
                Assert.InRange(v, -1f, 1f);
            }
        }
    }

    [Fact]
    public void Get01_2D_OutputInZeroToOne()
    {
        var noise = new PerlinNoise(3);

        for (int xi = 0; xi < 20; xi++)
        {
            for (int yi = 0; yi < 20; yi++)
            {
                float v = noise.Get01(xi * 0.25f, yi * 0.25f);
                Assert.InRange(v, 0f, 1f);
            }
        }
    }

    [Fact]
    public void Fractal_OutputInMinusOneToOne()
    {
        var noise = new PerlinNoise(5);

        for (int xi = 0; xi < 10; xi++)
        {
            for (int yi = 0; yi < 10; yi++)
            {
                float v = noise.Fractal(xi * 0.2f, yi * 0.2f, octaves: 4, persistence: 0.5f, lacunarity: 2f);
                Assert.InRange(v, -1f, 1f);
            }
        }
    }
}
