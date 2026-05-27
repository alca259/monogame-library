using Alca.MonoGame.Kernel.Physics;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Demo scene showcasing Physics2DWorld with RigidBody2D, BoxCollider2D, and CircleCollider2D.</summary>
public sealed class Physics2DScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly GameWorld _world = new();
    private readonly List<GameEntity> _balls = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;
    private Texture2D _ballTexture = null!;
    private Texture2D _groundTexture = null!;

    private Label _infoLabel = null!;

    private const float Gravity = 500f;
    private const float BallRadius = 15f;
    private const float GroundWidth = 900f;
    private const float GroundHeight = 20f;
    private const int MaxBalls = 20;

    private static readonly Vector2 GroundPosition = new(640, 640);
    private static readonly Vector2 ScreenCenter = new(640, 360);

    protected override void PostInitialize()
    {
        base.PostInitialize();

        _world.PhysicsWorld = new Physics2DWorld(new Vector2(0, Gravity));

        GameEntity ground = _world.CreateEntity("Ground", GroundPosition);
        ground.Add(new RigidBody2D { IsStatic = true });
        ground.Add(new BoxCollider2D { Size = new Vector2(GroundWidth, GroundHeight) });

        GameEntity leftWall = _world.CreateEntity("LeftWall", new Vector2(190, 360));
        leftWall.Add(new RigidBody2D { IsStatic = true });
        leftWall.Add(new BoxCollider2D { Size = new Vector2(20f, 600f) });

        GameEntity rightWall = _world.CreateEntity("RightWall", new Vector2(1090, 360));
        rightWall.Add(new RigidBody2D { IsStatic = true });
        rightWall.Add(new BoxCollider2D { Size = new Vector2(20f, 600f) });
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        _ballTexture = CreateCircleTexture(Core.GraphicsDevice, (int)BallRadius);
        _groundTexture = CreateFilledTexture(Core.GraphicsDevice, (int)GroundWidth, (int)GroundHeight, new Color(80, 100, 80));

        BuildUI();
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Scene 14: Physics2D Demo", Color = Color.DimGray });
        controls.Add(new Label { Font = _font, Text = "Physics2D Demo", Color = Color.Yellow, HAlign = HAlign.Center });
        controls.Add(new Label { Font = _font, Text = "Click: Spawn Ball", Color = Color.LightGray });

        var spawnBtn = new Button(_font, "Spawn Ball") { BackgroundPixel = _pixel };
        spawnBtn.Clicked += () => SpawnBall(ScreenCenter + new Vector2(0, -200));
        controls.Add(spawnBtn);

        var impulseBtn = new Button(_font, "Apply Impulse") { BackgroundPixel = _pixel };
        impulseBtn.Clicked += ApplyImpulseToAll;
        controls.Add(impulseBtn);

        var clearBtn = new Button(_font, "Clear Balls") { BackgroundPixel = _pixel };
        clearBtn.Clicked += ClearBalls;
        controls.Add(clearBtn);

        _infoLabel = new Label { Font = _font, Text = "Balls: 0", Color = Color.LightGray };
        controls.Add(_infoLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    private void SpawnBall(Vector2 position)
    {
        if (_balls.Count >= MaxBalls) return;

        var ball = _world.CreateEntity("Ball", position);
        ball.Add(new SpriteRendererBehaviour(_ballTexture) { Color = RandomBallColor() });
        ball.Add(new RigidBody2D());
        ball.Add(new CircleCollider2D { Radius = BallRadius, Restitution = 0.4f });
        _balls.Add(ball);
    }

    private void ApplyImpulseToAll()
    {
        for (int i = 0; i < _balls.Count; i++)
        {
            var rb = _balls[i].GetComponent<RigidBody2D>();
            rb?.ApplyImpulse(new Vector2(0, -300f));
        }
    }

    private void ClearBalls()
    {
        for (int i = 0; i < _balls.Count; i++)
            _world.Destroy(_balls[i]);
        _balls.Clear();
    }

    public override void Update(GameTime gameTime)
    {
        if (Core.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
        {
            var mousePos = Core.Input.Mouse.Position.ToVector2();
            if (mousePos.X > 200f)
                SpawnBall(mousePos);
        }

        _world.Update(gameTime);

        _infoLabel.Text = $"Balls: {_balls.Count}/{MaxBalls}";

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(20, 20, 30));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        DrawGround();
        DrawWalls();
        _world.Draw(gameTime, Core.SpriteBatch);
        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private void DrawGround()
    {
        Vector2 origin = new(_groundTexture.Width * 0.5f, _groundTexture.Height * 0.5f);
        Core.SpriteBatch.Draw(_groundTexture, GroundPosition, null, Color.White, 0f, origin, Vector2.One, SpriteEffects.None, 0f);
    }

    private void DrawWalls()
    {
        Core.SpriteBatch.Draw(_pixel, new Rectangle(190, 60, 20, 600), new Color(60, 80, 60));
        Core.SpriteBatch.Draw(_pixel, new Rectangle(1090, 60, 20, 600), new Color(60, 80, 60));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pixel?.Dispose();
            _ballTexture?.Dispose();
            _groundTexture?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Color RandomBallColor()
    {
        int h = Random.Shared.Next(360);
        return h switch
        {
            < 60 => Color.Orange,
            < 120 => Color.Yellow,
            < 180 => Color.Cyan,
            < 240 => Color.LightBlue,
            < 300 => Color.Violet,
            _ => Color.Salmon,
        };
    }

    private static Texture2D CreateCircleTexture(GraphicsDevice device, int radius)
    {
        int diameter = radius * 2;
        var texture = new Texture2D(device, diameter, diameter);
        var data = new Color[diameter * diameter];
        float rSq = (radius - 1f) * (radius - 1f);
        float cx = radius - 0.5f;
        float cy = radius - 0.5f;
        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                data[y * diameter + x] = dx * dx + dy * dy <= rSq ? Color.White : Color.Transparent;
            }
        }
        texture.SetData(data);
        return texture;
    }

    private static Texture2D CreateFilledTexture(GraphicsDevice device, int width, int height, Color color)
    {
        var texture = new Texture2D(device, width, height);
        var data = new Color[width * height];
        for (int i = 0; i < data.Length; i++) data[i] = color;
        texture.SetData(data);
        return texture;
    }
}
