namespace MonoGame.Editor.Core.Gizmos;

/// <summary>
/// Pure-logic controller for gizmo state, hit-testing, drag tracking, and command generation.
/// All spatial parameters use screen-space pixels unless explicitly noted as world-space.
/// Thread-safe: drag state is protected by an internal lock.
/// </summary>
public sealed class GizmoController
{
    // ── Visual constants (GizmoRenderer must match these values) ─────────────
    /// <summary>Length of each axis arrow in screen pixels.</summary>
    public const float ArrowLength = 80f;

    /// <summary>Size of the arrowhead square in screen pixels.</summary>
    public const float ArrowHeadSize = 12f;

    /// <summary>Radius of the rotation circle in screen pixels.</summary>
    public const float RotateRadius = 70f;

    /// <summary>Size of each scale handle square in screen pixels.</summary>
    public const float ScaleHandleSize = 10f;

    /// <summary>Half-size of a default object bounding box in world units (at Scale 1).</summary>
    public const float DefaultBoundsHalfSize = 24f;

    /// <summary>Screen-X offset from the gizmo origin to the Z depth handle stem.</summary>
    public const float ZHandleOffsetX = ArrowLength + ArrowHeadSize + 14f;

    /// <summary>Size of the Z depth diamond handle in screen pixels.</summary>
    public const float ZHandleSize = 12f;

    private const float HitTolerance = 10f;

    // ── Drag state (lock-protected) ──────────────────────────────────────────
    private readonly Lock _lock = new();
    private bool _dragging;
    private GizmoDragAxis _dragAxis;
    private float _dragWorldStartX, _dragWorldStartY;
    private float _objPosStartX, _objPosStartY;
    private float _objPosStartZ;
    private float _objRotStart;
    private float _objScaleStartX, _objScaleStartY;

    // ── Public properties ────────────────────────────────────────────────────

    /// <summary>Currently active transformation tool.</summary>
    public GizmoMode Mode { get; set; } = GizmoMode.Select;

    /// <summary>Whether the grid overlay is rendered.</summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>World-space size of each grid cell. Clamped to a minimum of 1.</summary>
    public float GridCellSize
    {
        get => _gridCellSize;
        set => _gridCellSize = Math.Max(1f, value);
    }

    /// <summary>When <c>true</c> (2.5D mode) the Move gizmo exposes a Z-depth handle.</summary>
    public bool IsDepthMode { get; set; }

    private float _gridCellSize = 32f;

    // ── Drag API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to start a drag on the gizmo handle closest to <paramref name="clickScreenX"/>,
    /// <paramref name="clickScreenY"/>. Saves the object's initial transform for undo.
    /// </summary>
    /// <returns><c>true</c> if a handle was hit and the drag started.</returns>
    public bool BeginDrag(
        float clickScreenX, float clickScreenY,
        float objScreenX,   float objScreenY,
        float clickWorldX,  float clickWorldY,
        EditorGameObject selected)
    {
        if (Mode == GizmoMode.Select || Mode == GizmoMode.Rect) return false;

        GizmoDragAxis axis = HitTest(clickScreenX, clickScreenY, objScreenX, objScreenY, Mode, IsDepthMode);
        if (axis == GizmoDragAxis.None) return false;

        lock (_lock)
        {
            _dragging        = true;
            _dragAxis        = axis;
            _dragWorldStartX = clickWorldX;
            _dragWorldStartY = clickWorldY;
            _objPosStartX    = selected.Position.X;
            _objPosStartY    = selected.Position.Y;
            _objPosStartZ    = selected.PositionZ;
            _objRotStart     = selected.Rotation;
            _objScaleStartX  = selected.Scale.X;
            _objScaleStartY  = selected.Scale.Y;
        }

        return true;
    }

