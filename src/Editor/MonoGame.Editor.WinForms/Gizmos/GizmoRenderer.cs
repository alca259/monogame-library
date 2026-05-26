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
    private static readonly XnaColor _gridColor       = new(70,  70,  70,  180);
    private static readonly XnaColor _originAxisColor = new(140, 140, 140, 220);
    private static readonly XnaColor _boundsColor     = new(255, 255, 255, 110);
    private static readonly XnaColor _axisXColor      = new(220, 60, 60);
    private static readonly XnaColor _axisYColor      = new(60, 200, 60);
    private static readonly XnaColor _axisXYColor     = new(230, 200, 40);
    private static readonly XnaColor _rotateColor     = new(255, 210, 80);

    // ── Drawing constants ────────────────────────────────────────────────────
    private const float LineThickness = 2.5f;
    private const int   MaxGridLines  = 100;

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
    public void Draw(EditorGameObject? selected, Matrix cameraTransform, int viewW, int viewH)
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

        // ── Pass 2: screen-space bounding box + handles ──────────────────────
        if (selected == null) return;

        XnaVector2 objScreen = XnaVector2.Transform(
            new XnaVector2(selected.Position.X, selected.Position.Y), cameraTransform);
        float zoom = cameraTransform.M11;

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend);
        try
        {
            DrawBoundingBox(objScreen, selected.Scale.X, selected.Scale.Y, zoom);

            if (_ctrl.Mode != GizmoMode.Select)
                DrawGizmoHandles(_ctrl.Mode, objScreen, selected.Rotation);
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

    // ── Gizmo handles ────────────────────────────────────────────────────────

    private void DrawGizmoHandles(GizmoMode mode, XnaVector2 origin, float rotationDegrees)
    {
        switch (mode)
        {
            case GizmoMode.Move:   DrawMoveGizmo(origin);   break;
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

    private void FillRect(XnaRect rect, XnaColor color)
    {
        if (_pixel == null || _spriteBatch == null) return;
        _spriteBatch.Draw(_pixel, rect, color);
    }

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
