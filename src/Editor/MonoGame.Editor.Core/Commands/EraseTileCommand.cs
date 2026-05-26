namespace MonoGame.Editor.Core.Commands;

/// <summary>Erases a tile from a tilemap layer via <see cref="ITileLayer"/>.</summary>
public sealed class EraseTileCommand : IEditorCommand
{
    private readonly ITileLayer _layer;
    private readonly int _column;
    private readonly int _row;
    private int? _previousTileId;

    /// <param name="layer">Target tile layer.</param>
    /// <param name="column">Column index of the tile to erase.</param>
    /// <param name="row">Row index of the tile to erase.</param>
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
