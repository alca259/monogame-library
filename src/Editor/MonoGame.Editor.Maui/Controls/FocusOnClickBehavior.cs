namespace MonoGame.Editor.Maui.Controls;

/// <summary>
/// Marca el panel como contexto de foco activo (<see cref="EditorContext.SetFocus"/>) cuando el
/// usuario hace clic/tap sobre él. Permite que los atajos de teclado respondan solo en el panel
/// activo. Se aplica en la raíz de cada panel: <c>&lt;controls:FocusOnClickBehavior Context="Assets"/&gt;</c>.
/// </summary>
public sealed class FocusOnClickBehavior : Behavior<View>
{
    /// <summary>Contexto de foco que este panel establece al ser pulsado.</summary>
    public static readonly BindableProperty ContextProperty = BindableProperty.Create(
        nameof(Context),
        typeof(EditorFocusContext),
        typeof(FocusOnClickBehavior),
        EditorFocusContext.Global);

    /// <summary>Contexto de foco que se activa al pulsar el panel.</summary>
    public EditorFocusContext Context
    {
        get => (EditorFocusContext)GetValue(ContextProperty);
        set => SetValue(ContextProperty, value);
    }

    private TapGestureRecognizer? _tap;
    private PointerGestureRecognizer? _pointer;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);

        _pointer = new PointerGestureRecognizer();
        _pointer.PointerPressed += OnActivated;
        bindable.GestureRecognizers.Add(_pointer);

        _tap = new TapGestureRecognizer();
        _tap.Tapped += OnActivated;
        bindable.GestureRecognizers.Add(_tap);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        if (_pointer is not null)
        {
            _pointer.PointerPressed -= OnActivated;
            bindable.GestureRecognizers.Remove(_pointer);
            _pointer = null;
        }
        if (_tap is not null)
        {
            _tap.Tapped -= OnActivated;
            bindable.GestureRecognizers.Remove(_tap);
            _tap = null;
        }

        base.OnDetachingFrom(bindable);
    }

    private void OnActivated(object? sender, EventArgs e)
        => EditorContext.Instance.SetFocus(Context);
}
