namespace Alca.MonoGame.Kernel.Mathematics.Noise;

/// <summary>
/// Classic Perlin noise generator. Seeded and deterministic.
/// Supports 1D, 2D, and fractal Brownian motion (fBm).
/// </summary>
public sealed class PerlinNoise
{
    // ── Permutation table ─────────────────────────────────────────────────────

    private readonly int[] _perm = new int[512];

    // 16 gradient directions for 2D
    private static readonly float[] GradX = { 1f, -1f, 1f, -1f, 1f, -1f, 1f, -1f, 0f, 0f, 0f, 0f, 1f, -1f, 0f, 0f };
    private static readonly float[] GradY = { 1f, 1f, -1f, -1f, 0f, 0f, 0f, 0f, 1f, -1f, 1f, -1f, 0f, 0f, 1f, -1f };

    // 4 gradient values for 1D
    private static readonly float[] Grad1D = { 1f, -1f, 1f, -1f };

    /// <summary>
    /// Initializes a new <see cref="PerlinNoise"/> generator with the given seed.
    /// The permutation table is built deterministically using a Fisher-Yates shuffle.
    /// </summary>
    public PerlinNoise(int seed = 0)
    {
        int[] source = new int[256];
        for (int i = 0; i < 256; i++)
            source[i] = i;

        var rng = new Random(seed);
        for (int i = 255; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (source[i], source[j]) = (source[j], source[i]);
        }

        // Duplicate the table to avoid index overflow
        for (int i = 0; i < 512; i++)
            _perm[i] = source[i & 255];
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns 1D Perlin noise for <paramref name="x"/>.
    /// Output is in approximately [-1, 1].
    /// </summary>
    public float Get(float x)
    {
        int xi = (int)MathF.Floor(x) & 255;
        float xf = x - MathF.Floor(x);
        float u = Fade(xf);

        int a = _perm[xi];
        int b = _perm[xi + 1];

        return MathHelper.Lerp(Grad1(a, xf), Grad1(b, xf - 1f), u);
    }

    /// <summary>
    /// Returns classic 2D Perlin noise for (<paramref name="x"/>, <paramref name="y"/>).
    /// Output is in approximately [-1, 1].
    /// </summary>
    public float Get(float x, float y)
    {
        int xi = (int)MathF.Floor(x) & 255;
        int yi = (int)MathF.Floor(y) & 255;

        float xf = x - MathF.Floor(x);
        float yf = y - MathF.Floor(y);

        float u = Fade(xf);
        float v = Fade(yf);

        int aa = _perm[_perm[xi] + yi];
        int ab = _perm[_perm[xi] + yi + 1];
        int ba = _perm[_perm[xi + 1] + yi];
        int bb = _perm[_perm[xi + 1] + yi + 1];

        float x1 = MathHelper.Lerp(Grad2(aa, xf, yf),       Grad2(ba, xf - 1f, yf),       u);
        float x2 = MathHelper.Lerp(Grad2(ab, xf, yf - 1f),  Grad2(bb, xf - 1f, yf - 1f),  u);

        return MathHelper.Lerp(x1, x2, v);
    }

    /// <summary>
    /// Returns 2D Perlin noise remapped to [0, 1].
    /// </summary>
    public float Get01(float x, float y) => (Get(x, y) + 1f) * 0.5f;

    /// <summary>
    /// Returns fractal Brownian motion (fBm) by stacking <paramref name="octaves"/> layers of
    /// Perlin noise. Result is normalized to approximately [-1, 1].
    /// </summary>
    /// <param name="x">World x coordinate.</param>
    /// <param name="y">World y coordinate.</param>
    /// <param name="octaves">Number of noise octaves to sum.</param>
    /// <param name="persistence">Amplitude multiplier per octave (0 < persistence < 1).</param>
    /// <param name="lacunarity">Frequency multiplier per octave (typically 2).</param>
    public float Fractal(float x, float y, int octaves, float persistence, float lacunarity)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += Get(x * frequency, y * frequency) * amplitude;
            maxAmplitude += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxAmplitude;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Perlin fade curve: 6t^5 − 15t^4 + 10t^3.</summary>
    private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    private static float Grad1(int hash, float x) => Grad1D[hash & 3] * x;

    private static float Grad2(int hash, float x, float y)
    {
        int h = hash & 15;
        return GradX[h] * x + GradY[h] * y;
    }
}
