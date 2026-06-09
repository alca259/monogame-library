namespace MonoGame.Editor.Maui.Views.Dialogs;

/// <summary>
/// Modelo de fila para el árbol de namespace del AddBehaviourDialog.
/// Los nodos namespace son expandibles pero no seleccionables como tipo.
/// Los nodos hoja representan tipos concretos de GameBehaviour.
/// </summary>
public sealed class BehaviourTreeNode
{
    /// <summary>Texto a mostrar en la fila (segmento de namespace o nombre corto del tipo).</summary>
    public string DisplayName { get; }

    /// <summary>
    /// Para hojas: nombre completo del tipo.
    /// Para nodos namespace: ruta completa del namespace (ej. "Alca.MonoGame.Kernel").
    /// </summary>
    public string FullKey { get; }

    public bool IsLeaf { get; }
    public bool IsPending { get; }
    public bool IsExpanded { get; set; }
    public bool HasChildren { get; }
    public int Depth { get; }

    public Thickness LeftPadding => new(Depth * 14 + 4, 3, 4, 3);

    public string ExpandIcon => IsLeaf
        ? string.Empty
        : (HasChildren ? (IsExpanded ? "▾" : "▶") : " ");

    public Color TextColor => IsPending
        ? Color.FromArgb("#6A6A72")
        : Color.FromArgb("#E6E6E8");

    /// <summary>Crea un nodo namespace (no hoja).</summary>
    public BehaviourTreeNode(string displayName, string namespacePath,
                              bool hasChildren, bool isExpanded, int depth)
    {
        DisplayName = displayName;
        FullKey = namespacePath;
        IsLeaf = false;
        IsPending = false;
        HasChildren = hasChildren;
        IsExpanded = isExpanded;
        Depth = depth;
    }

    /// <summary>Crea un nodo hoja (tipo concreto).</summary>
    public BehaviourTreeNode(string displayName, string fullTypeName,
                              bool isPending, int depth)
    {
        DisplayName = displayName;
        FullKey = fullTypeName;
        IsLeaf = true;
        IsPending = isPending;
        HasChildren = false;
        IsExpanded = false;
        Depth = depth;
    }
}
