namespace MonoGame.Editor.Maui.Controls;

/// <summary>
/// Sets text selection on the underlying WinUI TextBlock once the MAUI handler is attached.
/// Required because Label.IsTextSelectionEnabled is not available in MAUI 10 on Windows.
/// </summary>
public sealed class TextSelectableBehavior : Behavior<Label>
{
    protected override void OnAttachedTo(Label bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.HandlerChanged += OnHandlerChanged;
    }

    protected override void OnDetachingFrom(Label bindable)
    {
        bindable.HandlerChanged -= OnHandlerChanged;
        base.OnDetachingFrom(bindable);
    }

    private static void OnHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if (sender is Label label
            && label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock tb)
        {
            tb.IsTextSelectionEnabled = true;
        }
#endif
    }
}
