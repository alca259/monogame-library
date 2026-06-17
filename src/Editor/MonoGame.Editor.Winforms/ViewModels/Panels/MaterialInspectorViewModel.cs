namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>
/// ViewModel del inspector de materiales. Relé mínimo: re-expone los eventos del bus
/// al panel, que construye la UI dinámica (campos de shader y propiedades de material).
/// </summary>
public sealed class MaterialInspectorViewModel : ViewModelBase
{
    public event Action<AssetSelectedEvent>? AssetSelected;
    public event Action?                     ProjectOpened;

    protected override void RegisterEvents()
    {
        On<AssetSelectedEvent>(e => AssetSelected?.Invoke(e));
        On<ProjectOpenedEvent>(_ => ProjectOpened?.Invoke());
    }
}
