using XnaColor     = Microsoft.Xna.Framework.Color;
using XnaRect      = Microsoft.Xna.Framework.Rectangle;
using XnaVector2   = Microsoft.Xna.Framework.Vector2;
namespace MonoGame.Editor.WinForms.Gizmos;

/// <summary>
/// Renders the grid overlay, bounding-box selection, and gizmo handles into the editor viewport.
/// Must be initialised from the render thread via <see cref="Initialize"/> before calling <see cref="Draw"/>.
/// </summary>
public sealed class GizmoRenderer : IDisposable
{
    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly XnaColor _gridColor        = new(70,  70,  70,  180);
    private static readonly XnaColor _originAxisColor  = new(140, 140, 140, 220);
    private static readonly XnaColor _boundsColor      = new(255, 255, 255, 110);
    private static readonly XnaColor _dimBoundsColor   = new(180, 180, 180, 40);
    private static readonly XnaColor _axisXColor       = new(220, 60,  60);
    private static readonly XnaColor _axisYColor       = new(60,  200, 60);
    private static readonly XnaColor _axisXYColor      = new(230, 200, 40);
    private static readonly XnaColor _axisZColor       = new(60,  120, 240);
    private static readonly XnaColor _rotateColor      = new(255, 210, 80);
    private static readonly XnaColor _rectBoundsColor  = new(60,  200, 255, 180);
    private static readonly XnaColor _colliderColor    = new(0,   220, 220, 200);
    private static readonly XnaColor _lightRangeColor  = new(255, 210, 50,  140);

    // ── Drawing constants ────────────────────────────────────────────────────
    private const float  LineThickness          = 2.5f;
    private const int    MaxGridLines           = 100;
    private const string SpriteRendererSuffix   = "SpriteRendererBehaviour";
    private const string BoxColliderSuffix      = "BoxCollider2D";
    private const string CircleColliderSuffix   = "CircleCollider2D";
    private const string PolygonColliderSuffix  = "PolygonCollider2D";
    private const string PointLightSuffix       = "PointLight2D";
    private const string SpotLightSuffix        = "SpotLight2D";
    private const string DirectionalLightSuffix = "DirectionalLight2D";

    // ── Dependencies ─────────────────────────────────────────────────────────
    private readonly GizmoController _ctrl;
    private Texture2D?    _pixel;
    private SpriteBatch?  _spriteBatch;

    /// <summary>Returns <c>true</c> once <see cref="Initialize"/> has been called.</summary>
    public bool IsInitialized => _pixel != null;

    public GizmoRenderer(GizmoController controller) => _ctrl = controller;

