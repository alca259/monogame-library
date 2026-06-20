namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>Entrada de fichero <c>.cs</c> en el listado del Script Browser.</summary>
public sealed record ScriptEntry(string FilePath, string FileName);

/// <summary>
/// ViewModel de la pestaña Script Browser: árbol de subcarpetas del directorio de
/// scripts del juego y lista de ficheros <c>.cs</c> de la carpeta seleccionada.
/// </summary>
public sealed class ScriptBrowserViewModel : ViewModelBase
{
    private readonly List<FolderEntry> _allFolders = [];
    private string? _selectedFolderPath;

    public event Action? FolderListChanged;
    public event Action? ScriptListChanged;

    public IReadOnlyList<FolderEntry> FolderItems { get; private set; } = [];
    public IReadOnlyList<ScriptEntry> ScriptItems { get; private set; } = [];
    public bool CanCreateScript => !string.IsNullOrEmpty(_selectedFolderPath);

    protected override void RegisterEvents()
    {
        On<ProjectOpenedEvent>(OnProjectOpened);
    }

    protected override void OnAttached()
    {
        if (Context.ActiveProject is { } project)
            OnProjectOpened(new ProjectOpenedEvent(project));
    }

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _selectedFolderPath = null;
        _allFolders.Clear();
        ScriptItems = [];

        if (e.Project is not null && Directory.Exists(e.Project.GameScriptsPath))
            BuildFolderTree(e.Project.GameScriptsPath, 0);

        FolderItems = _allFolders.AsReadOnly();
        FolderListChanged?.Invoke();
        ScriptListChanged?.Invoke();
    }

    private void BuildFolderTree(string path, int depth)
    {
        string[] subdirs = Directory.GetDirectories(path)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToArray();
        bool hasChildren = subdirs.Length > 0;
        _allFolders.Add(new FolderEntry(path, depth, hasChildren, depth == 0, depth == 0));
        foreach (string sub in subdirs)
            BuildFolderTree(sub, depth + 1);
    }

    public void SelectFolder(string? path)
    {
        _selectedFolderPath = path;
        RefreshScripts();
    }

    private void RefreshScripts()
    {
        if (string.IsNullOrEmpty(_selectedFolderPath) || !Directory.Exists(_selectedFolderPath))
        {
            ScriptItems = [];
            ScriptListChanged?.Invoke();
            return;
        }

        List<ScriptEntry> entries = [];
        foreach (string file in Directory.GetFiles(_selectedFolderPath, "*.cs", SearchOption.TopDirectoryOnly)
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            entries.Add(new ScriptEntry(file, Path.GetFileName(file)));
        }

        ScriptItems = entries;
        ScriptListChanged?.Invoke();
    }

    public async Task CreateScriptAsync(ScriptCreationResult result)
    {
        if (!CanCreateScript) return;

        string targetFolder = string.IsNullOrWhiteSpace(result.RelativeFolder)
            ? _selectedFolderPath!
            : Path.Combine(_selectedFolderPath!, result.RelativeFolder.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(targetFolder);

        string filePath = Path.Combine(targetFolder, $"{result.ClassName}.cs");
        string content = $"namespace {result.NamespaceName};\r\n\r\npublic sealed class {result.ClassName}\r\n{{\r\n}}\r\n";

        await File.WriteAllTextAsync(filePath, content, System.Text.Encoding.UTF8).ConfigureAwait(true);
        SelectFolder(_selectedFolderPath);
    }
}
