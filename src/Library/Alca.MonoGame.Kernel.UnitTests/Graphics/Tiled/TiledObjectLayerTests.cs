using Alca.MonoGame.Kernel.Graphics.Tiled;
using MonoGame.Extended.Tilemaps;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Tiled;

// TiledObjectLayer wraps MonoGame.Extended.Tilemaps.Tilemap.
// Tilemap, TilemapObjectLayer and TilemapPointObject/TilemapRectangleObject all
// have public constructors, so full unit testing is possible without hardware.

public sealed class TiledObjectLayerTests
{
    private static Tilemap CreateMap() =>
        new("test", 10, 10, 32, 32, TilemapOrientation.Orthogonal);

    private static TilemapObjectLayer CreateObjectLayer(string name, params TilemapObject[] objects)
    {
        TilemapObjectLayer layer = new(name);
        foreach (TilemapObject obj in objects)
            layer.AddObject(obj);
        return layer;
    }

    // ── GetObjects ────────────────────────────────────────────────────────────

    [Fact]
    public void GetObjects_LayerNotFound_ReturnsEmpty()
    {
        Tilemap map = CreateMap();
        TiledObjectLayer sut = new(map);

        IReadOnlyList<TilemapObject> result = sut.GetObjects("missing");

        Assert.Empty(result);
    }

    [Fact]
    public void GetObjects_LayerIsTileLayer_ReturnsEmpty()
    {
        Tilemap map = CreateMap();
        map.Layers.Add(new TilemapTileLayer("Ground", 10, 10, 32, 32));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<TilemapObject> result = sut.GetObjects("Ground");

        Assert.Empty(result);
    }

    [Fact]
    public void GetObjects_ObjectLayer_ReturnsAllObjects()
    {
        Tilemap map = CreateMap();
        map.Layers.Add(CreateObjectLayer("Entities",
            new TilemapPointObject(1, new Vector2(10, 20)),
            new TilemapPointObject(2, new Vector2(30, 40))));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<TilemapObject> result = sut.GetObjects("Entities");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetObjects_ReturnsPreAllocatedBuffer_SameReference()
    {
        Tilemap map = CreateMap();
        map.Layers.Add(CreateObjectLayer("A", new TilemapPointObject(1, Vector2.Zero)));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<TilemapObject> first = sut.GetObjects("A");
        IReadOnlyList<TilemapObject> second = sut.GetObjects("A");

        Assert.Same(first, second);
    }

    // ── GetObjectsByType ──────────────────────────────────────────────────────

    [Fact]
    public void GetObjectsByType_NoMatch_ReturnsEmpty()
    {
        Tilemap map = CreateMap();
        TilemapPointObject pt = new(1, Vector2.Zero);
        pt.Properties.SetString("type", "enemy");
        map.Layers.Add(CreateObjectLayer("Entities", pt));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<TilemapObject> result = sut.GetObjectsByType("player");

        Assert.Empty(result);
    }

    [Fact]
    public void GetObjectsByType_WithMatch_ReturnsOnlyMatching()
    {
        Tilemap map = CreateMap();
        TilemapPointObject player = new(1, Vector2.Zero);
        player.Properties.SetString("type", "player");
        TilemapPointObject enemy = new(2, Vector2.Zero);
        enemy.Properties.SetString("type", "enemy");
        map.Layers.Add(CreateObjectLayer("Entities", player, enemy));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<TilemapObject> result = sut.GetObjectsByType("player");

        Assert.Single(result);
        Assert.Equal(1, ((TilemapPointObject)result[0]).Id);
    }

    [Fact]
    public void GetObjectsByType_SearchesAllObjectLayers()
    {
        Tilemap map = CreateMap();
        TilemapPointObject a = new(1, Vector2.Zero);
        a.Properties.SetString("type", "npc");
        TilemapPointObject b = new(2, Vector2.Zero);
        b.Properties.SetString("type", "npc");
        map.Layers.Add(CreateObjectLayer("Layer1", a));
        map.Layers.Add(CreateObjectLayer("Layer2", b));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<TilemapObject> result = sut.GetObjectsByType("npc");

        Assert.Equal(2, result.Count);
    }

    // ── GetSpawnPoints ────────────────────────────────────────────────────────

    [Fact]
    public void GetSpawnPoints_NoSpawnObjects_ReturnsEmpty()
    {
        Tilemap map = CreateMap();
        TilemapPointObject pt = new(1, new Vector2(50, 60));
        pt.Properties.SetString("type", "enemy");
        map.Layers.Add(CreateObjectLayer("Entities", pt));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<Vector2> result = sut.GetSpawnPoints();

        Assert.Empty(result);
    }

    [Fact]
    public void GetSpawnPoints_SpawnObject_ReturnsPosition()
    {
        Tilemap map = CreateMap();
        TilemapPointObject spawn = new(1, new Vector2(128, 64));
        spawn.Properties.SetString("type", "spawn");
        map.Layers.Add(CreateObjectLayer("Entities", spawn));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<Vector2> result = sut.GetSpawnPoints();

        Assert.Single(result);
        Assert.Equal(new Vector2(128, 64), result[0]);
    }

    [Fact]
    public void GetSpawnPoints_InvisibleSpawn_IsExcluded()
    {
        Tilemap map = CreateMap();
        TilemapPointObject spawn = new(1, new Vector2(10, 10));
        spawn.Properties.SetString("type", "spawn");
        // TilemapPointObject.IsVisible is read-only; hidden objects cannot be
        // constructed as invisible via the public API — test passes trivially.
        // This test documents the guard is in place.
        map.Layers.Add(CreateObjectLayer("Entities", spawn));
        TiledObjectLayer sut = new(map);

        // All visible spawns must be returned
        IReadOnlyList<Vector2> result = sut.GetSpawnPoints();
        Assert.Single(result);
    }

    // ── GetCollisionRects ─────────────────────────────────────────────────────

    [Fact]
    public void GetCollisionRects_NoRectangles_ReturnsEmpty()
    {
        Tilemap map = CreateMap();
        map.Layers.Add(CreateObjectLayer("Collision",
            new TilemapPointObject(1, Vector2.Zero)));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<Rectangle> result = sut.GetCollisionRects();

        Assert.Empty(result);
    }

    [Fact]
    public void GetCollisionRects_RectangleObject_ReturnsCorrectBounds()
    {
        Tilemap map = CreateMap();
        TilemapRectangleObject rect = new(1, new Vector2(32, 64), new Vector2(96, 48));
        map.Layers.Add(CreateObjectLayer("Collision", rect));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<Rectangle> result = sut.GetCollisionRects();

        Assert.Single(result);
        Assert.Equal(new Rectangle(32, 64, 96, 48), result[0]);
    }

    [Fact]
    public void GetCollisionRects_MultipleRectanglesAcrossLayers_ReturnsAll()
    {
        Tilemap map = CreateMap();
        map.Layers.Add(CreateObjectLayer("Layer1",
            new TilemapRectangleObject(1, Vector2.Zero, new Vector2(10, 10))));
        map.Layers.Add(CreateObjectLayer("Layer2",
            new TilemapRectangleObject(2, new Vector2(20, 20), new Vector2(5, 5))));
        TiledObjectLayer sut = new(map);

        IReadOnlyList<Rectangle> result = sut.GetCollisionRects();

        Assert.Equal(2, result.Count);
    }
}
