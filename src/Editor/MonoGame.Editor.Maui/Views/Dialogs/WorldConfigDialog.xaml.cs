using System.Globalization;
using MonoGame.Editor.Core.Models;

namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed partial class WorldConfigDialog : ContentPage
{
    private readonly TaskCompletionSource<EditorWorldConfig?> _tcs = new();

    private WorldConfigDialog() => InitializeComponent();

    public static async Task<EditorWorldConfig?> ShowAsync(INavigation navigation,
                                                            EditorWorldConfig? existing = null)
    {
        var dialog = new WorldConfigDialog();
        if (existing is not null) dialog.LoadFrom(existing);
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    private void LoadFrom(EditorWorldConfig cfg)
    {
        Physics2DCheck.IsChecked  = cfg.UsePhysics2D;
        GravityXEntry.Text        = cfg.GravityX.ToString(CultureInfo.InvariantCulture);
        GravityYEntry.Text        = cfg.GravityY.ToString(CultureInfo.InvariantCulture);
        PhysicsFields.IsVisible   = cfg.UsePhysics2D;

        LightingCheck.IsChecked   = cfg.UseLighting;
        AmbientREntry.Text        = cfg.AmbientColorRgba[0].ToString();
        AmbientGEntry.Text        = cfg.AmbientColorRgba[1].ToString();
        AmbientBEntry.Text        = cfg.AmbientColorRgba[2].ToString();
        AmbientAEntry.Text        = cfg.AmbientColorRgba[3].ToString();
        LightingFields.IsVisible  = cfg.UseLighting;

        NavigationCheck.IsChecked = cfg.UseNavigation;
        NavWidthEntry.Text        = cfg.NavGridWidth.ToString();
        NavHeightEntry.Text       = cfg.NavGridHeight.ToString();
        NavCellSizeEntry.Text     = cfg.NavGridCellSize.ToString(CultureInfo.InvariantCulture);
        NavOriginXEntry.Text      = cfg.NavGridOriginX.ToString(CultureInfo.InvariantCulture);
        NavOriginYEntry.Text      = cfg.NavGridOriginY.ToString(CultureInfo.InvariantCulture);
        NavigationFields.IsVisible = cfg.UseNavigation;

        AudioCheck.IsChecked = cfg.UseAudio;
    }

    private EditorWorldConfig BuildConfig() => new()
    {
        UsePhysics2D   = Physics2DCheck.IsChecked,
        GravityX       = ParseFloat(GravityXEntry.Text),
        GravityY       = ParseFloat(GravityYEntry.Text),
        UseLighting    = LightingCheck.IsChecked,
        AmbientColorRgba = [
            ParseInt(AmbientREntry.Text),
            ParseInt(AmbientGEntry.Text),
            ParseInt(AmbientBEntry.Text),
            ParseInt(AmbientAEntry.Text),
        ],
        UseNavigation  = NavigationCheck.IsChecked,
        NavGridWidth   = ParseInt(NavWidthEntry.Text),
        NavGridHeight  = ParseInt(NavHeightEntry.Text),
        NavGridCellSize = ParseFloat(NavCellSizeEntry.Text),
        NavGridOriginX  = ParseFloat(NavOriginXEntry.Text),
        NavGridOriginY  = ParseFloat(NavOriginYEntry.Text),
        UseAudio = AudioCheck.IsChecked,
    };

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnCancel(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        _ = Navigation.PopModalAsync();
    }

    private void OnSubmit(object sender, EventArgs e)
    {
        _tcs.TrySetResult(BuildConfig());
        _ = Navigation.PopModalAsync();
    }

    private void OnPhysics2DToggled(object sender, CheckedChangedEventArgs e)
        => PhysicsFields.IsVisible = e.Value;

    private void OnLightingToggled(object sender, CheckedChangedEventArgs e)
        => LightingFields.IsVisible = e.Value;

    private void OnNavigationToggled(object sender, CheckedChangedEventArgs e)
        => NavigationFields.IsVisible = e.Value;

    private static float ParseFloat(string? text)
        => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;

    private static int ParseInt(string? text)
        => int.TryParse(text, out int v) ? Math.Clamp(v, 0, 255) : 0;
}
