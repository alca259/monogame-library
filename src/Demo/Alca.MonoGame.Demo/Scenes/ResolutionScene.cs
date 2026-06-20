using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Overlays;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 25 — ResolutionManager virtual resolution with letterbox scaling demo.</summary>
public sealed class ResolutionScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIOverlayManager _overlayManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private ResolutionManager? _resManager;
    private Viewport _defaultViewport;
    private int _virtualW = 320;
    private int _virtualH = 180;
    private bool _letterbox = true;

    private Label _infoLabel = null!;
    private readonly System.Text.StringBuilder _sb = new(128);

    private static readonly (int W, int H, string Label)[] VirtualResolutions =
    [
        (320, 180, "320×180"),
        (640, 360, "640×360"),
        (800, 450, "800×450"),
        (1280, 720, "1280×720"),
    ];

    protected override void PostInitialize()
    {
        base.PostInitialize();
        _defaultViewport = Core.GraphicsDevice.Viewport;
        RecreateResManager();
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
    }

    private void RecreateResManager()
    {
        _resManager?.Dispose();
        _resManager = new ResolutionManager(Core.GraphicsDevice, Core.Window, _virtualW, _virtualH);
        _resManager.Update(_defaultViewport.Width, _defaultViewport.Height);
    }

    private void BuildUI()
    {
        _uiRoot.OverlayManager = _overlayManager;
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "ResolutionManager Demo", Color = Color.Yellow });

        controls.Add(new Label { Font = _font, Text = "Resolución virtual:", Color = Color.LightGray });
        var virtualDrop = new Dropdown(_overlayManager)
        {
            Pixel = _pixel,
            Font = _font,
            ScreenHeight = _defaultViewport.Height
        };
        for (int i = 0; i < VirtualResolutions.Length; i++)
            virtualDrop.AddItem(VirtualResolutions[i].Label);
        virtualDrop.SelectedIndex = 0;
        virtualDrop.SelectionChanged += idx =>
        {
            _virtualW = VirtualResolutions[idx].W;
            _virtualH = VirtualResolutions[idx].H;
            RecreateResManager();
        };
        controls.Add(virtualDrop);

        controls.Add(new Label { Font = _font, Text = "Letterbox:", Color = Color.LightGray });
        var lbChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _letterbox };
        lbChk.CheckedChanged += v => _letterbox = v;
        controls.Add(lbChk);

        _infoLabel = new Label { Font = _font, Text = "—", Color = Color.LightGreen };
        controls.Add(_infoLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        if (_resManager != null)
        {
            Matrix scale = _letterbox ? _resManager.WorldScaleMatrix : _resManager.ScaleMatrix;
            Vector3 s = scale.Translation;
            _ = s; // scale info available in ScaleMatrix

            _sb.Clear();
            _sb.Append("Virtual: ");
            _sb.Append(_resManager.VirtualWidth);
            _sb.Append('×');
            _sb.Append(_resManager.VirtualHeight);
            _sb.Append(" | Ventana: ");
            _sb.Append(_defaultViewport.Width);
            _sb.Append('×');
            _sb.Append(_defaultViewport.Height);
            _infoLabel.Text = _sb.ToString();
        }

        _overlayManager.Update(gameTime);
        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, _defaultViewport.Width, _defaultViewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        if (_resManager != null)
        {
            Matrix transform = _letterbox ? _resManager.WorldScaleMatrix : _resManager.ScaleMatrix;
            if (_letterbox) Core.GraphicsDevice.Viewport = _resManager.LetterboxViewport;

            Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, transform);
            DrawVirtualWorld();
            Core.SpriteBatch.End();

            if (_letterbox) Core.GraphicsDevice.Viewport = _defaultViewport;
        }

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private void DrawVirtualWorld()
    {
        int vw = _resManager?.VirtualWidth ?? 320;
        int vh = _resManager?.VirtualHeight ?? 180;

        Core.SpriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, vh), new Color(10, 10, 40));

        int cellW = vw / 8;
        int cellH = vh / 6;
        for (int row = 0; row < 6; row++)
        for (int col = 0; col < 8; col++)
        {
            if ((row + col) % 2 == 0)
                Core.SpriteBatch.Draw(_pixel, new Rectangle(col * cellW, row * cellH, cellW, cellH), new Color(30, 30, 60));
        }

        Core.SpriteBatch.DrawString(_font, $"VIRTUAL {vw}×{vh}", new Vector2(4, 4), Color.Yellow);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _resManager?.Dispose();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
