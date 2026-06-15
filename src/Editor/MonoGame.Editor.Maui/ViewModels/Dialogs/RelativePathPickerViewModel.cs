using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>
/// ViewModel del selector de ruta relativa. Navega el sistema de ficheros con raíz en
/// <see cref="BaseFolder"/> y devuelve la ruta relativa al elemento elegido.
/// En modo carpeta solo muestra y permite seleccionar directorios; en modo fichero
/// también muestra los ficheros que coincidan con el filtro de extensiones.
/// </summary>
public sealed partial class RelativePathPickerViewModel : DialogViewModel<string>
{
    private string _baseFolder = string.Empty;
    private bool _filesMode;
    private HashSet<string> _extensions = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _expandedPaths = new(StringComparer.OrdinalIgnoreCase);
    private string? _selectedPath;
    private bool _syncing;

    public ObservableCollection<FileSystemNode> Nodes { get; } = [];

    [ObservableProperty]
    private string _dialogTitle = "Select Path";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private FileSystemNode? _selectedNode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RelativePathDisplay))]
    private string _currentRelativePath = string.Empty;

    [ObservableProperty]
    private string _emptyMessage = "Empty folder";

    /// <summary>Texto del breadcrumb: la ruta relativa seleccionada o un placeholder.</summary>
    public string RelativePathDisplay => string.IsNullOrEmpty(CurrentRelativePath)
        ? "(no selection)"
        : CurrentRelativePath;

    /// <summary>
    /// Inicializa el VM con la carpeta raíz y las opciones del selector.
    /// </summary>
    /// <param name="baseFolder">Carpeta raíz absoluta desde la que se navega.</param>
    /// <param name="filesMode"><c>true</c> para seleccionar ficheros; <c>false</c> para carpetas.</param>
    /// <param name="extensions">Extensiones permitidas en modo fichero, ej. <c>[".csproj"]</c>. Nulo o vacío = cualquier fichero.</param>
    /// <param name="title">Texto del encabezado del diálogo.</param>
    public void Initialize(string baseFolder, bool filesMode = false,
                           string[]? extensions = null, string title = "Select Path")
    {
        _baseFolder = baseFolder;
        _filesMode = filesMode;
        _extensions = extensions is { Length: > 0 }
            ? new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        DialogTitle = title;

        RebuildTree();
    }

    partial void OnSelectedNodeChanged(FileSystemNode? value)
    {
        if (_syncing || value is null) return;

        if (value.IsDirectory) 
        {
            if (_expandedPaths.Contains(value.FullPath))
                _expandedPaths.Remove(value.FullPath);
            else
                _expandedPaths.Add(value.FullPath);

            if (!_filesMode)
            {
                _selectedPath = value.FullPath;
                CurrentRelativePath = Path.GetRelativePath(_baseFolder, value.FullPath);
            }
            else
            {
                // En modo fichero los directorios no son seleccionables
                _selectedPath = null;
                CurrentRelativePath = string.Empty;
            }

            RebuildTree();
            return;
        }

        // Nodo fichero — solo alcanzable cuando _filesMode == true
        _selectedPath = value.FullPath;
        CurrentRelativePath = Path.GetRelativePath(_baseFolder, value.FullPath);
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Submit()
    {
        if (string.IsNullOrEmpty(CurrentRelativePath)) return;
        Close(CurrentRelativePath);
    }

    private bool CanSubmit() => SelectedNode is not null;

    // ── Tree building ──────────────────────────────────────────────────────────

    private void RebuildTree()
    {
        _syncing = true;
        try
        {
            Nodes.Clear();
            SelectedNode = null;

            if (!Directory.Exists(_baseFolder))
            {
                EmptyMessage = $"Folder not found: {_baseFolder}";
                return;
            }

            EmptyMessage = "Empty folder";
            FlattenDirectory(_baseFolder, 0);

            // Restaurar la selección dentro del bloque syncing para que OnSelectedNodeChanged
            // no procese el nodo restaurado como un clic nuevo y evitar el bucle infinito.
            if (_selectedPath is null) return;

            FileSystemNode? toSelect = null;
            foreach (FileSystemNode n in Nodes)
            {
                if (n.FullPath.Equals(_selectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    toSelect = n;
                    break;
                }
            }

            if (toSelect is not null)
                SelectedNode = toSelect;
            else
            {
                _selectedPath = null;
                CurrentRelativePath = string.Empty;
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    private void FlattenDirectory(string path, int depth)
    {
        string[] subdirs;
        string[] files;

        try
        {
            subdirs = Directory.GetDirectories(path);
            Array.Sort(subdirs, StringComparer.OrdinalIgnoreCase);

            files = _filesMode ? Directory.GetFiles(path) : [];
            if (files.Length > 0) Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return;
        }

        foreach (string dir in subdirs)
        {
            bool expanded = _expandedPaths.Contains(dir);
            bool hasChildren = HasVisibleChildren(dir);
            Nodes.Add(new FileSystemNode(Path.GetFileName(dir), dir, true, depth, expanded, hasChildren));
            if (expanded)
                FlattenDirectory(dir, depth + 1);
        }

        foreach (string file in files)
        {
            if (_extensions.Count > 0 && !_extensions.Contains(Path.GetExtension(file)))
                continue;
            Nodes.Add(new FileSystemNode(Path.GetFileName(file), file, false, depth, false, false));
        }
    }

    private bool HasVisibleChildren(string dir)
    {
        try
        {
            if (Directory.GetDirectories(dir).Length > 0) return true;
            if (!_filesMode) return false;

            string[] files = Directory.GetFiles(dir);
            if (_extensions.Count == 0) return files.Length > 0;

            foreach (string f in files)
            {
                if (_extensions.Contains(Path.GetExtension(f))) return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
