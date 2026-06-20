using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Drawing;
using WinForms = System.Windows.Forms;

namespace MonoGame.Editor.Winforms.ViewModels;

/// <summary>
/// ViewModel de la ventana principal (port de MAUI): estado del toolbar, status bar,
/// comandos de menú/transport y suscripciones al bus. Los paneles y el viewport se
/// conectan aquí via <see cref="ViewportInvalidateRequested"/>.
/// </summary>
public sealed partial class EditorWindowViewModel : ViewModelBase
{
    #region Colores estáticos

    private static readonly Color BuildSuccessColor  = EditorColors.PlayGreen;
    private static readonly Color BuildErrorFgColor  = EditorColors.BuildErrorFg;
    private static readonly Color BuildErrorBgColor  = EditorColors.BuildErrorBg;
    private static readonly Color BuildNormalBgColor = EditorColors.PanelBackgroundAlt;
    private static readonly Color StatusDimColor     = EditorColors.TextSecondary;
    private static readonly Color StopActiveBgColor  = EditorColors.StopRed;
    private static readonly Color StopInactiveBgColor = EditorColors.PanelBackgroundAlt;
    private static readonly Color StopActiveFgColor   = Color.White;
    private static readonly Color StopInactiveFgColor = EditorColors.TextMuted;

    #endregion

    #region Campos

    private readonly EditorPreferences _preferences = new();
    private readonly ExternalPlayLauncher _externalLauncher = new();
    private bool _playPendingAfterBuild;

    #endregion

    /// <summary>Preferencias del editor (la vista las usa para el layout de paneles).</summary>
    public EditorPreferences Preferences => _preferences;

    /// <summary>Solicita a la vista repintar el viewport.</summary>
    public event Action? ViewportInvalidateRequested;

    #region Estado observable

    public enum SceneTools { Select, Move, Rotate, Scale, Rect, Pan, Universal }

    [ObservableProperty] private SceneTools _activeTool = SceneTools.Select;
    [ObservableProperty] private bool _isSnap;
    [ObservableProperty] private bool _toolMoveEnabled   = true;
    [ObservableProperty] private bool _toolRotateEnabled = true;
    [ObservableProperty] private bool _toolScaleEnabled  = true;
    [ObservableProperty] private bool _axisXEnabled = true;
    [ObservableProperty] private bool _axisYEnabled = true;
    [ObservableProperty] private bool _isNav;
    [ObservableProperty] private bool _isRes;
    [ObservableProperty] private bool _toolsEnabled = true;
    [ObservableProperty] private bool _canPlay;
    [ObservableProperty] private bool _canStop;
    [ObservableProperty] private bool _stopActive;
    [ObservableProperty] private string _playButtonText   = "▶";
    [ObservableProperty] private Color  _stopBackground   = StopInactiveBgColor;
    [ObservableProperty] private Color  _stopForeground   = StopInactiveFgColor;
    [ObservableProperty] private string _title            = "MonoGame Editor";
    [ObservableProperty] private string _buildStatusText  = "Ready";
    [ObservableProperty] private Color  _buildStatusColor = StatusDimColor;
    [ObservableProperty] private Color  _buildStatusBackground = BuildNormalBgColor;
    [ObservableProperty] private string _objectCountText  = string.Empty;
    [ObservableProperty] private string _fpsText          = "-- FPS";
    [ObservableProperty] private bool   _isViewportFocused;

    #endregion

