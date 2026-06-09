using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel del "Sprite Editor": carga/guarda <see cref="EditorSpriteMetadata"/>
/// (.sprite.json) para el asset de tipo sprite seleccionado en el browser.
/// </summary>
public sealed partial class SpriteInspectorViewModel : ViewModelBase
{
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private string _spriteName = "No sprite selected";

    [ObservableProperty]
    private string _texturePath = string.Empty;

    [ObservableProperty]
    private string _borderLeft = "0";

    [ObservableProperty]
    private string _borderRight = "0";

    [ObservableProperty]
    private string _borderTop = "0";

    [ObservableProperty]
    private string _borderBottom = "0";

    [ObservableProperty]
    private bool _tileEdges;

    [ObservableProperty]
    private bool _tileCenter;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _canSave;

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
        TexturePath = meta.TextureRelativePath;
        BorderLeft = meta.BorderLeft.ToString();
        BorderRight = meta.BorderRight.ToString();
        BorderTop = meta.BorderTop.ToString();
        BorderBottom = meta.BorderBottom.ToString();
        TileEdges = meta.TileEdges;
        TileCenter = meta.TileCenter;
        StatusText = string.Empty;
    }

    private void ClearForm()
    {
        _currentFilePath = string.Empty;
        SpriteName = "No sprite selected";
        CanSave = false;
        TexturePath = string.Empty;
        BorderLeft = "0";
        BorderRight = "0";
        BorderTop = "0";
        BorderBottom = "0";
        TileEdges = false;
        TileCenter = false;
        StatusText = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        if (string.IsNullOrEmpty(_currentFilePath)) return;

        var meta = new EditorSpriteMetadata
        {
            TextureRelativePath = TexturePath,
            BorderLeft = ParseInt(BorderLeft),
            BorderRight = ParseInt(BorderRight),
            BorderTop = ParseInt(BorderTop),
            BorderBottom = ParseInt(BorderBottom),
            TileEdges = TileEdges,
            TileCenter = TileCenter,
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
    }

    private static int ParseInt(string? text) => int.TryParse(text, out int v) ? v : 0;
}
