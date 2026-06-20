using Alca.MonoGame.Kernel.Persistence;
using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 36 — SaveManager with three save slots and ISaveable player data.</summary>
public sealed class PersistenceScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private readonly SaveManager _saveManager = new();
    private readonly PlayerSaveData _saveData = new();

    private readonly Label[] _slotLabels = new Label[3];
    private readonly System.Collections.Generic.List<string> _logLines = new(8);
    private Label _logDisplay = null!;
    private readonly System.Text.StringBuilder _logSb = new(512);
    private readonly System.Text.StringBuilder _sb = new(128);

    private sealed class PlayerSaveData : ISaveable
    {
        public string PlayerName = "Jugador";
        public int Level = 1;
        public int Score = 0;
        public Color FavoriteColor = Color.CornflowerBlue;
        public bool TutorialDone = false;

        public void Save(SaveDataWriter writer)
        {
            writer.Write(PlayerName);
            writer.Write(Level);
            writer.Write(Score);
            writer.Write(FavoriteColor);
            writer.Write(TutorialDone);
        }

        public void Load(SaveDataReader reader)
        {
            PlayerName = reader.ReadString();
            Level = reader.ReadInt();
            Score = reader.ReadInt();
            FavoriteColor = reader.ReadColor();
            TutorialDone = reader.ReadBool();
        }
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
        _ = RefreshSlotLabelsAsync();
    }

    private void BuildUI()
    {
        var root = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 40 };

        // --- Player data column ---
        var dataCol = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        dataCol.Add(backBtn);
        dataCol.Add(new Label { Font = _font, Text = "Datos del jugador", Color = Color.Yellow });

        dataCol.Add(new Label { Font = _font, Text = "Nombre:", Color = Color.LightGray });
        var nameBox = new TextBox(_font, _pixel, Core.Window);
        nameBox.SetText(_saveData.PlayerName);
        nameBox.TextChanged += t => _saveData.PlayerName = t;
        dataCol.Add(nameBox);

        dataCol.Add(new Label { Font = _font, Text = "Nivel (1–100):", Color = Color.LightGray });
        var levelBox = new NumericBox(_font, _pixel, Core.Window) { MinValue = 1, MaxValue = 100 };
        levelBox.SetText(_saveData.Level.ToString());
        levelBox.TextChanged += _ => _saveData.Level = levelBox.IntValue;
        dataCol.Add(levelBox);

        dataCol.Add(new Label { Font = _font, Text = "Puntuación:", Color = Color.LightGray });
        var scoreBox = new NumericBox(_font, _pixel, Core.Window) { MinValue = 0, MaxValue = 999999 };
        scoreBox.SetText(_saveData.Score.ToString());
        scoreBox.TextChanged += _ => _saveData.Score = scoreBox.IntValue;
        dataCol.Add(scoreBox);

        dataCol.Add(new Label { Font = _font, Text = "Tutorial completado:", Color = Color.LightGray });
        var tutChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _saveData.TutorialDone };
        tutChk.CheckedChanged += v => _saveData.TutorialDone = v;
        dataCol.Add(tutChk);

        root.Add(dataCol);

        // --- Slots column ---
        var slotsCol = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        slotsCol.Add(new Label { Font = _font, Text = "Slots de guardado", Color = Color.Yellow });

        for (int i = 0; i < 3; i++)
        {
            int slotIdx = i;
            string slotName = $"slot{i + 1}";

            _slotLabels[i] = new Label { Font = _font, Text = $"Slot {i + 1}: —", Color = Color.LightGray };
            slotsCol.Add(_slotLabels[i]);

            var slotRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            var saveBtn = new Button(_font, "Guardar") { BackgroundPixel = _pixel };
            saveBtn.Clicked += () =>
            {
                _ = SaveSlotAsync(slotName);
                AddLog($"Guardando slot {slotIdx + 1}…");
            };
            slotRow.Add(saveBtn);

            var loadBtn = new Button(_font, "Cargar") { BackgroundPixel = _pixel };
            loadBtn.Clicked += () => _ = LoadSlotAsync(slotName, slotIdx);
            slotRow.Add(loadBtn);

            var delBtn = new Button(_font, "Borrar") { BackgroundPixel = _pixel };
            delBtn.Clicked += () =>
            {
                _saveManager.DeleteSlot(slotName);
                _slotLabels[slotIdx].Text = $"Slot {slotIdx + 1}: Vacío";
                AddLog($"Slot {slotIdx + 1} borrado");
            };
            slotRow.Add(delBtn);
            slotsCol.Add(slotRow);
        }

        root.Add(slotsCol);

        // --- Log column ---
        var logCol = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        logCol.Add(new Label { Font = _font, Text = "Log de operaciones", Color = Color.Yellow });
        _logDisplay = new Label { Font = _font, Text = "(vacío)", Color = Color.LightGreen };
        logCol.Add(_logDisplay);
        var clearLogBtn = new Button(_font, "Limpiar log") { BackgroundPixel = _pixel };
        clearLogBtn.Clicked += () => { _logLines.Clear(); _logDisplay.Text = "(vacío)"; };
        logCol.Add(clearLogBtn);
        root.Add(logCol);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(root, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    private async System.Threading.Tasks.Task SaveSlotAsync(string slotName)
    {
        await _saveManager.SaveAsync(slotName, new ISaveable[] { _saveData });
        await RefreshSlotLabelsAsync();
        AddLog($"Slot guardado: {slotName}");
    }

    private async System.Threading.Tasks.Task LoadSlotAsync(string slotName, int slotIdx)
    {
        bool ok = await _saveManager.LoadAsync(slotName, new ISaveable[] { _saveData });
        AddLog(ok ? $"Slot {slotIdx + 1} cargado: {_saveData.PlayerName} Nv.{_saveData.Level}" : $"Slot {slotIdx + 1}: no encontrado");
    }

    private async System.Threading.Tasks.Task RefreshSlotLabelsAsync()
    {
        var slots = await _saveManager.GetSlotsAsync();
        for (int i = 0; i < 3; i++)
        {
            string slotName = $"slot{i + 1}";
            bool found = false;
            for (int j = 0; j < slots.Count; j++)
            {
                if (slots[j].Name == slotName)
                {
                    _slotLabels[i].Text = $"Slot {i + 1}: {slots[j].Name}";
                    found = true;
                    break;
                }
            }
            if (!found) _slotLabels[i].Text = $"Slot {i + 1}: Vacío";
        }
    }

    private void AddLog(string msg)
    {
        if (_logLines.Count >= 8) _logLines.RemoveAt(0);
        _logLines.Add(msg);
        _logSb.Clear();
        for (int i = 0; i < _logLines.Count; i++)
        {
            _logSb.Append(_logLines[i]);
            if (i < _logLines.Count - 1) _logSb.Append('\n');
        }
        _logDisplay.Text = _logSb.ToString();
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
