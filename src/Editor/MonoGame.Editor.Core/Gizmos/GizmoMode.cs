namespace MonoGame.Editor.Core.Gizmos;

/// <summary>Herramienta de transformación activa en el viewport del editor.</summary>
public enum GizmoMode
{
    /// <summary>Solo selección; sin manijas de transformación visibles.</summary>
    Select,

    /// <summary>Traslada el objeto seleccionado a lo largo de uno o ambos ejes.</summary>
    Move,

    /// <summary>Rota el objeto seleccionado alrededor de su pivote.</summary>
    Rotate,

    /// <summary>Escala el objeto seleccionado de forma uniforme o por eje.</summary>
    Scale,

    /// <summary>Herramienta de diseño rectangular; muestra una caja delimitadora discontinua, sin manijas de transformación.</summary>
    Rect,

    /// <summary>Muestra simultáneamente las manijas de Move, Rotate y Scale según <c>GizmoController.EnabledTools</c> y <c>EnabledAxes</c>.</summary>
    Universal,
}
