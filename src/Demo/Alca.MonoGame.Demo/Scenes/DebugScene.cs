using Alca.MonoGame.Kernel.Debug;
using Alca.MonoGame.Kernel.Graphics.Camera;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 35 — DebugDraw and DebugOverlay demo.</summary>
public sealed class DebugScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private readonly DebugOverlay _overlay = new();
    private Camera2D _camera = null!;

    private bool _showDebugDraw = true;
    private bool _showOverlay = true;
    private float _orbitAngle;

    protected override void PostInitialize()
    {
        base.PostInitialize();
        _camera = new Camera2D();
        DebugDraw.IsEnabled = true;

        _overlay.AddWatch("FPS", () => _overlay.FPS.ToString("F1"));
        _overlay.AddWatch("Modo", () => "Debug Demo");
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () =>
        {
            DebugDraw.Clear();
            Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        };
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Debug Tools Demo", Color = Color.Yellow });

        controls.Add(new Label { Font = _font, Text = "Mostrar DebugDraw:", Color = Color.LightGray });
        var drawChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _showDebugDraw };
        drawChk.CheckedChanged += v =>
        {
            _showDebugDraw = v;
            DebugDraw.IsEnabled = v;
        };
        controls.Add(drawChk);

        controls.Add(new Label { Font = _font, Text = "Mostrar DebugOverlay:", Color = Color.LightGray });
        var overlayChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _showOverlay };
        overlayChk.CheckedChanged += v => _showOverlay = v;
        controls.Add(overlayChk);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _orbitAngle += dt;

        int w = Core.GraphicsDevice.Viewport.Width;
        int h = Core.GraphicsDevice.Viewport.Height;

        if (_showDebugDraw)
        {
            Vector2 center = new(w / 2f, h / 2f);

            // Static shapes
            DebugDraw.DrawLine(new Vector2(50, 100), new Vector2(350, 100), Color.Red);
            DebugDraw.DrawRect(new Rectangle(100, 200, 120, 80), Color.Lime);
            DebugDraw.DrawCircle(new Vector2(350, 250), 50f, Color.Cyan, 24);
            DebugDraw.DrawPoint(new Vector2(500, 200), Color.Yellow, 8f);
            DebugDraw.DrawText(new Vector2(500, 300), "Debug Text!", Color.White);

            // Orbiting moving shapes
            for (int i = 0; i < 4; i++)
            {
                float a = _orbitAngle + i * MathHelper.PiOver2;
                Vector2 pos = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 120f;
                DebugDraw.DrawCircle(pos, 14f, Color.Orange, 12);
            }
        }

        _overlay.Update(gameTime);
        DebugDraw.Update(gameTime);

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, w, h);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(15, 15, 25));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        if (_showDebugDraw)
            DebugDraw.Draw(Core.SpriteBatch, _camera, _font);
        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);

        if (_showOverlay)
        {
            Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _overlay.Draw(Core.SpriteBatch, _font);
            Core.SpriteBatch.End();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DebugDraw.Clear();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
