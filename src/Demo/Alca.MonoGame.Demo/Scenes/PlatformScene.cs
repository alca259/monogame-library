using Alca.MonoGame.Kernel.Platform;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 41 — Platform information and capabilities demo.</summary>
public sealed class PlatformScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

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

        controls.Add(new Label { Font = _font, Text = "Platform Demo", Color = Color.Yellow, HAlign = HAlign.Center });

        PlatformManager platform = Core.Platform;
        string osInfo = Environment.OSVersion.ToString();
        string dataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        GraphicsAdapter adapter = Core.GraphicsDevice.Adapter;

        controls.Add(new Label { Font = _font, Text = $"Plataforma detectada: {platform.CurrentPlatform}", Color = Color.White });
        controls.Add(new Label { Font = _font, Text = $"¿Desktop?: {platform.IsDesktop}", Color = Color.LightGreen });
        controls.Add(new Label { Font = _font, Text = $"¿Mobile?: {platform.IsMobile}", Color = Color.LightGray });
        controls.Add(new Label { Font = _font, Text = $"¿Console?: {platform.IsConsole}", Color = Color.LightGray });
        controls.Add(new Label { Font = _font, Text = $"Resolución virtual: {platform.VirtualWidth}×{platform.VirtualHeight}", Color = Color.LightBlue });
        controls.Add(new Label { Font = _font, Text = $"Path de datos: {dataPath}", Color = Color.LightGray });
        controls.Add(new Label { Font = _font, Text = $"Versión OS: {osInfo}", Color = Color.LightGray });
        controls.Add(new Label { Font = _font, Text = $"GPU: {adapter?.Description ?? "Desconocido"}", Color = Color.LightGray });

        var infoPanel = new Panel
        {
            BackgroundTexture = _pixel,
            BackgroundColor = new Color(30, 35, 50),
            BorderColor = new Color(60, 70, 100),
            BorderThickness = 1,
        };
        infoPanel.Add(new Label
        {
            Font = _font,
            Text = $"Display: {adapter?.CurrentDisplayMode.Width}×{adapter?.CurrentDisplayMode.Height}",
            Color = Color.DimGray,
        });
        controls.Add(infoPanel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(20, 20));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
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
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
