namespace MonoGame.Editor.Maui.Views;

/// <summary>
/// TitleBar personalizado. Muestra «MonoGame Editor — Proyecto — Escena» con indicador ● de dirty.
/// Suscribe directamente al EventBus; vive toda la vida de la aplicación.
/// </summary>
public sealed class TitleBarView : TitleBar
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    private string? _projectName;
    private string? _sceneName;
    private bool _isDirty;

    private readonly Action<SceneDirtyChangedEvent> _onDirtyChanged;
    private readonly Action<ProjectOpenedEvent> _onProjectOpened;
    private readonly Action<SceneLoadedEvent> _onSceneLoaded;

    public TitleBarView()
    {
        BackgroundColor = Color.FromArgb("#1A1A1B");
        ForegroundColor = Color.FromArgb("#E6E6E8");

        _onDirtyChanged = e => MainThread.BeginInvokeOnMainThread(() => { _isDirty = e.IsDirty; Refresh(); });
        _onProjectOpened = e => MainThread.BeginInvokeOnMainThread(() => { _projectName = e.Project?.Name; _sceneName = null; Refresh(); });
        _onSceneLoaded = e => MainThread.BeginInvokeOnMainThread(() => { _sceneName = e.Scene?.Name; Refresh(); });

        _bus.Subscribe(_onDirtyChanged);
        _bus.Subscribe(_onProjectOpened);
        _bus.Subscribe(_onSceneLoaded);

        Refresh();
    }

    private void Refresh()
    {
        string text = "MonoGame Editor";
        if (_projectName is not null) text = $"MonoGame Editor — {_projectName}";
        if (_sceneName is not null) text = $"{text} — {_sceneName}";
        if (_isDirty) text = $"● {text}";
        Title = text;
    }
}
