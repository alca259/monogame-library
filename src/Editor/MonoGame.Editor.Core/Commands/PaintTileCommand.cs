namespace MonoGame.Editor.Core.Commands;

/// <summary>Pinta un tile en una capa de tilemap a través de <see cref="ITileLayer"/>.</summary>
public sealed class PaintTileCommand : IEditorCommand
{
    private readonly ITileLayer _layer;
    private readonly int _column;
    private readonly int _row;
    private readonly int _tileId;
    private int? _previousTileId;

    /// <param name="layer">Capa de tiles de destino.</param>
    /// <param name="column">Índice de columna del tile a pintar.</param>
    /// <param name="row">Índice de fila del tile a pintar.</param>
    /// <param name="tileId">ID del tile a colocar.</param>
    public PaintTileCommand(ITileLayer layer, int column, int row, int tileId)
    {
        _layer = layer;
        _column = column;
        _row = row;
        _tileId = tileId;
    }

    /// <inheritdoc/>
    public string Description => "Paint Tile";

    /// <inheritdoc/>
    public void Execute()
    {
        _previousTileId = _layer.GetTile(_column, _row);
        _layer.SetTile(_column, _row, _tileId);
    }

    /// <inheritdoc/>
    public void Undo() => _layer.SetTile(_column, _row, _previousTileId);
}
