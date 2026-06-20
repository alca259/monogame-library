using Alca.MonoGame.Kernel.Navigation.Steering;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 29 — Steering behaviors: Seek, Flee, Arrive, Wander, Separation.</summary>
public sealed class SteeringScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private readonly GameWorld _world = new();

    private GameEntity _targetEntity = null!;
    private GameEntity _seekAgent = null!;
    private GameEntity _fleeAgent = null!;
    private GameEntity _arriveAgent = null!;
    private GameEntity _wanderAgent = null!;
    private GameEntity _separationAgent = null!;

    private SteeringController _seekCtrl = null!;
    private SteeringController _fleeCtrl = null!;
    private SteeringController _arriveCtrl = null!;
    private SteeringController _wanderCtrl = null!;
    private SteeringController _separationCtrl = null!;

    private SeekBehavior _seekBehavior = null!;
    private FleeBehavior _fleeBehavior = null!;
    private ArriveBehavior _arriveBehavior = null!;
    private WanderBehavior _wanderBehavior = null!;
    private SeparationBehavior _separationBehavior = null!;

    private bool _showRadii;
    private bool _leftMouseWasDown;

    protected override void PreInitialize()
    {
        base.PreInitialize();
        _world.AudioController = Core.Audio;

        int w = Core.GraphicsDevice.Viewport.Width;
        int h = Core.GraphicsDevice.Viewport.Height;

        _targetEntity = _world.CreateEntity("Target", new Vector2(w / 2f, h / 2f));

        _seekAgent = _world.CreateEntity("Seek", new Vector2(100, 200));
        _seekBehavior = new SeekBehavior { Target = _targetEntity.Transform.Position2d, MaxSpeed = 150f };
        _seekCtrl = new SteeringController { MaxResultSpeed = 150f };
        _seekCtrl.Add(_seekBehavior);
        _seekAgent.Add(_seekCtrl);

        _fleeAgent = _world.CreateEntity("Flee", new Vector2(800, 400));
        _fleeBehavior = new FleeBehavior { Target = _targetEntity.Transform.Position2d, FleeRadius = 200f, MaxSpeed = 150f };
        _fleeCtrl = new SteeringController { MaxResultSpeed = 150f };
        _fleeCtrl.Add(_fleeBehavior);
        _fleeAgent.Add(_fleeCtrl);

        _arriveAgent = _world.CreateEntity("Arrive", new Vector2(300, 500));
        _arriveBehavior = new ArriveBehavior { Target = _targetEntity.Transform.Position2d, SlowRadius = 80f, MaxSpeed = 150f };
        _arriveCtrl = new SteeringController { MaxResultSpeed = 150f };
        _arriveCtrl.Add(_arriveBehavior);
        _arriveAgent.Add(_arriveCtrl);

        _wanderAgent = _world.CreateEntity("Wander", new Vector2(600, 200));
        _wanderBehavior = new WanderBehavior { WanderRadius = 50f, WanderDistance = 100f, MaxSpeed = 120f };
        _wanderCtrl = new SteeringController { MaxResultSpeed = 120f };
        _wanderCtrl.Add(_wanderBehavior);
        _wanderAgent.Add(_wanderCtrl);

        _separationAgent = _world.CreateEntity("Separation", new Vector2(500, 350));
        _separationBehavior = new SeparationBehavior { SeparationRadius = 80f, MaxSpeed = 150f };
        _separationCtrl = new SteeringController { MaxResultSpeed = 150f };
        _separationCtrl.Add(_separationBehavior);
        _separationAgent.Add(_separationCtrl);
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

        controls.Add(new Label { Font = _font, Text = "Steering Behaviors Demo", Color = Color.Yellow });

        controls.Add(new Label { Font = _font, Text = "MaxSpeed:", Color = Color.LightGray });
        var speedSlider = new Slider(_pixel) { MinValue = 50f, MaxValue = 400f, Step = 10f };
        speedSlider.Value = 150f;
        speedSlider.ValueChanged += v =>
        {
            _seekBehavior.MaxSpeed = v; _seekCtrl.MaxResultSpeed = v;
            _fleeBehavior.MaxSpeed = v; _fleeCtrl.MaxResultSpeed = v;
            _arriveBehavior.MaxSpeed = v; _arriveCtrl.MaxResultSpeed = v;
            _separationBehavior.MaxSpeed = v; _separationCtrl.MaxResultSpeed = v;
        };
        controls.Add(speedSlider);

        controls.Add(new Label { Font = _font, Text = "SlowRadius (Arrive):", Color = Color.LightGray });
        var slowSlider = new Slider(_pixel) { MinValue = 10f, MaxValue = 200f, Step = 5f };
        slowSlider.Value = _arriveBehavior.SlowRadius;
        slowSlider.ValueChanged += v => _arriveBehavior.SlowRadius = v;
        controls.Add(slowSlider);

        controls.Add(new Label { Font = _font, Text = "SeparationRadius:", Color = Color.LightGray });
        var sepSlider = new Slider(_pixel) { MinValue = 20f, MaxValue = 200f, Step = 5f };
        sepSlider.Value = _separationBehavior.SeparationRadius;
        sepSlider.ValueChanged += v => _separationBehavior.SeparationRadius = v;
        controls.Add(sepSlider);

        controls.Add(new Label { Font = _font, Text = "Mostrar radios:", Color = Color.LightGray });
        var radiiChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _showRadii };
        radiiChk.CheckedChanged += v => _showRadii = v;
        controls.Add(radiiChk);

        controls.Add(new Label { Font = _font, Text = "Click izq: mover objetivo", Color = Color.LightGray });

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        MouseState ms = Mouse.GetState();
        bool leftDown = ms.LeftButton == ButtonState.Pressed;
        if (leftDown && !_leftMouseWasDown)
            _targetEntity.Transform.Position2d = ms.Position.ToVector2();
        _leftMouseWasDown = leftDown;

        Vector2 tgt = _targetEntity.Transform.Position2d;
        _seekBehavior.Target = tgt;
        _fleeBehavior.Target = tgt;
        _arriveBehavior.Target = tgt;

        // Fill separation neighbors before world update
        _separationBehavior.Neighbors.Clear();
        _separationBehavior.Neighbors.Add(_seekAgent.Transform.Position2d);
        _separationBehavior.Neighbors.Add(_fleeAgent.Transform.Position2d);
        _separationBehavior.Neighbors.Add(_arriveAgent.Transform.Position2d);
        _separationBehavior.Neighbors.Add(_wanderAgent.Transform.Position2d);

        _world.Update(gameTime);

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(15, 20, 20));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        DrawAgent(_targetEntity, Color.CornflowerBlue, 10);
        DrawAgent(_seekAgent, Color.LimeGreen, 8);
        DrawAgent(_fleeAgent, Color.OrangeRed, 8);
        DrawAgent(_arriveAgent, Color.Cyan, 8);
        DrawAgent(_wanderAgent, Color.Yellow, 8);
        DrawAgent(_separationAgent, Color.Magenta, 8);

        if (_showRadii)
        {
            DrawCircle(_fleeBehavior.Target, _fleeBehavior.FleeRadius, Color.OrangeRed * 0.3f);
            DrawCircle(_arriveBehavior.Target, _arriveBehavior.SlowRadius, Color.Cyan * 0.3f);
            DrawCircle(_separationAgent.Transform.Position2d, _separationBehavior.SeparationRadius, Color.Magenta * 0.3f);
        }

        Core.SpriteBatch.End();
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private void DrawAgent(GameEntity entity, Color color, int radius)
    {
        Vector2 pos = entity.Transform.Position2d;
        Core.SpriteBatch.Draw(_pixel, new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, radius * 2), color);
    }

    private void DrawCircle(Vector2 center, float radius, Color color)
    {
        const int Segments = 24;
        float step = MathHelper.TwoPi / Segments;
        for (int i = 0; i < Segments; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;
            Vector2 p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
            Vector2 p2 = center + new Vector2(MathF.Cos(a2), MathF.Sin(a2)) * radius;
            DrawLine(p1, p2, color);
        }
    }

    private void DrawLine(Vector2 a, Vector2 b, Color color)
    {
        Vector2 d = b - a;
        float len = d.Length();
        if (len < 0.5f) return;
        float angle = MathF.Atan2(d.Y, d.X);
        Core.SpriteBatch.Draw(_pixel, a, null, color, angle, Vector2.Zero, new Vector2(len, 1f), SpriteEffects.None, 0f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
