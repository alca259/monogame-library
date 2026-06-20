using Alca.MonoGame.Kernel.Audio;
using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 17 — SpatialAudioSource, SpatialAudioListener, and AudioZone demo.</summary>
public sealed class AudioSpatialScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly GameWorld _world = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private GameEntity _listenerEntity = null!;
    private GameEntity _sourceEntity = null!;

    private SpatialAudioSource? _audioSource;
    private AudioZone? _audioZone;

    private Label _distLabel = null!;
    private float _maxDistance = 300f;
    private bool _soundLoaded;

    private readonly System.Text.StringBuilder _sb = new(64);

    protected override void PostInitialize()
    {
        base.PostInitialize();

        _world.AudioController = Core.Audio;

        _listenerEntity = _world.CreateEntity("Listener", new Vector2(300, 360));
        _listenerEntity.Add(new SpatialAudioListener { IsMain = true });

        _sourceEntity = _world.CreateEntity("Source", new Vector2(750, 300));
        _audioSource = new SpatialAudioSource { Loop = true };
        _audioZone = new AudioZone { Radius = _maxDistance };
        _sourceEntity.Add(_audioSource);
        _sourceEntity.Add(_audioZone);
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        try
        {
            var sound = Content.Load<SoundEffect>("SFX/ambient");
            if (_audioSource != null) _audioSource.Sound = sound;
            if (_audioZone != null) _audioZone.AmbientSound = sound;
            _soundLoaded = true;
        }
        catch { _soundLoaded = false; }

        BuildUI();
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Scene 17: Audio Spatial Demo", Color = Color.Yellow, HAlign = HAlign.Center });
        controls.Add(new Label { Font = _font, Text = "Listener (azul) sigue al ratón.", Color = Color.LightGray });
        controls.Add(new Label { Font = _font, Text = "Source (naranja) emite sonido en bucle.", Color = Color.LightGray });

        if (!_soundLoaded)
            controls.Add(new Label { Font = _font, Text = "⚠ SFX/ambient no encontrado", Color = Color.Orange });

        var moveBtn = new Button(_font, "Mover Source") { BackgroundPixel = _pixel };
        moveBtn.Clicked += () =>
        {
            _sourceEntity.Transform.Position2d = new Vector2(
                200 + Random.Shared.Next(880),
                100 + Random.Shared.Next(500));
        };
        controls.Add(moveBtn);

        var toggleBtn = new Button(_font, "Toggle Loop") { BackgroundPixel = _pixel };
        toggleBtn.Clicked += () =>
        {
            if (_audioSource != null)
            {
                if (_audioSource.State == SoundState.Playing) _audioSource.Pause();
                else _audioSource.Resume();
            }
        };
        controls.Add(toggleBtn);

        controls.Add(new Label { Font = _font, Text = "Radio máx", Color = Color.LightGray });
        var radiusSlider = new Slider(_pixel) { MinValue = 50f, MaxValue = 600f, Step = 10f };
        radiusSlider.Value = _maxDistance;
        radiusSlider.ValueChanged += v =>
        {
            _maxDistance = v;
            if (_audioZone != null) _audioZone.Radius = v;
        };
        controls.Add(radiusSlider);

        _distLabel = new Label { Font = _font, Text = "Distancia: — px — Vol: —", Color = Color.LightGreen };
        controls.Add(_distLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        var mousePos = Core.Input.Mouse.Position.ToVector2();
        _listenerEntity.Transform.Position2d = mousePos;

        _world.Update(gameTime);

        Vector2 listenerPos = _listenerEntity.Transform.Position2d;
        Vector2 sourcePos = _sourceEntity.Transform.Position2d;
        float dist = Vector2.Distance(listenerPos, sourcePos);
        float vol = Math.Clamp(1f - dist / _maxDistance, 0f, 1f);

        _sb.Clear();
        _sb.Append("Distancia: ");
        _sb.Append(((int)dist).ToString());
        _sb.Append(" px — Vol: ");
        _sb.Append(vol.ToString("F2"));
        _distLabel.Text = _sb.ToString();

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(15, 15, 30));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        Vector2 sourcePos = _sourceEntity.Transform.Position2d;
        Vector2 listenerPos = _listenerEntity.Transform.Position2d;

        DrawCircleOutline(sourcePos, _maxDistance, new Color(255, 140, 0, 50));
        DrawLine2D(listenerPos, sourcePos, Color.Gray * 0.4f);

        Core.SpriteBatch.Draw(_pixel, new Rectangle((int)sourcePos.X - 10, (int)sourcePos.Y - 10, 20, 20), Color.Orange);
        Core.SpriteBatch.Draw(_pixel, new Rectangle((int)listenerPos.X - 10, (int)listenerPos.Y - 10, 20, 20), Color.DeepSkyBlue);

        Core.SpriteBatch.End();
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private void DrawCircleOutline(Vector2 center, float radius, Color color)
    {
        const int Segments = 32;
        float step = MathF.PI * 2f / Segments;
        for (int i = 0; i < Segments; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;
            Vector2 p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
            Vector2 p2 = center + new Vector2(MathF.Cos(a2), MathF.Sin(a2)) * radius;
            DrawLine2D(p1, p2, color);
        }
    }

    private void DrawLine2D(Vector2 a, Vector2 b, Color color)
    {
        Vector2 diff = b - a;
        float len = diff.Length();
        if (len < 0.5f) return;
        float angle = MathF.Atan2(diff.Y, diff.X);
        Core.SpriteBatch.Draw(_pixel, a, null, color, angle, Vector2.Zero, new Vector2(len, 1f), SpriteEffects.None, 0f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
