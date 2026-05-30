namespace MonoGame.Editor.Maui.Controls;

/// <summary>
/// Viewport de MonoGame. Fase 0: placeholder visual con fondo oscuro.
/// Fase 1: implementación real con GraphicsDevice, SwapChainRenderTarget y render loop dedicado en hilo propio.
/// </summary>
public sealed class MonoGameView : ContentView
{
    public MonoGameView()
    {
        BackgroundColor = Color.FromArgb("#1d1d1e");
        Content = new Label
        {
            Text = "MonoGame Viewport",
            TextColor = Color.FromArgb("#646468"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontSize = 14,
        };
    }
}
