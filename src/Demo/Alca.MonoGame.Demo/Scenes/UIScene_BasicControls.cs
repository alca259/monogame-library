using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Controls.Selection;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 1/10 — demos Button, Label, Checkbox, and Panel.</summary>
public sealed class UIScene_BasicControls : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIFocusManager _focusManager = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private Checkbox _checkbox = null!;
    private Label _statusLabel = null!;
    private int _clickCount;

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 16 };

        var backBtn = new Button(_font, "← Menú");
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        root.Add(backBtn);
        root.Add(new Label { Font = _font, Text = "Scene 1/10: Basic Controls", Color = Color.DimGray });
        root.Add(new Label { Font = _font, Text = "Basic Controls Demo", Color = Color.Yellow, HAlign = HAlign.Center });

        var normalBtn = new Button(_font, "Normal Button") { TabIndex = 0 };
        normalBtn.Clicked += () => { _clickCount++; UpdateStatus(); };
        _focusManager.Register(normalBtn);
        root.Add(normalBtn);

        var disabledBtn = new Button(_font, "Disabled Button") { IsEnabled = false };
        root.Add(disabledBtn);

        _checkbox = new Checkbox(_font, "Toggle me") { Pixel = _pixel, TabIndex = 1 };
        _checkbox.CheckedChanged += _ => UpdateStatus();
        _focusManager.Register(_checkbox);
        root.Add(_checkbox);

        var panel = new Panel
        {
            BackgroundTexture = _pixel,
            BackgroundColor = new Color(60, 80, 60),
            BorderColor = Color.Green,
            BorderThickness = 2,
        };
        panel.Add(new Label { Font = _font, Text = "Soy un Panel" });
        root.Add(panel);

        _statusLabel = new Label { Font = _font, Text = "Clicks: 0 | Checked: False", Color = Color.LightGray };
        root.Add(_statusLabel);

        _uiRoot.Add(root);
    }

    private void UpdateStatus()
    {
        _statusLabel.Text = $"Clicks: {_clickCount} | Checked: {_checkbox.IsChecked}";
    }

    public override void Update(GameTime gameTime)
    {
        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse, _focusManager);
        _focusManager.Update(Core.Input.Keyboard, Core.Input.GamePads[0]);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(20, 20, 30));
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
