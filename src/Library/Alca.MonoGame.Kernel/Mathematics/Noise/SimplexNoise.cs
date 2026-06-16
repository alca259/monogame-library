namespace Alca.MonoGame.Kernel.Mathematics.Noise;

/// <summary>
/// Simplex noise generator based on Stefan Gustavson's algorithm.
/// Supports 2D and 3D, seeded and deterministic.
/// </summary>
public sealed class SimplexNoise
{
    #region Permutation table
    private readonly int[] _perm = new int[512];

    // 2D gradient table (12 directions for 3D reuse; first 8 for 2D)
    private static readonly int[,] _grad3 =
    {
        {  1,  1,  0 }, { -1,  1,  0 }, {  1, -1,  0 }, { -1, -1,  0 },
        {  1,  0,  1 }, { -1,  0,  1 }, {  1,  0, -1 }, { -1,  0, -1 },
        {  0,  1,  1 }, {  0, -1,  1 }, {  0,  1, -1 }, {  0, -1, -1 }
    };

    // Skewing / unskewing factors for 2D
    private const float F2 = 0.3660254037844386f;  // (sqrt(3)-1)/2
    private const float G2 = 0.2113248654051871f;  // (3-sqrt(3))/6

    // Skewing / unskewing factors for 3D
    private const float F3 = 1f / 3f;
    private const float G3 = 1f / 6f;

    /// <summary>
    /// Initializes a new <see cref="SimplexNoise"/> generator with the given seed.
    /// The permutation table is built deterministically using a Fisher-Yates shuffle.
    /// </summary>
    public SimplexNoise(int seed = 0)
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

        for (int i = 0; i < 512; i++)
            _perm[i] = source[i & 255];
    }
    #endregion

    #region Public API
    /// <summary>
    /// Returns 2D Simplex noise for (<paramref name="x"/>, <paramref name="y"/>).
    /// Output is in approximately [-1, 1].
    /// </summary>
    public float Get(float x, float y)
    {
        // Skew input to find simplex cell
        float s = (x + y) * F2;
        int i = FastFloor(x + s);
        int j = FastFloor(y + s);

        float t = (i + j) * G2;
        float x0 = x - (i - t);
        float y0 = y - (j - t);

        // Determine simplex: upper or lower triangle
        int i1, j1;
        if (x0 > y0) { i1 = 1; j1 = 0; }
        else         { i1 = 0; j1 = 1; }

        float x1 = x0 - i1 + G2;
        float y1 = y0 - j1 + G2;
        float x2 = x0 - 1f + 2f * G2;
        float y2 = y0 - 1f + 2f * G2;

        int ii = i & 255;
        int jj = j & 255;
        int gi0 = _perm[ii +      _perm[jj      ]] % 12;
        int gi1 = _perm[ii + i1 + _perm[jj + j1 ]] % 12;
        int gi2 = _perm[ii + 1  + _perm[jj + 1  ]] % 12;

        float n0 = CornerContrib2(gi0, x0, y0);
        float n1 = CornerContrib2(gi1, x1, y1);
        float n2 = CornerContrib2(gi2, x2, y2);

        // Scale to [-1, 1] — empirical factor for 2D Simplex
        return 70f * (n0 + n1 + n2);
    }

    /// <summary>
    /// Returns 3D Simplex noise for (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>).
    /// Output is in approximately [-1, 1].
    /// </summary>
    public float Get(float x, float y, float z)
    {
        // Skew input
        float s = (x + y + z) * F3;
        int i = FastFloor(x + s);
        int j = FastFloor(y + s);
        int k = FastFloor(z + s);

        float t = (i + j + k) * G3;
        float x0 = x - (i - t);
        float y0 = y - (j - t);
        float z0 = z - (k - t);

        // Determine simplex tetrahedron
        int i1, j1, k1, i2, j2, k2;
        if (x0 >= y0)
        {
            if (y0 >= z0)      { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
            else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; }
            else               { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; }
        }
        else
        {
            if (y0 < z0)       { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; }
            else if (x0 < z0)  { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; }
            else               { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
        }

        float x1 = x0 - i1 + G3;
        float y1 = y0 - j1 + G3;
        float z1 = z0 - k1 + G3;
        float x2 = x0 - i2 + 2f * G3;
        float y2 = y0 - j2 + 2f * G3;
        float z2 = z0 - k2 + 2f * G3;
        float x3 = x0 - 1f + 3f * G3;
        float y3 = y0 - 1f + 3f * G3;
        float z3 = z0 - 1f + 3f * G3;

        int ii = i & 255;
        int jj = j & 255;
        int kk = k & 255;
        int gi0 = _perm[ii +      _perm[jj +      _perm[kk      ]]] % 12;
        int gi1 = _perm[ii + i1 + _perm[jj + j1 + _perm[kk + k1 ]]] % 12;
        int gi2 = _perm[ii + i2 + _perm[jj + j2 + _perm[kk + k2 ]]] % 12;
        int gi3 = _perm[ii + 1  + _perm[jj + 1  + _perm[kk + 1  ]]] % 12;

        float n0 = CornerContrib3(gi0, x0, y0, z0);
        float n1 = CornerContrib3(gi1, x1, y1, z1);
        float n2 = CornerContrib3(gi2, x2, y2, z2);
        float n3 = CornerContrib3(gi3, x3, y3, z3);

        // Scale to [-1, 1] — empirical factor for 3D Simplex
        return 32f * (n0 + n1 + n2 + n3);
    }

    /// <summary>Returns 2D Simplex noise remapped to [0, 1].</summary>
    public float Get01(float x, float y) => (Get(x, y) + 1f) * 0.5f;
    #endregion

    #region Private helpers
    private static int FastFloor(float x) => x > 0 ? (int)x : (int)x - 1;

    private static float CornerContrib2(int gi, float x, float y)
    {
        float t = 0.5f - x * x - y * y;
        if (t < 0f) return 0f;
        t *= t;
        return t * t * (_grad3[gi, 0] * x + _grad3[gi, 1] * y);
    }

    private static float CornerContrib3(int gi, float x, float y, float z)
    {
        float t = 0.6f - x * x - y * y - z * z;
        if (t < 0f) return 0f;
        t *= t;
        return t * t * (_grad3[gi, 0] * x + _grad3[gi, 1] * y + _grad3[gi, 2] * z);
    }
    #endregion
}
