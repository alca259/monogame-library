namespace MonoGame.Editor.Maui.Rendering;

/// <summary>
/// IDrawable del viewport: grid sutil, previsualización de objetos de escena,
/// selection box con 8 handles y rotation handle, y gizmo de ejes (esquina inferior izquierda).
/// </summary>
public sealed class ViewportRenderer : IDrawable
{
    public EditorCamera2D Camera { get; } = new();

    public int GridCellSize { get; set; } = 26;

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
    }

    // ── Grid (no crosshair) ──────────────────────────────────────────────────

    private void DrawGrid(ICanvas canvas, RectF rect)
    {
        float step = GridCellSize * Camera.Zoom;
        if (step < 6) return;

        canvas.StrokeColor = ResolveColor("Border").WithAlpha(0.35f);
        canvas.StrokeSize  = 1;

        SizeF  size = new(rect.Width, rect.Height);
        PointF tl   = Camera.ScreenToWorld(new PointF(rect.Left, rect.Top),    size);
        PointF br   = Camera.ScreenToWorld(new PointF(rect.Right, rect.Bottom), size);

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

        PointF topLeft  = Camera.WorldToScreen(new PointF(obj.Position.X - halfW, obj.Position.Y - halfH), viewSize);
        PointF botRight = Camera.WorldToScreen(new PointF(obj.Position.X + halfW, obj.Position.Y + halfH), viewSize);

        if (topLeft.X > clipRect.Right || botRight.X < clipRect.Left) return;
        if (topLeft.Y > clipRect.Bottom || botRight.Y < clipRect.Top) return;

        RectF bounds = new(topLeft.X, topLeft.Y, botRight.X - topLeft.X, botRight.Y - topLeft.Y);

        canvas.FillColor   = ResolveColor("AccentBlue").WithAlpha(0.08f);
        canvas.StrokeColor = ResolveColor("AccentBlue").WithAlpha(0.5f);
        canvas.StrokeSize  = 1;
        canvas.FillRectangle(bounds);
        canvas.DrawRectangle(bounds);
    }

    // ── Selection box + 8 handles + rotation handle ──────────────────────────

    private SelectionInfo? BuildSelectionInfo(EditorGameObject obj, RectF rect)
    {
        const float defaultHalfSize = 16f;
        float halfW = defaultHalfSize * obj.Scale.X;
        float halfH = defaultHalfSize * obj.Scale.Y;

        SizeF  viewSize = new(rect.Width, rect.Height);
        PointF tl = Camera.WorldToScreen(new PointF(obj.Position.X - halfW, obj.Position.Y - halfH), viewSize);
        PointF br = Camera.WorldToScreen(new PointF(obj.Position.X + halfW, obj.Position.Y + halfH), viewSize);

        return new SelectionInfo(new RectF(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), obj.Rotation);
    }

    private void DrawSelection(ICanvas canvas, SelectionInfo sel)
    {
        RectF b = sel.ScreenBounds;

        canvas.StrokeColor = ResolveColor("AccentBlue");
        canvas.StrokeSize  = 1;
        canvas.DrawRectangle(b);

        const float h = 6f, hh = h / 2f;
        float midX = b.Center.X, midY = b.Center.Y;

        PointF[] pts =
        [
            new(b.Left,  b.Top),    new(midX, b.Top),    new(b.Right, b.Top),
            new(b.Left,  midY),                           new(b.Right, midY),
            new(b.Left,  b.Bottom), new(midX, b.Bottom), new(b.Right, b.Bottom),
        ];

        canvas.FillColor   = Colors.White;
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
        canvas.FillColor   = Colors.White;
        canvas.FillCircle(midX, rotY, 4f);
        canvas.StrokeColor = ResolveColor("AccentBlue");
        canvas.DrawCircle(midX, rotY, 4f);
    }

    // ── Gizmo transform handles ───────────────────────────────────────────────

    private void DrawGizmoHandles(ICanvas canvas, RectF rect, EditorGameObject selected)
    {
        GizmoMode mode = EditorContext.Instance.Gizmos.Mode;
        if (mode is GizmoMode.Select or GizmoMode.Rect) return;

        SizeF  viewSize = new(rect.Width, rect.Height);
        PointF o  = Camera.WorldToScreen(new PointF(selected.Position.X, selected.Position.Y), viewSize);
        float  ox = o.X, oy = o.Y;
        float  len = GizmoController.ArrowLength;

        Color axisX  = ResolveColor("AxisRed");
        Color axisY  = ResolveColor("AxisGreen");
        Color accent = ResolveColor("AccentBlue");

        switch (mode)
        {
            case GizmoMode.Move:
            {
                // X axis arrow (red)
                canvas.StrokeColor = axisX;
                canvas.StrokeSize  = 2;
                canvas.DrawLine(ox, oy, ox + len, oy);
                canvas.FillColor = axisX;
                canvas.FillRectangle(ox + len, oy - 6, 12, 12);

                // Y axis arrow (green, screen-Y up)
                canvas.StrokeColor = axisY;
                canvas.DrawLine(ox, oy, ox, oy - len);
                canvas.FillColor = axisY;
                canvas.FillRectangle(ox - 6, oy - len - 12, 12, 12);

                // XY free-move square (yellow)
                Color yellow = Color.FromArgb("#FFEE44");
                canvas.FillColor   = yellow.WithAlpha(0.55f);
                canvas.FillRectangle(ox + 12, oy - 28, 16, 16);
                canvas.StrokeColor = yellow;
                canvas.StrokeSize  = 1;
                canvas.DrawRectangle(ox + 12, oy - 28, 16, 16);

                // Z (depth) diamond handle — only in 2.5D depth mode
                if (EditorContext.Instance.Gizmos.IsDepthMode)
                {
                    float zhx  = ox + GizmoController.ZHandleOffsetX;
                    float zhy  = oy - GizmoController.ArrowLength;
                    float half = GizmoController.ZHandleSize / 2f;

                    // Connecting line from origin to diamond
                    canvas.StrokeColor = accent;
                    canvas.StrokeSize  = 1.5f;
                    canvas.DrawLine(ox, oy, zhx, zhy);

                    // Diamond shape
                    PathF diamond = new();
                    diamond.MoveTo(zhx,        zhy - half);
                    diamond.LineTo(zhx + half, zhy);
                    diamond.LineTo(zhx,        zhy + half);
                    diamond.LineTo(zhx - half, zhy);
                    diamond.Close();
                    canvas.FillColor = accent.WithAlpha(0.7f);
                    canvas.FillPath(diamond);
                    canvas.StrokeColor = accent;
                    canvas.StrokeSize  = 1;
                    canvas.DrawPath(diamond);
                }

                break;
            }

            case GizmoMode.Rotate:
            {
                float r = GizmoController.RotateRadius;
                canvas.StrokeColor = accent;
                canvas.StrokeSize  = 2;
                canvas.DrawCircle(ox, oy, r);

                // Orientation line — points in the direction of the object's current rotation
                float rad  = selected.Rotation * MathF.PI / 180f;
                float endX = ox + r * MathF.Cos(rad);
                float endY = oy + r * MathF.Sin(rad); // screen Y increases downward, matches Atan2 convention
                canvas.StrokeColor = accent;
                canvas.DrawLine(ox, oy, endX, endY);
                canvas.FillColor = Colors.White;
                canvas.FillCircle(endX, endY, 4f);
                canvas.StrokeColor = accent;
                canvas.DrawCircle(endX, endY, 4f);
                break;
            }

            case GizmoMode.Scale:
            {
                float sh  = GizmoController.ScaleHandleSize;
                float hsh = sh / 2f;

                // X arm + end handle (red)
                canvas.StrokeColor = axisX;
                canvas.StrokeSize  = 2;
                canvas.DrawLine(ox, oy, ox + len, oy);
                canvas.FillColor = axisX;
                canvas.FillRectangle(ox + len - hsh, oy - hsh, sh, sh);

                // Y arm + end handle (green)
                canvas.StrokeColor = axisY;
                canvas.DrawLine(ox, oy, ox, oy - len);
                canvas.FillColor = axisY;
                canvas.FillRectangle(ox - hsh, oy - len - hsh, sh, sh);

                // Center handle (white — uniform scale)
                canvas.FillColor   = Colors.White;
                canvas.StrokeColor = Colors.Gray;
                canvas.StrokeSize  = 1;
                canvas.FillRectangle(ox - hsh, oy - hsh, sh, sh);
                canvas.DrawRectangle(ox - hsh, oy - hsh, sh, sh);
                break;
            }
        }
    }

    // ── Axis gizmo, bottom-left ───────────────────────────────────────────────

    private static void DrawGizmo(ICanvas canvas, RectF rect)
    {
        float ox = 28, oy = rect.Height - 28, len = 22;
        canvas.StrokeSize = 2;

        Color axisX = ResolveColor("AxisRed");
        Color axisY = ResolveColor("AxisGreen");

        canvas.StrokeColor = axisX;
        canvas.DrawLine(ox, oy, ox + len, oy);
        canvas.FontColor = axisX;
        canvas.FontSize  = 10;
        canvas.DrawString("x", ox + len + 4, oy, HorizontalAlignment.Left);

        canvas.StrokeColor = axisY;
        canvas.DrawLine(ox, oy, ox, oy - len);
        canvas.FontColor = axisY;
        canvas.DrawString("y", ox, oy - len - 10, HorizontalAlignment.Center);

        canvas.FillColor = axisX;
        canvas.FillCircle(ox, oy, 2.5f);
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
