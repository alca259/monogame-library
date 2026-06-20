using Alca.MonoGame.Kernel.Audio;
using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 16 — AudioController, AudioMixer, and AudioMixerChannel demo.</summary>
public sealed class AudioBasicScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private readonly AudioMixer _mixer = new();
    private SoundEffect? _beep;
    private Song? _theme;
    private bool _sfxLoaded;
    private bool _musicLoaded;

    private Label _channelLabel = null!;
    private readonly System.Text.StringBuilder _channelSb = new(128);
    private bool _channelDirty;

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        try { _beep = Content.Load<SoundEffect>("SFX/beep"); _sfxLoaded = true; }
        catch { _sfxLoaded = false; }

        try { _theme = Content.Load<Song>("Music/theme"); _musicLoaded = true; }
        catch { _musicLoaded = false; }

        BuildUI();
        _channelDirty = true;
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () =>
        {
            MediaPlayer.Stop();
            Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        };
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "Audio Demo — Mixer & Channels", Color = Color.Yellow, HAlign = HAlign.Center });

        if (!_sfxLoaded)
            controls.Add(new Label { Font = _font, Text = "⚠ SFX/beep no encontrado — SFX desactivado", Color = Color.Orange });

        if (!_musicLoaded)
            controls.Add(new Label { Font = _font, Text = "⚠ Music/theme no encontrado — Música desactivada", Color = Color.Orange });

        var sfxBtn = new Button(_font, "Play SFX") { BackgroundPixel = _pixel, IsEnabled = _sfxLoaded };
        sfxBtn.Clicked += () =>
        {
            if (_beep != null)
            {
                float vol = _mixer.Sfx.EffectiveVolume * _mixer.Master.EffectiveVolume;
                Core.Audio.PlaySoundEffect(_beep, Math.Clamp(vol, 0f, 1f));
            }
        };
        controls.Add(sfxBtn);

        var playMusicBtn = new Button(_font, "Play Music") { BackgroundPixel = _pixel, IsEnabled = _musicLoaded };
        playMusicBtn.Clicked += () =>
        {
            if (_theme != null)
                Core.Audio.PlaySong(_theme);
        };
        controls.Add(playMusicBtn);

        var stopMusicBtn = new Button(_font, "Stop Music") { BackgroundPixel = _pixel };
        stopMusicBtn.Clicked += () => MediaPlayer.Stop();
        controls.Add(stopMusicBtn);

        controls.Add(new Label { Font = _font, Text = "Master Vol", Color = Color.LightGray });
        var masterSlider = new Slider(_pixel) { MinValue = 0f, MaxValue = 1f, Step = 0.01f };
        masterSlider.Value = _mixer.Master.Volume;
        masterSlider.ValueChanged += v => { _mixer.Master.Volume = v; _channelDirty = true; };
        controls.Add(masterSlider);

        controls.Add(new Label { Font = _font, Text = "Music Vol", Color = Color.LightGray });
        var musicSlider = new Slider(_pixel) { MinValue = 0f, MaxValue = 1f, Step = 0.01f };
        musicSlider.Value = _mixer.Music.Volume;
        musicSlider.ValueChanged += v =>
        {
            _mixer.Music.Volume = v;
            MediaPlayer.Volume = Math.Clamp(_mixer.Music.EffectiveVolume * _mixer.Master.EffectiveVolume, 0f, 1f);
            _channelDirty = true;
        };
        controls.Add(musicSlider);

        controls.Add(new Label { Font = _font, Text = "SFX Vol", Color = Color.LightGray });
        var sfxSlider = new Slider(_pixel) { MinValue = 0f, MaxValue = 1f, Step = 0.01f };
        sfxSlider.Value = _mixer.Sfx.Volume;
        sfxSlider.ValueChanged += v => { _mixer.Sfx.Volume = v; _channelDirty = true; };
        controls.Add(sfxSlider);

        _channelLabel = new Label { Font = _font, Text = "—", Color = Color.LightGreen };
        controls.Add(_channelLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(20, 20));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        if (_channelDirty)
        {
            _channelDirty = false;
            _channelSb.Clear();
            _channelSb.Append("Master=");
            _channelSb.Append(_mixer.Master.Volume.ToString("F2"));
            _channelSb.Append("  Music=");
            _channelSb.Append(_mixer.Music.Volume.ToString("F2"));
            _channelSb.Append("  SFX=");
            _channelSb.Append(_mixer.Sfx.Volume.ToString("F2"));
            _channelLabel.Text = _channelSb.ToString();
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
            MediaPlayer.Stop();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
