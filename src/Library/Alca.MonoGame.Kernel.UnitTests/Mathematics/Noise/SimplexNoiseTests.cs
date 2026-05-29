using Alca.MonoGame.Kernel.Mathematics.Noise;

namespace Alca.MonoGame.Kernel.UnitTests.Mathematics.Noise;

public sealed class SimplexNoiseTests
{
    // ── Output range ─────────────────────────────────────────────────────────

    [Fact]
    public void Get_2D_OutputApproximatelyInMinusOneToOne()
    {
        var noise = new SimplexNoise(0);

        for (int xi = 0; xi < 20; xi++)
        {
            for (int yi = 0; yi < 20; yi++)
            {
                float v = noise.Get(xi * 0.25f, yi * 0.25f);
                // The 70× scale factor in the algorithm occasionally produces values slightly
                // outside [-1, 1] at pathological inputs; allow a small tolerance.
                Assert.InRange(v, -1.1f, 1.1f);
            }
        }
    }

    [Fact]
    public void Get_3D_OutputApproximatelyInMinusOneToOne()
    {
        var noise = new SimplexNoise(1);

        for (int xi = 0; xi < 10; xi++)
        {
            for (int yi = 0; yi < 10; yi++)
            {
                for (int zi = 0; zi < 5; zi++)
                {
                    float v = noise.Get(xi * 0.25f, yi * 0.25f, zi * 0.25f);
                    Assert.InRange(v, -1.1f, 1.1f);
                }
            }
        }
    }

    [Fact]
    public void Get01_2D_OutputInZeroToOne()
    {
        var noise = new SimplexNoise(2);

        for (int xi = 0; xi < 20; xi++)
        {
            for (int yi = 0; yi < 20; yi++)
            {
                float v = noise.Get01(xi * 0.25f, yi * 0.25f);
                // Get01 = (Get + 1) * 0.5 — clamp tolerance mirrors 2D range tolerance.
                Assert.InRange(v, -0.05f, 1.05f);
            }
        }
    }

    // ── Determinism ───────────────────────────────────────────────────────────

    [Fact]
    public void Get_SameSeedSameInput_ReturnsSameValue()
    {
        var nA = new SimplexNoise(42);
        var nB = new SimplexNoise(42);

        float a = nA.Get(3.7f, 1.2f);
        float b = nB.Get(3.7f, 1.2f);

        Assert.Equal(a, b, 6);
    }

    [Fact]
    public void Get_DifferentSeeds_ProduceDifferentValues()
    {
        var nA = new SimplexNoise(0);
        var nB = new SimplexNoise(9999);

        bool anyDifference = false;
        for (int i = 1; i <= 10; i++)
        {
            if (MathF.Abs(nA.Get(i * 0.5f, i * 0.3f) - nB.Get(i * 0.5f, i * 0.3f)) > 1e-5f)
            {
                anyDifference = true;
                break;
            }
        }

        Assert.True(anyDifference);
    }
}
