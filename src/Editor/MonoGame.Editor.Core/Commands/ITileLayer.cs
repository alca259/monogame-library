namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Abstraction over a tilemap layer that allows tile read/write operations
/// without a direct dependency on MonoGame.Extended.Tiled.
/// </summary>
public interface ITileLayer
{
    /// <summary>Returns the tile ID at (<paramref name="column"/>, <paramref name="row"/>), or <c>null</c> if empty.</summary>
    int? GetTile(int column, int row);

    /// <summary>Sets the tile at (<paramref name="column"/>, <paramref name="row"/>) to <paramref name="tileId"/>, or clears it when <c>null</c>.</summary>
    void SetTile(int column, int row, int? tileId);
}
