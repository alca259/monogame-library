using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels;

/// <summary>
/// ViewModel de la ventana principal del editor: estado del toolbar y de la status bar,
/// comandos de menú/transport y suscripciones al <see cref="IEditorEventBus"/>. La interop
/// nativa (teclado, separadores), el viewport y la gestión de layout de paneles permanecen
/// en el code-behind; la VM solicita repintar el viewport mediante
/// <see cref="ViewportInvalidateRequested"/>.
/// </summary>
public sealed partial class EditorWindowViewModel : ViewModelBase
{
    #region Colors

    private static readonly Color BuildSuccessColor = Color.FromArgb("#46C66A");
    private static readonly Color BuildErrorColor = Colors.White;
    private static readonly Color BuildErrorBg = Color.FromArgb("#C73E3E");
    private static readonly Color BuildNormalBg = Color.FromArgb("#252528");
    private static readonly Color StatusDimColor = Color.FromArgb("#9A9AA2");
    private static readonly Color StopActiveBg = Color.FromArgb("#E5484D");
    private static readonly Color StopInactiveBg = Color.FromArgb("#252528");
    private static readonly Color StopActiveFg = Colors.White;
    private static readonly Color StopInactiveFg = Color.FromArgb("#6A6A72");

    #endregion

    #region Fields

    private readonly EditorPreferences _preferences = new();
    private readonly ExternalPlayLauncher _externalLauncher = new();
    private bool _playPendingAfterBuild;

    #endregion

    /// <summary>Preferencias del editor (la vista las usa para el layout de paneles).</summary>
    public EditorPreferences Preferences => _preferences;

    /// <summary>Solicita a la vista repintar el viewport.</summary>
    public event Action? ViewportInvalidateRequested;

    #region Observable state

    public enum SceneTools
    {
        Select,
        Move,
        Rotate,
        Scale,
        Rect,
        Pan,
        Universal
    }

    [ObservableProperty]
    private SceneTools _activeTool = SceneTools.Select;

    [ObservableProperty]
    private bool _isSnap;

    [ObservableProperty]
    private bool _toolMoveEnabled = true;

    [ObservableProperty]
    private bool _toolRotateEnabled = true;

    [ObservableProperty]
    private bool _toolScaleEnabled = true;

    [ObservableProperty]
    private bool _axisXEnabled = true;

    [ObservableProperty]
    private bool _axisYEnabled = true;

    [ObservableProperty]
    private bool _isNav;

    [ObservableProperty]
    private bool _isRes;

    [ObservableProperty]
    private bool _toolsEnabled = true;

    [ObservableProperty]
    private bool _canPlay;

    [ObservableProperty]
    private bool _canStop;

    [ObservableProperty]
    private bool _stopActive;

    [ObservableProperty]
    private string _playButtonText = "▶";

    [ObservableProperty]
    private Color _stopBackground = StopInactiveBg;

    [ObservableProperty]
    private Color _stopForeground = StopInactiveFg;

    [ObservableProperty]
    private string _title = "MonoGame Editor";

    [ObservableProperty]
    private string _buildStatusText = "Ready";

    [ObservableProperty]
    private Color _buildStatusColor = StatusDimColor;

    [ObservableProperty]
    private Color _buildStatusBackground = BuildNormalBg;

    [ObservableProperty]
    private string _objectCountText = string.Empty;

    [ObservableProperty]
    private string _fpsText = "-- FPS";

    [ObservableProperty]
    private bool _isViewportFocused;

    #endregion

    #region Bus subscriptions

    protected override void RegisterEvents()
    {
        On<FocusChangedEvent>(e => IsViewportFocused = e.NewContext is EditorFocusContext.Viewport);
        On<EditorStateChangedEvent>(OnEditorStateChanged);
        On<SceneLoadedEvent>(OnSceneLoaded);
        On<BuildOutputLineEvent>(OnBuildOutputLine);
        On<FpsUpdatedEvent>(e => FpsText = Context.State is EditorState.Playing ? $"{e.Fps} FPS" : "-- FPS");
        On<SceneDirtyChangedEvent>(_ => { UpdateTitle(); ViewportInvalidateRequested?.Invoke(); });
        On<ProjectOpenedEvent>(_ => UpdateTitle());
        On<GameObjectSelectedEvent>(_ => ViewportInvalidateRequested?.Invoke());
        On<GameObjectPropertyChangedEvent>(_ => ViewportInvalidateRequested?.Invoke());
        On<SceneCreatedEvent>(_ => ViewportInvalidateRequested?.Invoke());
        On<UndoPerformedEvent>(_ => ViewportInvalidateRequested?.Invoke());
        On<RedoPerformedEvent>(_ => ViewportInvalidateRequested?.Invoke());
        On<BuildFinishedEvent>(OnBuildFinished);
    }

