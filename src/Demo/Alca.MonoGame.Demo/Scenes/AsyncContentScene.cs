using Alca.MonoGame.Kernel.Content;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 38 — AsyncContentLoader and ContentGroupBuilder progressive loading demo.</summary>
public sealed class AsyncContentScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private AsyncContentLoader? _loader;
    private ContentLoadGroup? _groupA;
    private ContentLoadGroup? _groupB;

    private float _progressA;
    private float _progressB;
    private bool _groupADone;
    private bool _groupBDone;
    private bool _initialLoadComplete;
    private float _loadStartTime;
    private float _totalElapsed;

    private ProgressBar _progressBarA = null!;
    private ProgressBar _progressBarB = null!;
    private Label _currentAssetLabel = null!;
    private Label _timeLabel = null!;
    private Label _statusLabel = null!;

    private readonly System.Text.StringBuilder _sb = new(64);

    protected override void PostInitialize()
    {
        base.PostInitialize();
        _loader = new AsyncContentLoader();
        _loadStartTime = 0f;
        BeginLoad();
    }

    private void BeginLoad()
    {
        _progressA = 0f;
        _progressB = 0f;
        _groupADone = false;
        _groupBDone = false;
        _initialLoadComplete = false;
        _loadStartTime = _totalElapsed;

        _groupA = new ContentGroupBuilder()
            .Add<SpriteFont>("DefaultFont")
            .Add<SpriteFont>("DefaultFont")
            .Build();

        _groupB = new ContentGroupBuilder()
            .Add<SpriteFont>("DefaultFont")
            .Build();

        var progressA = new System.Progress<float>(p => _progressA = p);
        var progressB = new System.Progress<float>(p => _progressB = p);

        _ = _groupA.LoadAllAsync(_loader!, progressA, System.Threading.CancellationToken.None)
            .ContinueWith(_ => { _groupADone = true; });
        _ = _groupB.LoadAllAsync(_loader!, progressB, System.Threading.CancellationToken.None)
            .ContinueWith(_ => { _groupBDone = true; });
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
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Async Content Demo", Color = Color.Yellow });
        controls.Add(new Label { Font = _font, Text = "Grupo A: fuentes | Grupo B: fuentes", Color = Color.LightGray });

        controls.Add(new Label { Font = _font, Text = "Grupo A:", Color = Color.LightGray });
        _progressBarA = new ProgressBar { Pixel = _pixel };
        controls.Add(_progressBarA);

        controls.Add(new Label { Font = _font, Text = "Grupo B:", Color = Color.LightGray });
        _progressBarB = new ProgressBar { Pixel = _pixel };
        controls.Add(_progressBarB);

        _currentAssetLabel = new Label { Font = _font, Text = "Cargando…", Color = Color.LightGreen };
        controls.Add(_currentAssetLabel);

        _timeLabel = new Label { Font = _font, Text = "Tiempo: 0.00s", Color = Color.LightGreen };
        controls.Add(_timeLabel);

        _statusLabel = new Label { Font = _font, Text = "Estado: cargando", Color = Color.Orange };
        controls.Add(_statusLabel);

        var reloadABtn = new Button(_font, "Recargar grupo A") { BackgroundPixel = _pixel };
        reloadABtn.Clicked += () =>
        {
            _progressA = 0f;
            _groupADone = false;
            var progressA = new System.Progress<float>(p => _progressA = p);
            _ = _groupA!.LoadAllAsync(_loader!, progressA, System.Threading.CancellationToken.None)
                .ContinueWith(_ => { _groupADone = true; });
        };
        controls.Add(reloadABtn);

        var reloadBBtn = new Button(_font, "Recargar grupo B") { BackgroundPixel = _pixel };
        reloadBBtn.Clicked += () =>
        {
            _progressB = 0f;
            _groupBDone = false;
            var progressB = new System.Progress<float>(p => _progressB = p);
            _ = _groupB!.LoadAllAsync(_loader!, progressB, System.Threading.CancellationToken.None)
                .ContinueWith(_ => { _groupBDone = true; });
        };
        controls.Add(reloadBBtn);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(20, 20));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        _totalElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

        _loader?.FlushPending(Content);

        _progressBarA.Value = _progressA;
        _progressBarB.Value = _progressB;

        if (_groupADone && _groupBDone && !_initialLoadComplete)
            _initialLoadComplete = true;

        _sb.Clear();
        _sb.Append(_initialLoadComplete ? "Cargando assets…" : "Carga completada");
        _currentAssetLabel.Text = _sb.ToString();

        _sb.Clear();
        _sb.Append("Tiempo: ");
        _sb.Append((_totalElapsed - _loadStartTime).ToString("F2"));
        _sb.Append("s");
        _timeLabel.Text = _sb.ToString();

        _statusLabel.Text = _initialLoadComplete ? "Estado: completado" : "Estado: cargando";
        _statusLabel.Color = _initialLoadComplete ? Color.LimeGreen : Color.Orange;

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(10, 10, 25));
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loader?.Dispose();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
