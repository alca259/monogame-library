using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>ViewModel del diálogo "World Config": física 2D, iluminación, navegación y audio.</summary>
public sealed partial class WorldConfigViewModel : DialogViewModel<EditorWorldConfig>
{
    [ObservableProperty] private bool _usePhysics2D;
    [ObservableProperty] private string _gravityX = "0";
    [ObservableProperty] private string _gravityY = "-9.8";

    [ObservableProperty] private bool _useLighting;
    [ObservableProperty] private string _ambientR = "255";
    [ObservableProperty] private string _ambientG = "255";
    [ObservableProperty] private string _ambientB = "255";
    [ObservableProperty] private string _ambientA = "255";

    [ObservableProperty] private bool _useNavigation;
    [ObservableProperty] private string _navWidth = "64";
    [ObservableProperty] private string _navHeight = "64";
    [ObservableProperty] private string _navCellSize = "32";
    [ObservableProperty] private string _navOriginX = "0";
    [ObservableProperty] private string _navOriginY = "0";

    [ObservableProperty] private bool _useAudio;

    /// <summary>Carga los valores de una configuración existente.</summary>
    public void LoadFrom(EditorWorldConfig cfg)
    {
        UsePhysics2D = cfg.UsePhysics2D;
        GravityX     = cfg.GravityX.ToString(CultureInfo.InvariantCulture);
        GravityY     = cfg.GravityY.ToString(CultureInfo.InvariantCulture);

        UseLighting  = cfg.UseLighting;
        AmbientR     = cfg.AmbientColorRgba[0].ToString();
        AmbientG     = cfg.AmbientColorRgba[1].ToString();
        AmbientB     = cfg.AmbientColorRgba[2].ToString();
        AmbientA     = cfg.AmbientColorRgba[3].ToString();

        UseNavigation = cfg.UseNavigation;
        NavWidth      = cfg.NavGridWidth.ToString();
        NavHeight     = cfg.NavGridHeight.ToString();
        NavCellSize   = cfg.NavGridCellSize.ToString(CultureInfo.InvariantCulture);
        NavOriginX    = cfg.NavGridOriginX.ToString(CultureInfo.InvariantCulture);
        NavOriginY    = cfg.NavGridOriginY.ToString(CultureInfo.InvariantCulture);

        UseAudio = cfg.UseAudio;
    }

    private EditorWorldConfig BuildConfig() => new()
    {
        UsePhysics2D   = UsePhysics2D,
        GravityX       = ParseFloat(GravityX),
        GravityY       = ParseFloat(GravityY),
        UseLighting    = UseLighting,
        AmbientColorRgba =
        [
            ParseInt(AmbientR),
            ParseInt(AmbientG),
            ParseInt(AmbientB),
            ParseInt(AmbientA),
        ],
        UseNavigation   = UseNavigation,
        NavGridWidth    = ParseInt(NavWidth),
        NavGridHeight   = ParseInt(NavHeight),
        NavGridCellSize = ParseFloat(NavCellSize),
        NavGridOriginX  = ParseFloat(NavOriginX),
        NavGridOriginY  = ParseFloat(NavOriginY),
        UseAudio        = UseAudio,
    };

    [RelayCommand]
    private void Submit() => Close(BuildConfig());

    private static float ParseFloat(string? text)
        => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;

    private static int ParseInt(string? text)
        => int.TryParse(text, out int v) ? Math.Clamp(v, 0, 255) : 0;
}
