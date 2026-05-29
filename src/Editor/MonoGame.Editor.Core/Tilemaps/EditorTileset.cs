namespace MonoGame.Editor.Core.Tilemaps;

/// <summary>Representa un tileset referenciado por un tilemap.</summary>
public sealed class EditorTileset
{
    /// <summary>Obtiene el ID global del primer tile en este tileset.</summary>
    public int FirstGid { get; init; }

    /// <summary>Obtiene el nombre de visualización del tileset.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Obtiene la ruta de imagen relativa al archivo .tmx.</summary>
    public string ImagePath { get; init; } = string.Empty;

    /// <summary>Obtiene el ancho de cada tile en píxeles.</summary>
    public int TileWidth { get; init; }

    /// <summary>Obtiene el alto de cada tile en píxeles.</summary>
    public int TileHeight { get; init; }

    /// <summary>Obtiene el número de columnas de tiles en la imagen del tileset.</summary>
    public int Columns { get; init; }

    /// <summary>Obtiene el número total de tiles en este tileset.</summary>
    public int TileCount { get; init; }

    /// <summary>Devuelve los límites en píxeles de un tile dado su ID local (base 0) dentro de este tileset.</summary>
    public System.Drawing.Rectangle GetTileSourceRect(int localTileId)
    {
        if (Columns <= 0) return System.Drawing.Rectangle.Empty;
        int col = localTileId % Columns;
        int row = localTileId / Columns;
        return new System.Drawing.Rectangle(col * TileWidth, row * TileHeight, TileWidth, TileHeight);
    }
}
