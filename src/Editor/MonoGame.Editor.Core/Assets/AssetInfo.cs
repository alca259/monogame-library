namespace MonoGame.Editor.Core.Assets;

/// <summary>Descriptor inmutable para un único archivo de asset en disco.</summary>
/// <param name="AbsolutePath">Ruta completa al archivo del asset.</param>
/// <param name="RelativePath">Ruta relativa a la carpeta raíz de Content.</param>
/// <param name="Name">Nombre para mostrar (nombre de archivo sin la extensión más externa).</param>
/// <param name="Type">Tipo de asset clasificado.</param>
/// <param name="Extension">Extensión del archivo incluyendo el punto inicial, en minúsculas.</param>
/// <param name="SizeBytes">Tamaño del archivo en bytes, o 0 si el archivo ya no existe.</param>
public sealed record AssetInfo(
    string AbsolutePath,
    string RelativePath,
    string Name,
    AssetType Type,
    string Extension,
    long SizeBytes);
