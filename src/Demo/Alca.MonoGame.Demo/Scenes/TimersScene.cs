using Alca.MonoGame.Kernel.Timers;
using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 33 — TimerManager and GameTimer demo.</summary>
public sealed class TimersScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private Label _activeCountLabel = null!;
    private Label _repeatCountLabel = null!;
    private Label _statusLabel = null!;

    private GameTimer? _oneShotTimer;
    private GameTimer? _repeatTimer;
    private GameTimer? _fastTimer;

    private int _repeatCount;
    private string _oneShotStatus = "One-shot: pendiente";
    private string _fastStatus = "Fast 0.5s: pendiente";
    private bool _paused;
    private bool _dirty;

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
            CancelOwned();
            Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        };
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Timers Demo", Color = Color.Yellow, HAlign = HAlign.Center });

        var oneShotBtn = new Button(_font, "One-shot 3s") { BackgroundPixel = _pixel };
        oneShotBtn.Clicked += () =>
        {
            _oneShotTimer?.Cancel();
            _oneShotStatus = "One-shot: esperando 3s...";
            _dirty = true;
            _oneShotTimer = Core.Timers.Schedule(3f, () =>
            {
                _oneShotStatus = "One-shot: ¡Disparado!";
                _dirty = true;
            });
        };
        controls.Add(oneShotBtn);

        var repeatBtn = new Button(_font, "Repeat 1s") { BackgroundPixel = _pixel };
        repeatBtn.Clicked += () =>
        {
            _repeatTimer?.Cancel();
            _repeatCount = 0;
            _dirty = true;
            _repeatTimer = Core.Timers.ScheduleRepeating(1f, () =>
            {
                _repeatCount++;
                _dirty = true;
            });
        };
        controls.Add(repeatBtn);

        var fastBtn = new Button(_font, "Fast 0.5s") { BackgroundPixel = _pixel };
        fastBtn.Clicked += () =>
        {
            _fastTimer?.Cancel();
            _fastStatus = "Fast 0.5s: esperando...";
            _dirty = true;
            _fastTimer = Core.Timers.Schedule(0.5f, () =>
            {
                _fastStatus = "Fast 0.5s: ¡Disparado!";
                _dirty = true;
            });
        };
        controls.Add(fastBtn);

        var pauseBtn = new Button(_font, "Pause / Resume") { BackgroundPixel = _pixel };
        pauseBtn.Clicked += () =>
        {
            _paused = !_paused;
            if (_paused)
            {
                _oneShotTimer?.Pause();
                _repeatTimer?.Pause();
                _fastTimer?.Pause();
            }
            else
            {
                _oneShotTimer?.Resume();
                _repeatTimer?.Resume();
                _fastTimer?.Resume();
            }
            _dirty = true;
        };
        controls.Add(pauseBtn);

        var cancelBtn = new Button(_font, "Cancel All") { BackgroundPixel = _pixel };
        cancelBtn.Clicked += () =>
        {
            CancelOwned();
            _dirty = true;
        };
        controls.Add(cancelBtn);

        _activeCountLabel = new Label { Font = _font, Text = "Timers activos: 0", Color = Color.LightGreen };
        controls.Add(_activeCountLabel);
        _repeatCountLabel = new Label { Font = _font, Text = "Contador repeat: 0", Color = Color.White };
        controls.Add(_repeatCountLabel);
        _statusLabel = new Label { Font = _font, Text = "—", Color = Color.LightGray };
        controls.Add(_statusLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(20, 20));
        _uiRoot.Add(anchor);
    }

    private void CancelOwned()
    {
        _oneShotTimer?.Cancel();
        _repeatTimer?.Cancel();
        _fastTimer?.Cancel();
        _oneShotTimer = null;
        _repeatTimer = null;
        _fastTimer = null;
        _repeatCount = 0;
        _paused = false;
        _oneShotStatus = "One-shot: cancelado";
        _fastStatus = "Fast 0.5s: cancelado";
    }

    public override void Update(GameTime gameTime)
    {
        if (_dirty)
        {
            _dirty = false;
            int active = 0;
            if (_oneShotTimer != null && !_oneShotTimer.IsDone) active++;
            if (_repeatTimer != null && !_repeatTimer.IsDone) active++;
            if (_fastTimer != null && !_fastTimer.IsDone) active++;
            _activeCountLabel.Text = $"Timers activos: {active}{(_paused ? " (pausados)" : "")}";
            _repeatCountLabel.Text = $"Contador repeat: {_repeatCount}";
            _statusLabel.Text = $"{_oneShotStatus}\n{_fastStatus}";
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
            CancelOwned();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
