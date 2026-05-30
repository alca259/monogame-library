using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Pestaña Assets del dock inferior. Lista plana de assets del proyecto activo.
/// Fase 0: nombres de archivo sin árbol. Fase 5: árbol de carpetas + breadcrumb + filtros.
/// </summary>
public sealed partial class AssetBrowserView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;
    private readonly ObservableCollection<string> _items = [];

    private Action<ProjectOpenedEvent>?  _onProjectOpened;
    private Action<AssetImportedEvent>?  _onAssetImported;

    public AssetBrowserView()
    {
        InitializeComponent();
        AssetList.ItemsSource = _items;
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
        _onAssetImported = e => MainThread.BeginInvokeOnMainThread(() => OnAssetImported(e));
        _bus.Subscribe(_onProjectOpened);
        _bus.Subscribe(_onAssetImported);
    }

    private void Unsubscribe()
    {
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
        if (_onAssetImported is not null)  _bus.Unsubscribe(_onAssetImported);
    }

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _items.Clear();
        AssetPathLabel.Text = string.Empty;

        if (e.Project is null)
        {
            AssetCountLabel.Text = "0 assets";
            return;
        }

        // TODO Fase 5: cargar árbol de assets desde e.Project.ContentDirectory
        AssetCountLabel.Text = "0 assets";
    }

    private void OnAssetImported(AssetImportedEvent e)
    {
        if (!_items.Contains(e.Asset.Name))
            _items.Add(e.Asset.Name);

        int count = _items.Count;
        AssetCountLabel.Text = count == 1 ? "1 asset" : $"{count} assets";
    }

    private void OnFilterChanged(object sender, TextChangedEventArgs e)
    {
        // TODO Fase 5: filtrar lista en tiempo real
    }

    private void OnAssetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not string name) return;
        AssetPathLabel.Text = name;
        // TODO Fase 5: publicar AssetSelectedEvent con el AssetInfo completo
    }
}
