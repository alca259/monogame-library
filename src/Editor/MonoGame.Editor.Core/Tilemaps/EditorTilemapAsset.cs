namespace MonoGame.Editor.Core.Tilemaps;

/// <summary>Represents a loaded and editable tilemap read from a .tmx file.</summary>
public sealed class EditorTilemapAsset
{
    private readonly List<EditorTileset> _tilesets;
    private readonly List<EditorTileLayer> _layers;

    /// <summary>Gets the absolute path to the source .tmx file.</summary>
    public string FilePath { get; }

    /// <summary>Gets the map width in tiles.</summary>
    public int MapWidth { get; }

    /// <summary>Gets the map height in tiles.</summary>
    public int MapHeight { get; }

    /// <summary>Gets the tile width in pixels.</summary>
    public int TileWidth { get; }

    /// <summary>Gets the tile height in pixels.</summary>
    public int TileHeight { get; }

    /// <summary>Gets the tilesets referenced by this map.</summary>
    public IReadOnlyList<EditorTileset> Tilesets => _tilesets;

    /// <summary>Gets the tile layers in this map.</summary>
    public IReadOnlyList<EditorTileLayer> Layers => _layers;

    /// <param name="filePath">Absolute path to the source .tmx file.</param>
    /// <param name="mapWidth">Map width in tiles.</param>
    /// <param name="mapHeight">Map height in tiles.</param>
    /// <param name="tileWidth">Tile width in pixels.</param>
    /// <param name="tileHeight">Tile height in pixels.</param>
    /// <param name="tilesets">Tilesets referenced by the map.</param>
    /// <param name="layers">Tile layers.</param>
    public EditorTilemapAsset(
        string filePath,
        int mapWidth,
        int mapHeight,
        int tileWidth,
        int tileHeight,
        IEnumerable<EditorTileset> tilesets,
        IEnumerable<EditorTileLayer> layers)
    {
        FilePath = filePath;
        MapWidth = mapWidth;
        MapHeight = mapHeight;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        _tilesets = [.. tilesets];
        _layers = [.. layers];
    }

    /// <summary>
    /// Returns the tileset that owns the given global tile ID, or <c>null</c> if none matches.
    /// When multiple tilesets could match, returns the one with the highest <c>FirstGid</c> ≤ <paramref name="gid"/>.
    /// </summary>
    public EditorTileset? GetTilesetForGid(int gid)
    {
        EditorTileset? best = null;
        for (int i = 0; i < _tilesets.Count; i++)
        {
            var ts = _tilesets[i];
            if (ts.FirstGid <= gid)
                best = ts;
        }
        return best;
    }

    /// <summary>Converts a global tile ID to the local (0-based) ID within its tileset.</summary>
    public static int GetLocalId(int gid, EditorTileset tileset) => gid - tileset.FirstGid;
}
