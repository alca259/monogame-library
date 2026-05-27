using Alca.MonoGame.Kernel.StateMachine;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 32 — StateMachine generic FSM with visual diagram.</summary>
public sealed class StateMachineScene : Scene
{
    #region State enum and states
    private enum PlayerState { Idle, Walk, Run, Attack }

    private sealed class SimpleState : IState<PlayerState>
    {
        private readonly Action<PlayerState>? _onEnter;
        private readonly Action<PlayerState>? _onExit;

        public SimpleState(Action<PlayerState>? onEnter = null, Action<PlayerState>? onExit = null)
        {
            _onEnter = onEnter;
            _onExit = onExit;
        }

        public void Enter(PlayerState previous) => _onEnter?.Invoke(previous);
        public void Update(GameTime gameTime) { }
        public void Exit(PlayerState next) => _onExit?.Invoke(next);
    }
    #endregion

    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly StateMachine<PlayerState> _fsm = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private Label _currentStateLabel = null!;
    private Label _timeInStateLabel = null!;
    private Label _transitionCountLabel = null!;
    private Label _historyLabel = null!;
    private Label _errorLabel = null!;

    private float _timeInState;
    private int _transitionCount;
    private bool _autoPlay;
    private float _autoTimer;

    // Transition history — last 5 entries
    private readonly string[] _history = new string[5];
    private int _historyCount;
    private bool _historyDirty;
    private readonly System.Text.StringBuilder _historySb = new(256);

    // Diagram layout constants
    private static readonly Vector2[] StatePositions =
    {
        new(200, 180), // Idle
        new(450, 180), // Walk
        new(700, 180), // Run
        new(450, 320), // Attack
    };
    private static readonly string[] StateNames = { "Idle", "Walk", "Run", "Attack" };
    private const int BoxW = 110;
    private const int BoxH = 40;

    protected override void PostInitialize()
    {
        base.PostInitialize();

        _fsm.Register(PlayerState.Idle,   new SimpleState(prev => OnEnterState(PlayerState.Idle)));
        _fsm.Register(PlayerState.Walk,   new SimpleState(prev => OnEnterState(PlayerState.Walk)));
        _fsm.Register(PlayerState.Run,    new SimpleState(prev => OnEnterState(PlayerState.Run)));
        _fsm.Register(PlayerState.Attack, new SimpleState(prev => OnEnterState(PlayerState.Attack)));

        _fsm.Transition(PlayerState.Idle);
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

        controls.Add(new Label { Font = _font, Text = "StateMachine Demo", Color = Color.Yellow, HAlign = HAlign.Center });

        void AddTransitionBtn(string label, PlayerState target)
        {
            var btn = new Button(_font, label) { BackgroundPixel = _pixel };
            btn.Clicked += () => TryTransition(target);
            controls.Add(btn);
        }

        AddTransitionBtn("→ Idle",   PlayerState.Idle);
        AddTransitionBtn("→ Walk",   PlayerState.Walk);
        AddTransitionBtn("→ Run",    PlayerState.Run);
        AddTransitionBtn("→ Attack", PlayerState.Attack);

        var autoBtn = new Button(_font, "Auto Play") { BackgroundPixel = _pixel };
        autoBtn.Clicked += () => { _autoPlay = !_autoPlay; _autoTimer = 0f; };
        controls.Add(autoBtn);

        _currentStateLabel  = new Label { Font = _font, Text = "Estado: Idle", Color = Color.LightGreen };
        _timeInStateLabel   = new Label { Font = _font, Text = "Tiempo: 0.0s",  Color = Color.White };
        _transitionCountLabel = new Label { Font = _font, Text = "Transiciones: 0", Color = Color.White };
        _historyLabel       = new Label { Font = _font, Text = "—",             Color = Color.LightGray };
        _errorLabel         = new Label { Font = _font, Text = "",              Color = Color.OrangeRed };

        controls.Add(_currentStateLabel);
        controls.Add(_timeInStateLabel);
        controls.Add(_transitionCountLabel);
        controls.Add(_historyLabel);
        controls.Add(_errorLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopRight, new Vector2(-10, 10));
        _uiRoot.Add(anchor);
    }

    private static readonly PlayerState[] AutoCycle = { PlayerState.Idle, PlayerState.Walk, PlayerState.Run, PlayerState.Idle };
    private int _autoCycleIdx;

