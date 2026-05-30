using MonoGame.Editor.Core.Assets;

namespace MonoGame.Editor.Maui.Views;

/// <summary>
/// Ventana principal del editor. Gestiona el toolbar, menú, status bar y coordina el ciclo Play/Stop.
/// Los paneles se comunican exclusivamente a través de <see cref="IEditorEventBus"/>.
/// </summary>
public sealed partial class EditorWindow : ContentPage
{
    #region Constants / colors

    private static readonly Color ActiveToolBg   = Color.FromArgb("#4A9EFF");
    private static readonly Color ActiveToolFg   = Colors.White;
    private static readonly Color InactiveToolBg = Colors.Transparent;
    private static readonly Color InactiveToolFg = Color.FromArgb("#9A9AA2");

    private static readonly Color ActivePillBg       = Color.FromArgb("#4A9EFF");
    private static readonly Color InactivePillBg     = Color.FromArgb("#252528");
    private static readonly Color ActivePillFg       = Colors.White;
    private static readonly Color InactivePillFg     = Color.FromArgb("#9A9AA2");
    private static readonly Color PillBorderActive   = Color.FromArgb("#4A9EFF");
    private static readonly Color PillBorderInactive = Color.FromArgb("#34343A");

    private static readonly Color BuildSuccessColor = Color.FromArgb("#46C66A");
    private static readonly Color BuildErrorColor   = Colors.White;
    private static readonly Color BuildErrorBg      = Color.FromArgb("#C73E3E");
    private static readonly Color BuildNormalBg     = Color.FromArgb("#252528");

    private static readonly Color DropdownItemFg     = Color.FromArgb("#E6E6E8");
    private static readonly Color DropdownItemBg     = Colors.Transparent;
    private static readonly Color DropdownItemHoverBg = Color.FromArgb("#2E2E34");
    private static readonly Color DropdownSeparatorColor = Color.FromArgb("#34343A");

    #endregion

    #region Fields

    private readonly IEditorEventBus    _bus      = EditorContext.Instance.EventBus;
    private readonly GameObjectRegistry _registry = new();

    private string _activeTool = "Select";
    private bool _is2D   = true;
    private bool _isSnap = false;
    private bool _isNav  = false;
    private bool _isRes  = false;

    private string? _openMenuTag;

    private Action<EditorStateChangedEvent>?  _onStateChanged;
    private Action<SceneLoadedEvent>?         _onSceneLoaded;
    private Action<BuildOutputLineEvent>?     _onBuildOutput;
    private Action<FpsUpdatedEvent>?          _onFpsUpdated;
    private Action<SceneDirtyChangedEvent>?   _onSceneDirty;
    private Action<ProjectOpenedEvent>?       _onProjectOpened;

    #endregion

    public EditorWindow()
    {
        InitializeComponent();
        Subscribe();
        SetPillStyle(Toggle2DBtn, _is2D);
        SetPillStyle(ToggleSnapBtn, _isSnap);
        SetPillStyle(ToggleNavBtn, _isNav);
        SetPillStyle(ToggleResBtn, _isRes);
        UpdateToolButtons();
    }

