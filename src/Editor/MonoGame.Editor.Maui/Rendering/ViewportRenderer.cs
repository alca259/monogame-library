namespace MonoGame.Editor.Maui.Rendering;

/// <summary>
/// IDrawable del viewport: grid sutil, previsualización de objetos de escena,
/// selection box con 8 handles y rotation handle, gizmo de ejes (esquina inferior izquierda)
/// y gizmo de orientación interactivo (esquina superior derecha).
/// </summary>
public sealed class ViewportRenderer : IDrawable
{
    // ── Layout constants del gizmo de orientación ────────────────────────────
    private const float OGizmoCenterOffsetX = 52f; // desde rect.Right
    private const float OGizmoCenterOffsetY = 52f; // desde rect.Top
    private const float OGizmoSpokeRadius = 28f;
    private const float OGizmoHitRadius = 12f;

    public EditorCamera2D Camera { get; } = new();

    public int GridCellSize { get; set; } = 26;

    /// <summary>Plano ortográfico activo. Controla qué ejes del mundo se muestran y qué propiedades
    /// afectan el arrastre del gizmo de transformación.</summary>
    public ViewOrientation Orientation { get; set; } = ViewOrientation.Front;

    public void Draw(ICanvas canvas, RectF rect)
    {
        DrawGrid(canvas, rect);
        DrawSceneObjects(canvas, rect);

        EditorGameObject? selected = EditorContext.Instance.SelectedObject;
        if (selected is not null)
        {
            SelectionInfo? sel = BuildSelectionInfo(selected, rect);
            if (sel is not null)
                DrawSelection(canvas, sel);
            DrawGizmoHandles(canvas, rect, selected);
        }

        DrawGizmo(canvas, rect);
        DrawOrientationGizmo(canvas, rect);
    }

    // ── Grid ortográfico ─────────────────────────────────────────────────────

    private void DrawGrid(ICanvas canvas, RectF rect)
    {
        float step = GridCellSize * Camera.Zoom;
        if (step < 6) return;

        canvas.StrokeColor = ResolveColor("Border").WithAlpha(0.35f);
        canvas.StrokeSize = 1;

        SizeF size = new(rect.Width, rect.Height);
        PointF tl = Camera.ScreenToWorld(new PointF(rect.Left, rect.Top), size);
        PointF br = Camera.ScreenToWorld(new PointF(rect.Right, rect.Bottom), size);

        float startX = MathF.Floor(tl.X / GridCellSize) * GridCellSize;
        for (float wx = startX; wx <= br.X + GridCellSize; wx += GridCellSize)
        {
            PointF sp = Camera.WorldToScreen(new PointF(wx, 0), size);
            canvas.DrawLine(sp.X, rect.Top, sp.X, rect.Bottom);
        }

        float startY = MathF.Floor(tl.Y / GridCellSize) * GridCellSize;
        for (float wy = startY; wy <= br.Y + GridCellSize; wy += GridCellSize)
        {
            PointF sp = Camera.WorldToScreen(new PointF(0, wy), size);
            canvas.DrawLine(rect.Left, sp.Y, rect.Right, sp.Y);
        }
    }

    // ── Proyección según orientación ─────────────────────────────────────────

    /// <summary>
    /// Devuelve el punto 2D en espacio de mundo que representa el centro del objeto
    /// para la orientación activa, listo para pasarlo a <see cref="EditorCamera2D.WorldToScreen"/>.
    /// </summary>
    private PointF GetWorldCenter(EditorGameObject obj) => Orientation switch
    {
        ViewOrientation.Top => new PointF(obj.Position.X, obj.PositionZ),
        ViewOrientation.Right => new PointF(obj.PositionZ, obj.Position.Y),
        _ => new PointF(obj.Position.X, obj.Position.Y),
    };

    /// <summary>
    /// Convierte el centro del objeto a coordenadas de pantalla respetando la orientación activa.
    /// Úsalo en el code-behind siempre que necesites pasar <c>objScreenX/Y</c> al <see cref="MonoGame.Editor.Core.Gizmos.GizmoController"/>.
    /// </summary>
    public PointF GetObjectScreenCenter(EditorGameObject obj, SizeF viewportSize)
        => Camera.WorldToScreen(GetWorldCenter(obj), viewportSize);