    private bool IsTransitionAllowed(PlayerState from, PlayerState to) => (from, to) switch
    {
        (PlayerState.Idle,   PlayerState.Walk)   => true,
        (PlayerState.Walk,   PlayerState.Idle)   => true,
        (PlayerState.Walk,   PlayerState.Run)    => true,
        (PlayerState.Run,    PlayerState.Walk)   => true,
        (PlayerState.Idle,   PlayerState.Attack) => true,
        (PlayerState.Walk,   PlayerState.Attack) => true,
        (PlayerState.Run,    PlayerState.Attack) => true,
        (PlayerState.Attack, PlayerState.Idle)   => true,
        _ => false,
    };

    private void TryTransition(PlayerState target)
    {
        PlayerState current = _fsm.CurrentState;
        if (current == target) return;

        if (!IsTransitionAllowed(current, target))
        {
            _errorLabel.Text = $"Transición {current}→{target} no permitida";
            return;
        }

        _errorLabel.Text = "";
        _fsm.Transition(target);
    }

    private void OnEnterState(PlayerState state)
    {
        _timeInState = 0f;
        _transitionCount++;

        string entry = $"{_fsm.PreviousState}→{state}";
        if (_historyCount < _history.Length)
        {
            _history[_historyCount++] = entry;
        }
        else
        {
            for (int i = 0; i < _history.Length - 1; i++)
                _history[i] = _history[i + 1];
            _history[_history.Length - 1] = entry;
        }
        _historyDirty = true;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timeInState += dt;
        _fsm.Update(gameTime);

        if (_autoPlay)
        {
            _autoTimer += dt;
            if (_autoTimer >= 1.5f)
            {
                _autoTimer = 0f;
                _autoCycleIdx = (_autoCycleIdx + 1) % AutoCycle.Length;
                TryTransition(AutoCycle[_autoCycleIdx]);
            }
        }

        _currentStateLabel.Text = $"Estado: {_fsm.CurrentState}";
        _timeInStateLabel.Text = $"Tiempo: {_timeInState:F1}s";
        _transitionCountLabel.Text = $"Transiciones: {_transitionCount}";

        if (_historyDirty)
        {
            _historyDirty = false;
            _historySb.Clear();
            for (int i = 0; i < _historyCount; i++)
            {
                if (i > 0) _historySb.Append('\n');
                _historySb.Append(_history[i]);
            }
            _historyLabel.Text = _historySb.ToString();
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

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        DrawArrow(StatePositions[0], StatePositions[1], Color.Gray);  // Idle ↔ Walk
        DrawArrow(StatePositions[1], StatePositions[0], Color.Gray);
        DrawArrow(StatePositions[1], StatePositions[2], Color.Gray);  // Walk ↔ Run
        DrawArrow(StatePositions[2], StatePositions[1], Color.Gray);
        DrawArrow(StatePositions[0], StatePositions[3], new Color(100, 80, 60)); // Idle → Attack
        DrawArrow(StatePositions[1], StatePositions[3], new Color(100, 80, 60)); // Walk → Attack
        DrawArrow(StatePositions[2], StatePositions[3], new Color(100, 80, 60)); // Run → Attack
        DrawArrow(StatePositions[3], StatePositions[0], new Color(60, 80, 100)); // Attack → Idle

        for (int i = 0; i < 4; i++)
        {
            PlayerState state = (PlayerState)i;
            bool active = _fsm.CurrentState == state;
            Color boxColor = active ? Color.Yellow : new Color(50, 60, 80);
            Color textColor = active ? Color.Black : Color.LightGray;
            Vector2 pos = StatePositions[i];

            Core.SpriteBatch.Draw(_pixel,
                new Rectangle((int)pos.X, (int)pos.Y, BoxW, BoxH), boxColor);
            Core.SpriteBatch.DrawString(_font, StateNames[i],
                new Vector2(pos.X + 8, pos.Y + 10), textColor);
        }

        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private void DrawArrow(Vector2 from, Vector2 to, Color color)
    {
        Vector2 fromCenter = from + new Vector2(BoxW / 2f, BoxH / 2f);
        Vector2 toCenter   = to   + new Vector2(BoxW / 2f, BoxH / 2f);
        Vector2 diff = toCenter - fromCenter;
        float len = diff.Length();
        if (len < 1f) return;
        float angle = MathF.Atan2(diff.Y, diff.X);
        Core.SpriteBatch.Draw(_pixel, fromCenter, null, color, angle, Vector2.Zero, new Vector2(len, 1f), SpriteEffects.None, 0f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
