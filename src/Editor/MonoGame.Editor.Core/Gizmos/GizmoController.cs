namespace MonoGame.Editor.Core.Gizmos;

/// <summary>
/// Controlador de lógica pura para el estado del gizmo, prueba de colisión, seguimiento de arrastre y generación de comandos.
/// Todos los parámetros espaciales usan píxeles en espacio de pantalla salvo que se indique explícitamente espacio de mundo.
/// Seguro para hilos: el estado de arrastre está protegido por un bloqueo interno.
/// </summary>
public sealed class GizmoController
{
    // ── Constantes visuales (GizmoRenderer debe coincidir con estos valores) ──
    /// <summary>Longitud de cada flecha de eje en píxeles de pantalla.</summary>
    public const float ArrowLength = 80f;

    /// <summary>Tamaño del cuadrado de la punta de flecha en píxeles de pantalla.</summary>
    public const float ArrowHeadSize = 12f;

    /// <summary>Radio del círculo de rotación en píxeles de pantalla.</summary>
    public const float RotateRadius = 70f;

    /// <summary>Tamaño del cuadrado de cada manija de escala en píxeles de pantalla.</summary>
    public const float ScaleHandleSize = 10f;

    /// <summary>Semitamaño de la caja delimitadora predeterminada de un objeto en unidades de mundo (a Escala 1).</summary>
    public const float DefaultBoundsHalfSize = 24f;

    /// <summary>Desplazamiento en X de pantalla desde el origen del gizmo al vástago de la manija de profundidad Z.</summary>
    public const float ZHandleOffsetX = ArrowLength + ArrowHeadSize + 14f;

    /// <summary>Tamaño de la manija de diamante de profundidad Z en píxeles de pantalla.</summary>
    public const float ZHandleSize = 12f;

    private const float HitTolerance = 10f;

    // ── Estado de arrastre (protegido por bloqueo) ───────────────────────────
    private readonly Lock _lock = new();
    private bool _dragging;
    private GizmoDragAxis _dragAxis;
    private float _dragWorldStartX, _dragWorldStartY;
    private float _objPosStartX, _objPosStartY;
    private float _objPosStartZ;
    private float _objRotStart;
    private float _objScaleStartX, _objScaleStartY;

    // ── Propiedades públicas ──────────────────────────────────────────────────

    /// <summary>Herramienta de transformación actualmente activa.</summary>
    public GizmoMode Mode { get; set; } = GizmoMode.Select;

    /// <summary>Indica si se renderiza la superposición de cuadrícula.</summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>Tamaño en espacio de mundo de cada celda de cuadrícula. Limitado a un mínimo de 1.</summary>
    public float GridCellSize
    {
        get => _gridCellSize;
        set => _gridCellSize = Math.Max(1f, value);
    }

    /// <summary>Orientación ortográfica activa del viewport. Determina qué propiedad de mundo afecta cada eje del gizmo.</summary>
    public ViewOrientation Orientation { get; set; } = ViewOrientation.Front;

    /// <summary>Cuando es <c>true</c>, las operaciones de transformación se ajustan a pasos de cuadrícula/ángulo/escala durante el arrastre.</summary>
    public bool SnapEnabled { get; set; }

    /// <summary>Incremento de ajuste de rotación en grados. Activo cuando <see cref="SnapEnabled"/> es true.</summary>
    public float SnapRotationDegrees { get; set; } = 15f;

    /// <summary>Paso de ajuste de escala. Activo cuando <see cref="SnapEnabled"/> es true.</summary>
    public float SnapScaleStep { get; set; } = 0.1f;

    private float _gridCellSize = 32f;

    // ── API de arrastre ───────────────────────────────────────────────────────

    /// <summary>
    /// Intenta iniciar un arrastre en la manija del gizmo más cercana a <paramref name="clickScreenX"/>,
    /// <paramref name="clickScreenY"/>. Guarda la transformación inicial del objeto para deshacer.
    /// </summary>
    /// <returns><c>true</c> si se golpeó una manija y el arrastre se inició.</returns>
    public bool BeginDrag(
        float clickScreenX, float clickScreenY,
        float objScreenX, float objScreenY,
        float clickWorldX, float clickWorldY,
        EditorGameObject selected)
    {
        if (Mode == GizmoMode.Select || Mode == GizmoMode.Rect) return false;

        GizmoDragAxis axis = HitTest(clickScreenX, clickScreenY, objScreenX, objScreenY, Mode);
        if (axis == GizmoDragAxis.None) return false;

        lock (_lock)
        {
            _dragging = true;
            _dragAxis = axis;
            _dragWorldStartX = clickWorldX;
            _dragWorldStartY = clickWorldY;
            _objPosStartX = selected.Position.X;
            _objPosStartY = selected.Position.Y;
            _objPosStartZ = selected.PositionZ;
            _objRotStart = selected.Rotation;
            _objScaleStartX = selected.Scale.X;
            _objScaleStartY = selected.Scale.Y;
        }

        return true;
    }

