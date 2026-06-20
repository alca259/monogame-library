using Alca.MonoGame.Kernel.Input;

namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>
/// ViewModel de la pestaña Input Maps: lista los <c>*.input.json</c> del proyecto
/// y permite añadir/quitar acciones y bindings.
/// </summary>
public sealed class InputMapEditorViewModel : ViewModelBase
{
    private readonly List<string>     _filePaths = [];
    private InputEditorModel?         _model;
    private int                       _selectedFileIndex = -1;
    private string?                   _selectedAction;
    private string?                   _selectedBinding;

    public event Action? FileListChanged;
    public event Action? ActionListChanged;
    public event Action? BindingListChanged;

    public IReadOnlyList<string> FileNames     { get; private set; } = [];
    public IReadOnlyList<string> ActionItems   { get; private set; } = [];
    public IReadOnlyList<string> BindingItems  { get; private set; } = [];

    public bool    CanSave          => _model is not null;
    public bool    HasFile          => _selectedFileIndex >= 0;
    public bool    HasAction        => !string.IsNullOrEmpty(_selectedAction);
    public bool    HasBinding       => !string.IsNullOrEmpty(_selectedBinding);

    protected override void RegisterEvents()
    {
        On<ProjectOpenedEvent>(OnProjectOpened);
    }

    protected override void OnAttached()
    {
        if (Context.ActiveProject is { } project)
            OnProjectOpened(new ProjectOpenedEvent(project));
    }

    // ── Project opened ────────────────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _model = null;
        _filePaths.Clear();
        _selectedFileIndex = -1;
        _selectedAction    = null;
        _selectedBinding   = null;
        ActionItems   = [];
        BindingItems  = [];

        List<string> names = [];
        if (e.Project is not null && Directory.Exists(e.Project.ContentPath))
        {
            foreach (string file in Directory.GetFiles(e.Project.ContentPath, "*.input.json",
                         SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                _filePaths.Add(file);
                names.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        FileNames = names;
        FileListChanged?.Invoke();
    }

    // ── Seleción de fichero ───────────────────────────────────────────────────

    public async Task SelectFileAsync(int index)
    {
        if (index < 0 || index >= _filePaths.Count) return;
        _selectedFileIndex = index;
        _model = await InputEditorModel.LoadAsync(_filePaths[index]).ConfigureAwait(true);
        RebuildActionList();
    }

    // ── Listas ────────────────────────────────────────────────────────────────

    private void RebuildActionList()
    {
        _selectedAction  = null;
        _selectedBinding = null;
        List<string> items = [];
        if (_model is not null)
        {
            foreach (InputActionEntry action in _model.Actions)
                items.Add(action.Name);
        }
        ActionItems  = items;
        BindingItems = [];
        ActionListChanged?.Invoke();
        BindingListChanged?.Invoke();
    }

    public void SelectAction(string? name)
    {
        _selectedAction  = name;
        _selectedBinding = null;
        RebuildBindingList();
    }

    private void RebuildBindingList()
    {
        List<string> items = [];
        if (_model is not null && !string.IsNullOrEmpty(_selectedAction))
        {
            InputActionEntry? entry = _model.GetAction(_selectedAction);
            if (entry is not null)
            {
                foreach (InputBindingEntry b in entry.Bindings)
                    items.Add(b.ToDisplayString());
            }
        }
        BindingItems = items;
        BindingListChanged?.Invoke();
    }

    public void SelectBinding(string? display) => _selectedBinding = display;

    // ── CRUD ──────────────────────────────────────────────────────────────────

    public void AddAction(string name)
    {
        if (_model is null || string.IsNullOrWhiteSpace(name)) return;
        _model.AddAction(name.Trim());
        RebuildActionList();
    }

    public void RemoveAction()
    {
        if (_model is null || string.IsNullOrEmpty(_selectedAction)) return;
        _model.RemoveAction(_selectedAction);
        RebuildActionList();
    }

    public void AddBinding(DeviceType device, int code)
    {
        if (_model is null || string.IsNullOrEmpty(_selectedAction)) return;
        _model.AddBinding(_selectedAction, device, code);
        RebuildBindingList();
    }

    public void RemoveBinding()
    {
        if (_model is null || string.IsNullOrEmpty(_selectedAction) || string.IsNullOrEmpty(_selectedBinding)) return;

        InputActionEntry? entry = _model.GetAction(_selectedAction);
        if (entry is null) return;

        foreach (InputBindingEntry b in entry.Bindings)
        {
            if (b.ToDisplayString() == _selectedBinding)
            {
                _model.RemoveBinding(_selectedAction, b.DeviceType, b.Code);
                break;
            }
        }

        RebuildBindingList();
    }

    public async Task SaveAsync()
    {
        if (_model is null) return;
        await _model.SaveAsync().ConfigureAwait(true);
    }
}
