using Alca.MonoGame.Kernel.Graphics.Camera;
using Alca.MonoGame.Kernel.Graphics.Tiled;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 26 — TiledMapRenderer with Camera2D navigation demo.</summary>
public sealed class TiledMapScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private TiledMapRenderer? _mapRenderer;
    private Camera2D _camera = null!;
    private bool _mapLoaded;
    private bool _showCollisionLayer = true;
    private bool _showObjects = true;

    private Label _camLabel = null!;
    private Label _objectsLabel = null!;
    private readonly System.Text.StringBuilder _sb = new(64);

    protected override void PostInitialize()
    {
        base.PostInitialize();
        _camera = new Camera2D();
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        _mapRenderer = new TiledMapRenderer(Core.GraphicsDevice);
        try
        {
            _mapRenderer.Load(Content, "Maps/demo");
            _mapLoaded = true;
        }
        catch { _mapLoaded = false; }

        BuildUI();
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "TiledMap Demo", Color = Color.Yellow });

        if (!_mapLoaded)
            controls.Add(new Label { Font = _font, Text = "⚠ Maps/demo.tmx no encontrado", Color = Color.Orange });

        _camLabel = new Label { Font = _font, Text = "Cámara: 0, 0", Color = Color.LightGreen };
        controls.Add(_camLabel);

        _objectsLabel = new Label { Font = _font, Text = "Objetos: 0", Color = Color.LightGreen };
        controls.Add(_objectsLabel);

        controls.Add(new Label { Font = _font, Text = "Mostrar colisiones:", Color = Color.LightGray });
        var collChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _showCollisionLayer };
        collChk.CheckedChanged += v => _showCollisionLayer = v;
        controls.Add(collChk);

        controls.Add(new Label { Font = _font, Text = "Mostrar objetos:", Color = Color.LightGray });
        var objChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _showObjects };
        objChk.CheckedChanged += v => _showObjects = v;
        controls.Add(objChk);

        controls.Add(new Label { Font = _font, Text = "WASD: mover cámara", Color = Color.LightGray });

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        const float CamSpeed = 300f;
        KeyboardState ks = Keyboard.GetState();

        Vector2 move = Vector2.Zero;
        if (ks.IsKeyDown(Keys.W)) move.Y -= 1f;
        if (ks.IsKeyDown(Keys.S)) move.Y += 1f;
        if (ks.IsKeyDown(Keys.A)) move.X -= 1f;
        if (ks.IsKeyDown(Keys.D)) move.X += 1f;
        _camera.Position += move * (CamSpeed * dt);

        _mapRenderer?.Update(gameTime);

        Vector2 camPos = _camera.Position;
        _sb.Clear();
        _sb.Append("Cámara: ");
        _sb.Append(((int)camPos.X).ToString());
        _sb.Append(", ");
        _sb.Append(((int)camPos.Y).ToString());
        _camLabel.Text = _sb.ToString();

        int objCount = 0;
        if (_mapLoaded && _mapRenderer?.LoadedMap != null)
        {
            var layer = _mapRenderer.GetLayer("objects");
            if (layer != null) objCount = 0; // objects count via layer API
        }
        _objectsLabel.Text = $"Objetos: {objCount}";

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(20, 30, 20));

        if (_mapLoaded && _mapRenderer != null)
        {
            Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                null, null, null, null, _camera.GetTransformMatrix(Core.GraphicsDevice.Viewport));
            _mapRenderer.Draw(_camera, Core.SpriteBatch);

            if (_showCollisionLayer)
            {
                var collLayer = _mapRenderer.GetLayer("collision");
                if (collLayer != null)
                    _mapRenderer.DrawLayer(_camera, Core.SpriteBatch, "collision");
            }
            Core.SpriteBatch.End();
        }
        else
        {
            Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Core.SpriteBatch.Draw(_pixel, new Rectangle(200, 200, 400, 200), new Color(30, 30, 60));
            Core.SpriteBatch.DrawString(_font, "Mapa no cargado", new Vector2(280, 290), Color.Gray);
            Core.SpriteBatch.End();
        }

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mapRenderer?.Dispose();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
