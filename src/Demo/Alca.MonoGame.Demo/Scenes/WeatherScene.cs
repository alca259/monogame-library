using System.Text;
using Alca.MonoGame.Kernel.Physics;
using Alca.MonoGame.Kernel.Weather;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>
/// Scene 42 — demonstrates WeatherWorld: 10 predefined types, a custom weather,
/// wind-driven leaf entities, fog overlay, and runtime ModifyProfile.
/// </summary>
public sealed class WeatherScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly GameWorld _world = new();
    private readonly List<GameEntity> _leaves = new(LeafCount);
    private readonly StringBuilder _sb = new(128);

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private Label _weatherLabel = null!;
    private Label _tempLabel = null!;
    private Label _windLabel = null!;
    private Slider _transitionSlider = null!;
    private Slider _tempSpeedSlider = null!;
    private Slider _windSpeedSlider = null!;

    private float _labelTimer;

    private static readonly WeatherTypeId CustomWeatherId = new("radioactive_rain");

    private const int LeafCount = 12;
    private const float LeafStartX = 340f;
    private const float LeafStartY = 180f;
    private const float LeafSpacingX = 150f;
    private const float LeafSpacingY = 220f;
    private const float OffScreenBound = 820f;
    private const float LeftBound = 290f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void PostInitialize()
    {
        base.PostInitialize();

        var weatherWorld = new WeatherWorld();
        weatherWorld.RegisterCustomWeather(CustomWeatherId, BuildCustomWeather());

        _world.PhysicsWorld = new Physics2DWorld(new Vector2(0, 80f));
        _world.WeatherWorld = weatherWorld;

        SpawnLeaves();
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _world.Update(gameTime);

        WeatherWorld? wwSync = _world.WeatherWorld;
        if (wwSync is not null)
        {
            wwSync.TemperatureTransitionSpeed = _tempSpeedSlider.Value;
            wwSync.WindTransitionSpeed        = _windSpeedSlider.Value;
        }

        ResetFallenLeaves();

        _labelTimer += dt;
        if (_labelTimer >= 0.1f)
        {
            _labelTimer = 0f;
            UpdateInfoLabels();
        }

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        WeatherWorld? ww = _world.WeatherWorld;
        Color bg = ww is not null ? ww.ActiveProfile.SkyColor : new Color(20, 20, 30);
        if (bg.A == 0) bg = new Color(20, 20, 30);

        Core.GraphicsDevice.Clear(bg);

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        _world.Draw(gameTime, Core.SpriteBatch);
        DrawFogOverlay(ww);

        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };

        var backBtn = new Button(_font, "<- Menu") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        panel.Add(backBtn);

        panel.Add(new Label { Font = _font, Text = "Scene 42: Weather System", Color = Color.DimGray });
        panel.Add(new Label { Font = _font, Text = "--- Weather Types ---", Color = Color.Yellow });

        AddWeatherButton(panel, "Sunny",       WeatherTypeId.Sunny,        new Color(255, 230, 120));
        AddWeatherButton(panel, "Heat Wave",   WeatherTypeId.HeatWave,     new Color(255, 180, 60));
        AddWeatherButton(panel, "Cloudy",      WeatherTypeId.Cloudy,       new Color(180, 190, 200));
        AddWeatherButton(panel, "Fog",         WeatherTypeId.Fog,          new Color(200, 210, 220));
        AddWeatherButton(panel, "Storm",       WeatherTypeId.Storm,        new Color(120, 140, 200));
        AddWeatherButton(panel, "Thunderstorm",WeatherTypeId.Thunderstorm, new Color(100, 100, 200));
        AddWeatherButton(panel, "Hail Storm",  WeatherTypeId.HailStorm,    new Color(160, 210, 240));
        AddWeatherButton(panel, "Blizzard",    WeatherTypeId.Blizzard,     new Color(220, 230, 255));
        AddWeatherButton(panel, "Cold Snap",   WeatherTypeId.ColdSnap,     new Color(150, 200, 255));
        AddWeatherButton(panel, "Orange Wind", WeatherTypeId.OrangeWind,   new Color(255, 160, 50));

        var customBtn = new Button(_font, "Radioactive Rain") { BackgroundPixel = _pixel, NormalColor = new Color(50, 220, 50) };
        customBtn.Clicked += () => _world.WeatherWorld!.SetWeather(CustomWeatherId, GetTransitionDuration());
        panel.Add(customBtn);

        panel.Add(new Label { Font = _font, Text = "--- Actions ---", Color = Color.Yellow });

        var modifyBtn = new Button(_font, "Boost Wind (+5 km/h)") { BackgroundPixel = _pixel, NormalColor = Color.Orange };
        modifyBtn.Clicked += BoostCurrentProfileWind;
        panel.Add(modifyBtn);

        panel.Add(new Label { Font = _font, Text = "Transition duration (s):", Color = Color.LightGray });
        _transitionSlider = new Slider(_pixel) { MinValue = 0f, MaxValue = 10f, Step = 0.5f };
        _transitionSlider.Value = 3f;
        panel.Add(_transitionSlider);

        panel.Add(new Label { Font = _font, Text = "Temp transition speed:", Color = Color.LightGray });
        _tempSpeedSlider = new Slider(_pixel) { MinValue = 0.1f, MaxValue = 2.0f, Step = 0.1f };
        _tempSpeedSlider.Value = 0.3f;
        panel.Add(_tempSpeedSlider);

        panel.Add(new Label { Font = _font, Text = "Wind transition speed:", Color = Color.LightGray });
        _windSpeedSlider = new Slider(_pixel) { MinValue = 0.1f, MaxValue = 2.0f, Step = 0.1f };
        _windSpeedSlider.Value = 1.0f;
        panel.Add(_windSpeedSlider);

        panel.Add(new Label { Font = _font, Text = "--- Info ---", Color = Color.Yellow });

        _weatherLabel = new Label { Font = _font, Text = "Weather: sunny",   Color = Color.Yellow };
        _tempLabel    = new Label { Font = _font, Text = "Temp: 24.0 C",     Color = new Color(255, 160, 100) };
        _windLabel    = new Label { Font = _font, Text = "Wind: 0.0 km/h",   Color = new Color(120, 200, 255) };
        panel.Add(_weatherLabel);
        panel.Add(_tempLabel);
        panel.Add(_windLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(panel, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    private void AddWeatherButton(StackPanel parent, string text, WeatherTypeId id, Color color)
    {
        var btn = new Button(_font, text) { BackgroundPixel = _pixel, NormalColor = color };
        btn.Clicked += () => _world.WeatherWorld!.SetWeather(id, GetTransitionDuration());
        parent.Add(btn);
    }

    // ── Entity setup ──────────────────────────────────────────────────────────

    private void SpawnLeaves()
    {
        for (int i = 0; i < LeafCount; i++)
        {
            float x = LeafStartX + (i % 6) * LeafSpacingX;
            float y = LeafStartY + (i / 6) * LeafSpacingY;
            var entity = _world.CreateEntity($"Leaf{i}", new Vector2(x, y));
            entity.Transform.LocalScale2d = new Vector2(12f, 12f);
            entity.Add(new SpriteRendererBehaviour(_pixel) { Color = LeafColor(i) });
            entity.Add(new RigidBody2D { Mass = 0.4f, LinearDamping = 1.5f, AngularDamping = 2f });
            entity.Add(new WeatherBehaviour { ReceivesWind = true, WindForceMultiplier = 3f });
            _leaves.Add(entity);
        }
    }

    private void ResetFallenLeaves()
    {
        for (int i = 0; i < _leaves.Count; i++)
        {
            Vector2 pos = _leaves[i].Transform.Position2d;
            if (pos.Y > OffScreenBound || pos.X < LeftBound || pos.X > 1350f)
            {
                int col = i % 6;
                int row = i / 6;
                _leaves[i].Transform.Position2d = new Vector2(
                    LeafStartX + col * LeafSpacingX,
                    LeafStartY + row * LeafSpacingY);

                var rb = _leaves[i].GetComponent<RigidBody2D>();
                if (rb is not null)
                {
                    rb.LinearVelocity = Vector2.Zero;
                    rb.AngularVelocity = 0f;
                }
            }
        }
    }

    // ── Draw helpers ──────────────────────────────────────────────────────────

    private void DrawFogOverlay(WeatherWorld? ww)
    {
        if (ww is null) return;
        float fogDensity = ww.ActiveProfile.FogDensity;
        if (fogDensity <= 0.01f) return;

        Color fogColor = ww.ActiveProfile.FogColor;
        fogColor.A = (byte)(fogDensity * 200f);
        int w = Core.GraphicsDevice.Viewport.Width;
        int h = Core.GraphicsDevice.Viewport.Height;
        Core.SpriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), fogColor);
    }

    // ── Update helpers ────────────────────────────────────────────────────────

    private void UpdateInfoLabels()
    {
        WeatherWorld? ww = _world.WeatherWorld;
        if (ww is null) return;

        _sb.Clear();
        _sb.Append("Weather: ").Append(ww.CurrentWeather.Value);
        if (ww.IsTransitioning)
        {
            _sb.Append(" (");
            AppendFloat(_sb, ww.TransitionProgress * 100f, 0);
            _sb.Append("%)");
        }
        _weatherLabel.Text = _sb.ToString();

        _sb.Clear();
        _sb.Append("Temp: ");
        AppendFloat(_sb, ww.CurrentTemperature, 1);
        _sb.Append(" C");
        _tempLabel.Text = _sb.ToString();

        _sb.Clear();
        _sb.Append("Wind: ");
        AppendFloat(_sb, ww.CurrentWind.SpeedKmh, 1);
        _sb.Append(" km/h");
        _windLabel.Text = _sb.ToString();
    }

    private float GetTransitionDuration() => _transitionSlider?.Value ?? 3f;

    private void BoostCurrentProfileWind()
    {
        WeatherWorld? ww = _world.WeatherWorld;
        if (ww is null) return;

        if (ww.TryGetProfile(ww.CurrentWeather, out WeatherProfile current))
        {
            ww.ModifyProfile(ww.CurrentWeather, current with
            {
                WindSpeedMinKmh = current.WindSpeedMinKmh + 5f,
                WindSpeedMaxKmh = current.WindSpeedMaxKmh + 5f,
            });
        }
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    private static WeatherProfile BuildCustomWeather() => new()
    {
        TemperatureMin    = 22f,
        TemperatureMax    = 28f,
        WindSpeedMinKmh   = 8f,
        WindSpeedMaxKmh   = 16f,
        WindDirection     = new Vector2(-1f, 0.1f),
        WindTurbulence    = 0.6f,
        AmbientColor      = new Color(50, 200, 50),
        AmbientIntensity  = 0.8f,
        SkyColor          = new Color(20, 60, 20),
        FogColor          = Color.Transparent,
        FogDensity        = 0f,
        HasPrecipitation  = true,
        PrecipitationLevel = PrecipitationIntensity.High,
        HasLightning      = false,
        RainVolume        = 0.8f,
        WindVolume        = 0.4f,
        ThunderVolume     = 0f,
        CustomData        = "{\"type\":\"radioactive\",\"glowColor\":\"#00FF00\",\"toxicLevel\":9000}",
    };

    private static void AppendFloat(StringBuilder sb, float value, int decimals)
    {
        if (value < 0f) { sb.Append('-'); value = -value; }
        int intPart = (int)value;
        sb.Append(intPart);
        if (decimals <= 0) return;
        sb.Append('.');
        float frac = value - intPart;
        for (int d = 0; d < decimals; d++)
        {
            frac *= 10f;
            sb.Append((int)frac % 10);
        }
    }

    private static Color LeafColor(int index) => (index % 6) switch
    {
        0 => new Color(150, 200, 80),
        1 => new Color(200, 160, 60),
        2 => new Color(180, 100, 50),
        3 => new Color(100, 180, 80),
        4 => new Color(220, 180, 70),
        _ => new Color(160, 190, 90),
    };
}
