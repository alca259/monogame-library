namespace MonoGame.Editor.Core.Events;

/// <summary>Bus de eventos desacoplado para la comunicación entre paneles del editor.</summary>
public interface IEditorEventBus
{
    /// <summary>Publica un evento a todos los suscriptores registrados.</summary>
    void Publish<TEvent>(TEvent e) where TEvent : IEditorEvent;

    /// <summary>Suscribe un manejador para recibir eventos de tipo <typeparamref name="TEvent"/>.</summary>
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEditorEvent;

    /// <summary>Elimina un manejador previamente registrado.</summary>
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEditorEvent;
}
