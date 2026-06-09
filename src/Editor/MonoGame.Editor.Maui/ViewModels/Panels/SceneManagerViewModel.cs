using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGame.Editor.Maui.Views.Panels;
using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel de la pestaña Scenes del dock: lista las escenas del proyecto, permite
/// cargar (doble clic), crear, renombrar y eliminar, y refleja el estado dirty (●).
/// </summary>
public sealed partial class SceneManagerViewModel : ViewModelBase
{
    private string _scenesPath = string.Empty;
    private string _activeScenePath = string.Empty;

    protected override EditorFocusContext? FocusContext => EditorFocusContext.Scenes;

    public ObservableCollection<SceneItem> Items { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenameSceneCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSceneCommand))]
    private SceneItem? _selectedScene;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewSceneCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameSceneCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSceneCommand))]
    private bool _hasProject;

    [ObservableProperty]
    private string _sceneCountText = "0 scenes";

    [ObservableProperty]
    private string _activeSceneText = "No active scene";

    protected override void RegisterEvents()
    {
        On<ProjectOpenedEvent>(OnProjectOpened);
        On<SceneLoadedEvent>(OnSceneLoaded);
        On<SceneCreatedEvent>(OnSceneCreated);
        On<SceneDirtyChangedEvent>(OnSceneDirtyChanged);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        Items.Clear();
        SelectedScene = null;
        _activeScenePath = string.Empty;
        ActiveSceneText = "No active scene";

        HasProject = e.Project is not null;

        if (e.Project is null)
        {
            _scenesPath = string.Empty;
            SceneCountText = "0 scenes";
            return;
        }

        _scenesPath = e.Project.ScenesPath;

        if (!Directory.Exists(_scenesPath))
        {
            SceneCountText = "0 scenes";
            return;
        }

        foreach (string file in Directory.GetFiles(_scenesPath, "*.scene.json")
                                         .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            Items.Add(new SceneItem(file, LoadSceneAsync));

        UpdateSceneCount();
    }

    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        ActiveSceneText = e.Scene is not null
            ? $"Active: {e.Scene.Name}"
            : "No active scene";

        if (e.Scene is null)
        {
            _activeScenePath = string.Empty;
            return;
        }

        SceneItem? match = null;
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].Name == e.Scene.Name)
            {
                match = Items[i];
                break;
            }
        }
        _activeScenePath = match?.FilePath ?? string.Empty;
    }

    private void OnSceneCreated(SceneCreatedEvent e)
    {
        if (string.IsNullOrEmpty(_scenesPath)) return;

        string filePath = Path.Combine(_scenesPath, $"{e.Scene.Name}.scene.json");
        if (Items.Any(i => i.FilePath == filePath)) return;

        Items.Add(new SceneItem(filePath, LoadSceneAsync));
        UpdateSceneCount();
    }

    private void OnSceneDirtyChanged(SceneDirtyChangedEvent e)
    {
        if (string.IsNullOrEmpty(_activeScenePath)) return;

        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].FilePath == _activeScenePath)
            {
                Items[i] = new SceneItem(Items[i].FilePath, LoadSceneAsync, e.IsDirty);
                return;
            }
        }
    }

    // ── Load scene ────────────────────────────────────────────────────────────

    private async Task LoadSceneAsync(SceneItem item)
    {
        EditorScene? scene = await SceneSerializer.LoadAsync(item.FilePath).ConfigureAwait(false);
        if (scene is null) return;
        await MainThread.InvokeOnMainThreadAsync(() => Context.SetActiveScene(scene)).ConfigureAwait(false);
    }

    // ── Commands ────────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(HasProject))]
    private async Task NewSceneAsync()
    {
        if (DialogService.Navigation is not { } navigation) return;

        NewSceneResult? result = await NewSceneDialog.ShowAsync(navigation);
        if (result is null) return;
        if (string.IsNullOrEmpty(_scenesPath)) return;

        EditorScene scene = new()
        {
            Name = result.SceneName,
            WorldSize = new EditorVector2(result.WorldWidth, result.WorldHeight),
        };

        string filePath = Path.Combine(_scenesPath, $"{result.SceneName}.scene.json");
        await SceneSerializer.SaveAsync(scene, filePath).ConfigureAwait(true);

        Bus.Publish(new SceneCreatedEvent(scene));
    }

    [RelayCommand(CanExecute = nameof(CanModifyScene))]
    private async Task RenameSceneAsync()
    {
        if (SelectedScene is not { } item) return;

        string? newName = await DialogService.PromptAsync(
            "Rename scene", "Enter new name:", initialValue: item.Name, maxLength: 128);

        if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;

        string newPath = Path.Combine(_scenesPath, $"{newName}.scene.json");
        try { File.Move(item.FilePath, newPath); }
        catch (Exception ex) { Log($"[SceneManager] Failed to rename scene: {ex.Message}", LogLevel.Error); return; }

        int idx = -1;
        for (int i = 0; i < Items.Count; i++)
            if (Items[i].FilePath == item.FilePath) { idx = i; break; }

        if (idx >= 0)
            Items[idx] = new SceneItem(newPath, LoadSceneAsync);
    }

    [RelayCommand(CanExecute = nameof(CanModifyScene))]
    private async Task DeleteSceneAsync()
    {
        if (SelectedScene is not { } item) return;

        bool confirmed = await DialogService.ConfirmAsync(
            "Delete scene", $"Delete '{item.Name}'? This cannot be undone.", "Delete", "Cancel");

        if (!confirmed) return;

        try
        {
            if (File.Exists(item.FilePath))
                File.Delete(item.FilePath);
        }
        catch
        {
            return;
        }

        Items.Remove(item);
        UpdateSceneCount();
    }

    private bool CanModifyScene() => HasProject && SelectedScene is not null;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void UpdateSceneCount()
        => SceneCountText = Items.Count == 1 ? "1 scene" : $"{Items.Count} scenes";

    private static void Log(string message, LogLevel level = LogLevel.Info)
        => Bus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.UtcNow, level, message)));
}
