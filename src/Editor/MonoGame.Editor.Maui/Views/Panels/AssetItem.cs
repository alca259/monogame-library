namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>Modelo de fila para la lista de assets del asset browser.</summary>
public sealed class AssetItem
{
    public AssetInfo Info      { get; }
    public string    Icon      { get; }
    public string    TypeLabel { get; }
    public string    SizeLabel { get; }

    public AssetItem(AssetInfo info)
    {
        Info      = info;
        Icon      = GetIcon(info.Type);
        TypeLabel = info.Type.ToString();
        SizeLabel = FormatSize(info.SizeBytes);
    }

    private static string GetIcon(AssetType type) => type switch
    {
        AssetType.Texture   => "🖼",
        AssetType.Audio     => "🔊",
        AssetType.Font      => "F",
        AssetType.TiledMap  => "⊞",
        AssetType.Scene     => "☰",
        AssetType.Prefab    => "⬡",
        AssetType.Script    => "</>",
        AssetType.Sprite    => "✦",
        AssetType.Material  => "◈",
        AssetType.UITheme   => "◧",
        AssetType.Particles => "✧",
        AssetType.Animation => "▶",
        AssetType.InputMap  => "⌨",
        _                   => "·",
    };

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024        => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024} KB",
        _             => $"{bytes / (1024 * 1024)} MB",
    };
}
