using MauiColor = Microsoft.Maui.Graphics.Color;

namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed partial class RgbaColorPickerDialog : ContentPage
{
    private readonly TaskCompletionSource<MauiColor?> _tcs = new();

    private bool _updating;

    private RgbaColorPickerDialog() => InitializeComponent();

    public static async Task<MauiColor?> ShowAsync(INavigation navigation,
                                                    MauiColor? initial = null)
    {
        var dialog = new RgbaColorPickerDialog();
        if (initial is not null) dialog.LoadColor(initial);
        else dialog.UpdatePreview();
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    private void LoadColor(MauiColor color)
    {
        _updating = true;
        RedSlider.Value   = (int)(color.Red   * 255);
        GreenSlider.Value = (int)(color.Green  * 255);
        BlueSlider.Value  = (int)(color.Blue  * 255);
        AlphaSlider.Value = (int)(color.Alpha * 255);
        _updating = false;
        UpdatePreview();
    }

    private void OnSliderChanged(object sender, ValueChangedEventArgs e)
    {
        if (_updating) return;
        RedLabel.Text   = ((int)RedSlider.Value).ToString();
        GreenLabel.Text = ((int)GreenSlider.Value).ToString();
        BlueLabel.Text  = ((int)BlueSlider.Value).ToString();
        AlphaLabel.Text = ((int)AlphaSlider.Value).ToString();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        int r = (int)RedSlider.Value;
        int g = (int)GreenSlider.Value;
        int b = (int)BlueSlider.Value;
        int a = (int)AlphaSlider.Value;
        ColorPreview.Color = Color.FromRgba(r, g, b, a);
    }

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
        int r = (int)RedSlider.Value;
        int g = (int)GreenSlider.Value;
        int b = (int)BlueSlider.Value;
        int a = (int)AlphaSlider.Value;
        _tcs.TrySetResult(MauiColor.FromRgba(r, g, b, a));
        _ = Navigation.PopModalAsync();
    }
}
