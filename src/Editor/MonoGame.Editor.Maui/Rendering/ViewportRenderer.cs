using System.Reflection;

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

    // ── Caché de tipos para DrawBehaviourGizmos (evita scan de ensamblados por fotograma) ──
    private readonly Dictionary<string, Type?> _behaviourTypeCache = new();

    // ── Grid adaptativo ───────────────────────────────────────────────────────
    /// <summary>Separación mínima en píxeles entre líneas de cuadrícula antes de escalar el tamaño de celda.</summary>
    private const float MinGridStepPx = 40f;

    /// <summary>Factor por el que se multiplica el tamaño de celda cuando es demasiado denso. Cambiar para pruebas.</summary>
    public const float GridScaleFactor = 10f;

    public EditorCamera2D Camera { get; } = new();

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
            DrawBehaviourGizmos(canvas, rect, selected);
        }

        DrawGizmo(canvas, rect);
        DrawOrientationGizmo(canvas, rect);
    }

    // ── Grid ortográfico ─────────────────────────────────────────────────────

    private void DrawGrid(ICanvas canvas, RectF rect)
    {
        // Tamaño base de celda (unidades de mundo) procedente de GizmoController / ProjectSettings.
        float cellSize = EditorContext.Instance.Gizmos.GridCellSize;

        // Escalado adaptativo: aumentar el tamaño de celda mientras las líneas estén demasiado juntas.
        float step = cellSize * Camera.Zoom;
        while (step < MinGridStepPx)
        {
            cellSize *= GridScaleFactor;
            step = cellSize * Camera.Zoom;
            if (cellSize > 1e9f) return; // salvaguarda para zoom extremo
        }

        // Última comprobación: si sigue siendo invisible, no dibujar.
        if (step < 6f) return;

        canvas.StrokeColor = ResolveColor("Border").WithAlpha(0.35f);
        canvas.StrokeSize = 1;

        SizeF size = new(rect.Width, rect.Height);
        PointF tl = Camera.ScreenToWorld(new PointF(rect.Left, rect.Top), size);
        PointF br = Camera.ScreenToWorld(new PointF(rect.Right, rect.Bottom), size);

        float startX = MathF.Floor(tl.X / cellSize) * cellSize;
        for (float wx = startX; wx <= br.X + cellSize; wx += cellSize)
        {
            PointF sp = Camera.WorldToScreen(new PointF(wx, 0), size);
            canvas.DrawLine(sp.X, rect.Top, sp.X, rect.Bottom);
        }

        float startY = MathF.Floor(tl.Y / cellSize) * cellSize;
        for (float wy = startY; wy <= br.Y + cellSize; wy += cellSize)
        {
            PointF sp = Camera.WorldToScreen(new PointF(0, wy), size);
            canvas.DrawLine(rect.Left, sp.Y, rect.Right, sp.Y);
        }

        DrawGridScale(canvas, rect, cellSize);
    }

    /// <summary>Dibuja en la esquina inferior izquierda la escala actual de celda del grid.</summary>
    private void DrawGridScale(ICanvas canvas, RectF rect, float effectiveCellSize)
    {
        string label = FormatGridCellSize(effectiveCellSize);

        float px = 28f;
        float py = rect.Height - 14f;

        canvas.FontColor = ResolveColor("Border").WithAlpha(0.75f);
        canvas.FontSize = 10;
        canvas.DrawString(label, px, py, HorizontalAlignment.Left);
    }

    /// <summary>Formatea el tamaño de celda en metros con sufijo legible (m / km).</summary>
    private static string FormatGridCellSize(float meters)
    {
        if (meters >= 1000f)
            return $"Grid: {meters / 1000f:G3} km";
        if (meters >= 1f)
            return $"Grid: {meters:G3} m";
        return $"Grid: {meters * 100f:G3} cm";
    }

    // ── Proyección según orientación ─────────────────────────────────────────

    /// <summary>
    /// Devuelve el punto 2D en espacio de mundo que representa el centro del objeto
    /// para la orientación activa, listo para pasarlo a <see cref="EditorCamera2D.WorldToScreen"/>.
    /// </summary>
    private PointF GetWorldCenter(EditorGameObject obj) => Orientation switch
    {
        ViewOrientation.Top => new PointF(obj.Position.X, obj.Position.Z),
        ViewOrientation.Right => new PointF(obj.Position.Z, obj.Position.Y),
        _ => new PointF(obj.Position.X, obj.Position.Y),
    };

    /// <summary>Devuelve el componente de rotación relevante para la orientación activa (en grados).</summary>
    private float GetVisibleRotation(EditorGameObject obj) => Orientation switch
    {
        ViewOrientation.Top => obj.Rotation.Y,
        ViewOrientation.Right => obj.Rotation.X,
        _ => obj.Rotation.Z,
    };

    /// <summary>
    /// Devuelve los semitamaños de renderizado según la orientación activa.
    /// Con <c>defaultHalfSize = 0.5f</c>, un objeto a escala (1,1,1) ocupa exactamente
    /// 1×1 unidades de mundo (1 celda de grid cuando GridCellSize = 1).
    /// </summary>
    private (float halfW, float halfH) GetVisibleScale(EditorGameObject obj)
    {
        const float defaultHalfSize = 0.5f;
        return Orientation switch
        {
            ViewOrientation.Top => (defaultHalfSize * obj.Scale.X, defaultHalfSize * obj.Scale.Z),
            ViewOrientation.Right => (defaultHalfSize * obj.Scale.Z, defaultHalfSize * obj.Scale.Y),
            _ => (defaultHalfSize * obj.Scale.X, defaultHalfSize * obj.Scale.Y),
        };
    }

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

        (float halfW, float halfH) = GetVisibleScale(obj);

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
        (float halfW, float halfH) = GetVisibleScale(obj);

        SizeF viewSize = new(rect.Width, rect.Height);
        PointF wc = GetWorldCenter(obj);
        PointF tl = Camera.WorldToScreen(new PointF(wc.X - halfW, wc.Y - halfH), viewSize);
        PointF br = Camera.WorldToScreen(new PointF(wc.X + halfW, wc.Y + halfH), viewSize);

        return new SelectionInfo(new RectF(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), GetVisibleRotation(obj));
    }

    private void DrawSelection(ICanvas canvas, SelectionInfo sel)
    {
        RectF b = sel.ScreenBounds;
        Color accent = ResolveColor("AccentBlue");

        // Borde del rectángulo de selección
        canvas.StrokeColor = accent;
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(b);

        // Solo las 4 esquinas, mismo color que el borde (sin relleno blanco ni pivote)
        const float h = 6f, hh = h / 2f;

        PointF[] corners =
        [
            new(b.Left,  b.Top),
            new(b.Right, b.Top),
            new(b.Left,  b.Bottom),
            new(b.Right, b.Bottom),
        ];

        canvas.FillColor = accent;
        canvas.StrokeColor = accent;
        canvas.StrokeSize = 1;
        foreach (PointF p in corners)
            canvas.FillRectangle(p.X - hh, p.Y - hh, h, h);
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
                DrawRotateHandles(canvas, ox, oy, GetVisibleRotation(selected), GizmoController.RotateRadius, accent);
                break;

            case GizmoMode.Scale:
                DrawScaleHandles(canvas, ox, oy, GizmoController.ArrowLength, axes, axisX, axisY);
                break;

            case GizmoMode.Universal:
                DrawUniversalHandles(canvas, ox, oy, GetVisibleRotation(selected), axes,
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
        const float headSize = GizmoController.ArrowHeadSize;   // 14 px

        if (hasX)
        {
            // Línea del eje X
            canvas.StrokeColor = axisX;
            canvas.StrokeSize = 2;
            canvas.DrawLine(ox, oy, ox + len, oy);

            // Punta triangular apuntando a la derecha
            PathF arrowX = new();
            arrowX.MoveTo(ox + len, oy - 7);
            arrowX.LineTo(ox + len + headSize, oy);
            arrowX.LineTo(ox + len, oy + 7);
            arrowX.Close();
            canvas.FillColor = axisX;
            canvas.FillPath(arrowX);

            canvas.FontColor = axisX;
            canvas.FontSize = 9;
            canvas.DrawString(labelX, ox + len + headSize + 4, oy, HorizontalAlignment.Left);
        }

        if (hasY)
        {
            // Línea del eje Y (arriba en pantalla)
            canvas.StrokeColor = axisY;
            canvas.StrokeSize = 2;
            canvas.DrawLine(ox, oy, ox, oy - len);

            // Punta triangular apuntando hacia arriba
            PathF arrowY = new();
            arrowY.MoveTo(ox - 7, oy - len);
            arrowY.LineTo(ox, oy - len - headSize);
            arrowY.LineTo(ox + 7, oy - len);
            arrowY.Close();
            canvas.FillColor = axisY;
            canvas.FillPath(arrowY);

            canvas.FontColor = axisY;
            canvas.FontSize = 9;
            canvas.DrawString(labelY, ox, oy - len - headSize - 6, HorizontalAlignment.Center);
        }

        if (hasX && hasY)
        {
            // Cuadrado de movimiento libre (XY)
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

    // ── Gizmos de Behaviour (radius preview) ─────────────────────────────────

    /// <summary>
    /// Dibuja los gizmos visuales definidos por atributos en los Behaviours del objeto seleccionado.
    /// Actualmente renderiza un círculo por cada propiedad float con <c>[EditorRadiusPreview]</c>.
    /// </summary>
    private void DrawBehaviourGizmos(ICanvas canvas, RectF rect, EditorGameObject obj)
    {
        if (obj.Behaviours.Count == 0) return;

        SizeF size = new(rect.Width, rect.Height);
        PointF center = Camera.WorldToScreen(GetWorldCenter(obj), size);

        canvas.StrokeColor = Colors.White.WithAlpha(0.35f);
        canvas.StrokeSize = 1;

        foreach (EditorBehaviour behaviour in obj.Behaviours)
        {
            if (!_behaviourTypeCache.TryGetValue(behaviour.TypeName, out Type? type))
            {
                type = FindBehaviourType(behaviour.TypeName);
                _behaviourTypeCache[behaviour.TypeName] = type;
            }
            if (type is null) continue;

            foreach (PropertyInfo pi in type.GetProperties(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (pi.GetCustomAttribute<MonoGame.Editor.Core.Attributes.EditorRadiusPreviewAttribute>() is null) continue;
                if (!behaviour.Properties.TryGetValue(pi.Name, out System.Text.Json.JsonElement el)) continue;
                if (el.ValueKind != System.Text.Json.JsonValueKind.Number) continue;

                float worldRadius = el.GetSingle();
                if (worldRadius <= 0f) continue;

                float screenRadius = worldRadius * Camera.Zoom;
                if (screenRadius < 2f) continue;

                canvas.DrawCircle(center.X, center.Y, screenRadius);
            }
        }
    }

    private static Type? FindBehaviourType(string typeName)
    {
        // Intento directo por nombre completo
        foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type? t = asm.GetType(typeName);
                if (t is not null) return t;
            }
            catch { }
        }

        // Intento con el segmento antes de la coma (AssemblyQualifiedName → FullName)
        string lookup = typeName.Contains(',') ? typeName[..typeName.IndexOf(',')].Trim() : typeName;
        foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (Type t in asm.GetTypes())
                    if (t.FullName == lookup) return t;
            }
            catch { }
        }
        return null;
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
