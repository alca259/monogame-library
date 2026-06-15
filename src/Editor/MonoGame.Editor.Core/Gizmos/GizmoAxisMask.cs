namespace MonoGame.Editor.Core.Gizmos;

/// <summary>Ejes del gizmo que se muestran y responden a interacción.</summary>
[Flags]
public enum GizmoAxisMask
{
    /// <summary>Ningún eje activo.</summary>
    None = 0,

    /// <summary>Eje horizontal (X en vistas Front/Top; Z en vista Right).</summary>
    X = 1,

    /// <summary>Eje vertical (Y en vistas Front/Right; Z en vista Top).</summary>
    Y = 2,

    /// <summary>Ambos ejes activos.</summary>
    Both = X | Y,
}
