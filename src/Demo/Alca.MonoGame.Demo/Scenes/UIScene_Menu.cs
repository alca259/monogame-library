namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Main menu — clickable list of all demo scenes.</summary>
public sealed class UIScene_Menu : Scene
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
        _uiRoot.Add(new Label
        {
            Font = _font,
            Text = "MonoGame UI Demo — Selecciona una escena",
            Color = Color.Yellow,
            HAlign = HAlign.Center,
        });

        var buttonList = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        AddEntry(buttonList, "01. Basic Controls (Button, Label, Checkbox, Panel)", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_BasicControls>()));
        AddEntry(buttonList, "02. Input Text (TextBox, NumericBox, PasswordBox)", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_InputText>()));
        AddEntry(buttonList, "03. TextArea", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_TextArea>()));
        AddEntry(buttonList, "04. Sliders & Progress Bars", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Sliders>()));
        AddEntry(buttonList, "05. Selection (Dropdown, RadioButton)", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Selection>()));
        AddEntry(buttonList, "06. Color Picker (RGB + HSV)", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_ColorPicker>()));
        AddEntry(buttonList, "07. Layout (StackPanel, Flow, Grid, Anchor, Canvas)", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Layout>()));
        AddEntry(buttonList, "08. ScrollView", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_ScrollView>()));
        AddEntry(buttonList, "09. Tooltip", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Tooltip>()));
        AddEntry(buttonList, "10. Focus Manager (Tab navigation)", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Focus>()));
        AddEntry(buttonList, "11. UI Transitions (UITransitionManager)", () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Transitions>()));
        AddEntry(buttonList, "12. ECS Hierarchy Demo", () => Core.SceneManager.RequestChange(Core.GetService<EcsDemoScene>()));
        AddEntry(buttonList, "13. Camera2D (Shake, Zoom, Follow)", () => Core.SceneManager.RequestChange(Core.GetService<Camera2DScene>()));
        AddEntry(buttonList, "14. Physics2D (RigidBody, Colliders)", () => Core.SceneManager.RequestChange(Core.GetService<Physics2DScene>()));
        AddEntry(buttonList, "15. Navigation (NavGrid, A*, NavAgent)", () => Core.SceneManager.RequestChange(Core.GetService<NavigationScene>()));
        AddEntry(buttonList, "16. Audio Basic (AudioController, Mixer)", () => Core.SceneManager.RequestChange(Core.GetService<AudioBasicScene>()));
        AddEntry(buttonList, "19. Input (Actions, ActionMap, Rebinding)", () => Core.SceneManager.RequestChange(Core.GetService<InputScene>()));
        AddEntry(buttonList, "20. Animation (TextureAtlas, AnimStateMachine)", () => Core.SceneManager.RequestChange(Core.GetService<AnimationScene>()));
        AddEntry(buttonList, "22. Particles (ParticleBuilder, Emitter)", () => Core.SceneManager.RequestChange(Core.GetService<ParticlesScene>()));
        AddEntry(buttonList, "31. EventBus (Pub/Sub, ICancellableEvent)", () => Core.SceneManager.RequestChange(Core.GetService<EventBusScene>()));
        AddEntry(buttonList, "32. StateMachine (Generic FSM, Diagram)", () => Core.SceneManager.RequestChange(Core.GetService<StateMachineScene>()));
        AddEntry(buttonList, "33. Timers (TimerManager, GameTimer)", () => Core.SceneManager.RequestChange(Core.GetService<TimersScene>()));
        AddEntry(buttonList, "34. Tweening (EasingCatalog, Curves)", () => Core.SceneManager.RequestChange(Core.GetService<TweeningScene>()));
        AddEntry(buttonList, "41. Platform (PlatformManager, OS info)", () => Core.SceneManager.RequestChange(Core.GetService<PlatformScene>()));

        var scrollView = new ScrollView(Core.GraphicsDevice) { FixedSize = new Vector2(700, 560) };
        scrollView.Add(buttonList);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(scrollView, Anchor.TopCenter, new Vector2(0, 40));
        _uiRoot.Add(anchor);
    }

    private void AddEntry(StackPanel parent, string text, Action onClick)
    {
        var btn = new Button(_font, text)
        {
            BackgroundPixel = _pixel,
            NormalColor = Color.White,
            HoveredColor = Color.LightGray,
            HAlign = HAlign.Left,
        };
        btn.Clicked += onClick;
        parent.Add(btn);
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
