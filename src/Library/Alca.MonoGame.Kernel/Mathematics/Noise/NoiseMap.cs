namespace Alca.MonoGame.Kernel.Mathematics.Noise;

/// <summary>
/// Pre-allocated 2D noise map that stores float values in [0, 1].
/// Supports generation from <see cref="PerlinNoise"/> (with fBm) or <see cref="SimplexNoise"/>,
/// and export to a greyscale <see cref="Texture2D"/>.
/// </summary>
public sealed class NoiseMap
{
    private readonly float[,] _data;

    /// <summary>Gets the width of the noise map in cells.</summary>
    public int Width { get; }

    /// <summary>Gets the height of the noise map in cells.</summary>
    public int Height { get; }

    /// <summary>Gets the noise value at (<paramref name="x"/>, <paramref name="y"/>) in [0, 1].</summary>
    public float this[int x, int y] => _data[x, y];

    /// <summary>Initializes a new <see cref="NoiseMap"/> with the given dimensions and pre-allocates storage.</summary>
    public NoiseMap(int width, int height)
    {
        Width = width;
        Height = height;
        _data = new float[width, height];
    }

    #region Generation
    /// <summary>
    /// Fills the map using fractal Brownian motion from a <see cref="PerlinNoise"/> source.
    /// Each cell value is in [0, 1].
    /// </summary>
    /// <param name="noise">The noise generator.</param>
    /// <param name="scale">Coordinate scale (smaller = zoomed out, larger = zoomed in).</param>
    /// <param name="octaves">Number of fBm octaves.</param>
    /// <param name="persistence">Amplitude decay per octave.</param>
    /// <param name="lacunarity">Frequency multiplier per octave.</param>
    public void Generate(PerlinNoise noise, float scale, int octaves = 4, float persistence = 0.5f, float lacunarity = 2f)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float value = noise.Fractal(x * scale, y * scale, octaves, persistence, lacunarity);
                _data[x, y] = value;
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        // Remap to [0, 1]
        float range = max - min;
        if (range < float.Epsilon) range = float.Epsilon;

        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _data[x, y] = (_data[x, y] - min) / range;
    }

    /// <summary>
    /// Fills the map using a <see cref="SimplexNoise"/> source.
    /// Each cell value is in [0, 1].
    /// </summary>
    /// <param name="noise">The noise generator.</param>
    /// <param name="scale">Coordinate scale.</param>
    public void Generate(SimplexNoise noise, float scale)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _data[x, y] = noise.Get01(x * scale, y * scale);
    }
    #endregion

    #region Export
    /// <summary>
    /// Creates and returns a new greyscale <see cref="Texture2D"/> representing the noise map.
    /// Each pixel's RGB channels equal the cell value scaled to [0, 255].
    /// This method allocates — do not call on a hot path.
    /// </summary>
    public Texture2D ToTexture(GraphicsDevice gd)
    {
        var texture = new Texture2D(gd, Width, Height);
        Color[] pixels = new Color[Width * Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                byte value = (byte)(_data[x, y] * 255f);
                pixels[y * Width + x] = new Color(value, value, value, (byte)255);
            }
        }

        texture.SetData(pixels);
        return texture;
    }
    #endregion
}
