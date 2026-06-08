using CommunityToolkit.Mvvm.ComponentModel;

namespace MonoGame.Editor.Maui.ViewModels;

/// <summary>
/// Base de todas las ViewModels del editor. Expone el <see cref="IEditorEventBus"/>
/// compartido y centraliza el patrón de suscripción/desuscripción a eventos del bus,
/// envolviendo cada handler en <see cref="MainThread"/> para marshalling seguro a UI.
/// </summary>
/// <remarks>
/// El ciclo de vida se controla desde la vista: <see cref="Attach"/> en
/// <c>OnHandlerChanged</c> cuando el handler deja de ser nulo, y <see cref="Detach"/>
/// cuando vuelve a ser nulo. Ambos son idempotentes.
/// </remarks>
public abstract class ViewModelBase : ObservableObject
{
    private readonly List<Action> _unsubscribers = [];
    private bool _attached;

    /// <summary>Bus de eventos compartido del editor.</summary>
    protected static IEditorEventBus Bus => EditorContext.Instance.EventBus;

    /// <summary>Contexto/estado global del editor.</summary>
    protected static EditorContext Context => EditorContext.Instance;

    /// <summary>
    /// Suscribe un handler a un evento del bus. El handler se ejecuta siempre en el
    /// hilo de UI y se desuscribe automáticamente en <see cref="Detach"/>.
    /// </summary>
    protected void On<TEvent>(Action<TEvent> handler) where TEvent : IEditorEvent
    {
        void Wrapped(TEvent e) => MainThread.BeginInvokeOnMainThread(() => handler(e));
        Bus.Subscribe<TEvent>(Wrapped);
        _unsubscribers.Add(() => Bus.Unsubscribe<TEvent>(Wrapped));
    }

    /// <summary>Activa la ViewModel: registra eventos del bus. Idempotente.</summary>
    public void Attach()
    {
        if (_attached) return;
        _attached = true;
        RegisterEvents();
        OnAttached();
    }

    /// <summary>Desactiva la ViewModel: desuscribe todos los eventos. Idempotente.</summary>
    public void Detach()
    {
        if (!_attached) return;
        _attached = false;
        foreach (Action unsubscribe in _unsubscribers)
            unsubscribe();
        _unsubscribers.Clear();
        OnDetached();
    }

    /// <summary>Punto de registro de suscripciones al bus mediante <see cref="On{TEvent}"/>.</summary>
    protected virtual void RegisterEvents() { }

    /// <summary>Hook invocado tras <see cref="Attach"/> (p. ej. carga inicial de estado).</summary>
    protected virtual void OnAttached() { }

    /// <summary>Hook invocado tras <see cref="Detach"/>.</summary>
    protected virtual void OnDetached() { }
}
