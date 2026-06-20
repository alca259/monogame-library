using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Text.Json;

namespace MonoGame.Editor.Winforms.Rendering;

/// <summary>
/// Renderer GDI+ del viewport: grid adaptativo, objetos de escena, selection box, gizmos de
/// transformación, gizmo de ejes (esquina inferior izquierda) y gizmo de orientación (esquina
/// superior derecha). Port de <c>MonoGame.Editor.Maui.Rendering.ViewportRenderer</c>.
/// </summary>
internal sealed class GizmoRenderer : IDisposable
{
    // ── Constantes de layout ──────────────────────────────────────────────────
    private const float MinGridStepPx      = 40f;
    public  const float GridScaleFactor    = 10f;
    private const float OGizmoCenterOffsetX = 52f;
    private const float OGizmoCenterOffsetY = 52f;
    private const float OGizmoSpokeRadius  = 28f;
    private const float OGizmoHitRadius    = 12f;

    // ── Caché de pens y brushes (sin allocations por fotograma) ──────────────
    private readonly Pen         _gridPen;
    private readonly Pen         _axisXPen2;
    private readonly Pen         _axisYPen2;
    private readonly Pen         _accentPen2;
    private readonly Pen         _selectionPen;
    private readonly Pen         _behaviourPen;
    private readonly SolidBrush  _axisXBrush;
    private readonly SolidBrush  _axisYBrush;
    private readonly SolidBrush  _accentBrush;
    private readonly SolidBrush  _whiteBrush;
    private readonly SolidBrush  _yellowFillBrush;
    private readonly SolidBrush  _objFillBrush;
    private readonly SolidBrush  _textSecBrush;
    private readonly SolidBrush  _borderDimBrush;
    private readonly Font        _labelFont;
    private readonly Font        _gridFont;

    // Reutilizable para cabezas de flecha (evita alloc por fotograma)
    private readonly PointF[]    _tri = new PointF[3];

    // Caché de tipos de behaviour (evita scan de ensamblados por fotograma)
    private readonly Dictionary<string, Type?> _behaviourTypeCache = new();

    public GizmoRenderer()
    {
        _gridPen         = new Pen(Color.FromArgb(89,  EditorColors.Border), 1f);
        _axisXPen2       = new Pen(EditorColors.AxisRed,   2f);
        _axisYPen2       = new Pen(EditorColors.AxisGreen, 2f);
        _accentPen2      = new Pen(EditorColors.AccentBlue, 2f);
        _selectionPen    = new Pen(EditorColors.AccentBlue, 1f);
        _behaviourPen    = new Pen(Color.FromArgb(89, Color.White), 1f);
        _axisXBrush      = new SolidBrush(EditorColors.AxisRed);
        _axisYBrush      = new SolidBrush(EditorColors.AxisGreen);
        _accentBrush     = new SolidBrush(EditorColors.AccentBlue);
        _whiteBrush      = new SolidBrush(Color.White);
        _yellowFillBrush = new SolidBrush(Color.FromArgb(140, 0xEE, 0xEE, 0x44));
        _objFillBrush    = new SolidBrush(Color.FromArgb(20,  EditorColors.AccentBlue));
        _textSecBrush    = new SolidBrush(EditorColors.TextSecondary);
        _borderDimBrush  = new SolidBrush(Color.FromArgb(128, EditorColors.Border));
        _labelFont       = EditorFonts.Tiny;
        _gridFont        = EditorFonts.MonoSmall;
    }

    public void Dispose()
    {
        _gridPen.Dispose();
        _axisXPen2.Dispose();
        _axisYPen2.Dispose();
        _accentPen2.Dispose();
        _selectionPen.Dispose();
        _behaviourPen.Dispose();
        _axisXBrush.Dispose();
        _axisYBrush.Dispose();
        _accentBrush.Dispose();
        _whiteBrush.Dispose();
        _yellowFillBrush.Dispose();
        _objFillBrush.Dispose();
        _textSecBrush.Dispose();
        _borderDimBrush.Dispose();
    }

    // ── Entry point ───────────────────────────────────────────────────────────

