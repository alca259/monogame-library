using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel de la pestaña Input Maps: lista los <c>*.input.json</c> de
/// <see cref="EditorProject.ContentPath"/> y permite añadir/quitar acciones y bindings y
/// guardar.
/// </summary>
public sealed partial class InputMapEditorViewModel : ViewModelBase
{
    private readonly List<string> _filePaths = [];
    private InputEditorModel? _model;

    public ObservableCollection<string> FileNames    { get; } = [];
    public ObservableCollection<string> ActionItems  { get; } = [];
    public ObservableCollection<string> BindingItems { get; } = [];

    [ObservableProperty]
    private int _selectedFileIndex = -1;

    [ObservableProperty]
    private string? _selectedAction;

    [ObservableProperty]
    private string? _selectedBinding;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _canSave;

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
        ActionItems.Clear();
        BindingItems.Clear();
        _filePaths.Clear();
        FileNames.Clear();
        SelectedFileIndex = -1;
        SelectedAction    = null;
        CanSave           = false;

        if (e.Project is null || !Directory.Exists(e.Project.ContentPath)) return;

        foreach (string file in Directory.GetFiles(e.Project.ContentPath, "*.input.json", SearchOption.AllDirectories)
                                         .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            _filePaths.Add(file);
            FileNames.Add(Path.GetFileNameWithoutExtension(file));
        }
    }

    // ── File selection ────────────────────────────────────────────────────────

    partial void OnSelectedFileIndexChanged(int value) => _ = LoadSelectedFileAsync(value);

    private async Task LoadSelectedFileAsync(int idx)
    {
        if (idx < 0 || idx >= _filePaths.Count) return;

        _model = await InputEditorModel.LoadAsync(_filePaths[idx]).ConfigureAwait(true);
        RebuildActionList();
        CanSave = true;
    }

    // ── Lists ─────────────────────────────────────────────────────────────────

    private void RebuildActionList()
    {
        ActionItems.Clear();
        BindingItems.Clear();
        SelectedAction = null;
        if (_model is null) return;

        foreach (InputActionEntry action in _model.Actions)
            ActionItems.Add(action.Name);
    }

    partial void OnSelectedActionChanged(string? value) => RebuildBindingList();

    private void RebuildBindingList()
    {
        BindingItems.Clear();
        if (_model is null || string.IsNullOrEmpty(SelectedAction)) return;

        InputActionEntry? entry = _model.GetAction(SelectedAction);
        if (entry is null) return;

        foreach (InputBindingEntry binding in entry.Bindings)
            BindingItems.Add(binding.ToDisplayString());
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddActionAsync()
    {
        if (_model is null) return;

        string? name = await DialogService.PromptAsync("New Action", "Action name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        _model.AddAction(name);
        RebuildActionList();
    }

    [RelayCommand]
    private void RemoveAction()
    {
        if (_model is null || string.IsNullOrEmpty(SelectedAction)) return;

        _model.RemoveAction(SelectedAction);
        RebuildActionList();
    }

    [RelayCommand]
    private async Task AddBindingAsync()
    {
        if (_model is null || string.IsNullOrEmpty(SelectedAction)) return;

        string? deviceStr = await DialogService.PromptAsync("Add Binding", "Device type (Keyboard / Mouse / Gamepad):");
        if (string.IsNullOrWhiteSpace(deviceStr)) return;
        if (!Enum.TryParse(deviceStr, true, out Alca.MonoGame.Kernel.Input.DeviceType device)) return;

        string? codeStr = await DialogService.PromptAsync("Add Binding", "Key code (integer):", keyboard: Keyboard.Numeric);
        if (!int.TryParse(codeStr, out int code)) return;

        _model.AddBinding(SelectedAction, device, code);
        RebuildBindingList();
    }

    [RelayCommand]
    private void RemoveBinding()
    {
        if (_model is null || string.IsNullOrEmpty(SelectedAction)) return;

        InputActionEntry? entry = _model.GetAction(SelectedAction);
        if (entry is null || SelectedBinding is null) return;

        InputBindingEntry? match = null;
        foreach (InputBindingEntry b in entry.Bindings)
        {
            if (b.ToDisplayString() == SelectedBinding) { match = b; break; }
        }

        if (match is null) return;
        _model.RemoveBinding(SelectedAction, match.Value.DeviceType, match.Value.Code);
        RebuildBindingList();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_model is null) return;
        try
        {
            await _model.SaveAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await DialogService.AlertAsync("Save failed", ex.Message);
        }
    }
}