    /// <summary>
    /// Updates the selected object's transform to reflect the current mouse position during a drag.
    /// No-op if no drag is in progress.
    /// </summary>
    public void UpdateDrag(
        float worldX,    float worldY,
        float screenX,   float screenY,
        float objScreenX, float objScreenY,
        EditorGameObject selected)
    {
        GizmoDragAxis axis;
        float worldStartX, worldStartY;
        float posStartX, posStartY, posStartZ, rotStart, scaleStartX, scaleStartY;

        lock (_lock)
        {
            if (!_dragging) return;
            axis         = _dragAxis;
            worldStartX  = _dragWorldStartX;
            worldStartY  = _dragWorldStartY;
            posStartX    = _objPosStartX;
            posStartY    = _objPosStartY;
            posStartZ    = _objPosStartZ;
            rotStart     = _objRotStart;
            scaleStartX  = _objScaleStartX;
            scaleStartY  = _objScaleStartY;
        }

        float dx = worldX - worldStartX;
        float dy = worldY - worldStartY;

        switch (axis)
        {
            case GizmoDragAxis.X:
                selected.Position = new EditorVector2(posStartX + dx, posStartY);
                break;

            case GizmoDragAxis.Y:
                selected.Position = new EditorVector2(posStartX, posStartY + dy);
                break;

            case GizmoDragAxis.XY:
                selected.Position = new EditorVector2(posStartX + dx, posStartY + dy);
                break;

            case GizmoDragAxis.Z:
                // Drag up (decreasing screenY) = increase depth value (further from viewer).
                selected.PositionZ = posStartZ - dy;
                break;

            case GizmoDragAxis.Rotate:
            {
                float angle = MathF.Atan2(screenY - objScreenY, screenX - objScreenX);
                selected.Rotation = angle * (180f / MathF.PI);
                break;
            }

            case GizmoDragAxis.ScaleX:
            {
                float delta = dx * 0.02f;
                selected.Scale = new EditorVector2(
                    Math.Max(0.01f, scaleStartX + delta),
                    Math.Max(0.01f, scaleStartY));
                break;
            }

            case GizmoDragAxis.ScaleY:
            {
                float delta = -dy * 0.02f;
                selected.Scale = new EditorVector2(
                    Math.Max(0.01f, scaleStartX),
                    Math.Max(0.01f, scaleStartY + delta));
                break;
            }

            case GizmoDragAxis.ScaleUniform:
            {
                float dist  = MathF.Sqrt(dx * dx + dy * dy);
                float sign  = (dx + dy) > 0 ? 1f : -1f;
                float delta = sign * dist * 0.02f;
                selected.Scale = new EditorVector2(
                    Math.Max(0.01f, scaleStartX + delta),
                    Math.Max(0.01f, scaleStartY + delta));
                break;
            }
        }
    }

    /// <summary>
    /// Finalises the drag, applies snap when <paramref name="ctrlHeld"/> is <c>true</c>,
    /// and returns an undo/redo command. Returns <c>null</c> if no drag was in progress.
    /// </summary>
    public IEditorCommand? EndDrag(EditorGameObject? selected, bool ctrlHeld)
    {
        bool wasDragging;
        GizmoDragAxis axis;
        float posStartX, posStartY, posStartZ, rotStart, scaleStartX, scaleStartY;

        lock (_lock)
        {
            wasDragging  = _dragging;
            axis         = _dragAxis;
            posStartX    = _objPosStartX;
            posStartY    = _objPosStartY;
            posStartZ    = _objPosStartZ;
            rotStart     = _objRotStart;
            scaleStartX  = _objScaleStartX;
            scaleStartY  = _objScaleStartY;
            _dragging    = false;
        }

        if (!wasDragging || selected is null) return null;

        if (ctrlHeld && axis is GizmoDragAxis.X or GizmoDragAxis.Y or GizmoDragAxis.XY)
            selected.Position = SnapToGrid(selected.Position);

        EditorVector2 startPos   = new(posStartX, posStartY);
        EditorVector2 startScale = new(scaleStartX, scaleStartY);

        return axis switch
        {
            GizmoDragAxis.X or GizmoDragAxis.Y or GizmoDragAxis.XY
                => new MoveEntityCommand(selected, startPos, selected.Position),
            GizmoDragAxis.Z
                => new MoveEntityZCommand(selected, posStartZ, selected.PositionZ),
            GizmoDragAxis.Rotate
                => new RotateEntityCommand(selected, rotStart, selected.Rotation),
            GizmoDragAxis.ScaleX or GizmoDragAxis.ScaleY
                => new ScaleEntityCommand(selected, startScale, selected.Scale),
            GizmoDragAxis.ScaleUniform
                => new ScaleEntityCommand(selected, startScale, selected.Scale),
            _ => null,
        };
    }

