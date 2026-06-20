using Alca.MonoGame.Kernel.Graphics.Sprites;
using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Overlays;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 20 — TextureAtlas, Animation, and AnimationStateMachine demo.</summary>
public sealed class AnimationScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIOverlayManager _overlayManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private TextureAtlas _atlas = null!;
    private Texture2D _atlasTexture = null!;
    private AnimationStateMachine _stateMachine = null!;
    private Animation[] _animations = null!;

    private bool _paused;
    private Label _stateLabel = null!;
    private Label _speedLabel = null!;

    private static readonly string[] _animNames = { "Idle", "Walk", "Run", "Attack" };

    private const int FrameW  = 64;
    private const int FrameH  = 64;
    private const int FrameCols = 4;
    private const int FrameRows = 4;

    private static readonly Vector2 _spriteDrawPos = new(400, 280);

    protected override void PreInitialize()
    {
        base.PreInitialize();
        _stateMachine = new AnimationStateMachine();
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        _atlasTexture = BuildAtlasTexture(Core.GraphicsDevice);
        _atlas = new TextureAtlas(_atlasTexture);

        _animations = new Animation[_animNames.Length];
        for (int a = 0; a < _animNames.Length; a++)
        {
            var frames = new System.Collections.Generic.List<TextureRegion>(FrameCols);
            for (int f = 0; f < FrameCols; f++)
            {
                TextureRegion region = _atlas.AddRegion($"{_animNames[a]}_{f}", f * FrameW, a * FrameH, FrameW, FrameH);
                frames.Add(region);
            }
            var anim = new Animation(frames, TimeSpan.FromMilliseconds(180))
            {
                Name = _animNames[a],
                IsLooping = true,
                SpeedMultiplier = 1f,
            };
            _animations[a] = anim;
            _atlas.AddAnimation(_animNames[a], anim);
            _stateMachine.Register(_animNames[a], anim);
        }

        _stateMachine.Play(_animNames[0]);
        _uiRoot.OverlayManager = _overlayManager;
        BuildUI();
    }

    private static Texture2D BuildAtlasTexture(GraphicsDevice device)
    {
        int texW = FrameW * FrameCols;
        int texH = FrameH * FrameRows;
        var tex = new Texture2D(device, texW, texH);
        var data = new Color[texW * texH];

        Color[] baseColors = { Color.CornflowerBlue, Color.LimeGreen, Color.Orange, Color.Tomato };

        for (int row = 0; row < FrameRows; row++)
        {
            for (int col = 0; col < FrameCols; col++)
            {
                float bright = 0.55f + col * 0.15f;
                Color c = new Color(
                    (int)(baseColors[row].R * bright),
                    (int)(baseColors[row].G * bright),
                    (int)(baseColors[row].B * bright));

                int ox = col * FrameW;
                int oy = row * FrameH;

                for (int py = 4; py < FrameH - 4; py++)
                {
                    for (int px = 4; px < FrameW - 4; px++)
                        data[(oy + py) * texW + ox + px] = c;
                }
                // Border
                for (int px = 2; px < FrameW - 2; px++)
                {
                    data[(oy + 2) * texW + ox + px] = Color.White;
                    data[(oy + FrameH - 3) * texW + ox + px] = Color.White;
                }
                for (int py = 2; py < FrameH - 2; py++)
                {
                    data[(oy + py) * texW + ox + 2] = Color.White;
                    data[(oy + py) * texW + ox + FrameW - 3] = Color.White;
                }
            }
        }

        tex.SetData(data);
        return tex;
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Animation Demo", Color = Color.Yellow, HAlign = HAlign.Center });

        var dropdown = new Dropdown(_overlayManager)
        {
            Pixel = _pixel,
            Font = _font,
            ScreenHeight = Core.GraphicsDevice.Viewport.Height,
        };
        foreach (string name in _animNames) dropdown.AddItem(name);
        dropdown.SelectionChanged += i => _stateMachine.Play(_animNames[i]);
        controls.Add(new Label { Font = _font, Text = "Estado:", Color = Color.LightGray });
        controls.Add(dropdown);

        var speedSlider = new Slider(_pixel) { MinValue = 0.5f, MaxValue = 4f, Step = 0.1f };
        speedSlider.Value = 1f;
        speedSlider.ValueChanged += v =>
        {
            for (int i = 0; i < _animations.Length; i++)
                _animations[i].SpeedMultiplier = v;
            _speedLabel.Text = $"Velocidad: {v:F1}×";
        };
        controls.Add(new Label { Font = _font, Text = "Velocidad:", Color = Color.LightGray });
        controls.Add(speedSlider);
        _speedLabel = new Label { Font = _font, Text = "Velocidad: 1.0×", Color = Color.White };
        controls.Add(_speedLabel);

        var playPauseBtn = new Button(_font, "Play / Pause") { BackgroundPixel = _pixel };
        playPauseBtn.Clicked += () => _paused = !_paused;
        controls.Add(playPauseBtn);

        var loopCheck = new Checkbox(_font, "Loop") { Pixel = _pixel };
        loopCheck.CheckedChanged += v =>
        {
            for (int i = 0; i < _animations.Length; i++)
                _animations[i].IsLooping = v;
        };
        controls.Add(loopCheck);

        var resetBtn = new Button(_font, "Reset") { BackgroundPixel = _pixel };
        resetBtn.Clicked += () =>
        {
            string? current = _stateMachine.CurrentState;
            if (current != null) _stateMachine.Play(current);
        };
        controls.Add(resetBtn);

        _stateLabel = new Label { Font = _font, Text = "Estado: Idle", Color = Color.LightGreen };
        controls.Add(_stateLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopRight, new Vector2(-10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        if (!_paused) _stateMachine.Update(gameTime);
        _stateLabel.Text = $"Estado: {_stateMachine.CurrentState ?? "—"}  [{(_paused ? "Pausado" : "Play")}]";

        _uiRoot.Update(gameTime);
        _overlayManager.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(15, 15, 25));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        _stateMachine.Draw(Core.SpriteBatch, _spriteDrawPos);
        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _atlasTexture?.Dispose();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