    public void DrawAll(Graphics g, EditorCamera2D camera, ViewOrientation orientation, SizeF size)
    {
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        DrawGrid(g, camera, size);
        DrawSceneObjects(g, camera, orientation, size);

        EditorGameObject? selected = EditorContext.Instance.SelectedObject;
        if (selected is not null)
        {
            SelectionInfo? sel = BuildSelectionInfo(selected, camera, orientation, size);
            if (sel is not null)
                DrawSelection(g, sel);
            DrawGizmoHandles(g, camera, orientation, selected, size);
            DrawBehaviourGizmos(g, camera, orientation, selected, size);
        }

        DrawAxisGizmo(g, orientation, size);
        DrawOrientationGizmo(g, orientation, size);
    }

    // ── Grid ─────────────────────────────────────────────────────────────────

    private void DrawGrid(Graphics g, EditorCamera2D camera, SizeF size)
    {
        float cellSize = EditorContext.Instance.Gizmos.GridCellSize;
        float step = cellSize * camera.Zoom;
        while (step < MinGridStepPx)
        {
            cellSize *= GridScaleFactor;
            step = cellSize * camera.Zoom;
            if (cellSize > 1e9f) return;
        }
        if (step < 6f) return;

        PointF tl = camera.ScreenToWorld(PointF.Empty, size);
        PointF br = camera.ScreenToWorld(new PointF(size.Width, size.Height), size);

        float minX = Math.Min(tl.X, br.X), maxX = Math.Max(tl.X, br.X);
        float startX = MathF.Floor(minX / cellSize) * cellSize;
        for (float wx = startX; wx <= maxX + cellSize; wx += cellSize)
        {
            float sx = camera.WorldToScreen(new PointF(wx, 0f), size).X;
            g.DrawLine(_gridPen, sx, 0f, sx, size.Height);
        }

        float minY = Math.Min(tl.Y, br.Y), maxY = Math.Max(tl.Y, br.Y);
        float startY = MathF.Floor(minY / cellSize) * cellSize;
        for (float wy = startY; wy <= maxY + cellSize; wy += cellSize)
        {
            float sy = camera.WorldToScreen(new PointF(0f, wy), size).Y;
            g.DrawLine(_gridPen, 0f, sy, size.Width, sy);
        }

        DrawGridScale(g, size, cellSize);
    }

    private void DrawGridScale(Graphics g, SizeF size, float cellSize)
    {
        string label = FormatGridCellSize(cellSize);
        float px = 28f;
        float py = size.Height - 14f;
        g.DrawString(label, _gridFont, _textSecBrush, px, py - _gridFont.Height);
    }

    private static string FormatGridCellSize(float meters) =>
        meters >= 1000f ? $"Grid: {meters / 1000f:G3} km"
        : meters >= 1f  ? $"Grid: {meters:G3} m"
        :                  $"Grid: {meters * 100f:G3} cm";

    // ── Proyección por orientación ────────────────────────────────────────────

    private static PointF GetWorldCenter(EditorGameObject obj, ViewOrientation o) => o switch
    {
        ViewOrientation.Top   => new PointF(obj.Position.X, obj.Position.Z),
        ViewOrientation.Right => new PointF(obj.Position.Z, obj.Position.Y),
        _                     => new PointF(obj.Position.X, obj.Position.Y),
    };

    private static float GetVisibleRotation(EditorGameObject obj, ViewOrientation o) => o switch
    {
        ViewOrientation.Top   => obj.Rotation.Y,
        ViewOrientation.Right => obj.Rotation.X,
        _                     => obj.Rotation.Z,
    };

    private static (float halfW, float halfH) GetVisibleScale(EditorGameObject obj, ViewOrientation o)
    {
        const float d = 0.5f;
        return o switch
        {
            ViewOrientation.Top   => (d * obj.Scale.X, d * obj.Scale.Z),
            ViewOrientation.Right => (d * obj.Scale.Z, d * obj.Scale.Y),
            _                     => (d * obj.Scale.X, d * obj.Scale.Y),
        };
    }

