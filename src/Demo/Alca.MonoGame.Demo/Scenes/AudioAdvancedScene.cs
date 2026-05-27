using Alca.MonoGame.Kernel.Audio;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 18 — SoundEffectPool and AudioCrossfader demo.</summary>
public sealed class AudioAdvancedScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private SoundEffectPool? _pool;
    private bool _sfxLoaded;
    private float _pitch = 1f;
    private int _totalPlays;
    private readonly System.Text.StringBuilder _poolSb = new(64);
    private Label _poolLabel = null!;

    private readonly AudioCrossfader _crossfader = new();
    private SoundEffect? _trackA;
    private SoundEffect? _trackB;
    private bool _trackALoaded;
    private bool _trackBLoaded;
    private bool _playingA = true;
    private float _crossfadeDuration = 1.5f;
    private readonly System.Text.StringBuilder _crossSb = new(64);
    private Label _crossLabel = null!;

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        try
        {
            var beep = Content.Load<SoundEffect>("SFX/beep");
            _pool = new SoundEffectPool(beep, 16);
            _sfxLoaded = true;
        }
        catch { _sfxLoaded = false; }

        try { _trackA = Content.Load<SoundEffect>("Music/track_a"); _trackALoaded = true; }
        catch { _trackALoaded = false; }

        try { _trackB = Content.Load<SoundEffect>("Music/track_b"); _trackBLoaded = true; }
        catch { _trackBLoaded = false; }

        BuildUI();
    }

    private void BuildUI()
    {
        var root = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 60 };

        var poolCol = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        poolCol.Add(backBtn);
        poolCol.Add(new Label { Font = _font, Text = "SoundEffectPool Demo", Color = Color.Yellow });

        if (!_sfxLoaded)
            poolCol.Add(new Label { Font = _font, Text = "⚠ SFX/beep no encontrado", Color = Color.Orange });

        var spawnBtn = new Button(_font, "Spawn Sound") { BackgroundPixel = _pixel, IsEnabled = _sfxLoaded };
        spawnBtn.Clicked += () =>
        {
            _pool?.Play(1f, _pitch - 1f, 0f);
            _totalPlays++;
        };
        poolCol.Add(spawnBtn);

        poolCol.Add(new Label { Font = _font, Text = "Pitch (0.5–2.0)", Color = Color.LightGray });
        var pitchSlider = new Slider(_pixel) { MinValue = 0.5f, MaxValue = 2f, Step = 0.05f };
        pitchSlider.Value = _pitch;
        pitchSlider.ValueChanged += v => _pitch = v;
        poolCol.Add(pitchSlider);

        _poolLabel = new Label { Font = _font, Text = "Jugados: 0 | Pitch: 1.00", Color = Color.LightGreen };
        poolCol.Add(_poolLabel);

        var crossCol = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        crossCol.Add(new Label { Font = _font, Text = "AudioCrossfader Demo", Color = Color.Yellow });

        if (!_trackALoaded)
            crossCol.Add(new Label { Font = _font, Text = "⚠ Music/track_a no encontrado", Color = Color.Orange });
        if (!_trackBLoaded)
            crossCol.Add(new Label { Font = _font, Text = "⚠ Music/track_b no encontrado", Color = Color.Orange });

        bool tracksOk = _trackALoaded && _trackBLoaded;

        var fadeABBtn = new Button(_font, "Fade A→B") { BackgroundPixel = _pixel, IsEnabled = tracksOk };
        fadeABBtn.Clicked += () =>
        {
            if (_trackB != null) { _crossfader.CrossfadeTo(_trackB, _crossfadeDuration); _playingA = false; }
        };
        crossCol.Add(fadeABBtn);

        var fadeBABtn = new Button(_font, "Fade B→A") { BackgroundPixel = _pixel, IsEnabled = tracksOk };
        fadeBABtn.Clicked += () =>
        {
            if (_trackA != null) { _crossfader.CrossfadeTo(_trackA, _crossfadeDuration); _playingA = true; }
        };
        crossCol.Add(fadeBABtn);

        crossCol.Add(new Label { Font = _font, Text = "Duración crossfade (s)", Color = Color.LightGray });
        var durSlider = new Slider(_pixel) { MinValue = 0.5f, MaxValue = 3f, Step = 0.1f };
        durSlider.Value = _crossfadeDuration;
        durSlider.ValueChanged += v => _crossfadeDuration = v;
        crossCol.Add(durSlider);

        var stopBtn = new Button(_font, "Stop") { BackgroundPixel = _pixel };
        stopBtn.Clicked += () => _crossfader.Stop();
        crossCol.Add(stopBtn);

        _crossLabel = new Label { Font = _font, Text = "Reproduciendo: —", Color = Color.LightGreen };
        crossCol.Add(_crossLabel);

        root.Add(poolCol);
        root.Add(crossCol);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(root, Anchor.TopLeft, new Vector2(20, 20));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        _crossfader.Update(gameTime);

        _poolSb.Clear();
        _poolSb.Append("Jugados: ");
        _poolSb.Append(_totalPlays);
        _poolSb.Append(" | Pitch: ");
        _poolSb.Append(_pitch.ToString("F2"));
        _poolLabel.Text = _poolSb.ToString();

        _crossSb.Clear();
        _crossSb.Append("Reproduciendo: Track ");
        _crossSb.Append(_playingA ? 'A' : 'B');
        if (_crossfader.IsCrossfading) _crossSb.Append(" (crossfading…)");
        _crossLabel.Text = _crossSb.ToString();

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(15, 15, 25));
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _crossfader.Dispose();
            _pool?.Dispose();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
