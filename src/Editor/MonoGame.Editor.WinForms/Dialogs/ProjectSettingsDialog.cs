namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>Dialog for viewing and editing per-project editor settings.</summary>
public sealed partial class ProjectSettingsDialog : Form
{
    private readonly EditorProject _project = null!;
    private readonly ProjectSettings _settings = null!;

    /// <summary>Designer-only constructor.</summary>
    public ProjectSettingsDialog() => InitializeComponent();

    public ProjectSettingsDialog(EditorProject project, ProjectSettings settings)
    {
        _project  = project;
        _settings = settings;
        InitializeComponent();
        PopulateFromSettings();
    }

    #region Populate / Update UI

    private void PopulateFromSettings()
    {
        // General tab
        _projectNameBox.Text   = _project.Name;
        _versionBox.Text       = "1.0";
        _csprojBox.Text        = _project.GameCsprojPath;
        _editorFolderBox.Text  = _project.EditorPath;
        _rootNamespaceBox.Text = _settings.RootNamespace;
        _virtualWidthInput.Value  = Math.Clamp(_settings.VirtualWidth,  (int)_virtualWidthInput.Minimum,  (int)_virtualWidthInput.Maximum);
        _virtualHeightInput.Value = Math.Clamp(_settings.VirtualHeight, (int)_virtualHeightInput.Minimum, (int)_virtualHeightInput.Maximum);

        // Content tab
        _contentFolderBox.Text = _project.ContentPath;

        string mgcbDefault = Path.Combine(_project.ContentPath, "Content.mgcb");
        _mgcbFileBox.Text = File.Exists(mgcbDefault) ? mgcbDefault : string.Empty;

        if (_buildConfigCombo.Items.Contains(_settings.BuildConfiguration))
            _buildConfigCombo.SelectedItem = _settings.BuildConfiguration;
        else
            _buildConfigCombo.SelectedIndex = 0;

        // Localization tab
        _locFolderBox.Text = _project.LocalizationPath;

        RefreshLocaleDropdown();
        _defaultLocaleCombo.Text = _settings.DefaultLocale;

        _supportedLocalesGrid.Rows.Clear();
        for (int i = 0; i < _settings.SupportedLocales.Count; i++)
            _supportedLocalesGrid.Rows.Add(_settings.SupportedLocales[i]);

        // Code Generation tab
        _outputFolderBox.Text           = _settings.GeneratedCodeFolder;
        _generateOnSaveCheckBox.Checked = _settings.GenerateOnSave;
        UpdatePreviewOutputLabel();
    }

    private void RefreshLocaleDropdown()
    {
        string current = _defaultLocaleCombo.Text;
        _defaultLocaleCombo.Items.Clear();
        for (int i = 0; i < _settings.SupportedLocales.Count; i++)
            _defaultLocaleCombo.Items.Add(_settings.SupportedLocales[i]);
        if (!string.IsNullOrEmpty(current)) _defaultLocaleCombo.Text = current;
    }

    private void UpdatePreviewOutputLabel()
    {
        string folder = _outputFolderBox.Text.Trim();
        if (string.IsNullOrEmpty(_project.GameSourcePath) || string.IsNullOrEmpty(folder))
        {
            _previewOutputLabel.Text = "(set a game .csproj to see the path)";
            return;
        }
        _previewOutputLabel.Text = Path.Combine(_project.GameSourcePath, folder, "Scenes");
    }

    #endregion

    #region Browse handlers

    private void OnBrowseCsprojClick(object? sender, EventArgs e)
    {
        string initialDir = Directory.Exists(Path.GetDirectoryName(_csprojBox.Text))
            ? Path.GetDirectoryName(_csprojBox.Text)!
            : _project.RootPath;

        using OpenFileDialog dlg = new()
        {
            Title            = "Select the main game .csproj",
            Filter           = "MonoGame Project (*.csproj)|*.csproj",
            InitialDirectory = initialDir,
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
            _csprojBox.Text = dlg.FileName;
    }

    private void OnBrowseContentFolderClick(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dlg = new()
        {
            Description      = "Select the Content folder",
            InitialDirectory = Directory.Exists(_contentFolderBox.Text)
                ? _contentFolderBox.Text
                : _project.RootPath,
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _contentFolderBox.Text = dlg.SelectedPath;
        string mgcbPath = Path.Combine(dlg.SelectedPath, "Content.mgcb");
        if (File.Exists(mgcbPath)) _mgcbFileBox.Text = mgcbPath;
    }

    private void OnBrowseMgcbClick(object? sender, EventArgs e)
    {
        string initialDir = Directory.Exists(Path.GetDirectoryName(_mgcbFileBox.Text))
            ? Path.GetDirectoryName(_mgcbFileBox.Text)!
            : _project.ContentPath;

        using OpenFileDialog dlg = new()
        {
            Title            = "Select the MGCB file",
            Filter           = "MonoGame Content Builder (*.mgcb)|*.mgcb",
            InitialDirectory = initialDir,
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
            _mgcbFileBox.Text = dlg.FileName;
    }

    private void OnBrowseLocFolderClick(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dlg = new()
        {
            Description      = "Select the Localization folder",
            InitialDirectory = Directory.Exists(_locFolderBox.Text)
                ? _locFolderBox.Text
                : _project.RootPath,
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
            _locFolderBox.Text = dlg.SelectedPath;
    }

    #endregion

    #region Code Gen tab

    private void OnOutputFolderChanged(object? sender, EventArgs e) => UpdatePreviewOutputLabel();

    #endregion

    #region OK

    private void OnOkClick(object? sender, EventArgs e)
    {
        _settings.RootNamespace       = _rootNamespaceBox.Text.Trim();
        _settings.BuildConfiguration  = _buildConfigCombo.SelectedItem?.ToString() ?? "Debug";
        _settings.VirtualWidth        = (int)_virtualWidthInput.Value;
        _settings.VirtualHeight       = (int)_virtualHeightInput.Value;
        string folder = _outputFolderBox.Text.Trim();
        _settings.GeneratedCodeFolder = folder.Length > 0 ? folder : "Generated";
        _settings.GenerateOnSave      = _generateOnSaveCheckBox.Checked;

        string defaultLocale = _defaultLocaleCombo.Text.Trim();
        _settings.DefaultLocale = defaultLocale.Length > 0 ? defaultLocale : "en-US";

        _settings.SupportedLocales.Clear();
        for (int i = 0; i < _supportedLocalesGrid.Rows.Count; i++)
        {
            object? val = _supportedLocalesGrid.Rows[i].Cells[0].Value;
            if (val is string s && !string.IsNullOrWhiteSpace(s))
                _settings.SupportedLocales.Add(s.Trim());
        }

        if (_settings.SupportedLocales.Count == 0)
            _settings.SupportedLocales.Add("en-US");
    }

    #endregion
}
