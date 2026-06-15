using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel del panel Inspector: selección activa, estado de pestañas, registro de tipos
/// de behaviour y comandos de Transform/Active/Add-Behaviour. La construcción dinámica de
/// las tarjetas de behaviour y el cableado de los steppers se mantienen en la vista
/// (acoplados a controles concretos); la VM avisa con <see cref="RefreshRequested"/>.
/// </summary>
public sealed partial class InspectorViewModel : ViewModelBase
{
    private readonly GameObjectRegistry _registry = new();
    private bool _registryReady;

    protected override EditorFocusContext? FocusContext => EditorFocusContext.Inspector;

    /// <summary>Registro de tipos de behaviour (lo consume la vista para construir tarjetas).</summary>
    public GameObjectRegistry Registry => _registry;

    public bool RegistryReady => _registryReady;

    /// <summary>Solicita a la vista repoblar Transform y reconstruir las tarjetas de behaviour.</summary>
    public event Action? RefreshRequested;

    [ObservableProperty]
    private EditorGameObject? _selected;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private bool _contentEnabled = true;

    [ObservableProperty]
    private string _activeTab = "Inspector";

    [ObservableProperty]
    private string _objectName = string.Empty;

    [ObservableProperty]
    private string _objectIdShort = string.Empty;

    protected override void RegisterEvents()
    {
        On<GameObjectSelectedEvent>(e => { Selected = e.GameObject; Refresh(); });
        On<UndoPerformedEvent>(_ => Refresh());
        On<RedoPerformedEvent>(_ => Refresh());
        On<EditorStateChangedEvent>(e => ContentEnabled = e.NewState is EditorState.Editing);
        On<ProjectOpenedEvent>(e => { _ = ScanProjectAsync(); });
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
            ObjectName = Selected.Name;
            ObjectIdShort = Selected.Id.ToString()[..8];
        }
        RefreshRequested?.Invoke();
    }

    [RelayCommand]
    private void SelectTab(string? tab) => ActiveTab = tab ?? "Inspector";

    // ── Transform / active commands (invocados por la vista al confirmar valores) ──

    public void ApplyPosX(double v)
    {
        if (Selected is { } s)
            Context.Commands.Execute(new MoveEntityCommand(s, new EditorVector3((float)v, s.Position.Y, s.Position.Z)));
    }

    public void ApplyPosY(double v)
    {
        if (Selected is { } s)
            Context.Commands.Execute(new MoveEntityCommand(s, new EditorVector3(s.Position.X, (float)v, s.Position.Z)));
    }

    public void ApplyPosZ(double v)
    {
        if (Selected is { } s)
            Context.Commands.Execute(new MoveEntityCommand(s, new EditorVector3(s.Position.X, s.Position.Y, (float)v)));
    }

    public void ApplyRotX(double v)
    {
        if (Selected is { } s)
            Context.Commands.Execute(new RotateEntityCommand(s, new EditorVector3((float)v, s.Rotation.Y, s.Rotation.Z)));
    }

    public void ApplyRotY(double v)
    {
        if (Selected is { } s)
            Context.Commands.Execute(new RotateEntityCommand(s, new EditorVector3(s.Rotation.X, (float)v, s.Rotation.Z)));
    }

    public void ApplyRotZ(double v)
    {
        if (Selected is { } s)
            Context.Commands.Execute(new RotateEntityCommand(s, new EditorVector3(s.Rotation.X, s.Rotation.Y, (float)v)));
    }

    public void ApplyScaleX(double v)
    {
        if (Selected is { } s)
            Context.Commands.Execute(new ScaleEntityCommand(s, new EditorVector3((float)v, s.Scale.Y, s.Scale.Z)));
    }

    public void ApplyScaleY(double v)
    {
        if (Selected is { } s)
            Context.Commands.Execute(new ScaleEntityCommand(s, new EditorVector3(s.Scale.X, (float)v, s.Scale.Z)));
    }

    public void ApplyScaleZ(double v)
    {
        if (Selected is { } s)
            Context.Commands.Execute(new ScaleEntityCommand(s, new EditorVector3(s.Scale.X, s.Scale.Y, (float)v)));
    }

    /// <summary>Alias de compatibilidad para el stepper de profundidad del Inspector (equivale a ApplyPosZ).</summary>
    public void ApplyDepth(double v) => ApplyPosZ(v);

    public void SetActive(bool next)
    {
        if (Selected is not { } s || s.Active == next) return;
        Context.Commands.Execute(
            new SetPropertyCommand<bool>("Set Active", s.Active, next, v => s.Active = v));
    }

    // ── Add behaviour ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddBehaviourAsync()
    {
        if (Selected is not { } selected) return;
        if (DialogService.Navigation is not { } navigation) return;

        if (!_registryReady)
            await ScanProjectAsync().ConfigureAwait(true);

        string? typeName = await AddBehaviourDialog.ShowAsync(
            navigation, _registry,
            async () => await ScanProjectAsync().ConfigureAwait(true));
        if (string.IsNullOrEmpty(typeName)) return;

        Context.Commands.Execute(new AddBehaviourCommand(selected, new EditorBehaviour { TypeName = typeName }));
        RefreshRequested?.Invoke();
    }

    // ── Registry scanning ─────────────────────────────────────────────────────────

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
                    ? project.RootPath
                    : project.GameSourcePath;
                await _registry.ScanSourceAsync(sourceRoot).ConfigureAwait(true);
            }
        }
        catch (Exception) { }

        _registryReady = true;
        if (Selected is not null)
            RefreshRequested?.Invoke();
    }

    private async Task ScanProjectAssembliesAsync(EditorProject project, ProjectSettings settings)
    {
        try
        {
            string[] csprojFiles = Directory.GetFiles(project.RootPath, "*.csproj", SearchOption.AllDirectories);

            List<Task> tasks = [];
            foreach (string csproj in csprojFiles)
            {
                string dir = Path.GetDirectoryName(csproj) ?? string.Empty;
                string dllName = Path.GetFileNameWithoutExtension(csproj) + ".dll";
                string binDir = Path.Combine(dir, "bin", settings.BuildConfiguration);
                if (!Directory.Exists(binDir)) continue;

                string? dllPath = Directory.GetFiles(binDir, dllName, SearchOption.AllDirectories).FirstOrDefault();
                if (dllPath is not null)
                    tasks.Add(_registry.ScanFromAssemblyAsync(dllPath));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (IOException) { }
    }
}
