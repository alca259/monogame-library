using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Pestaña Scenes del dock inferior. Lista las escenas del proyecto activo.
/// Fase 0: lista plana de nombres. Fase 6: crear / eliminar / doble clic para cargar.
/// </summary>
public sealed partial class SceneManagerView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;
    private readonly ObservableCollection<string> _items = [];

    private Action<ProjectOpenedEvent>? _onProjectOpened;
    private Action<SceneLoadedEvent>?   _onSceneLoaded;
    private Action<SceneCreatedEvent>?  _onSceneCreated;

    public SceneManagerView()
    {
        InitializeComponent();
        SceneList.ItemsSource = _items;
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
        _onSceneLoaded   = e => MainThread.BeginInvokeOnMainThread(() => OnSceneLoaded(e));
        _onSceneCreated  = e => MainThread.BeginInvokeOnMainThread(() => OnSceneCreated(e));
        _bus.Subscribe(_onProjectOpened);
        _bus.Subscribe(_onSceneLoaded);
        _bus.Subscribe(_onSceneCreated);
    }

    private void Unsubscribe()
    {
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
        if (_onSceneLoaded is not null)   _bus.Unsubscribe(_onSceneLoaded);
        if (_onSceneCreated is not null)  _bus.Unsubscribe(_onSceneCreated);
    }

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _items.Clear();
        ActiveSceneLabel.Text = "No active scene";

        if (e.Project is null)
        {
            SceneCountLabel.Text = "0 scenes";
            return;
        }

        // TODO Fase 6: cargar lista de escenas desde e.Project
        SceneCountLabel.Text = "0 scenes";
    }

    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        ActiveSceneLabel.Text = e.Scene is not null
            ? $"Active: {e.Scene.Name}"
            : "No active scene";
    }

    private void OnSceneCreated(SceneCreatedEvent e)
    {
        if (!_items.Contains(e.Scene.Name))
            _items.Add(e.Scene.Name);

        int count = _items.Count;
        SceneCountLabel.Text = count == 1 ? "1 scene" : $"{count} scenes";
    }

    private void OnSceneSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TODO Fase 6: cargar escena seleccionada → LoadSceneCommand
    }

    private void OnNewSceneClicked(object sender, EventArgs e)
    {
        // TODO Fase 8: abrir NewSceneDialog
    }

    private void OnDeleteSceneClicked(object sender, EventArgs e)
    {
        // TODO Fase 6: DeleteSceneCommand
    }
}
