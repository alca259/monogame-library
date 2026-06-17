using System.Text.Json;

namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>
/// ViewModel del editor de sprites: carga y guarda <see cref="EditorSpriteMetadata"/>
/// (.sprite.json) para el asset de tipo sprite seleccionado en el browser.
/// </summary>
public sealed class SpriteInspectorViewModel : ViewModelBase
{
    private string _currentFilePath = string.Empty;

    public event Action? FormUpdated;

    public string  SpriteName    { get; private set; } = "No sprite selected";
    public string  TexturePath   { get; set; }         = string.Empty;
    public int     BorderLeft    { get; set; }
    public int     BorderRight   { get; set; }
    public int     BorderTop     { get; set; }
    public int     BorderBottom  { get; set; }
    public bool    TileEdges     { get; set; }
    public bool    TileCenter    { get; set; }
    public bool    CanSave       { get; private set; }
    public string  StatusText    { get; private set; } = string.Empty;

    protected override void RegisterEvents()
    {
        On<AssetSelectedEvent>(OnAssetSelected);
    }

    private void OnAssetSelected(AssetSelectedEvent e)
    {
        if (e.Asset is null || e.Asset.Type != AssetType.Sprite)
        {
            ClearForm();
            return;
        }

        _currentFilePath = e.Asset.AbsolutePath;
        SpriteName = e.Asset.Name;
        PopulateForm(LoadOrCreate(_currentFilePath));
        CanSave = true;
        FormUpdated?.Invoke();
    }

    private static EditorSpriteMetadata LoadOrCreate(string path)
    {
        if (!File.Exists(path)) return new EditorSpriteMetadata();
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EditorSpriteMetadata>(json) ?? new EditorSpriteMetadata();
        }
        catch { return new EditorSpriteMetadata(); }
    }

    private void PopulateForm(EditorSpriteMetadata meta)
    {
        TexturePath  = meta.TextureRelativePath;
        BorderLeft   = meta.BorderLeft;
        BorderRight  = meta.BorderRight;
        BorderTop    = meta.BorderTop;
        BorderBottom = meta.BorderBottom;
        TileEdges    = meta.TileEdges;
        TileCenter   = meta.TileCenter;
        StatusText   = string.Empty;
    }

    private void ClearForm()
    {
        _currentFilePath = string.Empty;
        SpriteName   = "No sprite selected";
        CanSave      = false;
        TexturePath  = string.Empty;
        BorderLeft   = BorderRight = BorderTop = BorderBottom = 0;
        TileEdges    = TileCenter = false;
        StatusText   = string.Empty;
        FormUpdated?.Invoke();
    }

    public void Save()
    {
        if (!CanSave || string.IsNullOrEmpty(_currentFilePath)) return;

        EditorSpriteMetadata meta = new()
        {
            TextureRelativePath = TexturePath,
            BorderLeft          = BorderLeft,
            BorderRight         = BorderRight,
            BorderTop           = BorderTop,
            BorderBottom        = BorderBottom,
            TileEdges           = TileEdges,
            TileCenter          = TileCenter,
        };

        try
        {
            string json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_currentFilePath, json);
            StatusText = "Saved";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }

        FormUpdated?.Invoke();
    }
}