    #region Lifecycle — keyboard shortcuts

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
    }

    // TODO: keyboard shortcuts require platform-specific WinUI implementation.
    // Wire up via native Microsoft.UI.Xaml.Input.KeyboardAccelerator when needed.

    #endregion

    #region EventBus subscriptions

    private void Log(string message, LogLevel level = LogLevel.Info)
        => _bus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.UtcNow, level, message)));

    private void Subscribe()
    {
        _onStateChanged  = e => MainThread.BeginInvokeOnMainThread(() => OnEditorStateChanged(e));
        _onSceneLoaded   = e => MainThread.BeginInvokeOnMainThread(() => OnSceneLoaded(e));
        _onBuildOutput   = e => MainThread.BeginInvokeOnMainThread(() => OnBuildOutputLine(e));
        _onFpsUpdated    = e => MainThread.BeginInvokeOnMainThread(() => FpsLabel.Text = $"{e.Fps} FPS");
        _onSceneDirty    = e => MainThread.BeginInvokeOnMainThread(() => OnSceneDirtyChanged(e));
        _onProjectOpened = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpened(e));

        _bus.Subscribe(_onStateChanged);
        _bus.Subscribe(_onSceneLoaded);
        _bus.Subscribe(_onBuildOutput);
        _bus.Subscribe(_onFpsUpdated);
        _bus.Subscribe(_onSceneDirty);
        _bus.Subscribe(_onProjectOpened);
    }

    private void OnEditorStateChanged(EditorStateChangedEvent e)
    {
        bool playing = e.NewState is EditorState.Playing or EditorState.Paused;
        bool paused  = e.NewState is EditorState.Paused;

        PlayBtn.IsEnabled  = !playing;
        PauseBtn.IsEnabled = playing;
        StopBtn.IsEnabled  = playing;

        if (playing)
        {
            StopBtn.BackgroundColor  = Color.FromArgb("#E5484D");
            StopBtn.TextColor        = Colors.White;
            PauseBtn.BackgroundColor = paused ? Color.FromArgb("#E8A050") : Color.FromArgb("#252528");
            PauseBtn.TextColor       = paused ? Colors.White : Color.FromArgb("#9A9AA2");
        }
        else
        {
            StopBtn.BackgroundColor  = Color.FromArgb("#252528");
            StopBtn.TextColor        = Color.FromArgb("#6A6A72");
            PauseBtn.BackgroundColor = Color.FromArgb("#252528");
            PauseBtn.TextColor       = Color.FromArgb("#6A6A72");
        }
    }

    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        int count = e.Scene?.RootGameObjects.Count ?? 0;
        ObjectCountLabel.Text = count == 1 ? "1 object in scene" : $"{count} objects in scene";
        UpdateTitleBar();
    }

    private void OnBuildOutputLine(BuildOutputLineEvent e)
    {
        string line = e.Line;

        if (line.Contains("Build succeeded", StringComparison.OrdinalIgnoreCase))
        {
            BuildStatusLabel.Text                  = "Build succeeded";
            BuildStatusLabel.TextColor             = BuildSuccessColor;
            BuildStatusSegment.BackgroundColor     = BuildNormalBg;
        }
        else if (line.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase)
              || (e.IsError && line.Contains("error", StringComparison.OrdinalIgnoreCase)))
        {
            BuildStatusLabel.Text                  = "Build failed";
            BuildStatusLabel.TextColor             = BuildErrorColor;
            BuildStatusSegment.BackgroundColor     = BuildErrorBg;
        }
    }

    private void OnSceneDirtyChanged(SceneDirtyChangedEvent e)
        => UpdateTitleBar();

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        UpdateTitleBar();
        _registry.Scan();
    }

    private void UpdateTitleBar()
    {
        EditorProject? project = EditorContext.Instance.ActiveProject;
        EditorScene?   scene   = EditorContext.Instance.ActiveScene;
        bool           dirty   = EditorContext.Instance.IsSceneDirty;

        string projectPart = project?.Name ?? "No Project";
        string scenePart   = scene?.Name   ?? "No Scene";
        string dirtyMark   = dirty ? " ●" : string.Empty;

        Title = $"MonoGame Editor — {projectPart} — {scenePart}{dirtyMark}";
    }

    #endregion

    #region Menu bar — dropdown management

    private void OnFileMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "File") { HideDropdown(); return; }
        ShowDropdown("File", 4, BuildFileMenuItems());
    }

    private void OnEditMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "Edit") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + 4);
        ShowDropdown("Edit", offsetX, BuildEditMenuItems());
    }

    private void OnProjectMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "Project") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + EditMenuBtn.Width + 4);
        ShowDropdown("Project", offsetX, BuildProjectMenuItems());
    }

    private void OnDebugMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "Debug") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + EditMenuBtn.Width + ProjectMenuBtn.Width + 4);
        ShowDropdown("Debug", offsetX, BuildDebugMenuItems());
    }

    private void OnMenuOverlayTapped(object? sender, TappedEventArgs e) => HideDropdown();

    private void ShowDropdown(string tag, int offsetX,
                              IEnumerable<(string Label, bool IsSeparator, Action? Action)> items)
    {
        _openMenuTag = tag;
        DropdownStack.Children.Clear();

        foreach (var (label, isSep, action) in items)
        {
            if (isSep)
            {
                DropdownStack.Children.Add(new BoxView
                {
                    HeightRequest   = 1,
                    Color           = DropdownSeparatorColor,
                    Margin          = new Thickness(8, 2),
                });
                continue;
            }

            bool isDisabled = action is null;
            var btn = new Button
            {
                Text              = label,
                BackgroundColor   = DropdownItemBg,
                TextColor         = isDisabled ? Color.FromArgb("#6A6A72") : DropdownItemFg,
                FontSize          = 13,
                HorizontalOptions = LayoutOptions.Fill,
                Padding           = new Thickness(16, 6),
                BorderWidth       = 0,
                IsEnabled         = !isDisabled,
            };

            if (action is not null)
            {
                var captured = action;
                btn.Clicked += (_, _) => { HideDropdown(); captured(); };
            }

            DropdownStack.Children.Add(btn);
        }

        DropdownPanel.Margin = new Thickness(offsetX, 28, 0, 0);
        MenuOverlay.IsVisible = true;
    }

    private void HideDropdown()
    {
        MenuOverlay.IsVisible = false;
        _openMenuTag = null;
    }

    #endregion

    #region Menu item builders

    private IEnumerable<(string, bool, Action?)> BuildFileMenuItems()
    {
        yield return ("New Project…",   false, () => _ = NewProjectAsync());
        yield return ("Open Project…",  false, () => _ = OpenProjectAsync());
        yield return ("---",            true,  null);
        yield return ("New Scene",      false, () => _ = NewSceneAsync());
        yield return ("Save Scene",     false, () => _ = SaveSceneAsync());
        yield return ("Save Scene As…", false, () => _ = SaveSceneAsAsync());
        yield return ("---",            true,  null);
        yield return ("Exit",           false, OnExitClicked);
    }

    private IEnumerable<(string, bool, Action?)> BuildEditMenuItems()
    {
        yield return ("Undo",       false, OnUndoClicked);
        yield return ("Redo",       false, OnRedoClicked);
        yield return ("---",        true,  null);
        yield return ("Cut",        false, null);
        yield return ("Copy",       false, null);
        yield return ("Paste",      false, null);
        yield return ("Duplicate",  false, OnDuplicateSelected);
        yield return ("Delete",     false, OnDeleteSelected);
        yield return ("---",        true,  null);
        yield return ("Select All", false, OnSelectAll);
    }

    private IEnumerable<(string, bool, Action?)> BuildProjectMenuItems()
    {
        yield return ("Project Settings…", false, () => _ = OpenProjectSettingsAsync());
        yield return ("---",               true,  null);
        yield return ("Build Content",     false, () => _ = BuildContentAsync());
        yield return ("Build Solution",    false, () => _ = BuildSolutionAsync());
        yield return ("Generate Code",     false, () => _ = GenerateCodeAsync());
        yield return ("---",               true,  null);
        yield return ("Run",               false, OnRunGame);
    }

    private IEnumerable<(string, bool, Action?)> BuildDebugMenuItems()
    {
        yield return ("Play",  false, OnPlayClicked);
        yield return ("Pause", false, OnPauseClicked);
        yield return ("Stop",  false, OnStopClicked);
    }

    #endregion

    #region Menu actions — File

    private async Task NewProjectAsync()
    {
        var result = await Views.Dialogs.NewProjectDialog.ShowAsync(Navigation);
        if (result is null) return;

        try
        {
            EditorProject project = await Task.Run(() =>
                ProjectManager.Create(result.ProjectName, result.ParentPath, result.GameCsprojPath))
                .ConfigureAwait(true);

            EditorContext.Instance.SetActiveProject(project);
            Log($"[Editor] Project '{project.Name}' created.");
        }
        catch (Exception ex)
        {
            Log($"[Editor] Failed to create project: {ex.Message}", LogLevel.Error);
        }
    }

    private async Task OpenProjectAsync()
    {
        try
        {
            string? path = await PickFolderAsync();
            if (path is null) return;

            EditorProject? project = await Task.Run(() => ProjectManager.Load(path)).ConfigureAwait(true);
            if (project is null)
            {
                Log($"[Editor] No valid project found at: {path}", LogLevel.Warning);
                return;
            }

            EditorContext.Instance.SetActiveProject(project);
            Log($"[Editor] Project '{project.Name}' opened.");
        }
        catch (Exception ex)
        {
            Log($"[Editor] Open project error: {ex.Message}", LogLevel.Error);
        }
    }

    private static async Task<string?> PickFolderAsync()
    {
        try
        {
            Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
                ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (win is null) return null;

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
            var picker = new Windows.Storage.Pickers.FolderPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            Windows.Storage.StorageFolder? folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
        catch
        {
            return null;
        }
    }

    private async Task NewSceneAsync()
    {
        var result = await Views.Dialogs.NewSceneDialog.ShowAsync(Navigation);
        if (result is null) return;

        EditorProject? project = EditorContext.Instance.ActiveProject;

        EditorScene scene = new()
        {
            Name      = result.SceneName,
            WorldSize = new EditorVector2(result.WorldWidth, result.WorldHeight),
        };

        if (project is not null && !string.IsNullOrEmpty(project.ScenesPath))
        {
            Directory.CreateDirectory(project.ScenesPath);
            string safeName = string.Concat(scene.Name.Split(Path.GetInvalidFileNameChars()));
            string scenePath = Path.Combine(project.ScenesPath, safeName + ".scene.json");
            scene.ScenePath  = scenePath;
            try
            {
                await SceneSerializer.SaveAsync(scene, scenePath).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Log($"[Editor] Failed to save scene: {ex.Message}", LogLevel.Error);
            }
        }

        EditorContext.Instance.SetActiveScene(scene);
        _bus.Publish(new SceneCreatedEvent(scene));
        Log($"[Editor] New scene '{scene.Name}' created.");
    }

    private async Task SaveSceneAsync()
    {
        EditorScene?   scene   = EditorContext.Instance.ActiveScene;
        EditorProject? project = EditorContext.Instance.ActiveProject;

        if (scene is null) return;

        string scenePath = scene.ScenePath;

        if (string.IsNullOrEmpty(scenePath))
        {
            await SaveSceneAsAsync().ConfigureAwait(true);
            return;
        }

        try
        {
            await SceneSerializer.SaveAsync(scene, scenePath).ConfigureAwait(true);
            EditorContext.Instance.MarkSceneClean();
            BuildStatusLabel.Text      = "Saved";
            BuildStatusLabel.TextColor = BuildSuccessColor;
            Log($"[Save] Scene saved to {scenePath}");

            if (project is not null)
                await TryGenerateCodeOnSaveAsync(scene, project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log($"[Save] Error: {ex.Message}", LogLevel.Error);
        }
    }

    private async Task SaveSceneAsAsync()
    {
        EditorScene?   scene   = EditorContext.Instance.ActiveScene;
        EditorProject? project = EditorContext.Instance.ActiveProject;

        if (scene is null) return;

        try
        {
            string initialDir = project is not null && Directory.Exists(project.ScenesPath)
                ? project.ScenesPath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string suggestedName = string.IsNullOrEmpty(scene.Name)
                ? "NewScene.scene.json"
                : $"{scene.Name}.scene.json";

            FileResult? picked = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Save Scene As",
            }).ConfigureAwait(true);

            if (picked is null) return;
            string path = picked.FullPath;
            if (!path.EndsWith(".scene.json", StringComparison.OrdinalIgnoreCase))
                path += ".scene.json";

            scene.ScenePath = path;
            scene.Name      = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(path));

            await SceneSerializer.SaveAsync(scene, path).ConfigureAwait(true);
            EditorContext.Instance.MarkSceneClean();
            Log($"[Save] Scene saved to {path}");
        }
        catch (Exception ex)
        {
            Log($"[Save] Error: {ex.Message}", LogLevel.Error);
        }
    }

    private async Task TryGenerateCodeOnSaveAsync(EditorScene scene, EditorProject project)
    {
        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        if (!settings.GenerateOnSave) return;
        if (string.IsNullOrEmpty(project.GameCsprojPath)) return;
        if (string.IsNullOrWhiteSpace(settings.RootNamespace)) return;

        _bus.Publish(new CodeGenStartedEvent(scene.Name));

        ICodeGenService codeGen = new SceneCodeGenerator();
        CodeGenResult result = await codeGen.GenerateSceneAsync(scene, project, settings)
                                            .ConfigureAwait(true);

        _bus.Publish(new CodeGenCompletedEvent(result));
        Log(
            result.Success ? $"[CodeGen] {result.OutputPath}" : $"[CodeGen] Error: {result.ErrorMessage}",
            result.Success ? LogLevel.Info : LogLevel.Error);
    }

    private void OnExitClicked()
        => Application.Current?.CloseWindow(Application.Current.Windows.First());

    #endregion

    #region Menu actions — Edit

    private void OnUndoClicked()
        => EditorContext.Instance.Commands.Undo();

    private void OnRedoClicked()
        => EditorContext.Instance.Commands.Redo();

    private void OnDeleteSelected()
    {
        EditorGameObject? obj   = EditorContext.Instance.SelectedObject;
        EditorScene?      scene = EditorContext.Instance.ActiveScene;
        if (obj is null || scene is null) return;
        EditorContext.Instance.Commands.Execute(new DeleteEntityCommand(obj, scene));
    }

    private void OnDuplicateSelected()
    {
        // Duplication via CreateEntityCommand from a clone; stubbed for Phase 12
    }

    private void OnSelectAll()
    {
        // Multi-selection across all root objects; stubbed for Phase 12
    }

    #endregion

    #region Menu actions — Project

    private async Task OpenProjectSettingsAsync()
    {
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (project is null) return;

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        await Views.Dialogs.ProjectSettingsDialog.ShowAsync(Navigation, project, settings)
                                                 .ConfigureAwait(true);
    }

    private async Task BuildContentAsync()
    {
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (project is null) return;

        string mgcbFile = Path.Combine(project.ContentPath, "Content.mgcb");
        if (!File.Exists(mgcbFile))
        {
            Log($"[Build] Content.mgcb not found at: {mgcbFile}", LogLevel.Warning);
            BuildStatusLabel.Text      = "Content.mgcb not found";
            BuildStatusLabel.TextColor = Color.FromArgb("#E8A050");
            return;
        }

        BuildStatusLabel.Text      = "Building content…";
        BuildStatusLabel.TextColor = Color.FromArgb("#9A9AA2");
        BuildStatusSegment.BackgroundColor = BuildNormalBg;

        int exit = await MgcbRunner.RunAsync(mgcbFile, line =>
            _bus.Publish(new BuildOutputLineEvent(line, IsErrorLine(line))))
            .ConfigureAwait(true);

        if (exit == 0)
        {
            _bus.Publish(new BuildOutputLineEvent("Build succeeded", false));
        }
        else
        {
            _bus.Publish(new BuildOutputLineEvent($"Build FAILED (exit {exit})", true));
        }

        _bus.Publish(new BuildFinishedEvent(exit, "Content"));
    }

    private async Task BuildSolutionAsync()
    {
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (project is null) return;

        string csproj = project.GameCsprojPath;
        if (string.IsNullOrEmpty(csproj) || !File.Exists(csproj))
        {
            Log("[Build] Game .csproj path not configured.", LogLevel.Warning);
            return;
        }

        BuildStatusLabel.Text      = "Building solution…";
        BuildStatusLabel.TextColor = Color.FromArgb("#9A9AA2");

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        int exit = await MgcbRunner.RunDotnetBuildAsync(csproj, settings.BuildConfiguration, line =>
            _bus.Publish(new BuildOutputLineEvent(line, IsErrorLine(line))))
            .ConfigureAwait(true);

        _bus.Publish(new BuildOutputLineEvent(
            exit == 0 ? "Build succeeded" : $"Build FAILED (exit {exit})",
            exit != 0));

        _bus.Publish(new BuildFinishedEvent(exit, "Solution"));
    }

    private async Task GenerateCodeAsync()
    {
        EditorScene?   scene   = EditorContext.Instance.ActiveScene;
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (scene is null || project is null) return;

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(settings.RootNamespace))
        {
            Log("[CodeGen] RootNamespace not set in Project Settings.", LogLevel.Warning);
            return;
        }

        var progressDlg = new Views.Dialogs.CodeGenProgressDialog();
        _ = Navigation.PushModalAsync(progressDlg);

        _bus.Publish(new CodeGenStartedEvent(scene.Name));
        ICodeGenService codeGen = new SceneCodeGenerator();

        CodeGenResult result = await codeGen.GenerateSceneAsync(scene, project, settings)
                                            .ConfigureAwait(true);

        progressDlg.AddFileResult(result.OutputPath ?? string.Empty, result.Success);
        progressDlg.MarkComplete(result.Success ? 1 : 0, result.Success ? 0 : 1);
        _bus.Publish(new CodeGenCompletedEvent(result));
        _bus.Publish(new BuildFinishedEvent(result.Success ? 0 : 1, "CodeGen"));
    }

    private void OnRunGame()
        => _ = RunGameAsync();

    private async Task RunGameAsync()
    {
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (project is null || string.IsNullOrEmpty(project.GameCsprojPath)) return;

        string dir = Path.GetDirectoryName(project.GameCsprojPath) ?? string.Empty;
        string exeName = Path.GetFileNameWithoutExtension(project.GameCsprojPath) + ".exe";

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        string[] searchDirs =
        [
            Path.Combine(dir, "bin", settings.BuildConfiguration, "net10.0-windows"),
            Path.Combine(dir, "bin", settings.BuildConfiguration, "net10.0-windows10.0.19041.0"),
        ];

        foreach (string candidate in searchDirs)
        {
            string exePath = Path.Combine(candidate, exeName);
            if (!File.Exists(exePath)) continue;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName         = exePath,
                WorkingDirectory = dir,
                UseShellExecute  = true,
            });
            return;
        }

        bool buildNow = await this.DisplayAlertAsync(
            "Executable not found",
            $"'{exeName}' was not found in the output directories. Build the solution first?",
            "Build Now",
            "Cancel").ConfigureAwait(true);

        if (buildNow)
            await BuildSolutionAsync().ConfigureAwait(true);
    }

    private static bool IsErrorLine(string line)
        => line.Contains(": error ", StringComparison.OrdinalIgnoreCase)
        || line.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Toolbar — gizmo tools

    private void OnToolClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        string tool = btn.CommandParameter as string ?? "Select";
        ActivateTool(tool);
    }

    private void ActivateTool(string tool)
    {
        _activeTool = tool;
        UpdateToolButtons();
        EditorContext.Instance.Gizmos.Mode = _activeTool switch
        {
            "Move"   => GizmoMode.Move,
            "Rotate" => GizmoMode.Rotate,
            "Scale"  => GizmoMode.Scale,
            "Rect"   => GizmoMode.Rect,
            _        => GizmoMode.Select,
        };
    }

    private void UpdateToolButtons()
    {
        SetToolStyle(SelectBtn, _activeTool == "Select");
        SetToolStyle(MoveBtn,   _activeTool == "Move");
        SetToolStyle(RotateBtn, _activeTool == "Rotate");
        SetToolStyle(ScaleBtn,  _activeTool == "Scale");
        SetToolStyle(RectBtn,   _activeTool == "Rect");
        SetToolStyle(PanBtn,    _activeTool == "Pan");
    }

    private static void SetToolStyle(Button btn, bool active)
    {
        btn.BackgroundColor = active ? ActiveToolBg  : InactiveToolBg;
        btn.TextColor       = active ? ActiveToolFg  : InactiveToolFg;
    }

    #endregion

    #region Toolbar — mode toggles

    private void OnToggle2D(object sender, EventArgs e)
    {
        _is2D = !_is2D;
        SetPillStyle(Toggle2DBtn, _is2D);
        EditorContext.Instance.Gizmos.IsDepthMode = !_is2D;
    }

    private void OnToggleSnap(object sender, EventArgs e) => OnToggleSnap();
    private void OnToggleSnap()
    {
        _isSnap = !_isSnap;
        SetPillStyle(ToggleSnapBtn, _isSnap);
        EditorContext.Instance.Gizmos.SnapEnabled = _isSnap;
    }

    private void OnToggleNav(object sender, EventArgs e)
    {
        _isNav = !_isNav;
        SetPillStyle(ToggleNavBtn, _isNav);
    }

    private void OnToggleRes(object sender, EventArgs e)
    {
        _isRes = !_isRes;
        SetPillStyle(ToggleResBtn, _isRes);
    }

    private static void SetPillStyle(Button btn, bool active)
    {
        btn.BackgroundColor = active ? ActivePillBg  : InactivePillBg;
        btn.TextColor       = active ? ActivePillFg  : InactivePillFg;
        btn.BorderColor     = active ? PillBorderActive : PillBorderInactive;
    }

    #endregion

    #region Toolbar — transport (Phase 10)

    private void OnPlayClicked(object sender, EventArgs e) => OnPlayClicked();
    private void OnPlayClicked()
    {
        EditorState state = EditorContext.Instance.State;

        if (state is EditorState.Paused)
        {
            EditorGameLoop.Current?.Resume();
            EditorContext.Instance.SetState(EditorState.Playing);
            return;
        }

        if (state is EditorState.Playing) return;

        EditorScene? scene = EditorContext.Instance.ActiveScene;
        if (scene is null) return;

        _registry.Scan();
        EditorContext.Instance.TakePlaySnapshot();

        PlayModeRunner runner = new(scene, _registry);
        EditorGameLoop.Current?.EnterPlay(runner);

        EditorContext.Instance.SetState(EditorState.Playing);
        Log("[Play] Play mode started.");
    }

    private void OnPauseClicked(object sender, EventArgs e) => OnPauseClicked();
    private void OnPauseClicked()
    {
        EditorState state = EditorContext.Instance.State;

        if (state is EditorState.Playing)
        {
            EditorGameLoop.Current?.Pause();
            EditorContext.Instance.SetState(EditorState.Paused);
            return;
        }

        if (state is EditorState.Paused)
        {
            EditorGameLoop.Current?.Resume();
            EditorContext.Instance.SetState(EditorState.Playing);
        }
    }

    private void OnStopClicked(object sender, EventArgs e) => OnStopClicked();
    private void OnStopClicked()
    {
        EditorState state = EditorContext.Instance.State;
        if (state is EditorState.Editing) return;

        PlayModeRunner? runner = EditorGameLoop.Current?.ExitPlay();
        runner?.Dispose();

        EditorScene? restored = EditorContext.Instance.RestoreFromSnapshot();
        EditorContext.Instance.ClearPlaySnapshot();

        if (restored is not null)
            EditorContext.Instance.SetActiveScene(restored);

        EditorContext.Instance.SetState(EditorState.Editing);
        Log("[Play] Play mode stopped.");
    }

    #endregion
}