    // ── Scene objects (placeholder rects) ────────────────────────────────────

    private void DrawSceneObjects(ICanvas canvas, RectF rect)
    {
        EditorScene? scene = EditorContext.Instance.ActiveScene;
        if (scene is null) return;

        SizeF size = new(rect.Width, rect.Height);
        DrawObjectList(canvas, scene.RootGameObjects, size, rect);
    }

    private void DrawObjectList(ICanvas canvas, List<EditorGameObject> objects, SizeF viewSize, RectF clipRect)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            DrawObject(canvas, objects[i], viewSize, clipRect);
            if (objects[i].Children.Count > 0)
                DrawObjectList(canvas, objects[i].Children, viewSize, clipRect);
        }
    }

    private void DrawObject(ICanvas canvas, EditorGameObject obj, SizeF viewSize, RectF clipRect)
    {
        if (!obj.Active) return;

        const float defaultHalfSize = 16f;
        float halfW = defaultHalfSize * obj.Scale.X;
        float halfH = defaultHalfSize * obj.Scale.Y;

        PointF wc = GetWorldCenter(obj);
        PointF topLeft = Camera.WorldToScreen(new PointF(wc.X - halfW, wc.Y - halfH), viewSize);
        PointF botRight = Camera.WorldToScreen(new PointF(wc.X + halfW, wc.Y + halfH), viewSize);

        if (topLeft.X > clipRect.Right || botRight.X < clipRect.Left) return;
        if (topLeft.Y > clipRect.Bottom || botRight.Y < clipRect.Top) return;

        RectF bounds = new(topLeft.X, topLeft.Y, botRight.X - topLeft.X, botRight.Y - topLeft.Y);

        canvas.FillColor = ResolveColor("AccentBlue").WithAlpha(0.08f);
        canvas.StrokeColor = ResolveColor("AccentBlue").WithAlpha(0.5f);
        canvas.StrokeSize = 1;
        canvas.FillRectangle(bounds);
        canvas.DrawRectangle(bounds);
    }

    // ── Selection box + 8 handles + rotation handle ──────────────────────────

    private SelectionInfo? BuildSelectionInfo(EditorGameObject obj, RectF rect)
    {
        const float defaultHalfSize = 16f;
        float halfW = defaultHalfSize * obj.Scale.X;
        float halfH = defaultHalfSize * obj.Scale.Y;

        SizeF viewSize = new(rect.Width, rect.Height);
        PointF wc = GetWorldCenter(obj);
        PointF tl = Camera.WorldToScreen(new PointF(wc.X - halfW, wc.Y - halfH), viewSize);
        PointF br = Camera.WorldToScreen(new PointF(wc.X + halfW, wc.Y + halfH), viewSize);

        return new SelectionInfo(new RectF(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), obj.Rotation);
    }

    private void DrawSelection(ICanvas canvas, SelectionInfo sel)
    {
        RectF b = sel.ScreenBounds;

        canvas.StrokeColor = ResolveColor("AccentBlue");
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(b);

        const float h = 6f, hh = h / 2f;
        float midX = b.Center.X, midY = b.Center.Y;

        PointF[] pts =
        [
            new(b.Left,  b.Top),    new(midX, b.Top),    new(b.Right, b.Top),
            new(b.Left,  midY),                           new(b.Right, midY),
            new(b.Left,  b.Bottom), new(midX, b.Bottom), new(b.Right, b.Bottom),
        ];

        canvas.FillColor = Colors.White;
        canvas.StrokeColor = ResolveColor("AccentBlue");
        foreach (PointF p in pts)
        {
            RectF r = new(p.X - hh, p.Y - hh, h, h);
            canvas.FillRectangle(r);
            canvas.DrawRectangle(r);
        }

        float rotY = b.Top - 18f;
        canvas.StrokeColor = ResolveColor("AccentBlue");
        canvas.DrawLine(midX, b.Top, midX, rotY);
        canvas.FillColor = Colors.White;
        canvas.FillCircle(midX, rotY, 4f);
        canvas.StrokeColor = ResolveColor("AccentBlue");
        canvas.DrawCircle(midX, rotY, 4f);
    }

    // ── Gizmo transform handles ───────────────────────────────────────────────

    private void DrawGizmoHandles(ICanvas canvas, RectF rect, EditorGameObject selected)
    {
        GizmoMode mode = EditorContext.Instance.Gizmos.Mode;
        if (mode is GizmoMode.Select or GizmoMode.Rect) return;

        SizeF viewSize = new(rect.Width, rect.Height);
        PointF o = Camera.WorldToScreen(GetWorldCenter(selected), viewSize);
        float ox = o.X, oy = o.Y;

        Color axisX = ResolveColor("AxisRed");
        Color axisY = ResolveColor("AxisGreen");
        Color accent = ResolveColor("AccentBlue");

        (string labelX, string labelY) = Orientation switch
        {
            ViewOrientation.Top => ("x", "z"),
            ViewOrientation.Right => ("z", "y"),
            _ => ("x", "y"),
        };

        GizmoAxisMask axes = EditorContext.Instance.Gizmos.EnabledAxes;

        switch (mode)
        {
            case GizmoMode.Move:
                DrawMoveHandles(canvas, ox, oy, GizmoController.ArrowLength, axes, axisX, axisY, labelX, labelY);
                break;

            case GizmoMode.Rotate:
                DrawRotateHandles(canvas, ox, oy, selected.Rotation, GizmoController.RotateRadius, accent);
                break;

            case GizmoMode.Scale:
                DrawScaleHandles(canvas, ox, oy, GizmoController.ArrowLength, axes, axisX, axisY);
                break;

            case GizmoMode.Universal:
                DrawUniversalHandles(canvas, ox, oy, selected.Rotation, axes,
                    axisX, axisY, accent, labelX, labelY);
                break;
        }
    }

    private static void DrawMoveHandles(
        ICanvas canvas, float ox, float oy, float len, GizmoAxisMask axes,
        Color axisX, Color axisY, string labelX, string labelY)
    {
        bool hasX = axes.HasFlag(GizmoAxisMask.X);
        bool hasY = axes.HasFlag(GizmoAxisMask.Y);

        if (hasX)
        {
            canvas.StrokeColor = axisX;
            canvas.StrokeSize = 2;
            canvas.DrawLine(ox, oy, ox + len, oy);
            canvas.FillColor = axisX;
            canvas.FillRectangle(ox + len, oy - 6, 12, 12);
            canvas.FontColor = axisX;
            canvas.FontSize = 9;
            canvas.DrawString(labelX, ox + len + 14, oy, HorizontalAlignment.Left);
        }

        if (hasY)
        {
            canvas.StrokeColor = axisY;
            canvas.StrokeSize = 2;
            canvas.DrawLine(ox, oy, ox, oy - len);
            canvas.FillColor = axisY;
            canvas.FillRectangle(ox - 6, oy - len - 12, 12, 12);
            canvas.FontColor = axisY;
            canvas.FontSize = 9;
            canvas.DrawString(labelY, ox, oy - len - 22, HorizontalAlignment.Center);
        }

        if (hasX && hasY)
        {
            Color yellow = Color.FromArgb("#FFEE44");
            canvas.FillColor = yellow.WithAlpha(0.55f);
            canvas.FillRectangle(ox + 12, oy - 28, 16, 16);
            canvas.StrokeColor = yellow;
            canvas.StrokeSize = 1;
            canvas.DrawRectangle(ox + 12, oy - 28, 16, 16);
        }
    }

    private static void DrawRotateHandles(
        ICanvas canvas, float ox, float oy, float rotationDeg, float radius, Color accent)
    {
        canvas.StrokeColor = accent;
        canvas.StrokeSize = 2;
        canvas.DrawCircle(ox, oy, radius);

        float rad = rotationDeg * MathF.PI / 180f;
        float endX = ox + radius * MathF.Cos(rad);
        float endY = oy + radius * MathF.Sin(rad);
        canvas.DrawLine(ox, oy, endX, endY);
        canvas.FillColor = Colors.White;
        canvas.FillCircle(endX, endY, 4f);
        canvas.StrokeColor = accent;
        canvas.DrawCircle(endX, endY, 4f);
    }

    private static void DrawScaleHandles(
        ICanvas canvas, float ox, float oy, float axisRadius, GizmoAxisMask axes,
        Color axisX, Color axisY)
    {
        bool hasX = axes.HasFlag(GizmoAxisMask.X);
        bool hasY = axes.HasFlag(GizmoAxisMask.Y);
        float sh = GizmoController.ScaleHandleSize;
        float hsh = sh / 2f;

        if (hasX)
        {
            canvas.StrokeColor = axisX;
            canvas.StrokeSize = 2;
            canvas.DrawLine(ox, oy, ox + axisRadius, oy);
            canvas.FillColor = axisX;
            canvas.FillRectangle(ox + axisRadius - hsh, oy - hsh, sh, sh);
        }

        if (hasY)
        {
            canvas.StrokeColor = axisY;
            canvas.StrokeSize = 2;
            canvas.DrawLine(ox, oy, ox, oy - axisRadius);
            canvas.FillColor = axisY;
            canvas.FillRectangle(ox - hsh, oy - axisRadius - hsh, sh, sh);
        }

        if (hasX && hasY)
        {
            canvas.FillColor = Colors.White;
            canvas.StrokeColor = Colors.Gray;
            canvas.StrokeSize = 1;
            canvas.FillRectangle(ox - hsh, oy - hsh, sh, sh);
            canvas.DrawRectangle(ox - hsh, oy - hsh, sh, sh);
        }
    }

    private static void DrawUniversalHandles(
        ICanvas canvas, float ox, float oy, float rotationDeg, GizmoAxisMask axes,
        Color axisX, Color axisY, Color accent, string labelX, string labelY)
    {
        GizmoTool tools = EditorContext.Instance.Gizmos.EnabledTools;

        // Dibujar en orden: Rotate (anillo exterior, fondo), Scale (media flecha), Move (flecha completa, frente)
        if (tools.HasFlag(GizmoTool.Rotate))
            DrawRotateHandles(canvas, ox, oy, rotationDeg, GizmoController.RotateRadius, accent);

        if (tools.HasFlag(GizmoTool.Scale))
            DrawScaleHandles(canvas, ox, oy, GizmoController.UniversalScaleAxisRadius, axes, axisX, axisY);

        if (tools.HasFlag(GizmoTool.Move))
            DrawMoveHandles(canvas, ox, oy, GizmoController.ArrowLength, axes, axisX, axisY, labelX, labelY);
    }

    // ── Axis gizmo, esquina inferior-izquierda ────────────────────────────────

    private void DrawGizmo(ICanvas canvas, RectF rect)
    {
        float ox = 28, oy = rect.Height - 28, len = 22;
        canvas.StrokeSize = 2;

        Color axisX = ResolveColor("AxisRed");
        Color axisY = ResolveColor("AxisGreen");

        (string labelX, string labelY) = Orientation switch
        {
            ViewOrientation.Top => ("x", "z"),
            ViewOrientation.Right => ("z", "y"),
            _ => ("x", "y"),
        };

        canvas.StrokeColor = axisX;
        canvas.DrawLine(ox, oy, ox + len, oy);
        canvas.FontColor = axisX;
        canvas.FontSize = 10;
        canvas.DrawString(labelX, ox + len + 4, oy, HorizontalAlignment.Left);

        canvas.StrokeColor = axisY;
        canvas.DrawLine(ox, oy, ox, oy - len);
        canvas.FontColor = axisY;
        canvas.DrawString(labelY, ox, oy - len - 10, HorizontalAlignment.Center);

        canvas.FillColor = axisX;
        canvas.FillCircle(ox, oy, 2.5f);
    }

    // ── Gizmo de orientación, esquina superior-derecha ────────────────────────

    /// <summary>
    /// Dibuja el gizmo de orientación estilo Unity en la esquina superior-derecha.
    /// Es puramente visual; el hit-test se resuelve por separado en <see cref="OrientationGizmoHitTest"/>.
    /// </summary>
    private void DrawOrientationGizmo(ICanvas canvas, RectF rect)
    {
        float gcx = rect.Right - OGizmoCenterOffsetX;
        float gcy = rect.Top + OGizmoCenterOffsetY;
        float r = OGizmoSpokeRadius;

        Color cX = ResolveColor("AxisRed");
        Color cY = ResolveColor("AxisGreen");
        Color cZ = Color.FromArgb("#4488FF");     // azul para Z

        // Posiciones de las puntas de los tres spokes (X, Y, Z)
        PointF tipX = new(gcx + r, gcy);
        PointF tipY = new(gcx, gcy - r);
        PointF tipZ = new(gcx - r * 0.65f, gcy + r * 0.65f); // diagonal inferior-izquierda

        // El eje "mirando hacia la cámara" se muestra como círculo; los demás como flecha
        bool xIsEye = Orientation == ViewOrientation.Right;
        bool yIsEye = Orientation == ViewOrientation.Top;
        bool zIsEye = Orientation == ViewOrientation.Front;

        canvas.StrokeSize = 2;

        DrawOGizmoSpoke(canvas, gcx, gcy, tipX, cX, "x", xIsEye);
        DrawOGizmoSpoke(canvas, gcx, gcy, tipY, cY, "y", yIsEye);
        DrawOGizmoSpoke(canvas, gcx, gcy, tipZ, cZ, "z", zIsEye);

        // Punto central
        canvas.FillColor = ResolveColor("Border").WithAlpha(0.5f);
        canvas.FillCircle(gcx, gcy, 4f);

        // Etiqueta de vista activa
        string viewLabel = Orientation switch
        {
            ViewOrientation.Top => "Top",
            ViewOrientation.Right => "Right",
            _ => "Front",
        };
        canvas.FontColor = ResolveColor("Border").WithAlpha(0.9f);
        canvas.FontSize = 10;
        canvas.DrawString(viewLabel, gcx, gcy + r + 12, HorizontalAlignment.Center);
    }

    private static void DrawOGizmoSpoke(
        ICanvas canvas, float cx, float cy, PointF tip,
        Color color, string label, bool isEye)
    {
        canvas.StrokeColor = isEye ? color : color.WithAlpha(0.65f);
        canvas.DrawLine(cx, cy, tip.X, tip.Y);

        if (isEye)
        {
            canvas.FillColor = color;
            canvas.FillCircle(tip.X, tip.Y, 5.5f);
            canvas.StrokeColor = color;
            canvas.DrawCircle(tip.X, tip.Y, 5.5f);
        }
        else
        {
            canvas.FillColor = color;
            canvas.FillCircle(tip.X, tip.Y, 4.5f);
        }

        canvas.FontColor = color;
        canvas.FontSize = 10;
        float labelX = tip.X + (tip.X > cx ? 8f : tip.X < cx ? -8f : 0f);
        float labelY = tip.Y + (tip.Y > cy ? 6f : -14f);
        canvas.DrawString(label, labelX, labelY, HorizontalAlignment.Center);
    }

    // ── Hit-test del gizmo de orientación ─────────────────────────────────────

    /// <summary>
    /// Comprueba si <paramref name="click"/> toca uno de los spokes del gizmo de orientación.
    /// Devuelve la nueva <see cref="ViewOrientation"/> o <c>null</c> si no hay hit.
    /// </summary>
    public ViewOrientation? OrientationGizmoHitTest(PointF click, RectF viewportRect)
    {
        float gcx = viewportRect.Right - OGizmoCenterOffsetX;
        float gcy = viewportRect.Top + OGizmoCenterOffsetY;
        float r = OGizmoSpokeRadius;

        PointF tipX = new(gcx + r, gcy);
        PointF tipY = new(gcx, gcy - r);
        PointF tipZ = new(gcx - r * 0.65f, gcy + r * 0.65f);

        if (DistSq(click, tipX) <= OGizmoHitRadius * OGizmoHitRadius) return ViewOrientation.Right;
        if (DistSq(click, tipY) <= OGizmoHitRadius * OGizmoHitRadius) return ViewOrientation.Top;
        if (DistSq(click, tipZ) <= OGizmoHitRadius * OGizmoHitRadius) return ViewOrientation.Front;

        return null;
    }

    private static float DistSq(PointF a, PointF b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Color ResolveColor(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out object? val) == true && val is Color c)
            return c;
        return Colors.Gray;
    }
}

/// <summary>Snapshot de la selección en espacio pantalla para que el renderer no dependa de MonoGame.</summary>
public sealed record SelectionInfo(RectF ScreenBounds, float RotationDegrees);
