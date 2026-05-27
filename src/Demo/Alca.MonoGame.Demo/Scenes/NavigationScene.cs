using Alca.MonoGame.Kernel.Navigation;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Demo scene showcasing NavGrid, Pathfinder, and NavAgent with A* pathfinding.</summary>
public sealed class NavigationScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly GameWorld _world = new();
    private readonly NavPath _displayPath = new(512);

    private NavGrid _navGrid = null!;
    private Pathfinder _pathfinder = null!;
    private NavAgent _navAgent = null!;
    private GameEntity _agentEntity = null!;

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;
    private Texture2D _agentTexture = null!;

    private Label _stateLabel = null!;
    private Label _waypointLabel = null!;

    private bool _showGrid = true;
    private bool _obstacleMode;

    private const int GridCols = 20;
    private const int GridRows = 15;
    private const float CellSize = 40f;
    private static readonly Vector2 GridOrigin = new(40, 40);

    protected override void PostInitialize()
    {
        base.PostInitialize();

        _navGrid = new NavGrid(GridCols, GridRows, CellSize, GridOrigin);
        _pathfinder = new Pathfinder(GridCols * GridRows);

        _world.NavGrid = _navGrid;
        _world.Pathfinder = _pathfinder;

        Vector2 startWorld = _navGrid.GridToWorld(1, 1);
        _agentEntity = _world.CreateEntity("Agent", startWorld);
        _agentEntity.Add(new SpriteRendererBehaviour(_agentTexture));
        _navAgent = _agentEntity.AddComponent<NavAgent>();
        _navAgent.Speed = 120f;
        _navAgent.StoppingDistance = 8f;
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        _agentTexture = CreateCircleTexture(Core.GraphicsDevice, 12);

        BuildUI();
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Scene 15: Navigation Demo", Color = Color.DimGray });
        controls.Add(new Label { Font = _font, Text = "Navigation Demo", Color = Color.Yellow, HAlign = HAlign.Center });
        controls.Add(new Label { Font = _font, Text = "Right-click: Set destination", Color = Color.LightGray });
        controls.Add(new Label { Font = _font, Text = "Left-click: Toggle obstacle", Color = Color.LightGray });

        var toggleGridBtn = new Button(_font, "Show/Hide Grid") { BackgroundPixel = _pixel };
        toggleGridBtn.Clicked += () => _showGrid = !_showGrid;
        controls.Add(toggleGridBtn);

        var toggleObstacleBtn = new Button(_font, "Obstacle Mode: OFF") { BackgroundPixel = _pixel };
        _obstacleMode = false;
        toggleObstacleBtn.Clicked += () =>
        {
            _obstacleMode = !_obstacleMode;
        };
        controls.Add(toggleObstacleBtn);

        var recomputeBtn = new Button(_font, "Recompute Path") { BackgroundPixel = _pixel };
        recomputeBtn.Clicked += () =>
        {
            if (_navAgent.HasPath)
                _navAgent.RecomputePath();
        };
        controls.Add(recomputeBtn);

        _stateLabel = new Label { Font = _font, Text = "Agent: Idle", Color = Color.LightGreen };
        controls.Add(_stateLabel);
        _waypointLabel = new Label { Font = _font, Text = "Waypoints: 0", Color = Color.LightGray };
        controls.Add(_waypointLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopRight, new Vector2(-10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);

        if (!_interactionManager.IsPointerOverUI)
        {
            Point mousePixel = Core.Input.Mouse.Position;
            Vector2 mouseWorld = mousePixel.ToVector2();

            if (Core.Input.Mouse.WasButtonJustPressed(MouseButton.Right))
            {
                _navGrid.WorldToGrid(mouseWorld, out int gx, out int gy);
                if (_navGrid.IsInBounds(gx, gy) && _navGrid.IsWalkable(gx, gy))
                {
                    Vector2 dest = _navGrid.GridToWorld(gx, gy);
                    _displayPath.Clear();
                    _pathfinder.FindPath(_navGrid, _agentEntity.Transform.Position2d, dest, _displayPath);
                    _navAgent.SetDestination(dest);
                }
            }

            if (_obstacleMode && Core.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
            {
                _navGrid.WorldToGrid(mouseWorld, out int gx, out int gy);
                if (_navGrid.IsInBounds(gx, gy))
                {
                    bool current = _navGrid.IsWalkable(gx, gy);
                    _navGrid.SetWalkable(gx, gy, !current);
                    _displayPath.Clear();
                }
            }
        }

        _world.Update(gameTime);

        _stateLabel.Text = _navAgent.IsMoving ? "Agent: Moving" : "Agent: Idle";
        _waypointLabel.Text = $"Waypoints: {_displayPath.Count}";
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(15, 20, 35));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (_showGrid)
            DrawGrid();

        DrawPath();
        _world.Draw(gameTime, Core.SpriteBatch);

        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private void DrawGrid()
    {
        int cellPx = (int)CellSize;
        for (int row = 0; row < GridRows; row++)
        {
            for (int col = 0; col < GridCols; col++)
            {
                bool walkable = _navGrid.IsWalkable(col, row);
                Vector2 cellWorld = _navGrid.GridToWorld(col, row);
                int x = (int)cellWorld.X;
                int y = (int)cellWorld.Y;

                Color fill = walkable ? new Color(30, 35, 50) : new Color(80, 40, 40);
                Core.SpriteBatch.Draw(_pixel, new Rectangle(x + 1, y + 1, cellPx - 2, cellPx - 2), fill);
                Core.SpriteBatch.Draw(_pixel, new Rectangle(x, y, cellPx, 1), new Color(50, 55, 70));
                Core.SpriteBatch.Draw(_pixel, new Rectangle(x, y, 1, cellPx), new Color(50, 55, 70));
            }
        }
    }

    private void DrawPath()
    {
        if (_displayPath.IsEmpty) return;

        for (int i = 0; i < _displayPath.Count - 1; i++)
        {
            DrawLine(_displayPath.GetWaypoint(i), _displayPath.GetWaypoint(i + 1), Color.LightGreen);
        }

        for (int i = 0; i < _displayPath.Count; i++)
        {
            Vector2 wp = _displayPath.GetWaypoint(i);
            Core.SpriteBatch.Draw(_pixel, new Rectangle((int)wp.X - 3, (int)wp.Y - 3, 6, 6), Color.Green);
        }
    }

    private void DrawLine(Vector2 from, Vector2 to, Color color)
    {
        Vector2 diff = to - from;
        float length = diff.Length();
        if (length < 0.01f) return;
        float angle = MathF.Atan2(diff.Y, diff.X);
        Core.SpriteBatch.Draw(_pixel, from, null, color, angle, Vector2.Zero, new Vector2(length, 1f), SpriteEffects.None, 0f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pixel?.Dispose();
            _agentTexture?.Dispose();
        }
        base.Dispose(disposing);
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
                data[y * diameter + x] = dx * dx + dy * dy <= rSq ? Color.Cyan : Color.Transparent;
            }
        }
        texture.SetData(data);
        return texture;
    }
}
