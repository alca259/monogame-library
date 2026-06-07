namespace MonoGame.Editor.Core.Gizmos;

/// <summary>Plano ortográfico mostrado en el viewport de escena.</summary>
public enum ViewOrientation
{
    /// <summary>Vista frontal: plano XY (vista de juego 2D por defecto).</summary>
    Front,
    /// <summary>Vista superior: plano XZ (X horizontal, Z vertical).</summary>
    Top,
    /// <summary>Vista lateral derecha: plano ZY (Z horizontal, Y vertical).</summary>
    Right,
}
