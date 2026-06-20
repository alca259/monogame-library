using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Rendering;

/// <summary>
/// Control WinForms del viewport del editor. Renderiza la escena activa con GDI+ (grid adaptativo,
/// objetos, selection box y gizmos de transformación) a través de <see cref="GizmoRenderer"/>.
/// El bucle de render es un <see cref="Timer"/> de ~60 fps con doble buffer nativo.
/// </summary>
/// <remarks>
/// En una iteración futura (post-Fase 3), el render GDI+ será reemplazado por un
/// <c>RenderTarget2D</c> de MonoGame off-screen con blit via Bitmap/OnPaint.
/// </remarks>
internal sealed class MonoGameViewportHost : Control
{
    // ── Propiedades de cámara / orientación ───────────────────────────────────

    /// <summary>Cámara 2D: pan y zoom. Accesible desde MainForm para FocusOnSelected.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EditorCamera2D Camera { get; } = new();

    /// <summary>Orientación ortográfica activa del viewport.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ViewOrientation Orientation { get; private set; } = ViewOrientation.Front;

    // ── Renderer y timer ──────────────────────────────────────────────────────

    private readonly GizmoRenderer _renderer = new();
    private readonly System.Windows.Forms.Timer _renderTimer = new() { Interval = 16 }; // ~60 fps

    // ── Estado de input (mouse) ───────────────────────────────────────────────
    private bool  _gizmoDragging;
    private bool  _isPanning;
    private float _panStartScreenX, _panStartScreenY;
    private float _panLastX, _panLastY;
    private float _lastMouseX, _lastMouseY;

    public MonoGameViewportHost()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
        UpdateStyles();

        BackColor = EditorColors.ViewportBackground;
        Dock      = DockStyle.Fill;

