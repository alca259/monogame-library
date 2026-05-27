using Alca.MonoGame.Kernel.Tweening;
using Alca.MonoGame.Kernel.UI.Transitions;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 11/12 — demos UITransitionManager and UITweenExtensions (FadeIn/Out, SlideIn/Out).</summary>
public sealed class UIScene_Transitions : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIOverlayManager _overlayManager = new();
    private readonly UITransitionManager _transitions = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    // Target: standalone panel, NOT inside _uiRoot, so Arrange calls don't override tween
    private Panel _targetPanel = null!;
    private Label _lastTransitionLabel = null!;
    private Label _stateLabel = null!;
    private Dropdown _dropdownIn = null!;
    private Dropdown _dropdownOut = null!;
    private Dropdown _dropdownEasing = null!;
    private Slider _durationSlider = null!;
    private Label _durationLabel = null!;

    private Rectangle _originalBounds;
    private bool _boundsInitialized;
    private bool _isPlaying;

    private static readonly UITransitionType[] InTransitions =
    [
        UITransitionType.FadeIn,
        UITransitionType.SlideInFromLeft,
        UITransitionType.SlideInFromRight,
        UITransitionType.SlideInFromTop,
        UITransitionType.SlideInFromBottom,
    ];

    private static readonly UITransitionType[] OutTransitions =
    [
        UITransitionType.FadeOut,
        UITransitionType.SlideOutToLeft,
        UITransitionType.SlideOutToRight,
        UITransitionType.SlideOutToTop,
        UITransitionType.SlideOutToBottom,
    ];

    private static readonly string[] EasingNames =
    [
        "Linear",
        "EaseOutQuad",
        "EaseInQuad",
        "EaseInOutQuad",
        "EaseOutBounce",
    ];

    private static readonly Func<float, float>[] Easings =
    [
        EasingCatalog.Linear,
        EasingCatalog.QuadOut,
        EasingCatalog.QuadIn,
        EasingCatalog.QuadInOut,
        EasingCatalog.BounceOut,
    ];

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        _uiRoot.OverlayManager = _overlayManager;
        BuildUI();
    }

    private void BuildUI()
    {
        // ── Controls column (inside _uiRoot, arranged every frame) ───────────
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Scene 11/12: Transitions", Color = Color.DimGray });
        controls.Add(new Label { Font = _font, Text = "Transitions Demo", Color = Color.Yellow, HAlign = HAlign.Center });

        controls.Add(new Label { Font = _font, Text = "Transition In:", Color = Color.White });
        _dropdownIn = BuildDropdown();
        _dropdownIn.AddItem("FadeIn");
        _dropdownIn.AddItem("SlideInFromLeft");
        _dropdownIn.AddItem("SlideInFromRight");
        _dropdownIn.AddItem("SlideInFromTop");
        _dropdownIn.AddItem("SlideInFromBottom");
        controls.Add(_dropdownIn);

        controls.Add(new Label { Font = _font, Text = "Transition Out:", Color = Color.White });
        _dropdownOut = BuildDropdown();
        _dropdownOut.AddItem("FadeOut");
        _dropdownOut.AddItem("SlideOutToLeft");
        _dropdownOut.AddItem("SlideOutToRight");
        _dropdownOut.AddItem("SlideOutToTop");
        _dropdownOut.AddItem("SlideOutToBottom");
        controls.Add(_dropdownOut);

        _durationLabel = new Label { Font = _font, Text = "Duracion: 0.5s", Color = Color.White };
        controls.Add(_durationLabel);
        _durationSlider = new Slider(_pixel) { MinValue = 0.2f, MaxValue = 2.0f, Step = 0.1f, Value = 0.5f };
        _durationSlider.ValueChanged += v => { _durationLabel.Text = $"Duracion: {v:F1}s"; };
        controls.Add(_durationSlider);

        controls.Add(new Label { Font = _font, Text = "Easing:", Color = Color.White });
        _dropdownEasing = BuildDropdown();
        foreach (string name in EasingNames)
            _dropdownEasing.AddItem(name);
        controls.Add(_dropdownEasing);

        var playInBtn = new Button(_font, "Play In") { BackgroundPixel = _pixel };
        playInBtn.Clicked += PlayIn;
        controls.Add(playInBtn);

        var playOutBtn = new Button(_font, "Play Out") { BackgroundPixel = _pixel };
        playOutBtn.Clicked += PlayOut;
        controls.Add(playOutBtn);

        var resetBtn = new Button(_font, "Reset") { BackgroundPixel = _pixel };
        resetBtn.Clicked += ResetTarget;
        controls.Add(resetBtn);

        _stateLabel = new Label { Font = _font, Text = "Estado: Idle", Color = Color.LightGreen };
        controls.Add(_stateLabel);

        _lastTransitionLabel = new Label { Font = _font, Text = "Ultima transicion: —", Color = Color.DimGray };
        controls.Add(_lastTransitionLabel);

        _uiRoot.Add(controls);

        // ── Target panel (standalone — not in _uiRoot, so layout never resets its Bounds) ──
        _targetPanel = new Panel
        {
            BackgroundTexture = _pixel,
            BackgroundColor = new Color(80, 60, 120),
            BorderColor = Color.MediumPurple,
            BorderThickness = 2,
        };
        _targetPanel.Add(new Label
        {
            Font = _font,
            Text = "Target",
            Color = Color.White,
            HAlign = HAlign.Center,
            VAlign = VAlign.Middle,
        });
    }

    private Dropdown BuildDropdown() => new(_overlayManager)
    {
        Pixel = _pixel,
        Font = _font,
        ScreenHeight = Core.GraphicsDevice.Viewport.Height,
    };

    private void PlayIn()
    {
        if (_isPlaying) return;
        int index = Math.Max(0, _dropdownIn.SelectedIndex);
        UITransitionType transition = InTransitions[index];
        _isPlaying = true;
        _stateLabel.Text = "Estado: Playing";
        _lastTransitionLabel.Text = $"Ultima: {transition}";
        _transitions.Play(_targetPanel, transition, _durationSlider.Value, GetEasing())
            .OnEnd(_ => { _isPlaying = false; _stateLabel.Text = "Estado: Idle"; });
    }

    private void PlayOut()
    {
        if (_isPlaying) return;
        int index = Math.Max(0, _dropdownOut.SelectedIndex);
        UITransitionType transition = OutTransitions[index];
        _isPlaying = true;
        _stateLabel.Text = "Estado: Playing";
        _lastTransitionLabel.Text = $"Ultima: {transition}";
        _transitions.Play(_targetPanel, transition, _durationSlider.Value, GetEasing())
            .OnEnd(_ => { _isPlaying = false; _stateLabel.Text = "Estado: Idle"; });
    }

    private void ResetTarget()
    {
        _targetPanel.Opacity = 1f;
        if (_boundsInitialized)
            _targetPanel.Arrange(_originalBounds);
        _isPlaying = false;
        _stateLabel.Text = "Estado: Idle";
    }

    private Func<float, float> GetEasing()
    {
        int index = Math.Max(0, _dropdownEasing.SelectedIndex);
        return Easings[index];
    }

    public override void Update(GameTime gameTime)
    {
        _uiRoot.Update(gameTime);
        _overlayManager.Update(gameTime);

        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);

        if (!_boundsInitialized)
        {
            // Position target panel at the right half of the screen, vertically centered
            int panelW = 200;
            int panelH = 120;
            int panelX = screen.Width * 3 / 4 - panelW / 2;
            int panelY = screen.Height / 2 - panelH / 2;
            Rectangle targetRect = new(panelX, panelY, panelW, panelH);
            _targetPanel.Measure(new Vector2(panelW, panelH));
            _targetPanel.Arrange(targetRect);
            _originalBounds = _targetPanel.Bounds;
            _boundsInitialized = true;
        }

        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(20, 20, 30));
        Core.SpriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp);
        _uiRoot.Draw(Core.SpriteBatch);
        _targetPanel.Draw(Core.SpriteBatch);
        _overlayManager.Draw(Core.SpriteBatch);
        Core.SpriteBatch.End();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
