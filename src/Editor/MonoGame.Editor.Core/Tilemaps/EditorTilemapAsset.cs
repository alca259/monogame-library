namespace MonoGame.Editor.Core.Tilemaps;

/// <summary>Representa un tilemap cargado y editable leído desde un archivo .tmx.</summary>
public sealed class EditorTilemapAsset
{
    private readonly List<EditorTileset> _tilesets;
    private readonly List<EditorTileLayer> _layers;

    /// <summary>Obtiene la ruta absoluta al archivo .tmx de origen.</summary>
    public string FilePath { get; }

    /// <summary>Obtiene el ancho del mapa en tiles.</summary>
    public int MapWidth { get; }

    /// <summary>Obtiene el alto del mapa en tiles.</summary>
    public int MapHeight { get; }

    /// <summary>Obtiene el ancho del tile en píxeles.</summary>
    public int TileWidth { get; }

    /// <summary>Obtiene el alto del tile en píxeles.</summary>
    public int TileHeight { get; }

    /// <summary>Obtiene los tilesets referenciados por este mapa.</summary>
    public IReadOnlyList<EditorTileset> Tilesets => _tilesets;

    /// <summary>Obtiene las capas de tiles en este mapa.</summary>
    public IReadOnlyList<EditorTileLayer> Layers => _layers;

    /// <param name="filePath">Ruta absoluta al archivo .tmx de origen.</param>
    /// <param name="mapWidth">Ancho del mapa en tiles.</param>
    /// <param name="mapHeight">Alto del mapa en tiles.</param>
    /// <param name="tileWidth">Ancho del tile en píxeles.</param>
    /// <param name="tileHeight">Alto del tile en píxeles.</param>
    /// <param name="tilesets">Tilesets referenciados por el mapa.</param>
    /// <param name="layers">Capas de tiles.</param>
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
    /// Devuelve el tileset propietario del ID global de tile indicado, o <c>null</c> si ninguno coincide.
    /// Cuando varios tilesets podrían coincidir, devuelve el que tiene el <c>FirstGid</c> más alto ≤ <paramref name="gid"/>.
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

    /// <summary>Convierte un ID global de tile al ID local (base 0) dentro de su tileset.</summary>
    public static int GetLocalId(int gid, EditorTileset tileset) => gid - tileset.FirstGid;
}
