namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>Diálogo para crear un nuevo proyecto de MonoGame Editor.</summary>
public sealed partial class NewProjectDialog : Form
{
    /// <summary>Constructor exclusivo del Designer.</summary>
    public NewProjectDialog()
    {
        InitializeComponent();
        WireEvents();
    }

    /// <summary>Nombre introducido por el usuario.</summary>
    public string ProjectName => _nameTextBox.Text.Trim();

    /// <summary>Carpeta padre seleccionada por el usuario (la subcarpeta del proyecto se creará dentro de ella).</summary>
    public string ParentPath => _locationTextBox.Text.Trim();

    /// <summary>Ruta absoluta al archivo .csproj del juego seleccionado por el usuario. Cadena vacía si no se ha seleccionado.</summary>
    public string GameCsprojPath => _csprojTextBox.Text.Trim();

    private void WireEvents()
    {
        _nameTextBox.TextChanged     += (_, _) => UpdatePreviewAndOk();
        _locationTextBox.TextChanged += (_, _) => UpdatePreviewAndOk();
        _csprojTextBox.TextChanged   += (_, _) => UpdatePreviewAndOk();
        _browseButton.Click          += OnBrowseClick;
        _browseCsprojButton.Click    += OnBrowseCsprojClick;
        _okButton.Click              += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        _cancelButton.Click          += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        string defaultLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _locationTextBox.Text = defaultLocation;
        UpdatePreviewAndOk();
    }

    private void OnBrowseClick(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dlg = new()
        {
            Description            = "Select the parent folder for the new project",
            UseDescriptionForTitle = true,
            InitialDirectory       = Directory.Exists(_locationTextBox.Text)
                ? _locationTextBox.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
            _locationTextBox.Text = dlg.SelectedPath;
    }

    private void OnBrowseCsprojClick(object? sender, EventArgs e)
    {
        using OpenFileDialog dlg = new()
        {
            Title            = "Select the main game .csproj",
            Filter           = "MonoGame Project (*.csproj)|*.csproj",
            InitialDirectory = Directory.Exists(ParentPath)
                ? ParentPath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        _csprojTextBox.Text = dlg.FileName;
        UpdatePreviewAndOk();
    }

    private void UpdatePreviewAndOk()
    {
        string name     = ProjectName;
        string location = ParentPath;
        string csproj   = GameCsprojPath;

        bool nameValid     = !string.IsNullOrWhiteSpace(name) && IsValidFolderName(name);
        bool locationValid = !string.IsNullOrWhiteSpace(location) && Directory.Exists(location);
        bool basicValid    = nameValid && locationValid;

        bool csprojProvided = !string.IsNullOrWhiteSpace(csproj);
        bool csprojMissing  = csprojProvided && !File.Exists(csproj);

        if (!basicValid)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location))
                _previewValueLabel.Text = string.Empty;
            else if (!nameValid)
                _previewValueLabel.Text = "Invalid project name.";
            else
                _previewValueLabel.Text = "Location folder does not exist.";

            _previewValueLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            _okButton.Enabled = false;
            return;
        }

        string targetPath = Path.Combine(location, name);
        bool targetExists = Directory.Exists(targetPath);
        bool alreadyInitialized = targetExists
            && File.Exists(Path.Combine(targetPath, "project.json"));

        if (alreadyInitialized)
        {
            _previewValueLabel.Text      = "Already an editor project — use 'Open Project' instead.";
            _previewValueLabel.ForeColor = System.Drawing.Color.OrangeRed;
            _okButton.Enabled = false;
        }
        else if (csprojMissing)
        {
            _previewValueLabel.Text      = $"{targetPath}  ⚠ .csproj file not found.";
            _previewValueLabel.ForeColor = System.Drawing.Color.OrangeRed;
            _okButton.Enabled = true;  // permite crear con advertencia
        }
        else if (targetExists)
        {
            _previewValueLabel.Text      = $"{targetPath}  (existing folder — editor will be initialized here)";
            _previewValueLabel.ForeColor = System.Drawing.Color.CornflowerBlue;
            _okButton.Enabled = true;
        }
        else
        {
            _previewValueLabel.Text      = targetPath;
            _previewValueLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            _okButton.Enabled = true;
        }
    }

    private static bool IsValidFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        char[] invalid = Path.GetInvalidFileNameChars();
        foreach (char c in name)
            if (Array.IndexOf(invalid, c) >= 0) return false;
        return true;
    }
}
