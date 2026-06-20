using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>
/// Scene 7/10 — demos StackPanel V/H, FlowLayoutPanel, GridLayout, AnchorLayout, and Canvas
/// arranged inside an outer GridLayout that fills the screen.
/// </summary>
public sealed class UIScene_Layout : Scene
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
        // Outer GridLayout: 3 cols × 3 rows (title + 2 content rows).
        var outerGrid = new GridLayout();
        outerGrid.ColumnDefinitions.Add(GridTrack.Star(1));
        outerGrid.ColumnDefinitions.Add(GridTrack.Star(1));
        outerGrid.ColumnDefinitions.Add(GridTrack.Star(1));
        outerGrid.RowDefinitions.Add(GridTrack.Fixed(46));
        outerGrid.RowDefinitions.Add(GridTrack.Star(1));
        outerGrid.RowDefinitions.Add(GridTrack.Star(1));

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        var backBtn = new Button(_font, "← Menú");
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        headerRow.Add(backBtn);
        headerRow.Add(new Label { Font = _font, Text = "Scene 7/10: Layout  |  StackPanel V/H · Flow · Grid · Anchor · Canvas", Color = Color.DimGray });
        outerGrid.Add(headerRow);
        outerGrid.SetCell(headerRow, 0, 0, 1, 3);

        // Zone 1 — StackPanel Vertical
        var (zone1, z1c) = CreateZone("StackPanel V", new Color(40, 50, 60));
        var sv = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };
        for (int i = 1; i <= 4; i++)
            sv.Add(new Label { Font = _font, Text = $"Ítem {i}", Color = Color.LightCyan });
        z1c.Add(sv);
        outerGrid.Add(zone1);
        outerGrid.SetCell(zone1, 1, 0);

        // Zone 2 — StackPanel Horizontal
        var (zone2, z2c) = CreateZone("StackPanel H", new Color(50, 40, 60));
        var sh = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        Color[] palette = [Color.Tomato, Color.Gold, Color.LimeGreen, Color.DeepSkyBlue];
        for (int i = 0; i < 4; i++)
            sh.Add(new Label { Font = _font, Text = $"H{i + 1}", Color = palette[i] });
        z2c.Add(sh);
        outerGrid.Add(zone2);
        outerGrid.SetCell(zone2, 1, 1);

        // Zone 3 — FlowLayoutPanel
        var (zone3, z3c) = CreateZone("FlowLayoutPanel", new Color(40, 60, 50));
        var flow = new FlowLayoutPanel { Spacing = 6 };
        for (int i = 1; i <= 8; i++)
            flow.Add(new Button(_font, $"Btn {i}"));
        z3c.Add(flow);
        outerGrid.Add(zone3);
        outerGrid.SetCell(zone3, 1, 2);

        // Zone 4 — GridLayout 3 cols × 2 rows
        var (zone4, z4c) = CreateZone("GridLayout 3×2", new Color(60, 40, 40));
        var innerGrid = new GridLayout();
        innerGrid.ColumnDefinitions.Add(GridTrack.Star(1));
        innerGrid.ColumnDefinitions.Add(GridTrack.Star(1));
        innerGrid.ColumnDefinitions.Add(GridTrack.Star(1));
        innerGrid.RowDefinitions.Add(GridTrack.Star(1));
        innerGrid.RowDefinitions.Add(GridTrack.Star(1));
        for (int r = 0; r < 2; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                var cell = new Label { Font = _font, Text = $"R{r}C{c}", Color = Color.White };
                innerGrid.Add(cell);
                innerGrid.SetCell(cell, r, c);
            }
        }
        z4c.Add(innerGrid);
        outerGrid.Add(zone4);
        outerGrid.SetCell(zone4, 2, 0);

        // Zone 5 — AnchorLayout
        var (zone5, z5c) = CreateZone("AnchorLayout", new Color(40, 40, 60));
        var anchor = new AnchorLayout();
        var tlLbl = new Label { Font = _font, Text = "TL", Color = Color.Yellow };
        var trLbl = new Label { Font = _font, Text = "TR", Color = Color.Yellow };
        var blLbl = new Label { Font = _font, Text = "BL", Color = Color.Yellow };
        var brLbl = new Label { Font = _font, Text = "BR", Color = Color.Yellow };
        var cLbl = new Label { Font = _font, Text = "CENTER", Color = Color.White };
        anchor.Add(tlLbl); anchor.SetAnchor(tlLbl, Anchor.TopLeft);
        anchor.Add(trLbl); anchor.SetAnchor(trLbl, Anchor.TopRight);
        anchor.Add(blLbl); anchor.SetAnchor(blLbl, Anchor.BottomLeft);
        anchor.Add(brLbl); anchor.SetAnchor(brLbl, Anchor.BottomRight);
        anchor.Add(cLbl); anchor.SetAnchor(cLbl, Anchor.Center);
        z5c.Add(anchor);
        outerGrid.Add(zone5);
        outerGrid.SetCell(zone5, 2, 1);

        // Zone 6 — Canvas (manual positioning)
        var (zone6, z6c) = CreateZone("Canvas", new Color(55, 45, 30));
        var canvas = new Canvas();
        var cLabel1 = new Label { Font = _font, Text = "A @ (10,30)", Color = Color.Coral };
        var cLabel2 = new Label { Font = _font, Text = "B @ (120,60)", Color = Color.Aquamarine };
        var cLabel3 = new Label { Font = _font, Text = "C @ (50,100)", Color = Color.Orchid };
        canvas.Add(cLabel1); canvas.SetOffset(cLabel1, new Vector2(10, 30));
        canvas.Add(cLabel2); canvas.SetOffset(cLabel2, new Vector2(120, 60));
        canvas.Add(cLabel3); canvas.SetOffset(cLabel3, new Vector2(50, 100));
        z6c.Add(canvas);
        outerGrid.Add(zone6);
        outerGrid.SetCell(zone6, 2, 2);

        _uiRoot.Add(outerGrid);
    }

    private (Panel panel, StackPanel content) CreateZone(string title, Color bg)
    {
        var content = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        content.Add(new Label { Font = _font, Text = title, Color = Color.LightGray });

        var panel = new Panel
        {
            BackgroundTexture = _pixel,
            BackgroundColor = bg,
            BorderColor = Color.SlateGray,
            BorderThickness = 1,
        };
        panel.Add(content);
        return (panel, content);
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
        Core.GraphicsDevice.Clear(new Color(15, 15, 20));
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
