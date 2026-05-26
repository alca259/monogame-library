namespace MonoGame.Editor.Core.Commands;

/// <summary>Paints a tile onto a tilemap layer via <see cref="ITileLayer"/>.</summary>
public sealed class PaintTileCommand : IEditorCommand
{
    private readonly ITileLayer _layer;
    private readonly int _column;
    private readonly int _row;
    private readonly int _tileId;
    private int? _previousTileId;

    /// <param name="layer">Target tile layer.</param>
    /// <param name="column">Column index of the tile to paint.</param>
    /// <param name="row">Row index of the tile to paint.</param>
    /// <param name="tileId">ID of the tile to place.</param>
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
