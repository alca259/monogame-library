using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed partial class RgbaColorPickerDialog : ContentPage
{
    private readonly TaskCompletionSource<XnaColor?> _tcs = new();

    private bool _updating;

    private RgbaColorPickerDialog() => InitializeComponent();

    public static async Task<XnaColor?> ShowAsync(INavigation navigation,
                                                   XnaColor? initial = null)
    {
        var dialog = new RgbaColorPickerDialog();
        if (initial.HasValue) dialog.LoadColor(initial.Value);
        else dialog.UpdatePreview();
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    private void LoadColor(XnaColor color)
    {
        _updating = true;
        RedSlider.Value   = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value  = color.B;
        AlphaSlider.Value = color.A;
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
        _tcs.TrySetResult(new XnaColor(r, g, b, a));
        _ = Navigation.PopModalAsync();
    }
}
