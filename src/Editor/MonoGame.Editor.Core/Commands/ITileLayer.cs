namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Abstracción sobre una capa de tilemap que permite operaciones de lectura/escritura de tiles
/// sin una dependencia directa en MonoGame.Extended.Tiled.
/// </summary>
public interface ITileLayer
{
    /// <summary>Devuelve el ID del tile en (<paramref name="column"/>, <paramref name="row"/>), o <c>null</c> si está vacío.</summary>
    int? GetTile(int column, int row);

    /// <summary>Establece el tile en (<paramref name="column"/>, <paramref name="row"/>) con <paramref name="tileId"/>, o lo elimina cuando el valor es <c>null</c>.</summary>
    void SetTile(int column, int row, int? tileId);
}
