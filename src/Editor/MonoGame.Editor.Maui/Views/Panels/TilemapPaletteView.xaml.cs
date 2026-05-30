namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Tilemap". Displays a GraphicsView placeholder grid; a full tileset
/// palette will replace this once tilemap layer events are wired up.
/// Subscribes only to <see cref="ProjectOpenedEvent"/> for now.
/// </summary>
public sealed partial class TilemapPaletteView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    private Action<ProjectOpenedEvent>? _onProjectOpened;

    public TilemapPaletteView()
    {
        InitializeComponent();
        PaletteCanvas.Drawable = new PlaceholderDrawable();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onProjectOpened = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpened(e));
        _bus.Subscribe(_onProjectOpened);
    }

    private void Unsubscribe()
    {
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
    }

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        TilemapInfoLabel.Text          = "No tilemap layer selected";
        PalettePlaceholderLabel.IsVisible = true;
    }

    // ── Placeholder canvas ────────────────────────────────────────────────────

    private sealed class PlaceholderDrawable : IDrawable
    {
        private const float CellSize = 32f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Color.FromArgb("#1E1E20");
            canvas.FillRectangle(dirtyRect);

            canvas.StrokeColor = Color.FromArgb("#2A2A2E");
            canvas.StrokeSize  = 1f;

            for (float x = 0; x < dirtyRect.Width; x += CellSize)
                canvas.DrawLine(x, 0, x, dirtyRect.Height);
            for (float y = 0; y < dirtyRect.Height; y += CellSize)
                canvas.DrawLine(0, y, dirtyRect.Width, y);
        }
    }
}
