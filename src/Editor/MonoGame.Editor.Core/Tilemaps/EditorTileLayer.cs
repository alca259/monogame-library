using System.Text;

namespace MonoGame.Editor.Core.Tilemaps;

/// <summary>An editable tile layer that implements <see cref="ITileLayer"/>.</summary>
public sealed class EditorTileLayer : ITileLayer
{
    private readonly int?[] _tiles;

    /// <summary>Gets the display name of the layer.</summary>
    public string Name { get; }

    /// <summary>Gets the width of the layer in tiles.</summary>
    public int Width { get; }

    /// <summary>Gets the height of the layer in tiles.</summary>
    public int Height { get; }

    /// <param name="name">Layer display name.</param>
    /// <param name="width">Width in tiles.</param>
    /// <param name="height">Height in tiles.</param>
    public EditorTileLayer(string name, int width, int height)
    {
        Name = name;
        Width = width;
        Height = height;
        _tiles = new int?[width * height];
    }

    /// <inheritdoc/>
    public int? GetTile(int column, int row)
    {
        if (column < 0 || column >= Width || row < 0 || row >= Height)
            return null;
        return _tiles[row * Width + column];
    }

    /// <inheritdoc/>
    public void SetTile(int column, int row, int? tileId)
    {
        if (column < 0 || column >= Width || row < 0 || row >= Height)
            return;
        _tiles[row * Width + column] = tileId;
    }

    /// <summary>
    /// Exports the tile data as a CSV-encoded string compatible with the TMX format.
    /// Empty tiles are written as <c>0</c>; the last tile has no trailing comma.
    /// </summary>
    public string ToCsvData()
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        for (int r = 0; r < Height; r++)
        {
            for (int c = 0; c < Width; c++)
            {
                sb.Append(_tiles[r * Width + c] ?? 0);
                bool isLastTile = r == Height - 1 && c == Width - 1;
                if (!isLastTile)
                    sb.Append(',');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
