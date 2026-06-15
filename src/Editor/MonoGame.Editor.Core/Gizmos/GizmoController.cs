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

    /// <summary>Tamaño (profundidad de la punta triangular) de la cabeza de flecha en píxeles de pantalla.</summary>
    public const float ArrowHeadSize = 14f;

    /// <summary>Radio del círculo de rotación en píxeles de pantalla.</summary>
    public const float RotateRadius = 70f;

    /// <summary>Tamaño del cuadrado de cada manija de escala en píxeles de pantalla.</summary>
    public const float ScaleHandleSize = 10f;

    /// <summary>Semitamaño de la caja delimitadora predeterminada de un objeto en unidades de mundo (a Escala 1).</summary>
    public const float DefaultBoundsHalfSize = 24f;

    /// <summary>Distancia desde el origen a las manijas de escala en modo Universal (mitad de la flecha para evitar solape).</summary>
    public const float UniversalScaleAxisRadius = ArrowLength * 0.55f;

    private const float HitTolerance = 10f;

    // ── Estado de arrastre (protegido por bloqueo) ───────────────────────────
    private readonly Lock _lock = new();
    private bool _dragging;
    private GizmoDragAxis _dragAxis;
    private float _dragWorldStartX, _dragWorldStartY;
    private float _objPosStartX, _objPosStartY, _objPosStartZ;
    private float _objRotStartX, _objRotStartY, _objRotStartZ;
    private float _objScaleStartX, _objScaleStartY, _objScaleStartZ;

    // ── Propiedades públicas ──────────────────────────────────────────────────

    /// <summary>Herramienta de transformación actualmente activa.</summary>
    public GizmoMode Mode { get; set; } = GizmoMode.Select;

    /// <summary>Herramientas activas en el modo <see cref="GizmoMode.Universal"/>.</summary>
    public GizmoTool EnabledTools { get; set; } = GizmoTool.Move | GizmoTool.Rotate | GizmoTool.Scale;

    /// <summary>Ejes habilitados para interacción y renderizado del gizmo.</summary>
    public GizmoAxisMask EnabledAxes { get; set; } = GizmoAxisMask.Both;

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
        if (Mode is GizmoMode.Select or GizmoMode.Rect) return false;

        GizmoDragAxis axis = HitTest(clickScreenX, clickScreenY, objScreenX, objScreenY, Mode, EnabledTools, EnabledAxes);
        if (axis == GizmoDragAxis.None) return false;

        lock (_lock)
        {
            _dragging = true;
            _dragAxis = axis;
            _dragWorldStartX = clickWorldX;
            _dragWorldStartY = clickWorldY;
            _objPosStartX = selected.Position.X;
            _objPosStartY = selected.Position.Y;
            _objPosStartZ = selected.Position.Z;
            _objRotStartX = selected.Rotation.X;
            _objRotStartY = selected.Rotation.Y;
            _objRotStartZ = selected.Rotation.Z;
            _objScaleStartX = selected.Scale.X;
            _objScaleStartY = selected.Scale.Y;
            _objScaleStartZ = selected.Scale.Z;
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
        float posStartX, posStartY, posStartZ;
        float rotStartX, rotStartY, rotStartZ;
        float scaleStartX, scaleStartY, scaleStartZ;

        lock (_lock)
        {
            if (!_dragging) return;
            axis = _dragAxis;
            worldStartX = _dragWorldStartX;
            worldStartY = _dragWorldStartY;
            posStartX = _objPosStartX;
            posStartY = _objPosStartY;
            posStartZ = _objPosStartZ;
            rotStartX = _objRotStartX;
            rotStartY = _objRotStartY;
            rotStartZ = _objRotStartZ;
            scaleStartX = _objScaleStartX;
            scaleStartY = _objScaleStartY;
            scaleStartZ = _objScaleStartZ;
        }

        float dx = worldX - worldStartX;
        float dy = worldY - worldStartY;

        switch (axis)
        {
            case GizmoDragAxis.X:
                // En vista Right, el eje X de pantalla mueve la profundidad Z del mundo.
                if (Orientation == ViewOrientation.Right)
                    selected.Position = new EditorVector3(posStartX, posStartY, posStartZ + dx);
                else
                {
                    selected.Position = new EditorVector3(posStartX + dx, posStartY, posStartZ);
                    if (SnapEnabled) selected.Position = SnapToGrid(selected.Position);
                }
                break;

            case GizmoDragAxis.Y:
                // En vista Top, el eje Y de pantalla mueve la profundidad Z del mundo.
                // Signo positivo: arrastrar hacia abajo (dy > 0) → Z aumenta (objeto baja en pantalla). ✓
                if (Orientation == ViewOrientation.Top)
                    selected.Position = new EditorVector3(posStartX, posStartY, posStartZ + dy);
                else
                {
                    selected.Position = new EditorVector3(posStartX, posStartY + dy, posStartZ);
                    if (SnapEnabled) selected.Position = SnapToGrid(selected.Position);
                }
                break;

            case GizmoDragAxis.XY:
                if (Orientation == ViewOrientation.Top)
                {
                    selected.Position = new EditorVector3(posStartX + dx, posStartY, posStartZ + dy);
                    if (SnapEnabled) selected.Position = SnapToGrid(selected.Position);
                }
                else if (Orientation == ViewOrientation.Right)
                {
                    selected.Position = new EditorVector3(posStartX, posStartY + dy, posStartZ + dx);
                    if (SnapEnabled) selected.Position = SnapToGrid(selected.Position);
                }
                else
                {
                    selected.Position = new EditorVector3(posStartX + dx, posStartY + dy, posStartZ);
                    if (SnapEnabled) selected.Position = SnapToGrid(selected.Position);
                }
                break;

            case GizmoDragAxis.Rotate:
                {
                    float angle = MathF.Atan2(screenY - objScreenY, screenX - objScreenX);
                    float deg = angle * (180f / MathF.PI);
                    if (SnapEnabled && SnapRotationDegrees > 0f)
                        deg = MathF.Round(deg / SnapRotationDegrees) * SnapRotationDegrees;

                    // Cada vista de ortografía rota alrededor de un eje diferente.
                    selected.Rotation = Orientation switch
                    {
                        ViewOrientation.Top => new EditorVector3(rotStartX, deg, rotStartZ),
                        ViewOrientation.Right => new EditorVector3(deg, rotStartY, rotStartZ),
                        _ => new EditorVector3(rotStartX, rotStartY, deg),  // Front → eje Z
                    };
                    break;
                }

            case GizmoDragAxis.ScaleX:
                {
                    float delta = dx * 0.02f;
                    // En vista Right, el eje X de pantalla controla Scale.Z.
                    if (Orientation == ViewOrientation.Right)
                    {
                        float newZ = Math.Max(0.01f, scaleStartZ + delta);
                        if (SnapEnabled && SnapScaleStep > 0f)
                            newZ = MathF.Round(newZ / SnapScaleStep) * SnapScaleStep;
                        selected.Scale = new EditorVector3(scaleStartX, scaleStartY, Math.Max(0.01f, newZ));
                    }
                    else
                    {
                        float newX = Math.Max(0.01f, scaleStartX + delta);
                        if (SnapEnabled && SnapScaleStep > 0f)
                            newX = MathF.Round(newX / SnapScaleStep) * SnapScaleStep;
                        selected.Scale = new EditorVector3(Math.Max(0.01f, newX), scaleStartY, scaleStartZ);
                    }
                    break;
                }

            case GizmoDragAxis.ScaleY:
                {
                    float delta = -dy * 0.02f;
                    // En vista Top, el eje Y de pantalla controla Scale.Z.
                    if (Orientation == ViewOrientation.Top)
                    {
                        float newZ = Math.Max(0.01f, scaleStartZ + delta);
                        if (SnapEnabled && SnapScaleStep > 0f)
                            newZ = MathF.Round(newZ / SnapScaleStep) * SnapScaleStep;
                        selected.Scale = new EditorVector3(scaleStartX, scaleStartY, Math.Max(0.01f, newZ));
                    }
                    else
                    {
                        float newY = Math.Max(0.01f, scaleStartY + delta);
                        if (SnapEnabled && SnapScaleStep > 0f)
                            newY = MathF.Round(newY / SnapScaleStep) * SnapScaleStep;
                        selected.Scale = new EditorVector3(scaleStartX, Math.Max(0.01f, newY), scaleStartZ);
                    }
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
                    float v = Math.Max(0.01f, newVal);
                    selected.Scale = new EditorVector3(v, Math.Max(0.01f, scaleStartY + delta), Math.Max(0.01f, scaleStartZ + delta));
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
        float posStartX, posStartY, posStartZ;
        float rotStartX, rotStartY, rotStartZ;
        float scaleStartX, scaleStartY, scaleStartZ;

        lock (_lock)
        {
            wasDragging = _dragging;
            axis = _dragAxis;
            posStartX = _objPosStartX;
            posStartY = _objPosStartY;
            posStartZ = _objPosStartZ;
            rotStartX = _objRotStartX;
            rotStartY = _objRotStartY;
            rotStartZ = _objRotStartZ;
            scaleStartX = _objScaleStartX;
            scaleStartY = _objScaleStartY;
            scaleStartZ = _objScaleStartZ;
            _dragging = false;
        }

        if (!wasDragging || selected is null) return null;

        if (!SnapEnabled && ctrlHeld && axis is GizmoDragAxis.X or GizmoDragAxis.Y or GizmoDragAxis.XY)
            selected.Position = SnapToGrid(selected.Position);

        EditorVector3 startPos = new(posStartX, posStartY, posStartZ);
        EditorVector3 startRot = new(rotStartX, rotStartY, rotStartZ);
        EditorVector3 startScale = new(scaleStartX, scaleStartY, scaleStartZ);

        return axis switch
        {
            GizmoDragAxis.X or GizmoDragAxis.Y or GizmoDragAxis.XY or GizmoDragAxis.Z
                => new MoveEntityCommand(selected, startPos, selected.Position),

            GizmoDragAxis.Rotate
                => new RotateEntityCommand(selected, startRot, selected.Rotation),

            GizmoDragAxis.ScaleX or GizmoDragAxis.ScaleY or GizmoDragAxis.ScaleUniform
                => new ScaleEntityCommand(selected, startScale, selected.Scale),

            _ => null,
        };
    }

    /// <summary>Ajusta una posición en espacio de mundo a la esquina de celda de cuadrícula más cercana.</summary>
    public EditorVector3 SnapToGrid(EditorVector3 worldPos)
    {
        float size = _gridCellSize;
        return new EditorVector3(
            MathF.Round(worldPos.X / size) * size,
            MathF.Round(worldPos.Y / size) * size,
            worldPos.Z);
    }

    // ── Prueba de colisión ────────────────────────────────────────────────────

    private static GizmoDragAxis HitTest(
        float px, float py, float ox, float oy,
        GizmoMode mode, GizmoTool tools, GizmoAxisMask axes) =>
        mode switch
        {
            GizmoMode.Move => HitTestMove(px, py, ox, oy, axes),
            GizmoMode.Rotate => HitTestRotate(px, py, ox, oy, RotateRadius),
            GizmoMode.Scale => HitTestScale(px, py, ox, oy, ArrowLength, axes),
            GizmoMode.Universal => HitTestUniversal(px, py, ox, oy, tools, axes),
            _ => GizmoDragAxis.None,
        };

    private static GizmoDragAxis HitTestMove(float px, float py, float ox, float oy, GizmoAxisMask axes)
    {
        bool hasX = axes.HasFlag(GizmoAxisMask.X);
        bool hasY = axes.HasFlag(GizmoAxisMask.Y);

        // Cuadrado libre XY (verificar primero; requiere ambos ejes)
        if (hasX && hasY && InRect(px, py, ox + 12, oy - 28, 16, 16)) return GizmoDragAxis.XY;

        // Eje X
        if (hasX && InRect(px, py, ox, oy - HitTolerance, ArrowLength + ArrowHeadSize, HitTolerance * 2))
            return GizmoDragAxis.X;

        // Eje Y (pantalla-Y arriba = dirección negativa)
        if (hasY && InRect(px, py, ox - HitTolerance, oy - ArrowLength - ArrowHeadSize, HitTolerance * 2, ArrowLength + ArrowHeadSize))
            return GizmoDragAxis.Y;

        return GizmoDragAxis.None;
    }

    private static GizmoDragAxis HitTestRotate(float px, float py, float ox, float oy, float radius)
    {
        float dx = px - ox;
        float dy = py - oy;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        return MathF.Abs(dist - radius) < 12f ? GizmoDragAxis.Rotate : GizmoDragAxis.None;
    }

    private static GizmoDragAxis HitTestScale(float px, float py, float ox, float oy, float axisRadius, GizmoAxisMask axes)
    {
        bool hasX = axes.HasFlag(GizmoAxisMask.X);
        bool hasY = axes.HasFlag(GizmoAxisMask.Y);
        float h = ScaleHandleSize / 2 + 4f;

        // Centro (escala uniforme; requiere ambos ejes)
        if (hasX && hasY && InRect(px, py, ox - h, oy - h, h * 2, h * 2)) return GizmoDragAxis.ScaleUniform;

        // Manija del extremo X
        if (hasX && InRect(px, py, ox + axisRadius - h, oy - h, h * 2, h * 2)) return GizmoDragAxis.ScaleX;

        // Manija del extremo Y
        if (hasY && InRect(px, py, ox - h, oy - axisRadius - h, h * 2, h * 2)) return GizmoDragAxis.ScaleY;

        return GizmoDragAxis.None;
    }

    private static GizmoDragAxis HitTestUniversal(
        float px, float py, float ox, float oy,
        GizmoTool tools, GizmoAxisMask axes)
    {
        bool hasX = axes.HasFlag(GizmoAxisMask.X);
        bool hasY = axes.HasFlag(GizmoAxisMask.Y);

        // Prioridad de resolución (específico → amplio para evitar solapamientos):
        // 1. Cuadrado libre XY de Move (centro pequeño, alta especificidad)
        if (tools.HasFlag(GizmoTool.Move) && hasX && hasY && InRect(px, py, ox + 12, oy - 28, 16, 16))
            return GizmoDragAxis.XY;

        // 2. Centro de escala uniforme (encima del origen)
        if (tools.HasFlag(GizmoTool.Scale) && hasX && hasY)
        {
            float h = ScaleHandleSize / 2 + 4f;
            if (InRect(px, py, ox - h, oy - h, h * 2, h * 2)) return GizmoDragAxis.ScaleUniform;
        }

        // 3. Manijas de escala en los extremos de eje (a UniversalScaleAxisRadius)
        if (tools.HasFlag(GizmoTool.Scale))
        {
            float h = ScaleHandleSize / 2 + 4f;
            if (hasX && InRect(px, py, ox + UniversalScaleAxisRadius - h, oy - h, h * 2, h * 2))
                return GizmoDragAxis.ScaleX;
            if (hasY && InRect(px, py, ox - h, oy - UniversalScaleAxisRadius - h, h * 2, h * 2))
                return GizmoDragAxis.ScaleY;
        }

        // 4. Flechas de Move a lo largo de los ejes completos
        if (tools.HasFlag(GizmoTool.Move))
        {
            if (hasX && InRect(px, py, ox, oy - HitTolerance, ArrowLength + ArrowHeadSize, HitTolerance * 2))
                return GizmoDragAxis.X;
            if (hasY && InRect(px, py, ox - HitTolerance, oy - ArrowLength - ArrowHeadSize, HitTolerance * 2, ArrowLength + ArrowHeadSize))
                return GizmoDragAxis.Y;
        }

        // 5. Anillo de rotación (independiente de ejes)
        if (tools.HasFlag(GizmoTool.Rotate))
        {
            float dx = px - ox;
            float dy = py - oy;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (MathF.Abs(dist - RotateRadius) < 12f) return GizmoDragAxis.Rotate;
        }

        return GizmoDragAxis.None;
    }

    private static bool InRect(float px, float py, float rx, float ry, float rw, float rh)
        => px >= rx && px <= rx + rw && py >= ry && py <= ry + rh;
}
