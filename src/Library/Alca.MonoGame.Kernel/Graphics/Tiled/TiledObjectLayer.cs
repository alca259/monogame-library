using MonoGame.Extended.Tilemaps;

namespace Alca.MonoGame.Kernel.Graphics.Tiled;

/// <summary>Provides high-level access to object layers in a loaded <see cref="Tilemap"/>.</summary>
public sealed class TiledObjectLayer
{
    private readonly Tilemap _tilemap;
    private readonly List<TilemapObject> _objectsBuffer;
    private readonly List<Vector2> _vectorBuffer;
    private readonly List<Rectangle> _rectBuffer;

    /// <summary>Initialises the accessor for the given loaded tilemap.</summary>
    public TiledObjectLayer(Tilemap tilemap)
    {
        _tilemap = tilemap;
        _objectsBuffer = new List<TilemapObject>(32);
        _vectorBuffer = new List<Vector2>(16);
        _rectBuffer = new List<Rectangle>(16);
    }

    /// <summary>Returns all objects in the named object layer. Returns an empty list if the layer does not exist.</summary>
    public IReadOnlyList<TilemapObject> GetObjects(string layerName)
    {
        _objectsBuffer.Clear();
        if (!_tilemap.Layers.TryGetValue(layerName, out TilemapLayer? raw) ||
            raw is not TilemapObjectLayer layer) return _objectsBuffer;
        foreach (TilemapObject obj in layer.Objects)
            _objectsBuffer.Add(obj);
        return _objectsBuffer;
    }

    /// <summary>
    /// Returns all objects across every object layer whose <c>type</c> custom property equals <paramref name="type"/>.
    /// Uses a for loop — no LINQ.
    /// </summary>
    public IReadOnlyList<TilemapObject> GetObjectsByType(string type)
    {
        _objectsBuffer.Clear();
        foreach (TilemapLayer layer in _tilemap.Layers)
        {
            if (layer is not TilemapObjectLayer objLayer) continue;
            foreach (TilemapObject obj in objLayer.Objects)
            {
                if (obj.Properties.GetString("type", "") == type)
                    _objectsBuffer.Add(obj);
            }
        }
        return _objectsBuffer;
    }

    /// <summary>Returns world positions of all visible point objects whose <c>type</c> custom property is <c>"spawn"</c>.</summary>
    public IReadOnlyList<Vector2> GetSpawnPoints()
    {
        _vectorBuffer.Clear();
        foreach (TilemapLayer layer in _tilemap.Layers)
        {
            if (layer is not TilemapObjectLayer objLayer) continue;
            foreach (TilemapObject obj in objLayer.Objects)
            {
                if (!obj.IsVisible) continue;
                if (obj is TilemapPointObject point &&
                    obj.Properties.GetString("type", "") == "spawn")
                    _vectorBuffer.Add(point.Position);
            }
        }
        return _vectorBuffer;
    }

    /// <summary>Returns axis-aligned rectangles from all visible rectangle objects across every object layer.</summary>
    public IReadOnlyList<Rectangle> GetCollisionRects()
    {
        _rectBuffer.Clear();
        foreach (TilemapLayer layer in _tilemap.Layers)
        {
            if (layer is not TilemapObjectLayer objLayer) continue;
            foreach (TilemapObject obj in objLayer.Objects)
            {
                if (!obj.IsVisible) continue;
                if (obj is TilemapRectangleObject rect)
                    _rectBuffer.Add(new Rectangle(
                        (int)rect.Position.X, (int)rect.Position.Y,
                        (int)rect.Size.X, (int)rect.Size.Y));
            }
        }
        return _rectBuffer;
    }
}
