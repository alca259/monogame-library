using System.Text;

namespace MonoGame.Editor.Core.Tilemaps;

/// <summary>Capa de tiles editable que implementa <see cref="ITileLayer"/>.</summary>
public sealed class EditorTileLayer : ITileLayer
{
    private readonly int?[] _tiles;

    /// <summary>Obtiene el nombre de visualización de la capa.</summary>
    public string Name { get; }

    /// <summary>Obtiene el ancho de la capa en tiles.</summary>
    public int Width { get; }

    /// <summary>Obtiene el alto de la capa en tiles.</summary>
    public int Height { get; }

    /// <param name="name">Nombre de visualización de la capa.</param>
    /// <param name="width">Ancho en tiles.</param>
    /// <param name="height">Alto en tiles.</param>
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
    /// Exporta los datos de tiles como cadena codificada en CSV compatible con el formato TMX.
    /// Los tiles vacíos se escriben como <c>0</c>; el último tile no lleva coma al final.
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
