namespace MonoGame.Editor.Core.Models;

/// <summary>Configura los subsistemas opcionales de GameWorld para una escena.</summary>
public sealed class EditorWorldConfig
{
    // ── Física 2D ────────────────────────────────────────────────────────────

    /// <summary>Obtiene o establece un valor que indica si Physics2DWorld está habilitado para esta escena.</summary>
    public bool UsePhysics2D { get; set; }

    /// <summary>Obtiene o establece el componente X del vector de gravedad (píxeles/s²). Valor predeterminado: 0.</summary>
    public float GravityX { get; set; } = 0f;

    /// <summary>Obtiene o establece el componente Y del vector de gravedad (píxeles/s²). Valor predeterminado: -9.8.</summary>
    public float GravityY { get; set; } = -9.8f;

    // ── Iluminación ───────────────────────────────────────────────────────────

    /// <summary>Obtiene o establece un valor que indica si LightingWorld está habilitado para esta escena.</summary>
    public bool UseLighting { get; set; }

    /// <summary>
    /// Obtiene o establece el color de luz ambiental como bytes RGBA [R, G, B, A].
    /// Valor predeterminado: negro (0, 0, 0, 255).
    /// </summary>
    public int[] AmbientColorRgba { get; set; } = [0, 0, 0, 255];

    // ── Navegación ────────────────────────────────────────────────────────────

    /// <summary>Obtiene o establece un valor que indica si NavGrid y Pathfinder están habilitados para esta escena.</summary>
    public bool UseNavigation { get; set; }

    /// <summary>Obtiene o establece el ancho de la cuadrícula de navegación en celdas.</summary>
    public int NavGridWidth { get; set; } = 32;

    /// <summary>Obtiene o establece la altura de la cuadrícula de navegación en celdas.</summary>
    public int NavGridHeight { get; set; } = 32;

    /// <summary>Obtiene o establece el tamaño de cada celda de navegación en píxeles.</summary>
    public float NavGridCellSize { get; set; } = 32f;

    /// <summary>Obtiene o establece el origen X en espacio de mundo de la cuadrícula de navegación.</summary>
    public float NavGridOriginX { get; set; }

    /// <summary>Obtiene o establece el origen Y en espacio de mundo de la cuadrícula de navegación.</summary>
    public float NavGridOriginY { get; set; }

    // ── Audio ─────────────────────────────────────────────────────────────────

    /// <summary>Obtiene o establece un valor que indica si AudioController está habilitado para esta escena.</summary>
    public bool UseAudio { get; set; }
}