    /// <summary>Creates GPU resources. Must be called from the render thread.</summary>
    public void Initialize(GraphicsDevice gd)
    {
        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { XnaColor.White });
        _spriteBatch = new SpriteBatch(gd);
    }

    /// <summary>
    /// Draws grid, bounding box, and gizmo handles for the current frame.
    /// Must be called from the render thread outside any active SpriteBatch pass.
    /// </summary>
    public void Draw(EditorGameObject? selected, Matrix cameraTransform, int viewW, int viewH,
                     bool isDepthMode = false, IReadOnlyList<EditorGameObject>? allRoots = null)
    {
        if (_pixel == null || _spriteBatch == null) return;

        // ── Pass 1: world-space grid ─────────────────────────────────────────
        if (_ctrl.ShowGrid)
        {
            _spriteBatch.Begin(
                transformMatrix: cameraTransform,
                samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend);
            try
            {
                DrawGrid(cameraTransform, viewW, viewH);
            }
            catch { /* ignore grid draw errors */ }
            finally
            {
                _spriteBatch.End();
            }
        }

        // ── Pass 2: screen-space bounding boxes + handles ────────────────────
        bool hasDimWork = allRoots is { Count: > 0 };
        if (selected == null && !hasDimWork) return;

        float zoom = cameraTransform.M11;

        // ── Pass 1b: world-space collider and light gizmos ───────────────────
        if (selected != null)
        {
            XnaVector2 worldOrigin = new(selected.Position.X, selected.Position.Y);
            float lineW = 1.5f / Math.Max(0.001f, zoom);

            _spriteBatch.Begin(
                transformMatrix: cameraTransform,
                samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend);
            try
            {
                DrawColliderGizmos(selected.Behaviours, worldOrigin, lineW);
                DrawLightGizmos(selected.Behaviours, worldOrigin, lineW, zoom);
            }
            catch { /* ignore gizmo errors */ }
            finally
            {
                _spriteBatch.End();
            }
        }

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend);
        try
        {
            // Dim outlines for every unselected object that has no visual representation.
            if (hasDimWork)
                DrawDimObjectBoxes(allRoots!, selected, cameraTransform, zoom);

            if (selected != null)
            {
                XnaVector2 objScreen = XnaVector2.Transform(
                    new XnaVector2(selected.Position.X, selected.Position.Y), cameraTransform);

                if (_ctrl.Mode == GizmoMode.Rect)
                {
                    DrawDashedBoundingBox(objScreen, selected.Scale.X, selected.Scale.Y, zoom);
                }
                else
                {
                    DrawBoundingBox(objScreen, selected.Scale.X, selected.Scale.Y, zoom);

                    if (_ctrl.Mode != GizmoMode.Select)
                        DrawGizmoHandles(_ctrl.Mode, objScreen, selected.Rotation, isDepthMode);
                }
            }
        }
        catch { /* ignore handle draw errors */ }
        finally
        {
            _spriteBatch.End();
        }
    }

    // ── Grid ─────────────────────────────────────────────────────────────────

    private void DrawGrid(Matrix cameraTransform, int viewW, int viewH)
    {
        Matrix inv  = Matrix.Invert(cameraTransform);
        float  zoom = cameraTransform.M11;
        float  lineW = 1f / zoom;   // 1 screen pixel expressed in world units

        XnaVector2 topLeft     = XnaVector2.Transform(XnaVector2.Zero,                   inv);
        XnaVector2 bottomRight = XnaVector2.Transform(new XnaVector2(viewW, viewH),      inv);

        float minX = Math.Min(topLeft.X, bottomRight.X);
        float maxX = Math.Max(topLeft.X, bottomRight.X);
        float minY = Math.Min(topLeft.Y, bottomRight.Y);
        float maxY = Math.Max(topLeft.Y, bottomRight.Y);

        float cell = _ctrl.GridCellSize;
        while (cell > 0 && (maxX - minX) / cell > MaxGridLines) cell *= 2;
        while (cell > 0 && (maxY - minY) / cell > MaxGridLines) cell *= 2;
        if (cell <= 0) return;

        float startX = MathF.Floor(minX / cell) * cell;
        float startY = MathF.Floor(minY / cell) * cell;

        for (float x = startX; x <= maxX; x += cell)
            DrawLine(new XnaVector2(x, minY), new XnaVector2(x, maxY), _gridColor, lineW);

        for (float y = startY; y <= maxY; y += cell)
            DrawLine(new XnaVector2(minX, y), new XnaVector2(maxX, y), _gridColor, lineW);

        // Origin axes (slightly brighter)
        DrawLine(new XnaVector2(0, minY), new XnaVector2(0, maxY), _originAxisColor, lineW * 2);
        DrawLine(new XnaVector2(minX, 0), new XnaVector2(maxX, 0), _originAxisColor, lineW * 2);
    }

    // ── Bounding box ─────────────────────────────────────────────────────────

    private void DrawBoundingBox(XnaVector2 centre, float scaleX, float scaleY, float zoom)
    {
        float halfW = scaleX * GizmoController.DefaultBoundsHalfSize * zoom;
        float halfH = scaleY * GizmoController.DefaultBoundsHalfSize * zoom;

        XnaVector2 tl = centre + new XnaVector2(-halfW, -halfH);
        XnaVector2 tr = centre + new XnaVector2( halfW, -halfH);
        XnaVector2 br = centre + new XnaVector2( halfW,  halfH);
        XnaVector2 bl = centre + new XnaVector2(-halfW,  halfH);

        DrawLine(tl, tr, _boundsColor);
        DrawLine(tr, br, _boundsColor);
        DrawLine(br, bl, _boundsColor);
        DrawLine(bl, tl, _boundsColor);
    }

    private void DrawDimBoundingBox(XnaVector2 centre, float scaleX, float scaleY, float zoom)
    {
        float halfW = scaleX * GizmoController.DefaultBoundsHalfSize * zoom;
        float halfH = scaleY * GizmoController.DefaultBoundsHalfSize * zoom;

        XnaVector2 tl = centre + new XnaVector2(-halfW, -halfH);
        XnaVector2 tr = centre + new XnaVector2( halfW, -halfH);
        XnaVector2 br = centre + new XnaVector2( halfW,  halfH);
        XnaVector2 bl = centre + new XnaVector2(-halfW,  halfH);

        DrawLine(tl, tr, _dimBoundsColor);
        DrawLine(tr, br, _dimBoundsColor);
        DrawLine(br, bl, _dimBoundsColor);
        DrawLine(bl, tl, _dimBoundsColor);
    }

    private void DrawDimObjectBoxes(IReadOnlyList<EditorGameObject> objects,
        EditorGameObject? selected, Matrix cameraTransform, float zoom)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            EditorGameObject obj = objects[i];
            if (obj.Active && !ReferenceEquals(obj, selected) && !HasVisualRepresentation(obj))
            {
                XnaVector2 s = XnaVector2.Transform(
                    new XnaVector2(obj.Position.X, obj.Position.Y), cameraTransform);
                DrawDimBoundingBox(s, obj.Scale.X, obj.Scale.Y, zoom);
            }
            if (obj.Children.Count > 0)
                DrawDimObjectBoxes(obj.Children, selected, cameraTransform, zoom);
        }
    }

    private static bool HasVisualRepresentation(EditorGameObject obj)
    {
        List<EditorBehaviour> behaviours = obj.Behaviours;
        for (int i = 0; i < behaviours.Count; i++)
        {
            if (behaviours[i].TypeName.EndsWith(SpriteRendererSuffix, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private void DrawDashedBoundingBox(XnaVector2 centre, float scaleX, float scaleY, float zoom)
    {
        float halfW = scaleX * GizmoController.DefaultBoundsHalfSize * zoom;
        float halfH = scaleY * GizmoController.DefaultBoundsHalfSize * zoom;

        XnaVector2 tl = centre + new XnaVector2(-halfW, -halfH);
        XnaVector2 tr = centre + new XnaVector2( halfW, -halfH);
        XnaVector2 br = centre + new XnaVector2( halfW,  halfH);
        XnaVector2 bl = centre + new XnaVector2(-halfW,  halfH);

        DrawDashedLine(tl, tr, _rectBoundsColor);
        DrawDashedLine(tr, br, _rectBoundsColor);
        DrawDashedLine(br, bl, _rectBoundsColor);
        DrawDashedLine(bl, tl, _rectBoundsColor);
    }

    // ── Gizmo handles ────────────────────────────────────────────────────────

    private void DrawGizmoHandles(GizmoMode mode, XnaVector2 origin, float rotationDegrees, bool isDepthMode)
    {
        switch (mode)
        {
            case GizmoMode.Move:
                DrawMoveGizmo(origin);
                if (isDepthMode) DrawZHandle(origin);
                break;
            case GizmoMode.Rotate: DrawRotateGizmo(origin, rotationDegrees); break;
            case GizmoMode.Scale:  DrawScaleGizmo(origin);  break;
        }
    }

    private void DrawMoveGizmo(XnaVector2 origin)
    {
        float ahHalf = GizmoController.ArrowHeadSize / 2f;
        int   ahInt  = (int)GizmoController.ArrowHeadSize;
        XnaVector2 xEnd = origin + new XnaVector2(GizmoController.ArrowLength, 0);
        XnaVector2 yEnd = origin + new XnaVector2(0, -GizmoController.ArrowLength);

        // X axis (right, red)
        DrawLine(origin, xEnd, _axisXColor, LineThickness);
        FillRect(new XnaRect((int)(xEnd.X - 2), (int)(xEnd.Y - ahHalf), ahInt, ahInt), _axisXColor);

        // Y axis (up, green — screen-Y inverted)
        DrawLine(origin, yEnd, _axisYColor, LineThickness);
        FillRect(new XnaRect((int)(yEnd.X - ahHalf), (int)(yEnd.Y - 2), ahInt, ahInt), _axisYColor);

        // XY-free square (yellow)
        FillRect(new XnaRect((int)(origin.X + 12), (int)(origin.Y - 28), 16, 16), _axisXYColor);
    }

    private void DrawZHandle(XnaVector2 origin)
    {
        float offsetX = GizmoController.ZHandleOffsetX;
        float len     = GizmoController.ArrowLength;
        float hs      = GizmoController.ZHandleSize / 2f;

        XnaVector2 basePoint = new(origin.X + offsetX, origin.Y);
        XnaVector2 tipPoint  = new(origin.X + offsetX, origin.Y - len);

        // Vertical stem (blue Z)
        DrawLine(basePoint, tipPoint, _axisZColor, LineThickness);

        // Diamond at tip — 4 segments
        XnaVector2 dTop   = new(tipPoint.X,       tipPoint.Y - hs);
        XnaVector2 dRight = new(tipPoint.X + hs,  tipPoint.Y);
        XnaVector2 dBot   = new(tipPoint.X,        tipPoint.Y + hs);
        XnaVector2 dLeft  = new(tipPoint.X - hs,  tipPoint.Y);

        DrawLine(dTop,   dRight, _axisZColor, LineThickness);
        DrawLine(dRight, dBot,   _axisZColor, LineThickness);
        DrawLine(dBot,   dLeft,  _axisZColor, LineThickness);
        DrawLine(dLeft,  dTop,   _axisZColor, LineThickness);
    }

    private void DrawRotateGizmo(XnaVector2 origin, float rotationDegrees)
    {
        const int segments   = 48;
        float     angleStep  = MathHelper.TwoPi / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;
            XnaVector2 p1 = origin + new XnaVector2(MathF.Cos(a1), MathF.Sin(a1)) * GizmoController.RotateRadius;
            XnaVector2 p2 = origin + new XnaVector2(MathF.Cos(a2), MathF.Sin(a2)) * GizmoController.RotateRadius;
            DrawLine(p1, p2, _rotateColor, LineThickness);
        }

        float rotationRad = MathHelper.ToRadians(rotationDegrees);
        XnaVector2 dir = new(MathF.Cos(rotationRad), MathF.Sin(rotationRad));
        XnaVector2 handlePos = origin + dir * GizmoController.RotateRadius;

        DrawLine(origin, handlePos, _rotateColor, 1.5f);
        FillRect(new XnaRect((int)(handlePos.X - 5), (int)(handlePos.Y - 5), 10, 10), _rotateColor);
    }

    private void DrawScaleGizmo(XnaVector2 origin)
    {
        float h    = GizmoController.ScaleHandleSize / 2f;
        int   hInt = (int)GizmoController.ScaleHandleSize;

        XnaVector2 xEnd = origin + new XnaVector2(GizmoController.ArrowLength, 0);
        XnaVector2 yEnd = origin + new XnaVector2(0, -GizmoController.ArrowLength);

        DrawLine(origin, xEnd, _axisXColor, LineThickness);
        DrawLine(origin, yEnd, _axisYColor, LineThickness);

        FillRect(new XnaRect((int)(xEnd.X - h), (int)(xEnd.Y - h), hInt, hInt), _axisXColor);
        FillRect(new XnaRect((int)(yEnd.X - h), (int)(yEnd.Y - h), hInt, hInt), _axisYColor);
        // Centre handle (uniform scale)
        FillRect(new XnaRect((int)(origin.X - h), (int)(origin.Y - h), hInt, hInt), XnaColor.White);
    }

    // ── Primitive helpers ────────────────────────────────────────────────────

    private void DrawLine(XnaVector2 from, XnaVector2 to, XnaColor color, float thickness = 1.5f)
    {
        if (_pixel == null || _spriteBatch == null) return;

        XnaVector2 delta  = to - from;
        float      length = delta.Length();
        if (length < 0.001f) return;

        float angle = MathF.Atan2(delta.Y, delta.X);
        _spriteBatch.Draw(
            _pixel, from, null, color, angle,
            XnaVector2.Zero, new XnaVector2(length, thickness),
            SpriteEffects.None, 0f);
    }

    private void DrawDashedLine(XnaVector2 from, XnaVector2 to, XnaColor color,
        float dashLen = 6f, float gapLen = 4f, float thickness = 1.5f)
    {
        if (_pixel == null || _spriteBatch == null) return;

        XnaVector2 delta = to - from;
        float      total = delta.Length();
        if (total < 0.001f) return;

        XnaVector2 dir    = delta / total;
        float      pos    = 0f;
        bool       doDraw = true;

        while (pos < total)
        {
            float segLen = doDraw ? dashLen : gapLen;
            float segEnd = Math.Min(pos + segLen, total);
            if (doDraw)
                DrawLine(from + dir * pos, from + dir * segEnd, color, thickness);
            pos    = segEnd;
            doDraw = !doDraw;
        }
    }

    private void FillRect(XnaRect rect, XnaColor color)
    {
        if (_pixel == null || _spriteBatch == null) return;
        _spriteBatch.Draw(_pixel, rect, color);
    }

    // ── Collider gizmos ──────────────────────────────────────────────────────

    private void DrawColliderGizmos(List<EditorBehaviour> behaviours, XnaVector2 worldOrigin, float lineW)
    {
        for (int i = 0; i < behaviours.Count; i++)
        {
            EditorBehaviour b = behaviours[i];

            if (b.TypeName.EndsWith(BoxColliderSuffix, StringComparison.Ordinal))
            {
                XnaVector2 size   = ReadVec2(b.Properties, "Size",   new XnaVector2(32, 32));
                XnaVector2 offset = ReadVec2(b.Properties, "Offset", XnaVector2.Zero);
                XnaVector2 center = worldOrigin + offset;
                float hw = size.X * 0.5f, hh = size.Y * 0.5f;

                XnaVector2 tl = center + new XnaVector2(-hw, -hh);
                XnaVector2 tr = center + new XnaVector2( hw, -hh);
                XnaVector2 br = center + new XnaVector2( hw,  hh);
                XnaVector2 bl = center + new XnaVector2(-hw,  hh);

                DrawLine(tl, tr, _colliderColor, lineW);
                DrawLine(tr, br, _colliderColor, lineW);
                DrawLine(br, bl, _colliderColor, lineW);
                DrawLine(bl, tl, _colliderColor, lineW);
            }
            else if (b.TypeName.EndsWith(CircleColliderSuffix, StringComparison.Ordinal))
            {
                float radius = ReadFloat(b.Properties, "Radius", 16f);
                XnaVector2 offset = ReadVec2(b.Properties, "Offset", XnaVector2.Zero);
                DrawCircleWorld(worldOrigin + offset, radius, _colliderColor, lineW);
            }
            else if (b.TypeName.EndsWith(PolygonColliderSuffix, StringComparison.Ordinal))
            {
                if (!b.Properties.TryGetValue("Vertices", out JsonElement verts) || verts.ValueKind != JsonValueKind.Array)
                    continue;

                List<XnaVector2> pts = [];
                foreach (JsonElement v in verts.EnumerateArray())
                    pts.Add(worldOrigin + ReadVec2FromElement(v));

                for (int j = 0; j < pts.Count; j++)
                    DrawLine(pts[j], pts[(j + 1) % pts.Count], _colliderColor, lineW);
            }
        }
    }

    // ── Light gizmos ─────────────────────────────────────────────────────────

    private void DrawLightGizmos(List<EditorBehaviour> behaviours, XnaVector2 worldOrigin, float lineW, float zoom)
    {
        for (int i = 0; i < behaviours.Count; i++)
        {
            EditorBehaviour b = behaviours[i];

            if (b.TypeName.EndsWith(PointLightSuffix, StringComparison.Ordinal))
            {
                float range = ReadFloat(b.Properties, "Range", 100f);
                DrawCircleWorld(worldOrigin, range, _lightRangeColor, lineW, 32);
            }
            else if (b.TypeName.EndsWith(SpotLightSuffix, StringComparison.Ordinal))
            {
                float range     = ReadFloat(b.Properties, "Range", 100f);
                float angleDeg  = ReadFloat(b.Properties, "Angle", 45f);
                XnaVector2 dir  = ReadVec2(b.Properties, "Direction", new XnaVector2(1, 0));
                float baseAngle = MathF.Atan2(dir.Y, dir.X);
                float halfRad   = MathHelper.ToRadians(angleDeg * 0.5f);

                XnaVector2 end1 = worldOrigin + new XnaVector2(MathF.Cos(baseAngle - halfRad), MathF.Sin(baseAngle - halfRad)) * range;
                XnaVector2 end2 = worldOrigin + new XnaVector2(MathF.Cos(baseAngle + halfRad), MathF.Sin(baseAngle + halfRad)) * range;

                DrawLine(worldOrigin, end1, _lightRangeColor, lineW);
                DrawLine(worldOrigin, end2, _lightRangeColor, lineW);
                DrawArcWorld(worldOrigin, range, baseAngle - halfRad, baseAngle + halfRad, _lightRangeColor, lineW);
            }
            else if (b.TypeName.EndsWith(DirectionalLightSuffix, StringComparison.Ordinal))
            {
                XnaVector2 dir = ReadVec2(b.Properties, "Direction", new XnaVector2(1, 0));
                if (dir.LengthSquared() < 0.0001f) continue;

                dir = XnaVector2.Normalize(dir);
                float arrowLen = 80f / Math.Max(0.001f, zoom);
                XnaVector2 end = worldOrigin + dir * arrowLen;
                DrawLine(worldOrigin, end, _lightRangeColor, lineW);

                float ang  = MathF.Atan2(dir.Y, dir.X);
                float ah   = 12f / Math.Max(0.001f, zoom);
                var left   = end + new XnaVector2(MathF.Cos(ang + 2.4f), MathF.Sin(ang + 2.4f)) * ah;
                var right  = end + new XnaVector2(MathF.Cos(ang - 2.4f), MathF.Sin(ang - 2.4f)) * ah;
                DrawLine(end, left,  _lightRangeColor, lineW);
                DrawLine(end, right, _lightRangeColor, lineW);
            }
        }
    }

    private void DrawCircleWorld(XnaVector2 center, float radius, XnaColor color, float lineW, int segments = 24)
    {
        float step = MathHelper.TwoPi / segments;
        for (int i = 0; i < segments; i++)
        {
            float a1 = step * i;
            float a2 = step * (i + 1);
            XnaVector2 p1 = center + new XnaVector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
            XnaVector2 p2 = center + new XnaVector2(MathF.Cos(a2), MathF.Sin(a2)) * radius;
            DrawLine(p1, p2, color, lineW);
        }
    }

    private void DrawArcWorld(XnaVector2 center, float radius, float startAngle, float endAngle, XnaColor color, float lineW, int segments = 16)
    {
        float range = endAngle - startAngle;
        float step  = range / segments;
        for (int i = 0; i < segments; i++)
        {
            float a1 = startAngle + step * i;
            float a2 = startAngle + step * (i + 1);
            XnaVector2 p1 = center + new XnaVector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
            XnaVector2 p2 = center + new XnaVector2(MathF.Cos(a2), MathF.Sin(a2)) * radius;
            DrawLine(p1, p2, color, lineW);
        }
    }

    // ── JSON property helpers ─────────────────────────────────────────────────

    private static XnaVector2 ReadVec2(Dictionary<string, JsonElement> props, string key, XnaVector2 def)
    {
        if (!props.TryGetValue(key, out JsonElement el) || el.ValueKind != JsonValueKind.Object) return def;
        return ReadVec2FromElement(el);
    }

    private static XnaVector2 ReadVec2FromElement(JsonElement el)
    {
        float x = el.TryGetProperty("X", out JsonElement xe) ? xe.GetSingle() : 0f;
        float y = el.TryGetProperty("Y", out JsonElement ye) ? ye.GetSingle() : 0f;
        return new XnaVector2(x, y);
    }

    private static float ReadFloat(Dictionary<string, JsonElement> props, string key, float def)
        => props.TryGetValue(key, out JsonElement el) ? el.GetSingle() : def;

    // ── IDisposable ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Dispose()
    {
        _spriteBatch?.Dispose();
        _pixel?.Dispose();
        _spriteBatch = null;
        _pixel       = null;
    }
}