    #region Suscripciones al bus

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
            StopActive     = true;
            StopBackground = StopActiveBgColor;
            StopForeground = StopActiveFgColor;
        }
        else
        {
            StopActive     = false;
            StopBackground = StopInactiveBgColor;
            StopForeground = StopInactiveFgColor;
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
        if (e.Line.Contains("Build succeeded", StringComparison.OrdinalIgnoreCase))
        {
            BuildStatusText       = "Build succeeded";
            BuildStatusColor      = BuildSuccessColor;
            BuildStatusBackground = BuildNormalBgColor;
        }
        else if (e.Line.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase)
              || (e.IsError && e.Line.Contains("error", StringComparison.OrdinalIgnoreCase)))
        {
            BuildStatusText       = "Build failed";
            BuildStatusColor      = BuildErrorFgColor;
            BuildStatusBackground = BuildErrorBgColor;
        }
    }

    private void UpdateTitle()
    {
        string projectPart = Context.ActiveProject?.Name ?? "No Project";
        string scenePart   = Context.ActiveScene?.Name   ?? "No Scene";
        string dirtyMark   = Context.IsSceneDirty ? " ●" : string.Empty;
        Title = $"MonoGame Editor — {projectPart} — {scenePart}{dirtyMark}";
    }

    #endregion

    #region Auto-load

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
            Context.Logger.Log($"[Editor] Auto-loaded project '{project.Name}'.");
            await TryLoadLastSceneForProjectAsync(project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Context.Logger.Log($"[Editor] Auto-load failed: {ex.Message}", LogLevel.Warning);
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
            Context.Logger.Log($"[Editor] Auto-loaded last scene '{scene.Name}'.");
        }
        catch (Exception ex)
        {
            Context.Logger.Log($"[Editor] Failed to auto-load last scene: {ex.Message}", LogLevel.Warning);
        }
    }

    #endregion

    #region Menús — File

    /// <summary>
    /// Hook de diálogo — asignado por <see cref="Forms.MainForm"/> en Fase 7.
    /// Devuelve <c>null</c> si el usuario cancela.
    /// </summary>
    public Func<Task<NewProjectResult?>>? RequestNewProjectDialog { get; set; }

    /// <summary>Hook de diálogo NewScene — asignado por MainForm en Fase 7.</summary>
    public Func<Task<NewSceneResult?>>? RequestNewSceneDialog { get; set; }

    [RelayCommand]
    private async Task NewProjectAsync()
    {
        if (RequestNewProjectDialog is null) return;
        NewProjectResult? result = await RequestNewProjectDialog.Invoke().ConfigureAwait(true);
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
            Context.Logger.Log($"[Editor] Project '{project.Name}' created.");
        }
        catch (Exception ex)
        {
            Context.Logger.Log($"[Editor] Failed to create project: {ex.Message}", LogLevel.Error);
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
        Context.Logger.Log("[Editor] Project closed.");
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        string? path = await DialogService.PickFolderAsync("Open Project").ConfigureAwait(true);
        if (path is null) return;
        await OpenProjectByPathAsync(path).ConfigureAwait(true);
    }

    [RelayCommand]
    public async Task OpenProjectByPathAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            bool remove = await DialogService.ConfirmAsync(
                "Proyecto no encontrado",
                $"No se encontró el proyecto en:\n{path}\n\n¿Eliminarlo de proyectos recientes?",
                "Eliminar", "Cancelar").ConfigureAwait(true);
            if (remove) _preferences.RemoveRecentProject(path);
            return;
        }

        try
        {
            EditorProject? project = await Task.Run(() => ProjectManager.Load(path)).ConfigureAwait(true);
            if (project is null)
            {
                await DialogService.AlertAsync("Proyecto inválido", $"No se encontró un proyecto válido en: {path}").ConfigureAwait(true);
                return;
            }
            Context.SetActiveProject(project);
            Context.SetActiveScene(null);
            _preferences.LastProjectPath = path;
            _preferences.AddRecentProject(path);
            await ApplyProjectSettingsAsync(project).ConfigureAwait(true);
            Context.Logger.Log($"[Editor] Project '{project.Name}' opened.");
            await TryLoadLastSceneForProjectAsync(project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Context.Logger.Log($"[Editor] Open project error: {ex.Message}", LogLevel.Error);
        }
    }

    [RelayCommand]
    private async Task NewSceneAsync()
    {
        if (RequestNewSceneDialog is null) return;
        NewSceneResult? result = await RequestNewSceneDialog.Invoke().ConfigureAwait(true);
        if (result is null) return;

        EditorProject? project = Context.ActiveProject;
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
            scene.ScenePath = scenePath;
            try { await SceneSerializer.SaveAsync(scene, scenePath).ConfigureAwait(true); }
            catch (Exception ex) { Context.Logger.Log($"[Editor] Failed to save scene: {ex.Message}", LogLevel.Error); }
        }

        Context.SetActiveScene(scene);
        Bus.Publish(new SceneCreatedEvent(scene));
        Context.Logger.Log($"[Editor] New scene '{scene.Name}' created.");
    }

    [RelayCommand]
    public async Task SaveSceneAsync()
    {
        EditorScene? scene = Context.ActiveScene;
        EditorProject? project = Context.ActiveProject;
        if (scene is null) return;

        if (string.IsNullOrEmpty(scene.ScenePath))
        {
            await SaveSceneAsAsync().ConfigureAwait(true);
            return;
        }

        try
        {
            await SceneSerializer.SaveAsync(scene, scene.ScenePath).ConfigureAwait(true);
            Context.MarkSceneClean();
            BuildStatusText = "Saved";
            BuildStatusColor = BuildSuccessColor;
            Context.Logger.Log($"[Save] Scene saved to {scene.ScenePath}");

            if (project is not null)
                await TryGenerateCodeOnSaveAsync(scene, project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Context.Logger.Log($"[Save] Error: {ex.Message}", LogLevel.Error);
        }
    }

    [RelayCommand]
    public async Task SaveSceneAsAsync()
    {
        EditorScene? scene = Context.ActiveScene;
        EditorProject? project = Context.ActiveProject;
        if (scene is null) return;

        try
        {
            string suggested = string.IsNullOrEmpty(scene.Name) ? "NewScene" : scene.Name;
            string? path = await DialogService.SaveFileAsync(
                suggestedName: suggested + ".scene.json",
                title: "Save Scene As",
                filter: "Scene files (*.scene.json)|*.scene.json|All files (*.*)|*.*").ConfigureAwait(true);
            if (string.IsNullOrEmpty(path)) return;

            scene.ScenePath = path;
            scene.Name      = Path.GetFileNameWithoutExtension(path).Replace(".scene", "");
            await SceneSerializer.SaveAsync(scene, path).ConfigureAwait(true);
            Context.MarkSceneClean();
            UpdateTitle();
            Context.Logger.Log($"[Save] Scene saved to {path}");
        }
        catch (Exception ex)
        {
            Context.Logger.Log($"[Save] Error: {ex.Message}", LogLevel.Error);
        }
    }

    private async Task TryGenerateCodeOnSaveAsync(EditorScene scene, EditorProject project)
    {
        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        if (!settings.GenerateOnSave) return;
        if (string.IsNullOrEmpty(project.GameCsprojPath) || string.IsNullOrWhiteSpace(settings.RootNamespace)) return;

        Bus.Publish(new CodeGenStartedEvent(scene.Name));
        ICodeGenService codeGen = new SceneCodeGenerator();
        CodeGenResult result = await codeGen.GenerateSceneAsync(scene, project, settings).ConfigureAwait(true);
        Bus.Publish(new CodeGenCompletedEvent(result));
        Context.Logger.Log(
            result.Success ? $"[CodeGen] {result.OutputPath}" : $"[CodeGen] Error: {result.ErrorMessage}",
            result.Success ? LogLevel.Info : LogLevel.Error);
    }

    [RelayCommand]
    private void Exit() => WinForms.Application.Exit();

    #endregion

    #region Menús — Edit

    [RelayCommand] private void Undo() => Context.Commands.Undo();
    [RelayCommand] private void Redo() => Context.Commands.Redo();

    [RelayCommand]
    public void DeleteSelected()
    {
        EditorGameObject? obj = Context.SelectedObject;
        EditorScene? scene    = Context.ActiveScene;
        if (obj is null || scene is null) return;
        Context.Commands.Execute(new DeleteEntityCommand(obj, scene));
    }

    [RelayCommand]
    private void DuplicateSelected()
    {
        EditorGameObject? obj = Context.SelectedObject;
        EditorScene? scene    = Context.ActiveScene;
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

    [RelayCommand] private void Copy()
    {
        EditorGameObject? obj = Context.SelectedObject;
        if (obj is null) return;
        Context.SetClipboard(DuplicateEntityCommand.DeepClone(obj, null));
    }

    [RelayCommand] private void Cut()
    {
        EditorGameObject? obj = Context.SelectedObject;
        EditorScene? scene    = Context.ActiveScene;
        if (obj is null || scene is null) return;
        Context.SetClipboard(DuplicateEntityCommand.DeepClone(obj, null));
        Context.Commands.Execute(new DeleteEntityCommand(obj, scene));
    }

    [RelayCommand] private void Paste()
    {
        EditorGameObject? clipboard = Context.ClipboardEntity;
        EditorScene? scene          = Context.ActiveScene;
        if (clipboard is null || scene is null) return;
        EditorGameObject? parent = Context.SelectedObject;
        EditorGameObject clone   = DuplicateEntityCommand.DeepClone(clipboard, parent);
        Context.Commands.Execute(new CreateEntityCommand(clone, scene, parent));
    }

    private static void CollectAll(List<EditorGameObject> source, List<EditorGameObject> result)
    {
        foreach (EditorGameObject obj in source)
        {
            result.Add(obj);
            CollectAll(obj.Children, result);
        }
    }

    #endregion

    #region Menús — Project

    public Func<Task>? RequestProjectSettingsDialog { get; set; }

    /// <summary>
    /// Hook: abre el formulario de progreso de code gen y devuelve los callbacks
    /// para reportar resultados. Asignado por MainForm en Fase 7.
    /// </summary>
    public Func<CodeGenProgressCallbacks?>? OpenCodeGenProgressDialog { get; set; }

    [RelayCommand]
    private async Task OpenProjectSettingsAsync()
    {
        EditorProject? project = Context.ActiveProject;
        if (project is null) return;

        if (RequestProjectSettingsDialog is not null)
            await RequestProjectSettingsDialog.Invoke().ConfigureAwait(true);

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
        }

        BuildStatusText       = "Building content…";
        BuildStatusColor      = StatusDimColor;
        BuildStatusBackground = BuildNormalBgColor;

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
            Context.Logger.Log("[Build] Game .csproj path not configured.", LogLevel.Warning);
            return;
        }

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        EditorScene? scene = Context.ActiveScene;
        if (scene is not null && !string.IsNullOrWhiteSpace(settings.RootNamespace)
            && !string.IsNullOrEmpty(project.GameSourcePath))
        {
            ICodeGenService codeGen = new SceneCodeGenerator();
            CodeGenResult genResult = await codeGen.GenerateSceneAsync(scene, project, settings).ConfigureAwait(true);
            Context.Logger.Log(
                genResult.Success ? $"[Build] CodeGen OK: {Path.GetFileName(genResult.OutputPath)}" : $"[Build] CodeGen: {genResult.ErrorMessage}",
                genResult.Success ? LogLevel.Info : LogLevel.Warning);
        }

        BuildStatusText  = "Building solution…";
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
        EditorScene? scene   = Context.ActiveScene;
        EditorProject? project = Context.ActiveProject;
        if (scene is null || project is null) return;

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(settings.RootNamespace))
        {
            Context.Logger.Log("[CodeGen] RootNamespace not set in Project Settings.", LogLevel.Warning);
            return;
        }

        CodeGenProgressCallbacks? progress = OpenCodeGenProgressDialog?.Invoke();

        Bus.Publish(new CodeGenStartedEvent(scene.Name));
        ICodeGenService codeGen = new SceneCodeGenerator();
        CodeGenResult result = await codeGen.GenerateSceneAsync(scene, project, settings).ConfigureAwait(true);

        progress?.AddFileResult(result.OutputPath ?? "(error)", result.Success);
        progress?.MarkComplete(result.Success ? 1 : 0, result.Success ? 0 : 1);

        Bus.Publish(new CodeGenCompletedEvent(result));
        Bus.Publish(new BuildFinishedEvent(result.Success ? 0 : 1, "CodeGen"));
        Context.Logger.Log(
            result.Success ? $"[CodeGen] {result.OutputPath}" : $"[CodeGen] Error: {result.ErrorMessage}",
            result.Success ? LogLevel.Info : LogLevel.Error);
    }

    [RelayCommand]
    private async Task RunGameAsync()
    {
        EditorProject? project = Context.ActiveProject;
        if (project is null || string.IsNullOrEmpty(project.GameCsprojPath)) return;

        string dir     = Path.GetDirectoryName(project.GameCsprojPath) ?? string.Empty;
        string exeName = Path.GetFileNameWithoutExtension(project.GameCsprojPath) + ".exe";
        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        string[] searchDirs =
        [
            Path.Combine(dir, "bin", settings.BuildConfiguration, "net10.0-windows"),
            Path.Combine(dir, "bin", settings.BuildConfiguration, "net10.0"),
        ];

        foreach (string candidate in searchDirs)
        {
            string exePath = Path.Combine(candidate, exeName);
            if (!File.Exists(exePath)) continue;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath, WorkingDirectory = dir, UseShellExecute = true,
            });
            return;
        }

        bool buildNow = await DialogService.ConfirmAsync(
            "Ejecutable no encontrado",
            $"'{exeName}' no se encontró. ¿Compilar la solución ahora?",
            "Compilar", "Cancelar").ConfigureAwait(true);
        if (buildNow) await BuildSolutionAsync().ConfigureAwait(true);
    }

    private static bool IsErrorLine(string line)
        => line.Contains(": error ", StringComparison.OrdinalIgnoreCase)
        || line.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase);

    private static string BuildEmptyMgcb() =>
        "#----------------------------- Global Properties ----------------------------#\n\n" +
        "/outputDir:bin/$(Platform)\n/intermediateDir:obj/$(Platform)\n/platform:DesktopGL\n" +
        "/config:\n/profile:Reach\n/compress:False\n\n" +
        "#-------------------------------- References --------------------------------#\n\n\n" +
        "#---------------------------------- Content ---------------------------------#\n";

    #endregion

    #region Toolbar — gizmo tools y toggles

    [RelayCommand]
    public void ActivateTool(object? parameter)
    {
        SceneTools tool = parameter switch
        {
            SceneTools t => t,
            string s when Enum.TryParse<SceneTools>(s, out SceneTools p) => p,
            _ => SceneTools.Select,
        };
        ActiveTool = tool;
        Context.Gizmos.Mode = tool switch
        {
            SceneTools.Move      => GizmoMode.Move,
            SceneTools.Rotate    => GizmoMode.Rotate,
            SceneTools.Scale     => GizmoMode.Scale,
            SceneTools.Rect      => GizmoMode.Rect,
            SceneTools.Universal => GizmoMode.Universal,
            _                    => GizmoMode.Select,
        };
        ViewportInvalidateRequested?.Invoke();
    }

    [RelayCommand] public void ToggleSnap()        { IsSnap = !IsSnap; Context.Gizmos.SnapEnabled = IsSnap; }
    [RelayCommand] public void ToggleToolMove()    { ToolMoveEnabled   = !ToolMoveEnabled;   UpdateEnabledTools(); }
    [RelayCommand] public void ToggleToolRotate()  { ToolRotateEnabled = !ToolRotateEnabled; UpdateEnabledTools(); }
    [RelayCommand] public void ToggleToolScale()   { ToolScaleEnabled  = !ToolScaleEnabled;  UpdateEnabledTools(); }
    [RelayCommand] public void ToggleAxisX()       { AxisXEnabled = !AxisXEnabled; UpdateEnabledAxes(); }
    [RelayCommand] public void ToggleAxisY()       { AxisYEnabled = !AxisYEnabled; UpdateEnabledAxes(); }
    [RelayCommand] private void ToggleNav()        => IsNav = !IsNav;
    [RelayCommand] private void ToggleRes()        => IsRes = !IsRes;

    private void UpdateEnabledTools()
    {
        GizmoTool tools = GizmoTool.None;
        if (ToolMoveEnabled)   tools |= GizmoTool.Move;
        if (ToolRotateEnabled) tools |= GizmoTool.Rotate;
        if (ToolScaleEnabled)  tools |= GizmoTool.Scale;
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

    #endregion

    #region Transport — Play / Stop

    [RelayCommand]
    public void Play()
    {
        if (Context.State is EditorState.Playing || _playPendingAfterBuild) return;
        EditorScene? scene   = Context.ActiveScene;
        EditorProject? project = Context.ActiveProject;
        if (scene is null) return;

        if (project is not null && !string.IsNullOrEmpty(scene.ScenePath))
        {
            string? exePath = FindGameExe(project);
            if (exePath is not null) { LaunchGame(exePath, scene); return; }

            Context.Logger.Log("[Play] Executable not found. Building solution first...");
            _playPendingAfterBuild = true;
            CanPlay = false;
            PlayButtonText = "⏳";
            _ = BuildSolutionAsync();
            return;
        }

        Context.Logger.Log("[Play] No project or scene configured.");
    }

    [RelayCommand]
    public void Stop()
    {
        if (Context.State is EditorState.Editing) return;
        _externalLauncher.Stop();
        EditorScene? restored = Context.RestoreFromSnapshot();
        Context.ClearPlaySnapshot();
        if (restored is not null) Context.SetActiveScene(restored);
        Context.SetState(EditorState.Editing);
        Context.Logger.Log("[Play] Play mode stopped.");
    }

    private static string? FindGameExe(EditorProject project)
    {
        string dir     = Path.GetDirectoryName(project.GameCsprojPath) ?? string.Empty;
        string exeName = Path.GetFileNameWithoutExtension(project.GameCsprojPath) + ".exe";
        string[] search =
        [
            Path.Combine(dir, "bin", "Debug",   "net10.0"),
            Path.Combine(dir, "bin", "Debug",   "net10.0-windows"),
            Path.Combine(dir, "bin", "Release",  "net10.0"),
            Path.Combine(dir, "bin", "Release",  "net10.0-windows"),
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
            onExited: () => UiDispatcher.Post(Stop));
        Context.Logger.Log("[Play] External game process started.");
    }

    private void OnBuildFinished(BuildFinishedEvent e)
    {
        if (!_playPendingAfterBuild || e.BuildType != "Solution") return;

        _playPendingAfterBuild = false;
        PlayButtonText = "▶";
        CanPlay = Context.ActiveScene is not null;

        if (!e.Success) { Context.Logger.Log("[Play] Build failed. Cannot launch game.", LogLevel.Error); return; }

        EditorScene? scene     = Context.ActiveScene;
        EditorProject? project = Context.ActiveProject;
        if (scene is null || project is null) return;

        string? exePath = FindGameExe(project);
        if (exePath is null) { Context.Logger.Log("[Play] Build succeeded but executable not found.", LogLevel.Error); return; }

        LaunchGame(exePath, scene);
    }

    #endregion
}
