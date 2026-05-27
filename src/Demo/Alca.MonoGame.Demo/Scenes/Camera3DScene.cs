using Alca.MonoGame.Kernel.Graphics.Camera;
using Alca.MonoGame.Kernel.Graphics.ThreeD;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 21 — Camera3D modes (Fixed, FirstPerson, ThirdPerson, TopDown) with PrimitiveBatch wireframes.</summary>
public sealed class Camera3DScene : Scene
{
    private enum CameraMode { Fixed, FirstPerson, ThirdPerson, TopDown }

    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIOverlayManager _overlayManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private PrimitiveBatch _primBatch = null!;
    private FirstPersonCamera3D _fpCamera = null!;
    private ThirdPersonCamera3D _tpCamera = null!;
    private TopDownCamera3D _tdCamera = null!;
    private FixedCamera3D _fixCamera = null!;
    private Camera3D _activeCamera = null!;
    private CameraMode _mode = CameraMode.Fixed;

    private Point _prevMouse;
    private Vector3 _tpTarget;
    private Vector3 _tdTarget;

    private Label _posLabel = null!;
    private Label _dirLabel = null!;
    private readonly System.Text.StringBuilder _sb = new(64);

    protected override void PostInitialize()
    {
        base.PostInitialize();
        float aspect = (float)Core.GraphicsDevice.Viewport.Width / Core.GraphicsDevice.Viewport.Height;
        _fpCamera = new FirstPersonCamera3D(new Vector3(0f, 1f, 5f), MathHelper.PiOver4, aspect);
        _tpCamera = new ThirdPersonCamera3D(Vector3.Zero, MathHelper.PiOver4, aspect);
        _tpCamera.Offset = new Vector3(0f, 3f, 8f);
        _tdCamera = new TopDownCamera3D(15f, MathHelper.PiOver4, aspect);
        _fixCamera = new FixedCamera3D(new Vector3(6f, 6f, 6f), Vector3.Zero, MathHelper.PiOver4, aspect);
        _activeCamera = _fixCamera;
        _prevMouse = Mouse.GetState().Position;
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        _primBatch = new PrimitiveBatch(Core.GraphicsDevice);
        BuildUI();
    }

    private void BuildUI()
    {
        _uiRoot.OverlayManager = _overlayManager;
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Camera3D Demo", Color = Color.Yellow });
        controls.Add(new Label { Font = _font, Text = "Modo cámara:", Color = Color.LightGray });

        var modeDropdown = new Dropdown(_overlayManager)
        {
            Pixel = _pixel,
            Font = _font,
            ScreenHeight = Core.GraphicsDevice.Viewport.Height
        };
        modeDropdown.AddItem("Fixed");
        modeDropdown.AddItem("First Person");
        modeDropdown.AddItem("Third Person");
        modeDropdown.AddItem("Top Down");
        modeDropdown.SelectedIndex = 0;
        modeDropdown.SelectionChanged += idx =>
        {
            _mode = idx switch
            {
                1 => CameraMode.FirstPerson,
                2 => CameraMode.ThirdPerson,
                3 => CameraMode.TopDown,
                _ => CameraMode.Fixed,
            };
            _activeCamera = _mode switch
            {
                CameraMode.FirstPerson => _fpCamera,
                CameraMode.ThirdPerson => _tpCamera,
                CameraMode.TopDown => _tdCamera,
                _ => _fixCamera,
            };
        };
        controls.Add(modeDropdown);

        _posLabel = new Label { Font = _font, Text = "Pos: —", Color = Color.LightGreen };
        _dirLabel = new Label { Font = _font, Text = "Dir: —", Color = Color.LightGreen };
        controls.Add(_posLabel);
        controls.Add(_dirLabel);
        controls.Add(new Label { Font = _font, Text = "WASD: mover | RMB + mover ratón: rotar (FP)", Color = Color.LightGray });

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MouseState ms = Mouse.GetState();
        KeyboardState ks = Keyboard.GetState();
        int dx = ms.X - _prevMouse.X;
        int dy = ms.Y - _prevMouse.Y;
        _prevMouse = ms.Position;

        const float MoveSpeed = 5f;
        const float LookSens = 0.005f;

        switch (_mode)
        {
            case CameraMode.FirstPerson:
                if (ms.RightButton == ButtonState.Pressed)
                    _fpCamera.Look(dx * LookSens, dy * LookSens);
                if (ks.IsKeyDown(Keys.W)) _fpCamera.MoveForward(MoveSpeed * dt);
                if (ks.IsKeyDown(Keys.S)) _fpCamera.MoveForward(-MoveSpeed * dt);
                if (ks.IsKeyDown(Keys.A)) _fpCamera.Strafe(-MoveSpeed * dt);
                if (ks.IsKeyDown(Keys.D)) _fpCamera.Strafe(MoveSpeed * dt);
                break;

            case CameraMode.ThirdPerson:
                if (ks.IsKeyDown(Keys.W)) _tpTarget.Z -= MoveSpeed * dt;
                if (ks.IsKeyDown(Keys.S)) _tpTarget.Z += MoveSpeed * dt;
                if (ks.IsKeyDown(Keys.A)) _tpTarget.X -= MoveSpeed * dt;
                if (ks.IsKeyDown(Keys.D)) _tpTarget.X += MoveSpeed * dt;
                _tpCamera.Update(_tpTarget, 0f, dt);
                break;

            case CameraMode.TopDown:
                if (ks.IsKeyDown(Keys.W)) _tdTarget.Z -= MoveSpeed * dt;
                if (ks.IsKeyDown(Keys.S)) _tdTarget.Z += MoveSpeed * dt;
                if (ks.IsKeyDown(Keys.A)) _tdTarget.X -= MoveSpeed * dt;
                if (ks.IsKeyDown(Keys.D)) _tdTarget.X += MoveSpeed * dt;
                _tdCamera.Follow(_tdTarget);
                break;
        }

        Vector3 pos = _activeCamera.Position;
        _sb.Clear();
        _sb.Append("Pos: ");
        _sb.Append(pos.X.ToString("F1"));
        _sb.Append(", ");
        _sb.Append(pos.Y.ToString("F1"));
        _sb.Append(", ");
        _sb.Append(pos.Z.ToString("F1"));
        _posLabel.Text = _sb.ToString();

        if (_mode == CameraMode.FirstPerson)
        {
            _sb.Clear();
            _sb.Append("Yaw: ");
            _sb.Append(MathHelper.ToDegrees(_fpCamera.Yaw).ToString("F0"));
            _sb.Append("° Pitch: ");
            _sb.Append(MathHelper.ToDegrees(_fpCamera.Pitch).ToString("F0"));
            _sb.Append("°");
            _dirLabel.Text = _sb.ToString();
        }
        else
        {
            _dirLabel.Text = string.Empty;
        }

        _overlayManager.Update(gameTime);
        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(20, 20, 35));

        _primBatch.Begin(_activeCamera, PrimitiveType.LineList);
        DrawFloorGrid();
        _primBatch.DrawWireBox(new BoundingBox(new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 1f, 0.5f)), Color.White);
        _primBatch.DrawWireSphere(new Vector3(0f, 0.5f, 0f), 0.5f, Color.Cyan);
        _primBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private void DrawFloorGrid()
    {
        Color gridColor = new(60, 60, 80);
        for (int i = -5; i <= 5; i++)
        {
            _primBatch.DrawLine(new Vector3(i, 0f, -5f), new Vector3(i, 0f, 5f), gridColor);
            _primBatch.DrawLine(new Vector3(-5f, 0f, i), new Vector3(5f, 0f, i), gridColor);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _primBatch?.Dispose();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