    /// <summary>
    /// Actualiza la transformación del objeto seleccionado para reflejar la posición actual del ratón durante un arrastre.
    /// No hace nada si no hay ningún arrastre en curso.
    /// </summary>
    public void UpdateDrag(
        float worldX, float worldY,
        float screenX, float screenY,
        float objScreenX, float objScreenY,
        EditorGameObject selected)
    {
        GizmoDragAxis axis;
        float worldStartX, worldStartY;
        float posStartX, posStartY, posStartZ, rotStart, scaleStartX, scaleStartY;

        lock (_lock)
        {
            if (!_dragging) return;
            axis = _dragAxis;
            worldStartX = _dragWorldStartX;
            worldStartY = _dragWorldStartY;
            posStartX = _objPosStartX;
            posStartY = _objPosStartY;
            posStartZ = _objPosStartZ;
            rotStart = _objRotStart;
            scaleStartX = _objScaleStartX;
            scaleStartY = _objScaleStartY;
        }

        float dx = worldX - worldStartX;
        float dy = worldY - worldStartY;

        switch (axis)
        {
            case GizmoDragAxis.X:
                if (Orientation == ViewOrientation.Right)
                {
                    selected.PositionZ = posStartZ + dx;
                }
                else
                {
                    selected.Position = new EditorVector2(posStartX + dx, posStartY);
                    if (SnapEnabled) selected.Position = SnapToGrid(selected.Position);
                }
                break;

            case GizmoDragAxis.Y:
                if (Orientation == ViewOrientation.Top)
                {
                    // Drag up (dy < 0) = aumentar Z (más profundidad / más lejos)
                    selected.PositionZ = posStartZ - dy;
                }
                else
                {
                    selected.Position = new EditorVector2(posStartX, posStartY + dy);
                    if (SnapEnabled) selected.Position = SnapToGrid(selected.Position);
                }
                break;

            case GizmoDragAxis.XY:
                if (Orientation == ViewOrientation.Top)
                {
                    selected.Position = new EditorVector2(posStartX + dx, posStartY);
                    selected.PositionZ = posStartZ - dy;
                    if (SnapEnabled) selected.Position = SnapToGrid(selected.Position);
                }
                else if (Orientation == ViewOrientation.Right)
                {
                    selected.Position = new EditorVector2(posStartX, posStartY + dy);
                    selected.PositionZ = posStartZ + dx;
                    if (SnapEnabled) selected.Position = SnapToGrid(selected.Position);
                }
                else
                {
                    selected.Position = new EditorVector2(posStartX + dx, posStartY + dy);
                    if (SnapEnabled) selected.Position = SnapToGrid(selected.Position);
                }
                break;

            case GizmoDragAxis.Rotate:
                {
                    float angle = MathF.Atan2(screenY - objScreenY, screenX - objScreenX);
                    float deg = angle * (180f / MathF.PI);
                    if (SnapEnabled && SnapRotationDegrees > 0f)
                        deg = MathF.Round(deg / SnapRotationDegrees) * SnapRotationDegrees;
                    selected.Rotation = deg;
                    break;
                }

            case GizmoDragAxis.ScaleX:
                {
                    float delta = dx * 0.02f;
                    float newX = Math.Max(0.01f, scaleStartX + delta);
                    if (SnapEnabled && SnapScaleStep > 0f)
                        newX = MathF.Round(newX / SnapScaleStep) * SnapScaleStep;
                    selected.Scale = new EditorVector2(Math.Max(0.01f, newX), Math.Max(0.01f, scaleStartY));
                    break;
                }

            case GizmoDragAxis.ScaleY:
                {
                    float delta = -dy * 0.02f;
                    float newY = Math.Max(0.01f, scaleStartY + delta);
                    if (SnapEnabled && SnapScaleStep > 0f)
                        newY = MathF.Round(newY / SnapScaleStep) * SnapScaleStep;
                    selected.Scale = new EditorVector2(Math.Max(0.01f, scaleStartX), Math.Max(0.01f, newY));
                    break;
                }

            case GizmoDragAxis.ScaleUniform:
                {
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    float sign = (dx + dy) > 0 ? 1f : -1f;
                    float delta = sign * dist * 0.02f;
                    float newVal = Math.Max(0.01f, scaleStartX + delta);
                    if (SnapEnabled && SnapScaleStep > 0f)
                        newVal = MathF.Round(newVal / SnapScaleStep) * SnapScaleStep;
                    selected.Scale = new EditorVector2(Math.Max(0.01f, newVal), Math.Max(0.01f, scaleStartY + delta));
                    break;
                }
        }
    }

