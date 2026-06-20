using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 30 — GameEntityPool with ProjectileBehaviour poolable entity demo.</summary>
public sealed class EntityPoolScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private GameWorld _world = null!;
    private GameEntityPool<ProjectileBehaviour>? _pool;
    private int _poolCapacity = 50;
    private int _totalGets;
    private int _totalMisses;

    private bool _showVisuals = true;

    // Track active projectiles for returning to pool (no LINQ; plain list)
    private readonly System.Collections.Generic.List<(GameEntity entity, ProjectileBehaviour behaviour)> _activeProjectiles = new(64);

    private Label _statsLabel = null!;
    private Label _getsMissLabel = null!;
    private readonly System.Text.StringBuilder _sb = new(128);

    private sealed class ProjectileBehaviour : GameBehaviour, IPoolable
    {
        public Vector2 Velocity;
        private float _lifetime;
        private float _maxLifetime = 3f;

        public float LifetimeRemaining => _lifetime;
        public bool IsExpired => _lifetime <= 0f;

        public void Reset()
        {
            Velocity = Vector2.Zero;
            _lifetime = _maxLifetime;
        }

        public override void Update(GameTime gameTime)
        {
            if (_lifetime <= 0f) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _lifetime -= dt;
            Entity.Transform.Position2d += Velocity * dt;
        }
    }

    protected override void PostInitialize()
    {
        base.PostInitialize();
        _world = new GameWorld { AudioController = Core.Audio };
        RebuildPool();
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
    }

    private void RebuildPool()
    {
        _activeProjectiles.Clear();
        _world.Destroy();
        _pool = new GameEntityPool<ProjectileBehaviour>(_world, "Projectile", _poolCapacity);
        _totalGets = 0;
        _totalMisses = 0;
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "EntityPool Demo", Color = Color.Yellow });

        var burstBtn = new Button(_font, "Disparar ráfaga (×10)") { BackgroundPixel = _pixel };
        burstBtn.Clicked += () => SpawnBurst(10);
        controls.Add(burstBtn);

        controls.Add(new Label { Font = _font, Text = "Capacidad pool:", Color = Color.LightGray });
        var capSlider = new Slider(_pixel) { MinValue = 10f, MaxValue = 200f, Step = 10f };
        capSlider.Value = _poolCapacity;
        capSlider.ValueChanged += v =>
        {
            _poolCapacity = (int)v;
            RebuildPool();
        };
        controls.Add(capSlider);

        _statsLabel = new Label { Font = _font, Text = "Pool: 0 activos / 0 reserva / 50 cap", Color = Color.LightGreen };
        _getsMissLabel = new Label { Font = _font, Text = "Gets: 0 | Misses: 0", Color = Color.LightGreen };
        controls.Add(_statsLabel);
        controls.Add(_getsMissLabel);

        controls.Add(new Label { Font = _font, Text = "Mostrar estado visual:", Color = Color.LightGray });
        var visChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _showVisuals };
        visChk.CheckedChanged += v => _showVisuals = v;
        controls.Add(visChk);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopRight, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    private void SpawnBurst(int count)
    {
        if (_pool == null) return;
        int w = Core.GraphicsDevice.Viewport.Width;
        int h = Core.GraphicsDevice.Viewport.Height;
        Vector2 center = new(w / 2f, h / 2f);

        for (int i = 0; i < count; i++)
        {
            if (_pool.AvailableCount == 0) { _totalMisses++; continue; }

            ProjectileBehaviour? behaviour = null;
            var entity = _pool.Get(e =>
            {
                behaviour = e.GetComponent<ProjectileBehaviour>();
            });

            if (behaviour == null) { _totalMisses++; continue; }

            float angle = Random.Shared.NextSingle() * MathHelper.TwoPi;
            float speed = 150f + Random.Shared.NextSingle() * 150f;
            behaviour.Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            entity.Transform.Position2d = center;
            _activeProjectiles.Add((entity, behaviour));
            _totalGets++;
        }
    }

    public override void Update(GameTime gameTime)
    {
        _world.Update(gameTime);

        int w = Core.GraphicsDevice.Viewport.Width;
        int h = Core.GraphicsDevice.Viewport.Height;

        // Return expired or out-of-bounds projectiles (iterate backwards, no LINQ)
        for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
        {
            var (entity, behaviour) = _activeProjectiles[i];
            Vector2 pos = entity.Transform.Position2d;
            bool outOfBounds = pos.X < -50 || pos.X > w + 50 || pos.Y < -50 || pos.Y > h + 50;
            if (behaviour.IsExpired || outOfBounds)
            {
                _pool?.Return(entity);
                _activeProjectiles.RemoveAt(i);
            }
        }

        int active = _activeProjectiles.Count;
        int avail = _pool?.AvailableCount ?? 0;

        _sb.Clear();
        _sb.Append("Pool: ");
        _sb.Append(active);
        _sb.Append(" activos / ");
        _sb.Append(avail);
        _sb.Append(" reserva / ");
        _sb.Append(_poolCapacity);
        _sb.Append(" cap");
        _statsLabel.Text = _sb.ToString();

        _sb.Clear();
        _sb.Append("Gets: ");
        _sb.Append(_totalGets);
        _sb.Append(" | Misses: ");
        _sb.Append(_totalMisses);
        _getsMissLabel.Text = _sb.ToString();

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, w, h);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(10, 10, 20));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        if (_showVisuals)
        {
            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                Vector2 pos = _activeProjectiles[i].entity.Transform.Position2d;
                Core.SpriteBatch.Draw(_pixel, new Rectangle((int)pos.X - 4, (int)pos.Y - 4, 8, 8), Color.LimeGreen);
            }
        }

        Core.SpriteBatch.End();
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
