namespace Alca.MonoGame.Kernel.DebugHelpers;

/// <summary>Type of a queued debug draw operation.</summary>
public enum DebugCommandType : byte
{
    /// <summary>A line between two points.</summary>
    Line,
    /// <summary>An axis-aligned rectangle outline.</summary>
    Rect,
    /// <summary>A circle outline approximated with segments.</summary>
    Circle,
    /// <summary>A small square or cross marker at a single point.</summary>
    Point,
    /// <summary>A text label at a position.</summary>
    Text,
}

/// <summary>A single queued debug draw command.</summary>
public struct DebugCommand
{
    /// <summary>The type of shape to render.</summary>
    public DebugCommandType Type;

    /// <summary>
    /// Primary vector — for Line: start; for Rect/Circle/Point: position or center;
    /// for Text: label position.
    /// </summary>
    public Vector2 A;

    /// <summary>
    /// Secondary vector — for Line: end point; for Rect: size (width, height);
    /// for Circle: X = radius; for Point/Text: unused.
    /// </summary>
    public Vector2 B;

    /// <summary>Draw color.</summary>
    public Color Color;

    /// <summary>Remaining lifetime in seconds. 0 means the command is drawn for one frame only.</summary>
    public float Lifetime;

    /// <summary>Text content. Only non-null for <see cref="DebugCommandType.Text"/> commands.</summary>
    public string? Text;

    /// <summary>Auxiliary size — radius for circles, cross-arm length for points.</summary>
    public float Size;
}
