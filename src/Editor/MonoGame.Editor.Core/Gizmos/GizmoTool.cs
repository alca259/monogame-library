namespace MonoGame.Editor.Core.Gizmos;

/// <summary>Herramientas de transformación que pueden activarse simultáneamente en el modo Universal.</summary>
[Flags]
public enum GizmoTool
{
    /// <summary>Ninguna herramienta activa.</summary>
    None = 0,

    /// <summary>Manijas de traslación (flechas X/Y + cuadrado libre).</summary>
    Move = 1,

    /// <summary>Anillo de rotación.</summary>
    Rotate = 2,

    /// <summary>Manijas de escala (extremos de eje + centro uniforme).</summary>
    Scale = 4,
}
