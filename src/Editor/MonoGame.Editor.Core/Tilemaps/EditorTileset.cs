namespace MonoGame.Editor.Core.Tilemaps;

/// <summary>Represents a tileset referenced by a tilemap asset.</summary>
public sealed class EditorTileset
{
    /// <summary>Gets the global ID of the first tile in this tileset.</summary>
    public int FirstGid { get; init; }

    /// <summary>Gets the display name of the tileset.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the image path relative to the .tmx file.</summary>
    public string ImagePath { get; init; } = string.Empty;

    /// <summary>Gets the width of each tile in pixels.</summary>
    public int TileWidth { get; init; }

    /// <summary>Gets the height of each tile in pixels.</summary>
    public int TileHeight { get; init; }

    /// <summary>Gets the number of tile columns in the tileset image.</summary>
    public int Columns { get; init; }

    /// <summary>Gets the total number of tiles in this tileset.</summary>
    public int TileCount { get; init; }

    /// <summary>Returns the pixel bounds of a tile given its local (0-based) ID within this tileset.</summary>
    public System.Drawing.Rectangle GetTileSourceRect(int localTileId)
    {
        if (Columns <= 0) return System.Drawing.Rectangle.Empty;
        int col = localTileId % Columns;
        int row = localTileId / Columns;
        return new System.Drawing.Rectangle(col * TileWidth, row * TileHeight, TileWidth, TileHeight);
    }
}
