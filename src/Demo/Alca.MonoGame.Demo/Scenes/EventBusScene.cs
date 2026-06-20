using Alca.MonoGame.Kernel.Events;
using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 31 — EventBus, EventChannel, and ICancellableEvent demo.</summary>
public sealed class EventBusScene : Scene
{
    #region Local event types
    private sealed record GameStartedEvent;
    private sealed record ScoreChangedEvent(int Delta);
    private sealed class PlayerDiedEvent : ICancellableEvent { public bool IsCancelled { get; set; } }
    #endregion

    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private Label _scoreLabel = null!;
    private Label _diedLabel = null!;
    private Label _logLabel = null!;

    private readonly string[] _logEntries = new string[10];
    private readonly System.Text.StringBuilder _logSb = new(512);
    private int _logCount;
    private bool _logDirty;

    private int _score;
    private int _diedCount;
    private int _cancelledCount;
    private bool _cancelDied;
    private bool _extraSubscribed;

    private readonly Action<ScoreChangedEvent> _extraHandler;

    public EventBusScene()
    {
        _extraHandler = evt => AddLog($"[Extra] ScoreChanged +{evt.Delta}");
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);

        BuildUI();
    }

    private void BuildUI()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 30 };

        // Left: publishers
        var left = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        left.Add(backBtn);

        left.Add(new Label { Font = _font, Text = "Publicar eventos", Color = Color.Yellow });

        var startBtn = new Button(_font, "Publish: GameStarted") { BackgroundPixel = _pixel };
        startBtn.Clicked += () => EventBus.Publish(new GameStartedEvent());
        left.Add(startBtn);

        var scoreBtn = new Button(_font, "Publish: ScoreChanged (+10)") { BackgroundPixel = _pixel };
        scoreBtn.Clicked += () => EventBus.Publish(new ScoreChangedEvent(10));
        left.Add(scoreBtn);

        var diedBtn = new Button(_font, "Publish: PlayerDied (cancelable)") { BackgroundPixel = _pixel };
        diedBtn.Clicked += () => EventBus.PublishCancellable(new PlayerDiedEvent());
        left.Add(diedBtn);

        var cancelCheck = new Checkbox(_font, "Cancelar PlayerDied") { Pixel = _pixel };
        cancelCheck.CheckedChanged += v => _cancelDied = v;
        left.Add(cancelCheck);

        row.Add(left);

        // Right: receivers / log
        var right = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        right.Add(new Label { Font = _font, Text = "Log de eventos", Color = Color.Yellow });

        _logLabel = new Label { Font = _font, Text = "(vacío)", Color = Color.LightGray };
        right.Add(_logLabel);

        _scoreLabel = new Label { Font = _font, Text = "Score acumulado: 0", Color = Color.LightGreen };
        right.Add(_scoreLabel);

        _diedLabel = new Label { Font = _font, Text = "PlayerDied: 0 | Cancelado: 0", Color = Color.Orange };
        right.Add(_diedLabel);

        var clearBtn = new Button(_font, "Limpiar log") { BackgroundPixel = _pixel };
        clearBtn.Clicked += () => { _logCount = 0; _logDirty = true; };
        right.Add(clearBtn);

        var subBtn = new Button(_font, "Subscribe / Unsubscribe extra") { BackgroundPixel = _pixel };
        subBtn.Clicked += () =>
        {
            if (!_extraSubscribed)
            {
                EventBus.Subscribe(_extraHandler);
                _extraSubscribed = true;
                AddLog("[Sys] Extra subscriber registrado");
            }
            else
            {
                EventBus.Unsubscribe(_extraHandler);
                _extraSubscribed = false;
                AddLog("[Sys] Extra subscriber eliminado");
            }
        };
        right.Add(subBtn);

        row.Add(right);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(row, Anchor.TopLeft, new Vector2(20, 20));
        _uiRoot.Add(anchor);
    }

    private void OnGameStarted(GameStartedEvent _) => AddLog("[Bus] GameStarted recibido");

    private void OnScoreChanged(ScoreChangedEvent evt)
    {
        _score += evt.Delta;
        AddLog($"[Bus] ScoreChanged +{evt.Delta} → total {_score}");
        _scoreLabel.Text = $"Score acumulado: {_score}";
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        if (_cancelDied)
        {
            evt.IsCancelled = true;
            _cancelledCount++;
            AddLog($"[Bus] PlayerDied CANCELADO ({_cancelledCount})");
        }
        else
        {
            _diedCount++;
            AddLog($"[Bus] PlayerDied recibido ({_diedCount})");
        }
        _diedLabel.Text = $"PlayerDied: {_diedCount} | Cancelado: {_cancelledCount}";
    }

    private void AddLog(string entry)
    {
        if (_logCount < _logEntries.Length)
        {
            _logEntries[_logCount++] = entry;
        }
        else
        {
            for (int i = 0; i < _logEntries.Length - 1; i++)
                _logEntries[i] = _logEntries[i + 1];
            _logEntries[_logEntries.Length - 1] = entry;
        }
        _logDirty = true;
    }

    public override void Update(GameTime gameTime)
    {
        if (_logDirty)
        {
            _logDirty = false;
            _logSb.Clear();
            if (_logCount == 0)
            {
                _logLabel.Text = "(vacío)";
            }
            else
            {
                for (int i = 0; i < _logCount; i++)
                {
                    if (i > 0) _logSb.Append('\n');
                    _logSb.Append(_logEntries[i]);
                }
                _logLabel.Text = _logSb.ToString();
            }
        }

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
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            if (_extraSubscribed) EventBus.Unsubscribe(_extraHandler);
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