    private void OnEditorStateChanged(EditorStateChangedEvent e)
    {
        bool playing = e.NewState is EditorState.Playing;
        bool hasScene = Context.ActiveScene is not null;

        CanPlay = !playing && hasScene;
        CanStop = playing;
        ToolsEnabled = !playing;

        if (playing)
        {
            StopActive = true;
            StopBackground = StopActiveBg;
            StopForeground = StopActiveFg;
        }
        else
        {
            StopActive = false;
            StopBackground = StopInactiveBg;
            StopForeground = StopInactiveFg;
            Bus.Publish(new FpsUpdatedEvent(0));
        }
    }

    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        Context.SetSelection(null);
        int count = e.Scene?.RootGameObjects.Count ?? 0;
        ObjectCountText = count == 1 ? "1 object in scene" : $"{count} objects in scene";
        UpdateTitle();
        ViewportInvalidateRequested?.Invoke();

        bool hasScene = e.Scene is not null;
        bool isPlaying = Context.State is EditorState.Playing;
        CanPlay = hasScene && !isPlaying;

        EditorProject? project = Context.ActiveProject;
        if (project is not null && e.Scene is not null && !string.IsNullOrEmpty(e.Scene.ScenePath))
            _ = Task.Run(() => ProjectManager.SaveLastOpenedScene(project, e.Scene.ScenePath));
    }

    private void OnBuildOutputLine(BuildOutputLineEvent e)
    {
        string line = e.Line;

        if (line.Contains("Build succeeded", StringComparison.OrdinalIgnoreCase))
        {
            BuildStatusText = "Build succeeded";
            BuildStatusColor = BuildSuccessColor;
            BuildStatusBackground = BuildNormalBg;
        }
        else if (line.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase)
              || (e.IsError && line.Contains("error", StringComparison.OrdinalIgnoreCase)))
        {
            BuildStatusText = "Build failed";
            BuildStatusColor = BuildErrorColor;
            BuildStatusBackground = BuildErrorBg;
        }
    }

    private void UpdateTitle()
    {
        EditorProject? project = Context.ActiveProject;
        EditorScene? scene = Context.ActiveScene;
        bool dirty = Context.IsSceneDirty;

        string projectPart = project?.Name ?? "No Project";
        string scenePart = scene?.Name ?? "No Scene";
        string dirtyMark = dirty ? " ●" : string.Empty;

        Title = $"MonoGame Editor — {projectPart} — {scenePart}{dirtyMark}";
    }

    #endregion

    #region Auto-load

    /// <summary>Carga automáticamente el último proyecto/escena al iniciar.</summary>
    public async Task TryAutoLoadLastProjectAsync()
    {
        if (string.IsNullOrEmpty(_preferences.LastProjectPath)) return;
        if (!Directory.Exists(_preferences.LastProjectPath)) return;

        try
        {
            EditorProject? project = await Task.Run(() => ProjectManager.Load(_preferences.LastProjectPath))
                                               .ConfigureAwait(true);
            if (project is null) return;
            Context.SetActiveProject(project);
            await ApplyProjectSettingsAsync(project).ConfigureAwait(true);
            Log($"[Editor] Auto-loaded project '{project.Name}'.");
            await TryLoadLastSceneForProjectAsync(project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log($"[Editor] Auto-load failed: {ex.Message}", LogLevel.Warning);
        }
    }

    private async Task ApplyProjectSettingsAsync(EditorProject project)
    {
        try
        {
            ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
            if (settings.GridCellSize > 0)
                Context.Gizmos.GridCellSize = settings.GridCellSize;
        }
        catch { }
    }

    private async Task TryLoadLastSceneForProjectAsync(EditorProject project)
    {
        string lastScene = await Task.Run(() => ProjectManager.GetLastOpenedScene(project.RootPath))
                                     .ConfigureAwait(true);
        if (string.IsNullOrEmpty(lastScene) || !File.Exists(lastScene)) return;

        try
        {
            EditorScene? scene = await SceneSerializer.LoadAsync(lastScene).ConfigureAwait(true);
            if (scene is null) return;
            Context.SetActiveScene(scene);
            Log($"[Editor] Auto-loaded last scene '{scene.Name}'.");
        }
        catch (Exception ex)
        {
            Log($"[Editor] Failed to auto-load last scene: {ex.Message}", LogLevel.Warning);
        }
    }

    #endregion

    #region Menu actions — File

    [RelayCommand]
    private async Task NewProjectAsync()
    {
        if (DialogService.Navigation is not { } navigation) return;

        var result = await NewProjectDialog.ShowAsync(navigation);
        if (result is null) return;

        try
        {
            EditorProject project = await Task.Run(() =>
                ProjectManager.Create(result.ProjectName, result.ParentPath, result.GameCsprojPath))
                .ConfigureAwait(true);

            Context.SetActiveProject(project);
            Context.SetActiveScene(null);
            _preferences.LastProjectPath = project.RootPath;
            _preferences.AddRecentProject(project.RootPath);
            Log($"[Editor] Project '{project.Name}' created.");
        }
        catch (Exception ex)
        {
            Log($"[Editor] Failed to create project: {ex.Message}", LogLevel.Error);
        }
    }

    [RelayCommand]
    private void CloseProject()
    {
        EditorProject? project = Context.ActiveProject;

        Context.SetActiveScene(null);

        if (project is not null)
            _ = Task.Run(() => ProjectManager.SaveLastOpenedScene(project, string.Empty));

        Context.SetActiveProject(null);
        _preferences.LastProjectPath = string.Empty;
        _preferences.Save();
        Log("[Editor] Project closed.");
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        try
        {
            string? path = await DialogService.PickFolderAsync();
            if (path is null) return;
            await OpenProjectByPathAsync(path).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log($"[Editor] Open project error: {ex.Message}", LogLevel.Error);
        }
    }

    [RelayCommand]
    public async Task OpenProjectByPathAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            bool remove = await DialogService.ConfirmAsync(
                "Project not found",
                $"The project '{Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}' could not be found at:\n{path}\n\nDo you want to remove it from recent projects?",
                "Remove",
                "Keep");
            if (remove)
                _preferences.RemoveRecentProject(path);
            return;
        }

        try
        {
            EditorProject? project = await Task.Run(() => ProjectManager.Load(path)).ConfigureAwait(true);
            if (project is null)
            {
                await DialogService.AlertAsync(title: "Invalid project", message: $"No valid project found at: {path}");
                return;
            }

            Context.SetActiveProject(project);
            Context.SetActiveScene(null);
            _preferences.LastProjectPath = path;
            _preferences.AddRecentProject(path);
            await ApplyProjectSettingsAsync(project).ConfigureAwait(true);
            Log($"[Editor] Project '{project.Name}' opened.");
            await TryLoadLastSceneForProjectAsync(project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log($"[Editor] Open project error: {ex.Message}", LogLevel.Error);
        }
    }

    [RelayCommand]
    private async Task NewSceneAsync()
    {
        if (DialogService.Navigation is not { } navigation) return;

        var result = await NewSceneDialog.ShowAsync(navigation);
        if (result is null) return;

        EditorProject? project = Context.ActiveProject;

        EditorScene scene = new()
        {
            Name = result.SceneName,
            WorldSize = new EditorVector2(result.WorldWidth, result.WorldHeight),
        };

        if (project is not null && !string.IsNullOrEmpty(project.ScenesPath))
        {
            Directory.CreateDirectory(project.ScenesPath);
            string safeName = string.Concat(scene.Name.Split(Path.GetInvalidFileNameChars()));
            string scenePath = Path.Combine(project.ScenesPath, safeName + ".scene.json");
            scene.ScenePath = scenePath;
            try
            {
                await SceneSerializer.SaveAsync(scene, scenePath).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Log($"[Editor] Failed to save scene: {ex.Message}", LogLevel.Error);
            }
        }

        Context.SetActiveScene(scene);
        Bus.Publish(new SceneCreatedEvent(scene));
        Log($"[Editor] New scene '{scene.Name}' created.");
    }

    [RelayCommand]
    public async Task SaveSceneAsync()
    {
        EditorScene? scene = Context.ActiveScene;
        EditorProject? project = Context.ActiveProject;
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
            Context.MarkSceneClean();
            BuildStatusText = "Saved";
            BuildStatusColor = BuildSuccessColor;
            Log($"[Save] Scene saved to {scenePath}");

            if (project is not null)
                await TryGenerateCodeOnSaveAsync(scene, project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log($"[Save] Error: {ex.Message}", LogLevel.Error);
        }
    }

    [RelayCommand]
    public async Task SaveSceneAsAsync()
    {
        EditorScene? scene = Context.ActiveScene;
        EditorProject? project = Context.ActiveProject;
        if (scene is null) return;
        if (DialogService.Navigation is not { } navigation) return;

        try
        {
            // Paso 1 — elegir carpeta de destino dentro del proyecto (o raíz del sistema si no hay proyecto)
            string baseFolder = project is not null && Directory.Exists(project.RootPath)
                ? project.RootPath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string? relFolder = await RelativePathPickerDialog.ShowAsync(
                navigation,
                baseFolder,
                filesMode: false,
                title: "Save Scene As — Select folder");
            if (relFolder is null) return;

            string destFolder = Path.Combine(baseFolder, relFolder);

            // Paso 2 — nombre del archivo
            string suggested = string.IsNullOrEmpty(scene.Name) ? "NewScene" : scene.Name;
            string? fileName = await DialogService.PromptAsync(
                "Save Scene As",
                "File name (without extension):",
                initialValue: suggested);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            // Construir ruta final garantizando extensión correcta
            string safeName = string.Concat(fileName.Trim().Split(Path.GetInvalidFileNameChars()));
            string path = Path.Combine(destFolder, safeName + ".scene.json");

            scene.ScenePath = path;
            scene.Name = safeName;

            await SceneSerializer.SaveAsync(scene, path).ConfigureAwait(true);
            Context.MarkSceneClean();
            UpdateTitle();
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

        Bus.Publish(new CodeGenStartedEvent(scene.Name));

        ICodeGenService codeGen = new SceneCodeGenerator();
        CodeGenResult result = await codeGen.GenerateSceneAsync(scene, project, settings).ConfigureAwait(true);

        Bus.Publish(new CodeGenCompletedEvent(result));
        Log(
            result.Success ? $"[CodeGen] {result.OutputPath}" : $"[CodeGen] Error: {result.ErrorMessage}",
            result.Success ? LogLevel.Info : LogLevel.Error);
    }

    [RelayCommand]
    private void Exit()
        => Application.Current?.CloseWindow(Application.Current.Windows.First());

    #endregion

    #region Menu actions — Edit

    [RelayCommand]
    private void Undo() => Context.Commands.Undo();

    [RelayCommand]
    private void Redo() => Context.Commands.Redo();

    [RelayCommand]
    public void DeleteSelected()
    {
        EditorGameObject? obj = Context.SelectedObject;
        EditorScene? scene = Context.ActiveScene;
        if (obj is null || scene is null) return;
        Context.Commands.Execute(new DeleteEntityCommand(obj, scene));
    }

    [RelayCommand]
    private void DuplicateSelected()
    {
        EditorGameObject? obj = Context.SelectedObject;
        EditorScene? scene = Context.ActiveScene;
        if (obj is null || scene is null) return;
        Context.Commands.Execute(new DuplicateEntityCommand(obj, scene));
    }

    [RelayCommand]
    private void SelectAll()
    {
        EditorScene? scene = Context.ActiveScene;
        if (scene is null) return;
        List<EditorGameObject> all = [];
        CollectAll(scene.RootGameObjects, all);
        Context.SetMultiSelection(all);
    }

    [RelayCommand]
    private void Copy()
    {
        EditorGameObject? obj = Context.SelectedObject;
        if (obj is null) return;
        Context.SetClipboard(DuplicateEntityCommand.DeepClone(obj, null));
    }

    [RelayCommand]
    private void Cut()
    {
        EditorGameObject? obj = Context.SelectedObject;
        EditorScene? scene = Context.ActiveScene;
        if (obj is null || scene is null) return;
        Context.SetClipboard(DuplicateEntityCommand.DeepClone(obj, null));
        Context.Commands.Execute(new DeleteEntityCommand(obj, scene));
    }

    [RelayCommand]
    private void Paste()
    {
        EditorGameObject? clipboard = Context.ClipboardEntity;
        EditorScene? scene = Context.ActiveScene;
        if (clipboard is null || scene is null) return;
        EditorGameObject? parent = Context.SelectedObject;
        EditorGameObject clone = DuplicateEntityCommand.DeepClone(clipboard, parent);
        Context.Commands.Execute(new CreateEntityCommand(clone, scene, parent));
    }

    private static void CollectAll(List<EditorGameObject> objects, List<EditorGameObject> result)
    {
        foreach (EditorGameObject obj in objects)
        {
            result.Add(obj);
            CollectAll(obj.Children, result);
        }
    }

    #endregion

    #region Menu actions — Project

    [RelayCommand]
    private async Task OpenProjectSettingsAsync()
    {
        EditorProject? project = Context.ActiveProject;
        if (project is null) return;
        if (DialogService.Navigation is not { } navigation) return;

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        await ProjectSettingsDialog.ShowAsync(navigation, project, settings).ConfigureAwait(true);

        // Recargar y aplicar ajustes de runtime que el diálogo pudo cambiar.
        ProjectSettings updated = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        if (updated.GridCellSize > 0)
            Context.Gizmos.GridCellSize = updated.GridCellSize;
        ViewportInvalidateRequested?.Invoke();
    }

    [RelayCommand]
    public async Task BuildContentAsync()
    {
        EditorProject? project = Context.ActiveProject;
        if (project is null) return;

        string mgcbFile = Path.Combine(project.ContentPath, "Content.mgcb");
        if (!File.Exists(mgcbFile))
        {
            Directory.CreateDirectory(project.ContentPath);
            File.WriteAllText(mgcbFile, BuildEmptyMgcb(), System.Text.Encoding.UTF8);
            Log($"[Build] Created empty Content.mgcb at: {mgcbFile}", LogLevel.Info);
        }

        BuildStatusText = "Building content…";
        BuildStatusColor = StatusDimColor;
        BuildStatusBackground = BuildNormalBg;

        int exit = await MgcbRunner.RunAsync(mgcbFile, line =>
            Bus.Publish(new BuildOutputLineEvent(line, IsErrorLine(line)))).ConfigureAwait(true);

        Bus.Publish(exit == 0
            ? new BuildOutputLineEvent("Build succeeded", false)
            : new BuildOutputLineEvent($"Build FAILED (exit {exit})", true));

        Bus.Publish(new BuildFinishedEvent(exit, "Content"));
    }

    [RelayCommand]
    public async Task BuildSolutionAsync()
    {
        EditorProject? project = Context.ActiveProject;
        if (project is null) return;

        string csproj = project.GameCsprojPath;
        if (string.IsNullOrEmpty(csproj) || !File.Exists(csproj))
        {
            Log("[Build] Game .csproj path not configured.", LogLevel.Warning);
            return;
        }

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);

        EditorScene? scene = Context.ActiveScene;
        if (scene is not null && !string.IsNullOrWhiteSpace(settings.RootNamespace)
            && !string.IsNullOrEmpty(project.GameSourcePath))
        {
            ICodeGenService codeGen = new SceneCodeGenerator();
            CodeGenResult genResult = await codeGen.GenerateSceneAsync(scene, project, settings).ConfigureAwait(true);
            if (!genResult.Success)
                Log($"[Build] CodeGen: {genResult.ErrorMessage}", LogLevel.Warning);
            else
                Log($"[Build] CodeGen OK: {Path.GetFileName(genResult.OutputPath)}", LogLevel.Info);
        }

        BuildStatusText = "Building solution…";
        BuildStatusColor = StatusDimColor;

        int exit = await MgcbRunner.RunDotnetBuildAsync(csproj, settings.BuildConfiguration, line =>
            Bus.Publish(new BuildOutputLineEvent(line, IsErrorLine(line)))).ConfigureAwait(true);

        Bus.Publish(new BuildOutputLineEvent(
            exit == 0 ? "Build succeeded" : $"Build FAILED (exit {exit})", exit != 0));

        Bus.Publish(new BuildFinishedEvent(exit, "Solution"));
    }

    [RelayCommand]
    public async Task GenerateCodeAsync()
    {
        EditorScene? scene = Context.ActiveScene;
        EditorProject? project = Context.ActiveProject;
        if (scene is null || project is null) return;
        if (DialogService.Navigation is not { } navigation) return;

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(settings.RootNamespace))
        {
            Log("[CodeGen] RootNamespace not set in Project Settings.", LogLevel.Warning);
            return;
        }

        var progressDlg = new CodeGenProgressDialog();
        _ = navigation.PushModalAsync(progressDlg);

        Bus.Publish(new CodeGenStartedEvent(scene.Name));
        ICodeGenService codeGen = new SceneCodeGenerator();

        CodeGenResult result = await codeGen.GenerateSceneAsync(scene, project, settings).ConfigureAwait(true);

        progressDlg.AddFileResult(result.OutputPath ?? string.Empty, result.Success);
        progressDlg.MarkComplete(result.Success ? 1 : 0, result.Success ? 0 : 1);
        Bus.Publish(new CodeGenCompletedEvent(result));
        Bus.Publish(new BuildFinishedEvent(result.Success ? 0 : 1, "CodeGen"));
    }

    [RelayCommand]
    private async Task RunGameAsync()
    {
        EditorProject? project = Context.ActiveProject;
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
                FileName = exePath,
                WorkingDirectory = dir,
                UseShellExecute = true,
            });
            return;
        }

        bool buildNow = await DialogService.ConfirmAsync(
            "Executable not found",
            $"'{exeName}' was not found in the output directories. Build the solution first?",
            "Build Now",
            "Cancel");

        if (buildNow)
            await BuildSolutionAsync().ConfigureAwait(true);
    }

    private static bool IsErrorLine(string line)
        => line.Contains(": error ", StringComparison.OrdinalIgnoreCase)
        || line.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase);

    private static string BuildEmptyMgcb() =>
        "#----------------------------- Global Properties ----------------------------#\n\n" +
        "/outputDir:bin/$(Platform)\n" +
        "/intermediateDir:obj/$(Platform)\n" +
        "/platform:DesktopGL\n" +
        "/config:\n" +
        "/profile:Reach\n" +
        "/compress:False\n\n" +
        "#-------------------------------- References --------------------------------#\n\n\n" +
        "#---------------------------------- Content ---------------------------------#\n";

    #endregion

    #region Toolbar — gizmo tools & toggles

    [RelayCommand]
    public void ActivateTool(object? parameter)
    {
        // Acepta tanto SceneTools (llamadas desde código) como string (CommandParameter de XAML).
        SceneTools tool = parameter switch
        {
            SceneTools t => t,
            string s when Enum.TryParse<SceneTools>(s, out SceneTools parsed) => parsed,
            _ => SceneTools.Select,
        };
        ActiveTool = tool;
        Context.Gizmos.Mode = tool switch
        {
            SceneTools.Move => GizmoMode.Move,
            SceneTools.Rotate => GizmoMode.Rotate,
            SceneTools.Scale => GizmoMode.Scale,
            SceneTools.Rect => GizmoMode.Rect,
            SceneTools.Universal => GizmoMode.Universal,
            _ => GizmoMode.Select,
        };
        ViewportInvalidateRequested?.Invoke();
    }

    [RelayCommand]
    public void ToggleSnap()
    {
        IsSnap = !IsSnap;
        Context.Gizmos.SnapEnabled = IsSnap;
    }

    [RelayCommand]
    public void ToggleToolMove()
    {
        ToolMoveEnabled = !ToolMoveEnabled;
        UpdateEnabledTools();
    }

    [RelayCommand]
    public void ToggleToolRotate()
    {
        ToolRotateEnabled = !ToolRotateEnabled;
        UpdateEnabledTools();
    }

    [RelayCommand]
    public void ToggleToolScale()
    {
        ToolScaleEnabled = !ToolScaleEnabled;
        UpdateEnabledTools();
    }

    [RelayCommand]
    public void ToggleAxisX()
    {
        AxisXEnabled = !AxisXEnabled;
        UpdateEnabledAxes();
    }

    [RelayCommand]
    public void ToggleAxisY()
    {
        AxisYEnabled = !AxisYEnabled;
        UpdateEnabledAxes();
    }

    private void UpdateEnabledTools()
    {
        GizmoTool tools = GizmoTool.None;
        if (ToolMoveEnabled) tools |= GizmoTool.Move;
        if (ToolRotateEnabled) tools |= GizmoTool.Rotate;
        if (ToolScaleEnabled) tools |= GizmoTool.Scale;
        Context.Gizmos.EnabledTools = tools;
        ViewportInvalidateRequested?.Invoke();
    }

    private void UpdateEnabledAxes()
    {
        GizmoAxisMask axes = GizmoAxisMask.None;
        if (AxisXEnabled) axes |= GizmoAxisMask.X;
        if (AxisYEnabled) axes |= GizmoAxisMask.Y;
        Context.Gizmos.EnabledAxes = axes;
        ViewportInvalidateRequested?.Invoke();
    }

    [RelayCommand]
    private void ToggleNav() => IsNav = !IsNav;

    [RelayCommand]
    private void ToggleRes() => IsRes = !IsRes;

    #endregion

    #region Transport — Play / Stop

    [RelayCommand]
    public void Play()
    {
        if (Context.State is EditorState.Playing) return;
        if (_playPendingAfterBuild) return;

        EditorScene? scene = Context.ActiveScene;
        EditorProject? project = Context.ActiveProject;
        if (scene is null) return;

        if (project is not null && !string.IsNullOrEmpty(scene.ScenePath))
        {
            string? exePath = FindGameExe(project);
            if (exePath is not null)
            {
                LaunchGame(exePath, scene);
                return;
            }

            Log("[Play] Executable not found. Building solution first...");
            _playPendingAfterBuild = true;
            CanPlay = false;
            PlayButtonText = "⏳";
            _ = BuildSolutionAsync();
            return;
        }

        Log("[Play] No project or scene configured.");
    }

    [RelayCommand]
    public void Stop()
    {
        if (Context.State is EditorState.Editing) return;

        _externalLauncher.Stop();

        EditorScene? restored = Context.RestoreFromSnapshot();
        Context.ClearPlaySnapshot();

        if (restored is not null)
            Context.SetActiveScene(restored);

        Context.SetState(EditorState.Editing);
        Log("[Play] Play mode stopped.");
    }

    private static string? FindGameExe(EditorProject project)
    {
        string dir = Path.GetDirectoryName(project.GameCsprojPath) ?? string.Empty;
        string exeName = Path.GetFileNameWithoutExtension(project.GameCsprojPath) + ".exe";
        string[] search =
        [
            Path.Combine(dir, "bin", "Debug",   "net10.0"),
            Path.Combine(dir, "bin", "Debug",   "net10.0-windows"),
            Path.Combine(dir, "bin", "Debug",   "net10.0-windows10.0.19041.0"),
            Path.Combine(dir, "bin", "Release",  "net10.0"),
            Path.Combine(dir, "bin", "Release",  "net10.0-windows"),
            Path.Combine(dir, "bin", "Release",  "net10.0-windows10.0.19041.0"),
        ];
        foreach (string candidate in search)
        {
            string p = Path.Combine(candidate, exeName);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private void LaunchGame(string exePath, EditorScene scene)
    {
        Context.TakePlaySnapshot();
        Context.SetState(EditorState.Playing);
        _externalLauncher.Launch(
            exePath,
            scene.ScenePath,
            logLine: line =>
            {
                if (line.StartsWith("[FPS]", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(line.AsSpan(5).Trim(), out int fps))
                    Bus.Publish(new FpsUpdatedEvent(fps));
                else
                    Bus.Publish(new BuildOutputLineEvent(line, false));
            },
            onExited: () => MainThread.BeginInvokeOnMainThread(Stop));
        Log("[Play] External game process started.");
    }

    private void OnBuildFinished(BuildFinishedEvent e)
    {
        if (!_playPendingAfterBuild || e.BuildType != "Solution") return;

        _playPendingAfterBuild = false;
        PlayButtonText = "▶";

        bool hasScene = Context.ActiveScene is not null;
        CanPlay = hasScene;

        if (!e.Success)
        {
            Log("[Play] Build failed. Cannot launch game.", LogLevel.Error);
            return;
        }

        EditorScene? scene = Context.ActiveScene;
        EditorProject? project = Context.ActiveProject;
        if (scene is null || project is null) return;

        string? exePath = FindGameExe(project);
        if (exePath is null)
        {
            Log("[Play] Build succeeded but executable not found.", LogLevel.Error);
            return;
        }

        LaunchGame(exePath, scene);
    }

    #endregion

    private static void Log(string message, LogLevel level = LogLevel.Info)
        => Bus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.UtcNow, level, message)));
}
