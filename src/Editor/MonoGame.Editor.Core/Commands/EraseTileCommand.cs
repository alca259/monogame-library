namespace MonoGame.Editor.Core.Commands;

/// <summary>Borra un tile de una capa de tilemap a través de <see cref="ITileLayer"/>.</summary>
public sealed class EraseTileCommand : IEditorCommand
{
    private readonly ITileLayer _layer;
    private readonly int _column;
    private readonly int _row;
    private int? _previousTileId;

    /// <param name="layer">Capa de tiles de destino.</param>
    /// <param name="column">Índice de columna del tile a borrar.</param>
    /// <param name="row">Índice de fila del tile a borrar.</param>
    public EraseTileCommand(ITileLayer layer, int column, int row)
    {
        _layer = layer;
        _column = column;
        _row = row;
    }

    /// <inheritdoc/>
    public string Description => "Erase Tile";

    /// <inheritdoc/>
    public void Execute()
    {
        _previousTileId = _layer.GetTile(_column, _row);
        _layer.SetTile(_column, _row, null);
    }

    /// <inheritdoc/>
    public void Undo() => _layer.SetTile(_column, _row, _previousTileId);
}
