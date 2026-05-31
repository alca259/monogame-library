using System.Text.Json;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Sprite Editor". Loads/saves <see cref="EditorSpriteMetadata"/> (.sprite.json)
/// for the currently selected sprite asset.
/// </summary>
public sealed partial class SpriteInspectorView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    private Action<AssetSelectedEvent>? _onAssetSelected;
    private string _currentFilePath = string.Empty;

    public SpriteInspectorView()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onAssetSelected = e => MainThread.BeginInvokeOnMainThread(() => OnAssetSelected(e));
        _bus.Subscribe(_onAssetSelected);
    }

    private void Unsubscribe()
    {
        if (_onAssetSelected is not null) _bus.Unsubscribe(_onAssetSelected);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnAssetSelected(AssetSelectedEvent e)
    {
        if (e.Asset is null || e.Asset.Type != AssetType.Sprite)
        {
            ClearForm();
            return;
        }

        _currentFilePath     = e.Asset.AbsolutePath;
        SpriteNameLabel.Text = e.Asset.Name;

        EditorSpriteMetadata meta = LoadOrCreate(_currentFilePath);
        PopulateForm(meta);
        SaveButton.IsEnabled = true;
    }

    private static EditorSpriteMetadata LoadOrCreate(string path)
    {
        if (!File.Exists(path)) return new EditorSpriteMetadata();
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EditorSpriteMetadata>(json) ?? new EditorSpriteMetadata();
        }
        catch
        {
            return new EditorSpriteMetadata();
        }
    }

    private void PopulateForm(EditorSpriteMetadata meta)
    {
        TexturePathEntry.Text  = meta.TextureRelativePath;
        BorderLeftEntry.Text   = meta.BorderLeft.ToString();
        BorderRightEntry.Text  = meta.BorderRight.ToString();
        BorderTopEntry.Text    = meta.BorderTop.ToString();
        BorderBottomEntry.Text = meta.BorderBottom.ToString();
        TileEdgesCheck.IsChecked  = meta.TileEdges;
        TileCenterCheck.IsChecked = meta.TileCenter;
        SpriteStatusLabel.Text = string.Empty;
    }

    private void ClearForm()
    {
        _currentFilePath       = string.Empty;
        SpriteNameLabel.Text   = "No sprite selected";
        SaveButton.IsEnabled   = false;
        TexturePathEntry.Text  = string.Empty;
        BorderLeftEntry.Text   = "0";
        BorderRightEntry.Text  = "0";
        BorderTopEntry.Text    = "0";
        BorderBottomEntry.Text = "0";
        TileEdgesCheck.IsChecked  = false;
        TileCenterCheck.IsChecked = false;
        SpriteStatusLabel.Text = string.Empty;
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath)) return;

        var meta = new EditorSpriteMetadata
        {
            TextureRelativePath = TexturePathEntry.Text ?? string.Empty,
            BorderLeft   = ParseInt(BorderLeftEntry.Text),
            BorderRight  = ParseInt(BorderRightEntry.Text),
            BorderTop    = ParseInt(BorderTopEntry.Text),
            BorderBottom = ParseInt(BorderBottomEntry.Text),
            TileEdges    = TileEdgesCheck.IsChecked,
            TileCenter   = TileCenterCheck.IsChecked,
        };

        try
        {
            string json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_currentFilePath, json);
            SpriteStatusLabel.Text = "Saved";
        }
        catch (Exception ex)
        {
            SpriteStatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private static int ParseInt(string? text) =>
        int.TryParse(text, out int v) ? v : 0;
}
