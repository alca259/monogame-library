using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Panel izquierdo: muestra la jerarquía de entidades de la escena activa.
/// Fase 0: lista plana de nombres. Fase 3: árbol completo con drag-drop y menú contextual.
/// </summary>
public sealed partial class SceneHierarchyView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;
    private readonly ObservableCollection<string> _items = [];

    private Action<SceneLoadedEvent>?        _onSceneLoaded;
    private Action<GameObjectSelectedEvent>? _onObjectSelected;

    public SceneHierarchyView()
    {
        InitializeComponent();
        HierarchyList.ItemsSource = _items;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onSceneLoaded    = e => MainThread.BeginInvokeOnMainThread(() => OnSceneLoaded(e));
        _onObjectSelected = e => MainThread.BeginInvokeOnMainThread(() => OnObjectSelected(e));
        _bus.Subscribe(_onSceneLoaded);
        _bus.Subscribe(_onObjectSelected);
    }

    private void Unsubscribe()
    {
        if (_onSceneLoaded is not null)    _bus.Unsubscribe(_onSceneLoaded);
        if (_onObjectSelected is not null) _bus.Unsubscribe(_onObjectSelected);
    }

    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        _items.Clear();
        if (e.Scene is null)
        {
            CountLabel.Text = "0 entities";
            StatusLabel.Text = "0 objects in scene";
            return;
        }

        foreach (EditorGameObject obj in e.Scene.RootGameObjects)
            _items.Add(obj.Name);

        int count = _items.Count;
        CountLabel.Text = count == 1 ? "1 entity" : $"{count} entities";
        StatusLabel.Text = count == 1 ? "1 object in scene" : $"{count} objects in scene";
    }

    private void OnObjectSelected(GameObjectSelectedEvent e)
    {
        // TODO Fase 3: resaltar ítem seleccionado en la lista y hacer scroll hasta él
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not string name) return;

        var scene = EditorContext.Instance.ActiveScene;
        if (scene is null) return;

        var obj = scene.RootGameObjects.FirstOrDefault(o => o.Name == name);
        if (obj is not null)
            _bus.Publish(new GameObjectSelectedEvent(obj));
    }

    private void OnAddClicked(object sender, EventArgs e)
    {
        // TODO Fase 3: CreateGameObjectCommand
    }

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        // TODO Fase 3: DeleteGameObjectCommand
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // TODO Fase 3: filtrar árbol en tiempo real (sin modificar la estructura real)
    }
}
