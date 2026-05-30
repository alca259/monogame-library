namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Panel derecho: muestra las propiedades del objeto seleccionado.
/// Fase 0: cabecera y sección Transform. Fase 4: tarjetas de behaviour dinámicas.
/// </summary>
public sealed partial class InspectorView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;
    private EditorGameObject? _selected;
    private bool _suppressTransformEvents;

    private Action<GameObjectSelectedEvent>? _onObjectSelected;

    private static readonly Color ActiveTabFg   = Color.FromArgb("#d6d6d8");
    private static readonly Color InactiveTabFg = Color.FromArgb("#a7a7ab");

    public InspectorView()
    {
        InitializeComponent();
        SetActiveInspectorTab(InspectorTabBtn);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onObjectSelected = e => MainThread.BeginInvokeOnMainThread(() => OnObjectSelected(e));
        _bus.Subscribe(_onObjectSelected);
    }

    private void Unsubscribe()
    {
        if (_onObjectSelected is not null) _bus.Unsubscribe(_onObjectSelected);
    }

    private void OnObjectSelected(GameObjectSelectedEvent e)
    {
        _selected = e.GameObject;
        RefreshInspector();
    }

    private void RefreshInspector()
    {
        bool hasSelection = _selected is not null;
        NoSelectionLabel.IsVisible = !hasSelection;
        ObjectHeader.IsVisible     = hasSelection;
        TransformSection.IsVisible = hasSelection;
        AddBehaviourBtn.IsVisible  = hasSelection;

        if (_selected is null) return;

        ObjectNameLabel.Text  = _selected.Name;
        ObjectIdLabel.Text    = _selected.Id.ToString()[..8];
        ObjectActiveCheck.IsChecked = _selected.Active;

        _suppressTransformEvents = true;
        PosXEntry.Text  = _selected.Position.X.ToString("F3");
        PosYEntry.Text  = _selected.Position.Y.ToString("F3");
        RotZEntry.Text  = _selected.Rotation.ToString("F3");
        ScaleXEntry.Text = _selected.Scale.X.ToString("F3");
        ScaleYEntry.Text = _selected.Scale.Y.ToString("F3");
        DepthEntry.Text = _selected.PositionZ.ToString("F3");
        _suppressTransformEvents = false;
    }

    #region Tab switching

    private void OnInspectorTabClicked(object sender, EventArgs e)
    {
        SetActiveInspectorTab(InspectorTabBtn);
        InspectorContent.IsVisible = true;
        MaterialContent.IsVisible  = false;
        UIThemeContent.IsVisible   = false;
    }

    private void OnMaterialTabClicked(object sender, EventArgs e)
    {
        SetActiveInspectorTab(MaterialTabBtn);
        InspectorContent.IsVisible = false;
        MaterialContent.IsVisible  = true;
        UIThemeContent.IsVisible   = false;
    }

    private void OnUIThemeTabClicked(object sender, EventArgs e)
    {
        SetActiveInspectorTab(UIThemeTabBtn);
        InspectorContent.IsVisible = false;
        MaterialContent.IsVisible  = false;
        UIThemeContent.IsVisible   = true;
    }

    private void SetActiveInspectorTab(Button active)
    {
        foreach (Button btn in new[] { InspectorTabBtn, MaterialTabBtn, UIThemeTabBtn })
            btn.TextColor = btn == active ? ActiveTabFg : InactiveTabFg;
    }

    #endregion

    #region Transform events

    private void OnObjectActiveChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_selected is null) return;
        // TODO Fase 4: SetPropertyCommand para Active
    }

    private void OnTransformChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTransformEvents || _selected is null) return;
        // TODO Fase 4: SetTransformCommand
    }

    private void OnAddBehaviourTapped(object sender, TappedEventArgs e)
    {
        // TODO Fase 8: abrir AddBehaviourDialog
    }

    #endregion
}
