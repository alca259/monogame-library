using Alca.MonoGame.Kernel.Graphics.Particles;
using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Overlays;
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Particles;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 22 — ParticleBuilder and ParticleEffect demo.</summary>
public sealed class ParticlesScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIOverlayManager _overlayManager = new();
    private Texture2D _pixel = null!;
    private Texture2D _particleTex = null!;
    private SpriteFont _font = null!;

    private ParticleEffect? _effect;

    private Label _countLabel = null!;
    private int _selectedPreset;
    private float _emissionRate = 80f;
    private float _lifetime = 1.5f;
    private float _speed = 100f;

    private int _prevActiveCount = -1;
    private readonly System.Text.StringBuilder _countSb = new(48);

    private static readonly string[] PresetNames = { "Fire", "Snow", "Explosion", "Sparks" };
    private static readonly Vector2 EmitterPos = new(640, 360);

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _particleTex = new Texture2D(Core.GraphicsDevice, 6, 6);
        var pData = new Color[36];
        for (int i = 0; i < 36; i++) pData[i] = Color.White;
        _particleTex.SetData(pData);

        _font = Content.Load<SpriteFont>("DefaultFont");

        ApplyPreset(_selectedPreset);
        _uiRoot.OverlayManager = _overlayManager;
        BuildUI();
    }

    private void ApplyPreset(int idx)
    {
        _effect?.Dispose();

        var region = new Texture2DRegion(_particleTex, 0, 0, 6, 6);
        int capacity = (int)(_emissionRate * _lifetime * 2f);
        if (capacity < 50) capacity = 50;

        var builder = new ParticleBuilder()
            .WithTextureRegion(region)
            .WithCapacity(capacity)
            .WithLifetime(_lifetime * 0.6f, _lifetime);

        switch (idx)
        {
            case 0: // Fire
                builder.WithSprayProfile(0.4f, _speed)
                       .WithGravity(-80f)
                       .WithColorRange(Color.OrangeRed, Color.Yellow);
                break;
            case 1: // Snow
                builder.WithCircleProfile(400f)
                       .WithGravity(25f)
                       .WithColorRange(Color.White, Color.LightBlue);
                break;
            case 2: // Explosion
                builder.WithCircleProfile(0f)
                       .WithSprayProfile(MathF.PI * 2f, _speed * 2f)
                       .WithGravity(60f)
                       .WithColorRange(Color.Yellow, Color.OrangeRed);
                break;
            case 3: // Sparks
                builder.WithSprayProfile(0.6f, _speed * 1.5f)
                       .WithGravity(120f)
                       .WithColorRange(Color.Yellow, Color.White);
                break;
        }

        _effect = builder.Build();
        _effect.Position = EmitterPos;
        _prevActiveCount = -1;
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Particles Demo", Color = Color.Yellow, HAlign = HAlign.Center });
        controls.Add(new Label { Font = _font, Text = "Click izq: burst en cursor", Color = Color.LightGray });

        var dropdown = new Dropdown(_overlayManager)
        {
            Pixel = _pixel,
            Font = _font,
            ScreenHeight = Core.GraphicsDevice.Viewport.Height,
        };
        foreach (string n in PresetNames) dropdown.AddItem(n);
        dropdown.SelectionChanged += i => { _selectedPreset = i; ApplyPreset(i); };
        controls.Add(new Label { Font = _font, Text = "Preset:", Color = Color.LightGray });
        controls.Add(dropdown);

        var emitSlider = new Slider(_pixel) { MinValue = 10f, MaxValue = 500f, Step = 10f };
        emitSlider.Value = _emissionRate;
        emitSlider.ValueChanged += v => { _emissionRate = v; ApplyPreset(_selectedPreset); };
        controls.Add(new Label { Font = _font, Text = "Emisión/s", Color = Color.LightGray });
        controls.Add(emitSlider);

        var lifeSlider = new Slider(_pixel) { MinValue = 0.5f, MaxValue = 5f, Step = 0.1f };
        lifeSlider.Value = _lifetime;
        lifeSlider.ValueChanged += v => { _lifetime = v; ApplyPreset(_selectedPreset); };
        controls.Add(new Label { Font = _font, Text = "Vida (s)", Color = Color.LightGray });
        controls.Add(lifeSlider);

        var speedSlider = new Slider(_pixel) { MinValue = 10f, MaxValue = 300f, Step = 10f };
        speedSlider.Value = _speed;
        speedSlider.ValueChanged += v => { _speed = v; ApplyPreset(_selectedPreset); };
        controls.Add(new Label { Font = _font, Text = "Velocidad", Color = Color.LightGray });
        controls.Add(speedSlider);

        var burstBtn = new Button(_font, "Burst aquí (×200)") { BackgroundPixel = _pixel };
        burstBtn.Clicked += () => _effect?.Trigger(EmitterPos, 0f);
        controls.Add(burstBtn);

        var clearBtn = new Button(_font, "Limpiar") { BackgroundPixel = _pixel };
        clearBtn.Clicked += () => ApplyPreset(_selectedPreset);
        controls.Add(clearBtn);

        _countLabel = new Label { Font = _font, Text = "Partículas activas: 0", Color = Color.LightGreen };
        controls.Add(_countLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.BottomLeft, new Vector2(10, -10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        _uiRoot.Update(gameTime);
        _overlayManager.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);

        if (!_interactionManager.IsPointerOverUI && Core.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
            _effect?.Trigger(Core.Input.MousePosition, 0f);

        if (_effect is not null)
        {
            _effect.Position = EmitterPos;
            _effect.Update(gameTime);
        }

        int activeCount = _effect?.Emitters is { } emitters ? CountActiveParticles(emitters) : 0;
        if (activeCount != _prevActiveCount)
        {
            _prevActiveCount = activeCount;
            _countSb.Clear();
            _countSb.Append("Partículas activas: ");
            _countSb.Append(activeCount);
            _countLabel.Text = _countSb.ToString();
        }
    }

    private static int CountActiveParticles(System.Collections.Generic.List<ParticleEmitter> emitters)
    {
        int count = 0;
        for (int i = 0; i < emitters.Count; i++)
            count += emitters[i].ActiveParticles;
        return count;
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(10, 10, 20));

        if (_effect is not null)
        {
            Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            global::MonoGame.Extended.Particles.SpriteBatchExtensions.Draw(Core.SpriteBatch, _effect);
            Core.SpriteBatch.End();
        }

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _effect?.Dispose();
            _pixel?.Dispose();
            _particleTex?.Dispose();
        }
        base.Dispose(disposing);
    }
}
