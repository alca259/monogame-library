using System.Collections.ObjectModel;
using Alca.MonoGame.Kernel.Input;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Input Maps". Lists *.input.json files in ContentPath; lets the user add/remove
/// actions and bindings and save back.
/// </summary>
public sealed partial class InputMapEditorView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    private readonly ObservableCollection<string> _actionItems  = [];
    private readonly ObservableCollection<string> _bindingItems = [];
    private readonly List<string> _filePaths = [];

    private InputEditorModel? _model;
    private string _selectedAction = string.Empty;

    private Action<ProjectOpenedEvent>? _onProjectOpened;

    public InputMapEditorView()
    {
        InitializeComponent();
        ActionsList.ItemsSource  = _actionItems;
        BindingsList.ItemsSource = _bindingItems;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onProjectOpened = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpened(e));
        _bus.Subscribe(_onProjectOpened);
    }

    private void Unsubscribe()
    {
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
    }

    // ── Project opened ────────────────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _model = null;
        _actionItems.Clear();
        _bindingItems.Clear();
        _filePaths.Clear();
        FilePickerControl.ItemsSource = null;
        SaveButton.IsEnabled = false;
        _selectedAction = string.Empty;

        if (e.Project is null || !Directory.Exists(e.Project.ContentPath)) return;

        List<string> displayNames = [];
        foreach (string file in Directory.GetFiles(e.Project.ContentPath, "*.input.json",
                                                    SearchOption.AllDirectories)
                                         .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            _filePaths.Add(file);
            displayNames.Add(Path.GetFileNameWithoutExtension(file));
        }

        FilePickerControl.ItemsSource = displayNames;
    }

    // ── File selection ────────────────────────────────────────────────────────

    private async void OnFilePicked(object sender, EventArgs e)
    {
        int idx = FilePickerControl.SelectedIndex;
        if (idx < 0 || idx >= _filePaths.Count) return;

        _model = await InputEditorModel.LoadAsync(_filePaths[idx]).ConfigureAwait(true);
        RebuildActionList();
        SaveButton.IsEnabled = true;
    }

    // ── Lists ─────────────────────────────────────────────────────────────────

    private void RebuildActionList()
    {
        _actionItems.Clear();
        _bindingItems.Clear();
        _selectedAction = string.Empty;
        if (_model is null) return;

        foreach (InputActionEntry action in _model.Actions)
            _actionItems.Add(action.Name);
    }

    private void RebuildBindingList()
    {
        _bindingItems.Clear();
        if (_model is null || string.IsNullOrEmpty(_selectedAction)) return;

        InputActionEntry? entry = _model.GetAction(_selectedAction);
        if (entry is null) return;

        foreach (InputBindingEntry binding in entry.Bindings)
            _bindingItems.Add(binding.ToDisplayString());
    }

    private void OnActionSelected(object sender, SelectionChangedEventArgs e)
    {
        _selectedAction = e.CurrentSelection.FirstOrDefault() as string ?? string.Empty;
        RebuildBindingList();
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    private async void OnAddActionClicked(object sender, EventArgs e)
    {
        if (_model is null) return;
        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? name = await page.DisplayPromptAsync("New Action", "Action name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        _model.AddAction(name);
        RebuildActionList();
    }

    private void OnRemoveActionClicked(object sender, EventArgs e)
    {
        if (_model is null || string.IsNullOrEmpty(_selectedAction)) return;

        _model.RemoveAction(_selectedAction);
        RebuildActionList();
    }

    private async void OnAddBindingClicked(object sender, EventArgs e)
    {
        if (_model is null || string.IsNullOrEmpty(_selectedAction)) return;
        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? deviceStr = await page.DisplayPromptAsync(
            "Add Binding",
            "Device type (Keyboard / Mouse / Gamepad):");
        if (string.IsNullOrWhiteSpace(deviceStr)) return;

        if (!Enum.TryParse<DeviceType>(deviceStr, true, out DeviceType device)) return;

        string? codeStr = await page.DisplayPromptAsync(
            "Add Binding",
            "Key code (integer):",
            keyboard: Keyboard.Numeric);
        if (!int.TryParse(codeStr, out int code)) return;

        _model.AddBinding(_selectedAction, device, code);
        RebuildBindingList();
    }

    private void OnRemoveBindingClicked(object sender, EventArgs e)
    {
        if (_model is null || string.IsNullOrEmpty(_selectedAction)) return;

        InputActionEntry? entry = _model.GetAction(_selectedAction);
        if (entry is null) return;

        if (BindingsList.SelectedItem is not string displayStr) return;

        // Find the binding whose display string matches
        InputBindingEntry? match = null;
        foreach (InputBindingEntry b in entry.Bindings)
        {
            if (b.ToDisplayString() == displayStr)
            {
                match = b;
                break;
            }
        }

        if (match is null) return;
        _model.RemoveBinding(_selectedAction, match.Value.DeviceType, match.Value.Code);
        RebuildBindingList();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_model is null) return;
        try
        {
            await _model.SaveAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
                await page.DisplayAlertAsync("Save failed", ex.Message, "OK");
        }
    }
}
