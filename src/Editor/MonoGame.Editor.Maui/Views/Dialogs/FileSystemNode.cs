namespace MonoGame.Editor.Maui.Views.Dialogs;

/// <summary>Modelo de fila para el árbol del selector de ruta relativa.</summary>
public sealed class FileSystemNode
{
    /// <summary>Nombre del fichero o carpeta (solo el segmento final de la ruta).</summary>
    public string DisplayName { get; }

    /// <summary>Ruta absoluta del fichero o carpeta.</summary>
    public string FullPath { get; }

    /// <summary><c>true</c> si el nodo es un directorio; <c>false</c> si es un fichero.</summary>
    public bool IsDirectory { get; }

    /// <summary>Profundidad en el árbol (0 = hijos directos de la raíz).</summary>
    public int Depth { get; }

    /// <summary>Indica si el directorio está expandido. Ignorado cuando <see cref="IsDirectory"/> es <c>false</c>.</summary>
    public bool IsExpanded { get; }

    /// <summary>Indica si este directorio contiene subdirectorios o ficheros visibles.</summary>
    public bool HasChildren { get; }

    /// <summary>Padding izquierdo para la indentación visual según <see cref="Depth"/>.</summary>
    public Thickness LeftPadding => new(Depth * 14 + 4, 3, 4, 3);

    /// <summary>Glifo de expansión para directorios; vacío para ficheros.</summary>
    public string ExpandIcon => IsDirectory
        ? (HasChildren ? (IsExpanded ? "▾" : "▶") : " ")
        : string.Empty;

    /// <summary>Color del texto: primario para carpetas, secundario para ficheros.</summary>
    public Color TextColor => IsDirectory
        ? Color.FromArgb("#E6E6E8")
        : Color.FromArgb("#9A9AA2");

    public FileSystemNode(string displayName, string fullPath, bool isDirectory,
                          int depth, bool isExpanded, bool hasChildren)
    {
        DisplayName = displayName;
        FullPath    = fullPath;
        IsDirectory = isDirectory;
        Depth       = depth;
        IsExpanded  = isExpanded;
        HasChildren = hasChildren;
    }
}
