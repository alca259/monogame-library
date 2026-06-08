namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel del "Material Editor". Centraliza la suscripción al <see cref="IEditorEventBus"/>
/// (con marshalling al hilo de UI vía <see cref="ViewModelBase"/>) y la reexpone como eventos.
/// La construcción dinámica de secciones por shader, los getters de formulario y el preview se
/// mantienen en el code-behind por su fuerte acoplamiento a los controles concretos.
/// </summary>
public sealed class MaterialInspectorViewModel : ViewModelBase
{
    /// <summary>Asset seleccionado en el browser (ya marshalado al hilo de UI).</summary>
    public event Action<AssetSelectedEvent>? AssetSelected;

    /// <summary>Proyecto abierto/cerrado (ya marshalado al hilo de UI).</summary>
    public event Action<ProjectOpenedEvent>? ProjectOpened;

    protected override void RegisterEvents()
    {
        On<AssetSelectedEvent>(e => AssetSelected?.Invoke(e));
        On<ProjectOpenedEvent>(e => ProjectOpened?.Invoke(e));
    }
}
