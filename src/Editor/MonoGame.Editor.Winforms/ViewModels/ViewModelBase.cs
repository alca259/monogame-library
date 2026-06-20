using CommunityToolkit.Mvvm.ComponentModel;
using MonoGame.Editor.Core;
using MonoGame.Editor.Core.Events;
using MonoGame.Editor.Winforms.Infrastructure;

namespace MonoGame.Editor.Winforms.ViewModels;

/// <summary>
/// Base de todas las ViewModels del editor WinForms. Replica la lógica de
/// <c>ViewModelBase</c> de MAUI, sustituyendo <c>MainThread.BeginInvokeOnMainThread</c>
/// por <see cref="UiDispatcher.Post"/>.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private readonly List<Action> _unsubscribers = [];
    private bool _attached;
    private bool _isFocused;

    /// <summary>
    /// Contexto de foco que representa este panel. Si se sobrescribe, la base mantiene
    /// <see cref="IsFocused"/> sincronizado con <see cref="EditorContext.ActiveFocus"/>.
    /// </summary>
    protected virtual EditorFocusContext? FocusContext => null;

    /// <summary><c>true</c> cuando este panel es el contexto de foco activo.</summary>
    public bool IsFocused
    {
        get => _isFocused;
        private set => SetProperty(ref _isFocused, value);
    }

    /// <summary>Bus de eventos compartido del editor.</summary>
    protected static IEditorEventBus Bus => EditorContext.Instance.EventBus;

    /// <summary>Contexto/estado global del editor.</summary>
    protected static EditorContext Context => EditorContext.Instance;

    /// <summary>
    /// Suscribe un handler a un evento del bus. El handler se ejecuta siempre en el
    /// hilo UI mediante <see cref="UiDispatcher.Post"/> y se desuscribe automáticamente
    /// en <see cref="Detach"/>.
    /// </summary>
    protected void On<TEvent>(Action<TEvent> handler) where TEvent : IEditorEvent
    {
        void Wrapped(TEvent e) => UiDispatcher.Post(() => handler(e));
        Bus.Subscribe<TEvent>(Wrapped);
        _unsubscribers.Add(() => Bus.Unsubscribe<TEvent>(Wrapped));
    }

    /// <summary>Activa la ViewModel: registra eventos del bus. Idempotente.</summary>
    public void Attach()
    {
        if (_attached) return;
        _attached = true;

        if (FocusContext is { } ctx)
        {
            On<FocusChangedEvent>(e => IsFocused = e.NewContext == ctx);
            IsFocused = Context.ActiveFocus == ctx;
        }

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
