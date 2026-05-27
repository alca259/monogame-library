using Alca.MonoGame.Kernel.Graphics.Camera;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Demo scene showcasing Camera2D with Shake, Zoom, and Follow effects.</summary>
public sealed class Camera2DScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly Camera2D _camera = new();
    private readonly CameraEffects _cameraEffects = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;
    private Texture2D _orbitSprite = null!;
    private Texture2D _centerSprite = null!;

    private Label _infoLabel = null!;
    private Label _followStateLabel = null!;

    private float _orbitAngle;
    private bool _followEnabled;

    private const float OrbitRadius = 150f;
    private const float OrbitSpeed = 1.2f;

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        _orbitSprite = CreateFilledTexture(Core.GraphicsDevice, 40, 40, Color.Orange);
        _centerSprite = CreateFilledTexture(Core.GraphicsDevice, 20, 20, Color.Yellow);

        BuildUI();
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Scene 13: Camera2D Demo", Color = Color.DimGray });
        controls.Add(new Label { Font = _font, Text = "Camera2D Demo", Color = Color.Yellow, HAlign = HAlign.Center });

        var shakeBtn = new Button(_font, "Shake") { BackgroundPixel = _pixel };
        shakeBtn.Clicked += () => _cameraEffects.Shake(_camera, 8f, 0.5f);
        controls.Add(shakeBtn);

        var zoomInBtn = new Button(_font, "Zoom In") { BackgroundPixel = _pixel };
        zoomInBtn.Clicked += () => _cameraEffects.ZoomTo(_camera, Math.Min(_camera.Zoom + 0.5f, _camera.MaxZoom), 0.3f);
        controls.Add(zoomInBtn);

        var zoomOutBtn = new Button(_font, "Zoom Out") { BackgroundPixel = _pixel };
        zoomOutBtn.Clicked += () => _cameraEffects.ZoomTo(_camera, Math.Max(_camera.Zoom - 0.5f, _camera.MinZoom), 0.3f);
        controls.Add(zoomOutBtn);

        var zoomResetBtn = new Button(_font, "Zoom Reset") { BackgroundPixel = _pixel };
        zoomResetBtn.Clicked += () => _cameraEffects.ZoomTo(_camera, 1f, 0.3f);
        controls.Add(zoomResetBtn);

        var followBtn = new Button(_font, "Toggle Follow") { BackgroundPixel = _pixel };
        _followStateLabel = new Label { Font = _font, Text = "Follow: OFF", Color = Color.LightGray };
        followBtn.Clicked += () =>
        {
            _followEnabled = !_followEnabled;
            _followStateLabel.Text = _followEnabled ? "Follow: ON" : "Follow: OFF";
            if (!_followEnabled)
                _cameraEffects.PanTo(_camera, new Vector2(640, 360), 0.4f);
        };
        controls.Add(followBtn);
        controls.Add(_followStateLabel);

        _infoLabel = new Label { Font = _font, Text = "Pos: 640,360  Zoom: 1.00", Color = Color.LightGray };
        controls.Add(_infoLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _orbitAngle += dt * OrbitSpeed;

        _cameraEffects.Update(gameTime);

        if (_followEnabled)
        {
            Vector2 orbitPos = GetOrbitPosition();
            _camera.Follow(orbitPos, 0.05f);
        }

        _infoLabel.Text = $"Pos: {_camera.Position.X:F0},{_camera.Position.Y:F0}  Zoom: {_camera.Zoom:F2}";

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(10, 20, 40));
        Viewport viewport = Core.GraphicsDevice.Viewport;
        Matrix transform = _camera.GetTransformMatrix(viewport);

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, transformMatrix: transform);
        DrawGrid();
        DrawSprite(_centerSprite, new Vector2(640, 360));
        DrawSprite(_orbitSprite, GetOrbitPosition());
        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private Vector2 GetOrbitPosition() =>
        new(640 + MathF.Cos(_orbitAngle) * OrbitRadius, 360 + MathF.Sin(_orbitAngle) * OrbitRadius);

    private void DrawGrid()
    {
        for (int x = 0; x <= 1280; x += 80)
            Core.SpriteBatch.Draw(_pixel, new Rectangle(x, 0, 1, 720), new Color(40, 40, 60));
        for (int y = 0; y <= 720; y += 80)
            Core.SpriteBatch.Draw(_pixel, new Rectangle(0, y, 1280, 1), new Color(40, 40, 60));
    }

    private void DrawSprite(Texture2D tex, Vector2 worldPos)
    {
        Vector2 origin = new(tex.Width * 0.5f, tex.Height * 0.5f);
        Core.SpriteBatch.Draw(tex, worldPos, null, Color.White, 0f, origin, Vector2.One, SpriteEffects.None, 0f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pixel?.Dispose();
            _orbitSprite?.Dispose();
            _centerSprite?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Texture2D CreateFilledTexture(GraphicsDevice device, int width, int height, Color color)
    {
        var texture = new Texture2D(device, width, height);
        var data = new Color[width * height];
        for (int i = 0; i < data.Length; i++) data[i] = color;
        texture.SetData(data);
        return texture;
    }
}
