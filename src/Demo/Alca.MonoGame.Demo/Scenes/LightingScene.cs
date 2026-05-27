using Alca.MonoGame.Kernel.Lighting;
using Alca.MonoGame.Kernel.Lighting.GPU;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 39 — LightingWorld with PointLight2D, SpotLight2D, DirectionalLight2D, AmbientLight demo.</summary>
public sealed class LightingScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private readonly GameWorld _world = new();
    private LightingWorld _lightingWorld = null!;
    private LightingRenderPipeline? _pipeline;
    private bool _pipelineLoaded;

    private GameEntity _pointLightEntity = null!;
    private GameEntity _spotLightEntity = null!;
    private GameEntity _dirLightEntity = null!;
    private GameEntity _ambientEntity = null!;

    private PointLight2D _pointLight = null!;
    private SpotLight2D _spotLight = null!;
    private DirectionalLight2D _dirLight = null!;
    private AmbientLight _ambientLight = null!;

    private bool _pointEnabled = true;
    private bool _spotEnabled = true;
    private bool _dirEnabled = true;

    // Drag state
    private int _dragging = -1; // 0=point, 1=spot
    private bool _leftWasDown;

    protected override void PostInitialize()
    {
        base.PostInitialize();
        _lightingWorld = new LightingWorld { AmbientColor = new Color(20, 20, 30) };
        _world.LightingWorld = _lightingWorld;
        _world.AudioController = Core.Audio;

        int w = Core.GraphicsDevice.Viewport.Width;
        int h = Core.GraphicsDevice.Viewport.Height;

        _ambientEntity = _world.CreateEntity("Ambient", Vector2.Zero);
        _ambientLight = new AmbientLight { Color = new Color(20, 20, 30), Intensity = 0.1f };
        _ambientEntity.Add(_ambientLight);

        _pointLightEntity = _world.CreateEntity("PointLight", new Vector2(w * 0.3f, h * 0.4f));
        _pointLight = new PointLight2D { Color = Color.Red, Range = 200f, Intensity = 1f };
        _pointLightEntity.Add(_pointLight);

        _spotLightEntity = _world.CreateEntity("SpotLight", new Vector2(w * 0.6f, h * 0.4f));
        _spotLight = new SpotLight2D { Color = Color.DeepSkyBlue, Range = 300f, Intensity = 1f, OuterAngle = 45f, InnerAngle = 20f };
        _spotLightEntity.Add(_spotLight);

        _dirLightEntity = _world.CreateEntity("DirLight", Vector2.Zero);
        _dirLight = new DirectionalLight2D { Color = Color.White, Intensity = 0.3f, Direction = new Vector2(1f, 0.5f) };
        _dirLightEntity.Add(_dirLight);
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        _pipeline = new LightingRenderPipeline(Core.GraphicsDevice, _lightingWorld, 64);
        try
        {
            _pipeline.LoadEffect(Content, "Shaders/Lighting");
            _pipelineLoaded = true;
        }
        catch { _pipelineLoaded = false; }

        BuildUI();
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Lighting 2D Demo", Color = Color.Yellow });

        if (!_pipelineLoaded)
            controls.Add(new Label { Font = _font, Text = "⚠ Shaders/Lighting.fx no encontrado", Color = Color.Orange });

        controls.Add(new Label { Font = _font, Text = "Luz ambiental (0–1):", Color = Color.LightGray });
        var ambSlider = new Slider(_pixel) { MinValue = 0f, MaxValue = 1f, Step = 0.01f };
        ambSlider.Value = _ambientLight.Intensity;
        ambSlider.ValueChanged += v => _ambientLight.Intensity = v;
        controls.Add(ambSlider);

        controls.Add(new Label { Font = _font, Text = "PointLight radio:", Color = Color.LightGray });
        var ptRadiusSlider = new Slider(_pixel) { MinValue = 50f, MaxValue = 400f, Step = 10f };
        ptRadiusSlider.Value = _pointLight.Range;
        ptRadiusSlider.ValueChanged += v => _pointLight.Range = v;
        controls.Add(ptRadiusSlider);

        controls.Add(new Label { Font = _font, Text = "SpotLight ángulo exterior:", Color = Color.LightGray });
        var spotAngleSlider = new Slider(_pixel) { MinValue = 10f, MaxValue = 120f, Step = 5f };
        spotAngleSlider.Value = _spotLight.OuterAngle;
        spotAngleSlider.ValueChanged += v => _spotLight.OuterAngle = v;
        controls.Add(spotAngleSlider);

        controls.Add(new Label { Font = _font, Text = "Activar PointLight:", Color = Color.LightGray });
        var ptChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _pointEnabled };
        ptChk.CheckedChanged += v => { _pointEnabled = v; _pointLight.Intensity = v ? 1f : 0f; };
        controls.Add(ptChk);

        controls.Add(new Label { Font = _font, Text = "Activar SpotLight:", Color = Color.LightGray });
        var stChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _spotEnabled };
        stChk.CheckedChanged += v => { _spotEnabled = v; _spotLight.Intensity = v ? 1f : 0f; };
        controls.Add(stChk);

        controls.Add(new Label { Font = _font, Text = "Activar DirLight:", Color = Color.LightGray });
        var dtChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _dirEnabled };
        dtChk.CheckedChanged += v => { _dirEnabled = v; _dirLight.Intensity = v ? 0.3f : 0f; };
        controls.Add(dtChk);

        controls.Add(new Label { Font = _font, Text = "Click+drag: mover luces", Color = Color.LightGray });

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopRight, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        MouseState ms = Mouse.GetState();
        Vector2 mousePos = ms.Position.ToVector2();
        bool leftDown = ms.LeftButton == ButtonState.Pressed;

        if (leftDown && !_leftWasDown)
        {
            if (Vector2.Distance(mousePos, _pointLightEntity.Transform.Position2d) < 20f) _dragging = 0;
            else if (Vector2.Distance(mousePos, _spotLightEntity.Transform.Position2d) < 20f) _dragging = 1;
            else _dragging = -1;
        }
        else if (!leftDown) _dragging = -1;

        if (_dragging == 0) _pointLightEntity.Transform.Position2d = mousePos;
        else if (_dragging == 1) _spotLightEntity.Transform.Position2d = mousePos;
        _leftWasDown = leftDown;

        _world.Update(gameTime);

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(10, 10, 15));

        if (_pipelineLoaded && _pipeline != null)
        {
            _pipeline.BeginSceneCapture();
            DrawScene();
            _pipeline.EndSceneCapture();
            _pipeline.ApplyLighting(LightingLayer.World, Core.SpriteBatch);
        }
        else
        {
            DrawScene();
        }

        DrawLightIndicators();
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private void DrawScene()
    {
        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        Color[] colors = [Color.DarkRed, Color.DarkGreen, Color.DarkBlue, Color.DarkOrange, Color.Purple, Color.Teal];
        for (int i = 0; i < 12; i++)
        {
            int col = i % 4;
            int row = i / 4;
            Core.SpriteBatch.Draw(_pixel,
                new Rectangle(60 + col * 200, 80 + row * 150, 140, 100),
                colors[i % colors.Length] * 0.9f);
        }

        Core.SpriteBatch.End();
    }

    private void DrawLightIndicators()
    {
        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        Vector2 ptPos = _pointLightEntity.Transform.Position2d;
        Vector2 stPos = _spotLightEntity.Transform.Position2d;
        Core.SpriteBatch.Draw(_pixel, new Rectangle((int)ptPos.X - 8, (int)ptPos.Y - 8, 16, 16), Color.Red);
        Core.SpriteBatch.Draw(_pixel, new Rectangle((int)stPos.X - 8, (int)stPos.Y - 8, 16, 16), Color.DeepSkyBlue);

        Core.SpriteBatch.End();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pipeline?.Dispose();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
