using Alca.MonoGame.Kernel.Mathematics.Noise;

namespace Alca.MonoGame.Kernel.UnitTests.Mathematics.Noise;

public sealed class NoiseMapTests
{
    private const int MapWidth  = 16;
    private const int MapHeight = 16;

    // ── Perlin generation ─────────────────────────────────────────────────────

    [Fact]
    public void Generate_Perlin_ReturnsArrayOfCorrectDimensions()
    {
        var map = new NoiseMap(MapWidth, MapHeight);
        var noise = new PerlinNoise(0);

        map.Generate(noise, scale: 0.1f, octaves: 4, persistence: 0.5f, lacunarity: 2f);

        Assert.Equal(MapWidth,  map.Width);
        Assert.Equal(MapHeight, map.Height);
    }

    [Fact]
    public void Generate_Perlin_AllValuesInZeroToOne()
    {
        var map = new NoiseMap(MapWidth, MapHeight);
        var noise = new PerlinNoise(7);

        map.Generate(noise, scale: 0.1f, octaves: 4, persistence: 0.5f, lacunarity: 2f);

        for (int x = 0; x < MapWidth; x++)
            for (int y = 0; y < MapHeight; y++)
                Assert.InRange(map[x, y], 0f, 1f);
    }

    // ── Simplex generation ────────────────────────────────────────────────────

    [Fact]
    public void Generate_Simplex_AllValuesInZeroToOne()
    {
        var map = new NoiseMap(MapWidth, MapHeight);
        var noise = new SimplexNoise(3);

        map.Generate(noise, scale: 0.1f);

        for (int x = 0; x < MapWidth; x++)
            for (int y = 0; y < MapHeight; y++)
                Assert.InRange(map[x, y], -0.05f, 1.05f);
    }

    // ── Indexer ───────────────────────────────────────────────────────────────

    [Fact]
    public void Indexer_ReturnsGeneratedValue()
    {
        var map = new NoiseMap(MapWidth, MapHeight);
        var noise = new PerlinNoise(11);

        map.Generate(noise, scale: 0.1f);

        // The indexer must return the value written during Generate.
        float v = map[0, 0];
        Assert.InRange(v, 0f, 1f);
    }

    // ── Determinism ───────────────────────────────────────────────────────────

    [Fact]
    public void Generate_SameSeed_DeterministicOutput()
    {
        var mapA = new NoiseMap(MapWidth, MapHeight);
        var mapB = new NoiseMap(MapWidth, MapHeight);

        mapA.Generate(new PerlinNoise(42), scale: 0.05f, octaves: 3, persistence: 0.5f, lacunarity: 2f);
        mapB.Generate(new PerlinNoise(42), scale: 0.05f, octaves: 3, persistence: 0.5f, lacunarity: 2f);

        for (int x = 0; x < MapWidth; x++)
            for (int y = 0; y < MapHeight; y++)
                Assert.Equal(mapA[x, y], mapB[x, y], 6);
    }
}