    /// <summary>Snaps a world-space position to the nearest grid cell corner.</summary>
    public EditorVector2 SnapToGrid(EditorVector2 worldPos)
    {
        float size = _gridCellSize;
        return new EditorVector2(
            MathF.Round(worldPos.X / size) * size,
            MathF.Round(worldPos.Y / size) * size);
    }

    // ── Hit testing ──────────────────────────────────────────────────────────

    private static GizmoDragAxis HitTest(float px, float py, float ox, float oy, GizmoMode mode, bool isDepthMode) =>
        mode switch
        {
            GizmoMode.Move when isDepthMode => HitTestMoveWithZ(px, py, ox, oy),
            GizmoMode.Move   => HitTestMove(px, py, ox, oy),
            GizmoMode.Rotate => HitTestRotate(px, py, ox, oy),
            GizmoMode.Scale  => HitTestScale(px, py, ox, oy),
            _                => GizmoDragAxis.None,
        };

    private static GizmoDragAxis HitTestMoveWithZ(float px, float py, float ox, float oy)
    {
        GizmoDragAxis result = HitTestMove(px, py, ox, oy);
        return result != GizmoDragAxis.None ? result : HitTestZHandle(px, py, ox, oy);
    }

    private static GizmoDragAxis HitTestZHandle(float px, float py, float ox, float oy)
    {
        float hx = ox + ZHandleOffsetX;
        float hy = oy - ArrowLength;
        float h  = ZHandleSize / 2f + 4f;
        return InRect(px, py, hx - h, hy - h, h * 2f, h * 2f) ? GizmoDragAxis.Z : GizmoDragAxis.None;
    }

    private static GizmoDragAxis HitTestMove(float px, float py, float ox, float oy)
    {
        // XY-free square (check first — it overlaps the axis starts)
        if (InRect(px, py, ox + 12, oy - 28, 16, 16)) return GizmoDragAxis.XY;

        // X axis
        if (InRect(px, py, ox, oy - HitTolerance, ArrowLength + ArrowHeadSize, HitTolerance * 2))
            return GizmoDragAxis.X;

        // Y axis (screen-Y up = negative direction)
        if (InRect(px, py, ox - HitTolerance, oy - ArrowLength - ArrowHeadSize, HitTolerance * 2, ArrowLength + ArrowHeadSize))
            return GizmoDragAxis.Y;

        return GizmoDragAxis.None;
    }

    private static GizmoDragAxis HitTestRotate(float px, float py, float ox, float oy)
    {
        float dx   = px - ox;
        float dy   = py - oy;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        return MathF.Abs(dist - RotateRadius) < 12f ? GizmoDragAxis.Rotate : GizmoDragAxis.None;
    }

    private static GizmoDragAxis HitTestScale(float px, float py, float ox, float oy)
    {
        float h = ScaleHandleSize / 2 + 4f;

        // Centre (uniform scale)
        if (InRect(px, py, ox - h, oy - h, h * 2, h * 2)) return GizmoDragAxis.ScaleUniform;

        // X-end handle
        if (InRect(px, py, ox + ArrowLength - h, oy - h, h * 2, h * 2)) return GizmoDragAxis.ScaleX;

        // Y-end handle
        if (InRect(px, py, ox - h, oy - ArrowLength - h, h * 2, h * 2)) return GizmoDragAxis.ScaleY;

        return GizmoDragAxis.None;
    }

    private static bool InRect(float px, float py, float rx, float ry, float rw, float rh)
        => px >= rx && px <= rx + rw && py >= ry && py <= ry + rh;
}