        _renderTimer.Tick += (_, _) => Invalidate();
    }

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _renderTimer.Start();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _renderTimer.Stop();
        base.OnHandleDestroyed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderTimer.Dispose();
            _renderer.Dispose();
        }
        base.Dispose(disposing);
    }

    // ── Render ────────────────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        _renderer.DrawAll(e.Graphics, Camera, Orientation, new SizeF(Width, Height));
    }

    // ── Mouse — zoom ──────────────────────────────────────────────────────────

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        float factor = e.Delta > 0 ? 1.12f : 1f / 1.12f;
        Camera.ZoomAt(factor, new PointF(e.X, e.Y), new SizeF(Width, Height));
        Invalidate();
    }

    // ── Mouse — pan y gizmo drag ──────────────────────────────────────────────

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        EditorContext.Instance.SetFocus(EditorFocusContext.Viewport);

        float mx = e.X, my = e.Y;
        _lastMouseX = mx;
        _lastMouseY = my;

        if (e.Button == MouseButtons.Middle ||
            (e.Button == MouseButtons.Right && Control.ModifierKeys == Keys.None))
        {
            _isPanning = true;
            _panStartScreenX = mx;
            _panStartScreenY = my;
            _panLastX = mx;
            _panLastY = my;
            Cursor = Cursors.SizeAll;
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            // Intentar iniciar drag de gizmo
            EditorGameObject? sel = EditorContext.Instance.SelectedObject;
            GizmoMode mode = EditorContext.Instance.Gizmos.Mode;

            if (sel is not null && mode is not GizmoMode.Select and not GizmoMode.Rect)
            {
                SizeF vs = new(Width, Height);
                PointF clickWorld = Camera.ScreenToWorld(new PointF(mx, my), vs);
                PointF objScreen  = _renderer.GetObjectScreenCenter(sel, Camera, Orientation, vs);
                _gizmoDragging = EditorContext.Instance.Gizmos.BeginDrag(
                    mx, my, objScreen.X, objScreen.Y, clickWorld.X, clickWorld.Y, sel);
            }

            _panStartScreenX = mx;
            _panStartScreenY = my;
            _panLastX = mx;
            _panLastY = my;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        float mx = e.X, my = e.Y;
        _lastMouseX = mx;
        _lastMouseY = my;

        if (_isPanning)
        {
            float dx = mx - _panLastX;
            float dy = my - _panLastY;
            _panLastX = mx;
            _panLastY = my;
            float zoom = Camera.Zoom;
            Camera.Pan(new PointF(-dx / zoom, dy / zoom));
            Invalidate();
            return;
        }

        if (_gizmoDragging && e.Button == MouseButtons.Left)
        {
            EditorGameObject? sel = EditorContext.Instance.SelectedObject;
            if (sel is not null)
            {
                SizeF vs = new(Width, Height);
                PointF world    = Camera.ScreenToWorld(new PointF(mx, my), vs);
                PointF objScreen = _renderer.GetObjectScreenCenter(sel, Camera, Orientation, vs);
                EditorContext.Instance.Gizmos.UpdateDrag(
                    world.X, world.Y, mx, my, objScreen.X, objScreen.Y, sel);
                Invalidate();
            }
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        float mx = e.X, my = e.Y;

        if (_isPanning)
        {
            _isPanning = false;
            Cursor = Cursors.Default;
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (_gizmoDragging)
            {
                IEditorCommand? cmd = EditorContext.Instance.Gizmos.EndDrag(
                    EditorContext.Instance.SelectedObject, ctrlHeld: Control.ModifierKeys == Keys.Control);
                if (cmd is not null)
                {
                    EditorContext.Instance.Commands.Execute(cmd);
                    if (EditorContext.Instance.SelectedObject is EditorGameObject dragSel)
                        EditorContext.Instance.EventBus.Publish(new GameObjectPropertyChangedEvent(dragSel));
                }
                _gizmoDragging = false;
                Invalidate();
                return;
            }

            // Clic de selección (sin arrastre apreciable)
            float dist = MathF.Sqrt((mx - _panStartScreenX) * (mx - _panStartScreenX) +
                                    (my - _panStartScreenY) * (my - _panStartScreenY));
            if (dist < 5f)
                HandleClick(mx, my);
        }
    }

    // ── Clic de selección ─────────────────────────────────────────────────────

    private void HandleClick(float mx, float my)
    {
        SizeF vs  = new(Width, Height);
        PointF pt = new(mx, my);

        // Orientation gizmo hit-test (prioridad)
        ViewOrientation? newOri = _renderer.OrientationGizmoHitTest(pt, vs);
        if (newOri.HasValue)
        {
            Orientation = newOri.Value;
            EditorContext.Instance.Gizmos.Orientation = newOri.Value;
            Invalidate();
            return;
        }

        // Solo seleccionar en modo Select
        if (EditorContext.Instance.Gizmos.Mode != GizmoMode.Select) return;

        EditorScene? scene = EditorContext.Instance.ActiveScene;
        if (scene is null) return;

        PointF worldPos = Camera.ScreenToWorld(pt, vs);
        EditorGameObject? hit = HitTest(scene.RootGameObjects, worldPos);
        EditorContext.Instance.SetSelection(hit);
    }

    private EditorGameObject? HitTest(List<EditorGameObject> objects, PointF worldPos)
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            EditorGameObject obj = objects[i];
            if (!obj.Active) continue;

            if (obj.Children.Count > 0)
            {
                EditorGameObject? child = HitTest(obj.Children, worldPos);
                if (child is not null) return child;
            }

            const float d = 0.5f;
            (float halfW, float halfH) = Orientation switch
            {
                ViewOrientation.Top   => (d * obj.Scale.X, d * obj.Scale.Z),
                ViewOrientation.Right => (d * obj.Scale.Z, d * obj.Scale.Y),
                _                     => (d * obj.Scale.X, d * obj.Scale.Y),
            };

            PointF center = Orientation switch
            {
                ViewOrientation.Top   => new PointF(obj.Position.X, obj.Position.Z),
                ViewOrientation.Right => new PointF(obj.Position.Z, obj.Position.Y),
                _                     => new PointF(obj.Position.X, obj.Position.Y),
            };

            if (worldPos.X >= center.X - halfW && worldPos.X <= center.X + halfW &&
                worldPos.Y >= center.Y - halfH && worldPos.Y <= center.Y + halfH)
                return obj;
        }
        return null;
    }

    // ── Foco ─────────────────────────────────────────────────────────────────

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        EditorContext.Instance.SetFocus(EditorFocusContext.Viewport);
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        EditorContext.Instance.SetFocus(EditorFocusContext.Global);
    }

    // ── Helpers públicos ──────────────────────────────────────────────────────

    /// <summary>Centra la cámara en el objeto seleccionado actualmente.</summary>
    public void FocusOnSelected()
    {
        EditorGameObject? sel = EditorContext.Instance.SelectedObject;
        if (sel is null) return;
        Camera.Position = Orientation switch
        {
            ViewOrientation.Top   => new PointF(sel.Position.X, sel.Position.Z),
            ViewOrientation.Right => new PointF(sel.Position.Z, sel.Position.Y),
            _                     => new PointF(sel.Position.X, sel.Position.Y),
        };
        Invalidate();
    }

    // Tab-stop para recibir foco con teclado
    protected override bool IsInputKey(Keys keyData) => true;
}