    /// <summary>Centro del objeto en coordenadas de pantalla; úsalo para pasar a <see cref="GizmoController"/>.</summary>
    public PointF GetObjectScreenCenter(EditorGameObject obj, EditorCamera2D camera, ViewOrientation orientation, SizeF size)
        => camera.WorldToScreen(GetWorldCenter(obj, orientation), size);

    // ── Objetos de escena ─────────────────────────────────────────────────────

    private void DrawSceneObjects(Graphics g, EditorCamera2D camera, ViewOrientation orientation, SizeF size)
    {
        EditorScene? scene = EditorContext.Instance.ActiveScene;
        if (scene is null) return;
        DrawObjectList(g, camera, orientation, scene.RootGameObjects, size);
    }

    private void DrawObjectList(Graphics g, EditorCamera2D camera, ViewOrientation orientation,
        List<EditorGameObject> objects, SizeF size)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            DrawObject(g, camera, orientation, objects[i], size);
            if (objects[i].Children.Count > 0)
                DrawObjectList(g, camera, orientation, objects[i].Children, size);
        }
    }

    private void DrawObject(Graphics g, EditorCamera2D camera, ViewOrientation orientation,
        EditorGameObject obj, SizeF size)
    {
        if (!obj.Active) return;

        (float halfW, float halfH) = GetVisibleScale(obj, orientation);
        PointF wc      = GetWorldCenter(obj, orientation);
        PointF topLeft = camera.WorldToScreen(new PointF(wc.X - halfW, wc.Y - halfH), size);
        PointF botRight = camera.WorldToScreen(new PointF(wc.X + halfW, wc.Y + halfH), size);

        if (topLeft.X > size.Width || botRight.X < 0) return;
        if (topLeft.Y > size.Height || botRight.Y < 0) return;

        RectangleF bounds = new(topLeft.X, topLeft.Y, botRight.X - topLeft.X, botRight.Y - topLeft.Y);

        // Usar pen de 1px para el borde a 50% y brush para el relleno a 8%
        using Pen borderPen = new(Color.FromArgb(128, EditorColors.AccentBlue), 1f);
        g.FillRectangle(_objFillBrush, bounds);
        g.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    // ── Selection box ─────────────────────────────────────────────────────────

    private sealed record SelectionInfo(RectangleF ScreenBounds, float RotationDeg);

    private SelectionInfo? BuildSelectionInfo(EditorGameObject obj, EditorCamera2D camera,
        ViewOrientation orientation, SizeF size)
    {
        (float halfW, float halfH) = GetVisibleScale(obj, orientation);
        PointF wc  = GetWorldCenter(obj, orientation);
        PointF tl  = camera.WorldToScreen(new PointF(wc.X - halfW, wc.Y - halfH), size);
        PointF br  = camera.WorldToScreen(new PointF(wc.X + halfW, wc.Y + halfH), size);
        return new SelectionInfo(new RectangleF(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y),
                                 GetVisibleRotation(obj, orientation));
    }

    private void DrawSelection(Graphics g, SelectionInfo sel)
    {
        RectangleF b = sel.ScreenBounds;
        g.DrawRectangle(_selectionPen, b.X, b.Y, b.Width, b.Height);

        const float h = 6f, hh = h / 2f;
        PointF[] corners =
        [
            new(b.Left,  b.Top),    new(b.Right, b.Top),
            new(b.Left,  b.Bottom), new(b.Right, b.Bottom),
        ];
        foreach (PointF p in corners)
            g.FillRectangle(_accentBrush, p.X - hh, p.Y - hh, h, h);
    }

    // ── Gizmo handles de transformación ──────────────────────────────────────

    private void DrawGizmoHandles(Graphics g, EditorCamera2D camera, ViewOrientation orientation,
        EditorGameObject selected, SizeF size)
    {
        GizmoMode mode = EditorContext.Instance.Gizmos.Mode;
        if (mode is GizmoMode.Select or GizmoMode.Rect) return;

        PointF o = camera.WorldToScreen(GetWorldCenter(selected, orientation), size);
        float ox = o.X, oy = o.Y;

        (string labelX, string labelY) = orientation switch
        {
            ViewOrientation.Top   => ("x", "z"),
            ViewOrientation.Right => ("z", "y"),
            _                     => ("x", "y"),
        };

        GizmoAxisMask axes = EditorContext.Instance.Gizmos.EnabledAxes;
        float rot = GetVisibleRotation(selected, orientation);

        switch (mode)
        {
            case GizmoMode.Move:
                DrawMoveHandles(g, ox, oy, GizmoController.ArrowLength, axes, labelX, labelY);
                break;
            case GizmoMode.Rotate:
                DrawRotateHandles(g, ox, oy, rot, GizmoController.RotateRadius);
                break;
            case GizmoMode.Scale:
                DrawScaleHandles(g, ox, oy, GizmoController.ArrowLength, axes);
                break;
            case GizmoMode.Universal:
                DrawUniversalHandles(g, ox, oy, rot, axes, labelX, labelY);
                break;
        }
    }

    private void DrawMoveHandles(Graphics g, float ox, float oy, float len, GizmoAxisMask axes,
        string labelX, string labelY)
    {
        bool hasX = axes.HasFlag(GizmoAxisMask.X);
        bool hasY = axes.HasFlag(GizmoAxisMask.Y);
        const float head = GizmoController.ArrowHeadSize;

        if (hasX)
        {
            g.DrawLine(_axisXPen2, ox, oy, ox + len, oy);
            _tri[0] = new PointF(ox + len, oy - 7);
            _tri[1] = new PointF(ox + len + head, oy);
            _tri[2] = new PointF(ox + len, oy + 7);
            g.FillPolygon(_axisXBrush, _tri);
            DrawLabel(g, labelX, ox + len + head + 4, oy, _axisXBrush);
        }
        if (hasY)
        {
            g.DrawLine(_axisYPen2, ox, oy, ox, oy - len);
            _tri[0] = new PointF(ox - 7, oy - len);
            _tri[1] = new PointF(ox, oy - len - head);
            _tri[2] = new PointF(ox + 7, oy - len);
            g.FillPolygon(_axisYBrush, _tri);
            DrawLabelCentered(g, labelY, ox, oy - len - head - 6, _axisYBrush);
        }
        if (hasX && hasY)
        {
            g.FillRectangle(_yellowFillBrush, ox + 12, oy - 28, 16, 16);
            using Pen yp = new(Color.FromArgb(0xFF, 0xEE, 0x44), 1f);
            g.DrawRectangle(yp, ox + 12, oy - 28, 16, 16);
        }
    }

    private void DrawRotateHandles(Graphics g, float ox, float oy, float rotDeg, float radius)
    {
        g.DrawEllipse(_accentPen2, ox - radius, oy - radius, radius * 2, radius * 2);
        float rad  = rotDeg * MathF.PI / 180f;
        float endX = ox + radius * MathF.Cos(rad);
        float endY = oy + radius * MathF.Sin(rad);
        g.DrawLine(_accentPen2, ox, oy, endX, endY);
        g.FillEllipse(_whiteBrush, endX - 4, endY - 4, 8, 8);
        g.DrawEllipse(_accentPen2, endX - 4, endY - 4, 8, 8);
    }

    private void DrawScaleHandles(Graphics g, float ox, float oy, float axisR, GizmoAxisMask axes)
    {
        bool hasX = axes.HasFlag(GizmoAxisMask.X);
        bool hasY = axes.HasFlag(GizmoAxisMask.Y);
        float sh = GizmoController.ScaleHandleSize, hsh = sh / 2f;

        if (hasX)
        {
            g.DrawLine(_axisXPen2, ox, oy, ox + axisR, oy);
            g.FillRectangle(_axisXBrush, ox + axisR - hsh, oy - hsh, sh, sh);
        }
        if (hasY)
        {
            g.DrawLine(_axisYPen2, ox, oy, ox, oy - axisR);
            g.FillRectangle(_axisYBrush, ox - hsh, oy - axisR - hsh, sh, sh);
        }
        if (hasX && hasY)
        {
            g.FillRectangle(_whiteBrush, ox - hsh, oy - hsh, sh, sh);
            using Pen gp = new(Color.Gray, 1f);
            g.DrawRectangle(gp, ox - hsh, oy - hsh, sh, sh);
        }
    }

    private void DrawUniversalHandles(Graphics g, float ox, float oy, float rotDeg,
        GizmoAxisMask axes, string labelX, string labelY)
    {
        GizmoTool tools = EditorContext.Instance.Gizmos.EnabledTools;
        if (tools.HasFlag(GizmoTool.Rotate))
            DrawRotateHandles(g, ox, oy, rotDeg, GizmoController.RotateRadius);
        if (tools.HasFlag(GizmoTool.Scale))
            DrawScaleHandles(g, ox, oy, GizmoController.UniversalScaleAxisRadius, axes);
        if (tools.HasFlag(GizmoTool.Move))
            DrawMoveHandles(g, ox, oy, GizmoController.ArrowLength, axes, labelX, labelY);
    }

    // ── Behaviour gizmos (radio circles) ─────────────────────────────────────

    private void DrawBehaviourGizmos(Graphics g, EditorCamera2D camera, ViewOrientation orientation,
        EditorGameObject obj, SizeF size)
    {
        if (obj.Behaviours.Count == 0) return;
        PointF center = camera.WorldToScreen(GetWorldCenter(obj, orientation), size);

        foreach (EditorBehaviour behaviour in obj.Behaviours)
        {
            if (!_behaviourTypeCache.TryGetValue(behaviour.TypeName, out Type? type))
            {
                type = FindBehaviourType(behaviour.TypeName);
                _behaviourTypeCache[behaviour.TypeName] = type;
            }

            if (type is not null)
            {
                foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (pi.GetCustomAttribute<Core.Attributes.EditorRadiusPreviewAttribute>() is null) continue;
                    DrawRadiusCircle(g, camera, center, behaviour, pi.Name);
                }
            }

            // Built-in radius previews via BehaviourEditorRegistry (from Drawers)
            // Note: Drawers are in MonoGame.Editor.Maui; for Fase 3 we skip that integration.
        }
    }

    private void DrawRadiusCircle(Graphics g, EditorCamera2D camera, PointF center,
        EditorBehaviour behaviour, string propName)
    {
        if (!behaviour.Properties.TryGetValue(propName, out JsonElement el)) return;
        if (el.ValueKind != JsonValueKind.Number) return;
        float worldRadius = el.GetSingle();
        if (worldRadius <= 0f) return;
        float sr = worldRadius * camera.Zoom;
        if (sr < 2f) return;
        g.DrawEllipse(_behaviourPen, center.X - sr, center.Y - sr, sr * 2, sr * 2);
    }

    private static Type? FindBehaviourType(string typeName)
    {
        string lookup = typeName.Contains(',') ? typeName[..typeName.IndexOf(',')].Trim() : typeName;
        foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type? t = asm.GetType(lookup);
                if (t is not null) return t;
                foreach (Type t2 in asm.GetTypes())
                    if (t2.FullName == lookup) return t2;
            }
            catch { }
        }
        return null;
    }

    // ── Axis gizmo (esquina inferior-izquierda) ───────────────────────────────

    private void DrawAxisGizmo(Graphics g, ViewOrientation orientation, SizeF size)
    {
        float ox = 28, oy = size.Height - 28, len = 22;

        (string labelX, string labelY) = orientation switch
        {
            ViewOrientation.Top   => ("x", "z"),
            ViewOrientation.Right => ("z", "y"),
            _                     => ("x", "y"),
        };

        g.DrawLine(_axisXPen2, ox, oy, ox + len, oy);
        DrawLabel(g, labelX, ox + len + 4, oy, _axisXBrush);

        g.DrawLine(_axisYPen2, ox, oy, ox, oy - len);
        DrawLabelCentered(g, labelY, ox, oy - len - 10, _axisYBrush);

        g.FillEllipse(_axisXBrush, ox - 2.5f, oy - 2.5f, 5f, 5f);
    }

    // ── Orientation gizmo (esquina superior-derecha) ──────────────────────────

    private void DrawOrientationGizmo(Graphics g, ViewOrientation orientation, SizeF size)
    {
        float gcx = size.Width  - OGizmoCenterOffsetX;
        float gcy = OGizmoCenterOffsetY;
        float r   = OGizmoSpokeRadius;

        PointF tipX = new(gcx + r, gcy);
        PointF tipY = new(gcx, gcy - r);
        PointF tipZ = new(gcx - r * 0.65f, gcy + r * 0.65f);

        Color cX = EditorColors.AxisRed;
        Color cY = EditorColors.AxisGreen;
        Color cZ = Color.FromArgb(0x44, 0x88, 0xFF);

        bool xIsEye = orientation == ViewOrientation.Right;
        bool yIsEye = orientation == ViewOrientation.Top;
        bool zIsEye = orientation == ViewOrientation.Front;

        DrawOGizmoSpoke(g, gcx, gcy, tipX, cX, "x", xIsEye);
        DrawOGizmoSpoke(g, gcx, gcy, tipY, cY, "y", yIsEye);
        DrawOGizmoSpoke(g, gcx, gcy, tipZ, cZ, "z", zIsEye);

        // Centro
        g.FillEllipse(_borderDimBrush, gcx - 4f, gcy - 4f, 8f, 8f);

        // Etiqueta de orientación activa
        string viewLabel = orientation switch
        {
            ViewOrientation.Top   => "Top",
            ViewOrientation.Right => "Right",
            _                     => "Front",
        };
        using SolidBrush labelBrush = new(Color.FromArgb(230, EditorColors.Border));
        DrawLabelCentered(g, viewLabel, gcx, gcy + r + 12, labelBrush);
    }

    private void DrawOGizmoSpoke(Graphics g, float cx, float cy, PointF tip, Color color, string label, bool isEye)
    {
        using Pen spokePen = new(isEye ? color : Color.FromArgb(165, color), 2f);
        g.DrawLine(spokePen, cx, cy, tip.X, tip.Y);

        float dotR = isEye ? 5.5f : 4.5f;
        using SolidBrush dotBrush = new(color);
        g.FillEllipse(dotBrush, tip.X - dotR, tip.Y - dotR, dotR * 2, dotR * 2);
        if (isEye) g.DrawEllipse(spokePen, tip.X - dotR, tip.Y - dotR, dotR * 2, dotR * 2);

        float lx = tip.X + (tip.X > cx ? 8f : tip.X < cx ? -8f : 0f);
        float ly = tip.Y + (tip.Y > cy ? 6f : -14f);
        DrawLabelCentered(g, label, lx, ly, dotBrush);
    }

    // ── Hit-test del gizmo de orientación ─────────────────────────────────────

    /// <summary>
    /// Devuelve la nueva <see cref="ViewOrientation"/> si el clic toca un spoke,
    /// o <c>null</c> si no hay hit.
    /// </summary>
    public ViewOrientation? OrientationGizmoHitTest(PointF click, SizeF viewSize)
    {
        float gcx = viewSize.Width  - OGizmoCenterOffsetX;
        float gcy = OGizmoCenterOffsetY;
        float r   = OGizmoSpokeRadius;

        PointF tipX = new(gcx + r, gcy);
        PointF tipY = new(gcx, gcy - r);
        PointF tipZ = new(gcx - r * 0.65f, gcy + r * 0.65f);

        if (DistSq(click, tipX) <= OGizmoHitRadius * OGizmoHitRadius) return ViewOrientation.Right;
        if (DistSq(click, tipY) <= OGizmoHitRadius * OGizmoHitRadius) return ViewOrientation.Top;
        if (DistSq(click, tipZ) <= OGizmoHitRadius * OGizmoHitRadius) return ViewOrientation.Front;
        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void DrawLabel(Graphics g, string text, float x, float y, Brush brush)
        => g.DrawString(text, _labelFont, brush, x, y - _labelFont.Height * 0.5f);

    private void DrawLabelCentered(Graphics g, string text, float cx, float cy, Brush brush)
    {
        SizeF sz = g.MeasureString(text, _labelFont);
        g.DrawString(text, _labelFont, brush, cx - sz.Width * 0.5f, cy - _labelFont.Height * 0.5f);
    }

    private static float DistSq(PointF a, PointF b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