    /// <summary>
    /// Finaliza el arrastre, aplica el ajuste cuando <paramref name="ctrlHeld"/> es <c>true</c>,
    /// y devuelve un comando de deshacer/rehacer. Devuelve <c>null</c> si no había ningún arrastre en curso.
    /// </summary>
    public IEditorCommand? EndDrag(EditorGameObject? selected, bool ctrlHeld)
    {
        bool wasDragging;
        GizmoDragAxis axis;
        float posStartX, posStartY, posStartZ, rotStart, scaleStartX, scaleStartY;

        lock (_lock)
        {
            wasDragging = _dragging;
            axis = _dragAxis;
            posStartX = _objPosStartX;
            posStartY = _objPosStartY;
            posStartZ = _objPosStartZ;
            rotStart = _objRotStart;
            scaleStartX = _objScaleStartX;
            scaleStartY = _objScaleStartY;
            _dragging = false;
        }

        if (!wasDragging || selected is null) return null;

        // Ajuste al soltar: cuando SnapEnabled está activo, ya se ajustó durante el arrastre.
        // Cuando Ctrl está presionado y SnapEnabled está desactivado, aplicar un ajuste de cuadrícula puntual.
        if (!SnapEnabled && ctrlHeld && axis is GizmoDragAxis.X or GizmoDragAxis.Y or GizmoDragAxis.XY)
            selected.Position = SnapToGrid(selected.Position);

        EditorVector2 startPos = new(posStartX, posStartY);
        EditorVector2 startScale = new(scaleStartX, scaleStartY);

        // Produce the most appropriate undo command based on axis + orientation.
        // For combined XY drags in non-Front views both position and PositionZ change;
        // we prioritise the MoveEntityCommand (position) since it covers the majority of cases.
        return axis switch
        {
            GizmoDragAxis.X when Orientation == ViewOrientation.Right
                => new MoveEntityZCommand(selected, posStartZ, selected.PositionZ),

            GizmoDragAxis.Y when Orientation == ViewOrientation.Top
                => new MoveEntityZCommand(selected, posStartZ, selected.PositionZ),

            GizmoDragAxis.X or GizmoDragAxis.Y or GizmoDragAxis.XY
                => new MoveEntityCommand(selected, startPos, selected.Position),

            GizmoDragAxis.Z
                => new MoveEntityZCommand(selected, posStartZ, selected.PositionZ),

            GizmoDragAxis.Rotate
                => new RotateEntityCommand(selected, rotStart, selected.Rotation),

            GizmoDragAxis.ScaleX or GizmoDragAxis.ScaleY or GizmoDragAxis.ScaleUniform
                => new ScaleEntityCommand(selected, startScale, selected.Scale),

            _ => null,
        };
    }

    /// <summary>Ajusta una posición en espacio de mundo a la esquina de celda de cuadrícula más cercana.</summary>
    public EditorVector2 SnapToGrid(EditorVector2 worldPos)
    {
        float size = _gridCellSize;
        return new EditorVector2(
            MathF.Round(worldPos.X / size) * size,
            MathF.Round(worldPos.Y / size) * size);
    }

    // ── Prueba de colisión ────────────────────────────────────────────────────

    private static GizmoDragAxis HitTest(float px, float py, float ox, float oy, GizmoMode mode) =>
        mode switch
        {
            GizmoMode.Move => HitTestMove(px, py, ox, oy),
            GizmoMode.Rotate => HitTestRotate(px, py, ox, oy),
            GizmoMode.Scale => HitTestScale(px, py, ox, oy),
            _ => GizmoDragAxis.None,
        };

    private static GizmoDragAxis HitTestMove(float px, float py, float ox, float oy)
    {
        // Cuadrado libre XY (verificar primero, ya que se superpone con los inicios de eje)
        if (InRect(px, py, ox + 12, oy - 28, 16, 16)) return GizmoDragAxis.XY;

        // Eje X
        if (InRect(px, py, ox, oy - HitTolerance, ArrowLength + ArrowHeadSize, HitTolerance * 2))
            return GizmoDragAxis.X;

        // Eje Y (pantalla-Y arriba = dirección negativa)
        if (InRect(px, py, ox - HitTolerance, oy - ArrowLength - ArrowHeadSize, HitTolerance * 2, ArrowLength + ArrowHeadSize))
            return GizmoDragAxis.Y;

        return GizmoDragAxis.None;
    }

    private static GizmoDragAxis HitTestRotate(float px, float py, float ox, float oy)
    {
        float dx = px - ox;
        float dy = py - oy;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        return MathF.Abs(dist - RotateRadius) < 12f ? GizmoDragAxis.Rotate : GizmoDragAxis.None;
    }

    private static GizmoDragAxis HitTestScale(float px, float py, float ox, float oy)
    {
        float h = ScaleHandleSize / 2 + 4f;

        // Centro (escala uniforme)
        if (InRect(px, py, ox - h, oy - h, h * 2, h * 2)) return GizmoDragAxis.ScaleUniform;

        // Manija del extremo X
        if (InRect(px, py, ox + ArrowLength - h, oy - h, h * 2, h * 2)) return GizmoDragAxis.ScaleX;

        // Manija del extremo Y
        if (InRect(px, py, ox - h, oy - ArrowLength - h, h * 2, h * 2)) return GizmoDragAxis.ScaleY;

        return GizmoDragAxis.None;
    }

    private static bool InRect(float px, float py, float rx, float ry, float rw, float rh)
        => px >= rx && px <= rx + rw && py >= ry && py <= ry + rh;
}
