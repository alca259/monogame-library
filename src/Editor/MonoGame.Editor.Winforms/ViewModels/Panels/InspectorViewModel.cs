using CommunityToolkit.Mvvm.ComponentModel;

namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>
/// ViewModel del panel Inspector: selección activa, Transform, Active y escaneo de behaviours.
/// La construcción dinámica de tarjetas de behaviour se reserva para Fase 5.
/// </summary>
public sealed partial class InspectorViewModel : ViewModelBase
{
    private readonly GameObjectRegistry _registry = new();
    private bool _registryReady;

    protected override EditorFocusContext? FocusContext => EditorFocusContext.Inspector;

    public GameObjectRegistry Registry     => _registry;
    public bool               RegistryReady => _registryReady;

    /// <summary>Solicita a la vista repoblar Transform y reconstruir tarjetas de behaviour.</summary>
    public event Action? RefreshRequested;

    /// <summary>
    /// Solicita actualizar SOLO los valores de Transform, sin reconstruir las tarjetas.
    /// Evita el bucle Stepper.ValueCommitted → SetProperty → BuildBehaviourCards.
    /// </summary>
    public event Action? TransformOnlyRefreshRequested;

    [ObservableProperty]
    private EditorGameObject? _selected;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private bool _contentEnabled = true;

    [ObservableProperty]
    private string _objectName = string.Empty;

    [ObservableProperty]
    private string _objectIdShort = string.Empty;

    // ── Eventos del bus ────────────────────────────────────────────────────────

    protected override void RegisterEvents()
    {
        On<GameObjectSelectedEvent>(e =>
        {
            if (Selected == e.GameObject) return;
            Selected = e.GameObject;
            Refresh();
        });
        On<GameObjectPropertyChangedEvent>(_ => TransformOnlyRefreshRequested?.Invoke());
        On<UndoPerformedEvent>(_ => Refresh());
        On<RedoPerformedEvent>(_ => Refresh());
        On<EditorStateChangedEvent>(e => ContentEnabled = e.NewState is EditorState.Editing);
        On<ProjectOpenedEvent>(evt => _ = ScanProjectAsync());
    }

    protected override void OnAttached()
    {
        _registry.Scan();
        if (Context.ActiveProject is not null) _ = ScanProjectAsync();
    }

    private void Refresh()
    {
        HasSelection = Selected is not null;
        if (Selected is not null)
        {
            ObjectName    = Selected.Name;
            ObjectIdShort = Selected.Id.ToString()[..8];
        }
        RefreshRequested?.Invoke();
    }

    // ── Transform ─────────────────────────────────────────────────────────────

    public void ApplyPosX(double v)   { if (Selected is { } s) ExecuteTransform(s, new MoveEntityCommand(s,   new EditorVector3((float)v, s.Position.Y, s.Position.Z))); }
    public void ApplyPosY(double v)   { if (Selected is { } s) ExecuteTransform(s, new MoveEntityCommand(s,   new EditorVector3(s.Position.X, (float)v, s.Position.Z))); }
    public void ApplyPosZ(double v)   { if (Selected is { } s) ExecuteTransform(s, new MoveEntityCommand(s,   new EditorVector3(s.Position.X, s.Position.Y, (float)v))); }
    public void ApplyRotX(double v)   { if (Selected is { } s) ExecuteTransform(s, new RotateEntityCommand(s, new EditorVector3((float)v, s.Rotation.Y, s.Rotation.Z))); }
    public void ApplyRotY(double v)   { if (Selected is { } s) ExecuteTransform(s, new RotateEntityCommand(s, new EditorVector3(s.Rotation.X, (float)v, s.Rotation.Z))); }
    public void ApplyRotZ(double v)   { if (Selected is { } s) ExecuteTransform(s, new RotateEntityCommand(s, new EditorVector3(s.Rotation.X, s.Rotation.Y, (float)v))); }
    public void ApplyScaleX(double v) { if (Selected is { } s) ExecuteTransform(s, new ScaleEntityCommand(s,  new EditorVector3((float)v, s.Scale.Y, s.Scale.Z))); }
    public void ApplyScaleY(double v) { if (Selected is { } s) ExecuteTransform(s, new ScaleEntityCommand(s,  new EditorVector3(s.Scale.X, (float)v, s.Scale.Z))); }
    public void ApplyScaleZ(double v) { if (Selected is { } s) ExecuteTransform(s, new ScaleEntityCommand(s,  new EditorVector3(s.Scale.X, s.Scale.Y, (float)v))); }

    public void SetActive(bool next)
    {
        if (Selected is not { } s || s.Active == next) return;
        Context.Commands.Execute(
            new SetPropertyCommand<bool>("Set Active", s.Active, next, v => s.Active = v));
    }

    private void ExecuteTransform(EditorGameObject target, IEditorCommand cmd)
    {
        Context.Commands.Execute(cmd);
        Context.EventBus.Publish(new GameObjectSelectedEvent(target));
    }

    // ── Registry / escaneo de assemblies ─────────────────────────────────────

    public async Task ScanProjectAsync()
    {
        _registryReady = false;
        _registry.Scan();

        EditorProject? project = Context.ActiveProject;
        if (project is null) { _registryReady = true; return; }

        try
        {
            ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
            bool isDebug = settings.BuildConfiguration.Equals("Debug", StringComparison.OrdinalIgnoreCase);
            if (isDebug)
            {
                await ScanProjectAssembliesAsync(project, settings).ConfigureAwait(true);
                string sourceRoot = string.IsNullOrEmpty(project.GameSourcePath)
                    ? project.RootPath : project.GameSourcePath;
                await _registry.ScanSourceAsync(sourceRoot).ConfigureAwait(true);
            }
        }
        catch (Exception) { }

        _registryReady = true;
        if (Selected is not null) RefreshRequested?.Invoke();
    }

    private async Task ScanProjectAssembliesAsync(EditorProject project, ProjectSettings settings)
    {
        try
        {
            string[] files = Directory.GetFiles(project.RootPath, "*.csproj", SearchOption.AllDirectories);
            List<Task> tasks = [];
            foreach (string csproj in files)
            {
                string dir     = Path.GetDirectoryName(csproj) ?? string.Empty;
                string dllName = Path.GetFileNameWithoutExtension(csproj) + ".dll";
                string binDir  = Path.Combine(dir, "bin", settings.BuildConfiguration);
                if (!Directory.Exists(binDir)) continue;
                string? dll = Directory.GetFiles(binDir, dllName, SearchOption.AllDirectories).FirstOrDefault();
                if (dll is not null) tasks.Add(_registry.ScanFromAssemblyAsync(dll));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (IOException) { }
    }
}
