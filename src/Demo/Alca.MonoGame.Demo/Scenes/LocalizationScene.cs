using Alca.MonoGame.Kernel.Localization;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 37 — LocalizationManager with ES/EN/FR language switching demo.</summary>
public sealed class LocalizationScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private LocalizationManager _loc = null!;
    private string _selectedCulture = "es";

    private readonly Label[] _localizedLabels = new Label[8];
    private Label _activeLangLabel = null!;

    private static readonly string[] _labelKeys =
    [
        "titulo", "subtitulo", "descripcion",
        "menu.inicio", "menu.opciones", "menu.salir",
        "msg.bienvenida", "msg.puntuacion"
    ];

    protected override void PreInitialize()
    {
        base.PreInitialize();
        _loc = new LocalizationManager();
        EnsureLocaleFiles();
        _loc.LoadLanguage("es");
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
        RefreshLocalizedLabels();
    }

    private void EnsureLocaleFiles()
    {
        string exeDir = System.AppContext.BaseDirectory;
        string locDir = System.IO.Path.Combine(exeDir, "Content", "Localization");
        System.IO.Directory.CreateDirectory(locDir);

        string esPath = System.IO.Path.Combine(locDir, "es.json");
        if (!System.IO.File.Exists(esPath))
            System.IO.File.WriteAllText(esPath,
                "{\n" +
                "  \"titulo\": \"Demostración de Localización\",\n" +
                "  \"subtitulo\": \"Alca MonoGame Library\",\n" +
                "  \"descripcion\": \"Sistema de cadenas localizadas\",\n" +
                "  \"menu.inicio\": \"Inicio\",\n" +
                "  \"menu.opciones\": \"Opciones\",\n" +
                "  \"menu.salir\": \"Salir\",\n" +
                "  \"msg.bienvenida\": \"¡Bienvenido al juego!\",\n" +
                "  \"msg.puntuacion\": \"Puntuación: {0}\"\n" +
                "}");

        string enPath = System.IO.Path.Combine(locDir, "en.json");
        if (!System.IO.File.Exists(enPath))
            System.IO.File.WriteAllText(enPath,
                "{\n" +
                "  \"titulo\": \"Localization Demo\",\n" +
                "  \"subtitulo\": \"Alca MonoGame Library\",\n" +
                "  \"descripcion\": \"Localized string system\",\n" +
                "  \"menu.inicio\": \"Start\",\n" +
                "  \"menu.opciones\": \"Options\",\n" +
                "  \"menu.salir\": \"Exit\",\n" +
                "  \"msg.bienvenida\": \"Welcome to the game!\",\n" +
                "  \"msg.puntuacion\": \"Score: {0}\"\n" +
                "}");

        string frPath = System.IO.Path.Combine(locDir, "fr.json");
        if (!System.IO.File.Exists(frPath))
            System.IO.File.WriteAllText(frPath,
                "{\n" +
                "  \"titulo\": \"Démo de Localisation\",\n" +
                "  \"subtitulo\": \"Alca MonoGame Library\",\n" +
                "  \"descripcion\": \"Système de chaînes localisées\",\n" +
                "  \"menu.inicio\": \"Démarrer\",\n" +
                "  \"menu.opciones\": \"Options\",\n" +
                "  \"menu.salir\": \"Quitter\",\n" +
                "  \"msg.bienvenida\": \"Bienvenue dans le jeu!\",\n" +
                "  \"msg.puntuacion\": \"Score: {0}\"\n" +
                "}");
    }

    private void BuildUI()
    {
        var root = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 80 };

        // --- Selector column ---
        var selectorCol = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        selectorCol.Add(backBtn);

        selectorCol.Add(new Label { Font = _font, Text = "Localización Demo", Color = Color.Yellow });

        string[] groups = ["es", "en", "fr"];
        string[] groupLabels = ["Español (ES)", "English (EN)", "Français (FR)"];
        var radioGroup = new RadioGroup();
        radioGroup.SelectionChanged += idx => _selectedCulture = groups[idx];

        for (int i = 0; i < groups.Length; i++)
        {
            var rb = new RadioButton(_font, _pixel, groupLabels[i], radioGroup);
            if (i == 0) radioGroup.Select(rb);
            selectorCol.Add(rb);
        }

        var applyBtn = new Button(_font, "Aplicar idioma") { BackgroundPixel = _pixel };
        applyBtn.Clicked += () =>
        {
            _loc.LoadLanguage(_selectedCulture);
            RefreshLocalizedLabels();
        };
        selectorCol.Add(applyBtn);

        _activeLangLabel = new Label { Font = _font, Text = $"Idioma activo: es", Color = Color.LightGreen };
        selectorCol.Add(_activeLangLabel);

        root.Add(selectorCol);

        // --- Localized strings column ---
        var stringsCol = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };
        stringsCol.Add(new Label { Font = _font, Text = "Cadenas localizadas:", Color = Color.Yellow });

        for (int i = 0; i < _labelKeys.Length; i++)
        {
            int idx = i;
            _localizedLabels[i] = new Label { Font = _font, Text = $"{_labelKeys[idx]}: …", Color = Color.White };
            stringsCol.Add(_localizedLabels[i]);
        }

        root.Add(stringsCol);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(root, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    private void RefreshLocalizedLabels()
    {
        for (int i = 0; i < _labelKeys.Length; i++)
            _localizedLabels[i].Text = $"{_labelKeys[i]}: {_loc[_labelKeys[i]].Value}";
        _activeLangLabel.Text = $"Idioma activo: {_loc.CurrentCulture}";
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
