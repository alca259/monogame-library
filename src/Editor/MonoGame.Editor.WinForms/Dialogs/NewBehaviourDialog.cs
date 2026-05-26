using System.Text.RegularExpressions;

namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>
/// Modal dialog that lets the user scaffold a new <c>GameBehaviour</c> subclass.
/// Accessible from InspectorPanel (Add Behaviour → Create New...) and from Project menu.
/// </summary>
public sealed class NewBehaviourDialog : Form
{
    private static readonly Regex _identifierRegex = new(
        @"^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly TextBox           _classNameBox;
    private readonly ComboBox          _namespaceBox;
    private readonly TextBox           _subfolderBox;
    private readonly Button            _browseFolderButton;
    private readonly CheckedListBox    _methodsList;
    private readonly Button            _createButton;
    private readonly Button            _cancelButton;
    private readonly Label             _validationLabel;

    private readonly string _gameSourcePath;
    private readonly string _projectRootPath;

    /// <summary>The validated class name entered by the user.</summary>
    public string ClassName => _classNameBox.Text.Trim();

    /// <summary>The namespace selected or typed by the user.</summary>
    public string NamespaceName => _namespaceBox.Text.Trim();

    /// <summary>Relative subfolder (relative to GameSourcePath).</summary>
    public string RelativeFolder => _subfolderBox.Text.Trim();

    /// <summary>Lifecycle methods chosen to override.</summary>
    public IReadOnlyList<string> SelectedMethods
    {
        get
        {
            List<string> result = [];
            for (int i = 0; i < _methodsList.CheckedItems.Count; i++)
                result.Add(_methodsList.CheckedItems[i]!.ToString()!);
            return result;
        }
    }

    /// <summary>Creates the dialog, optionally pre-populating known namespaces.</summary>
    public NewBehaviourDialog(string gameSourcePath = "", string projectRootPath = "",
        string defaultNamespace = "", IEnumerable<string>? knownNamespaces = null)
    {
        _gameSourcePath = gameSourcePath;
        _projectRootPath = projectRootPath;

        Text            = "Create New Behaviour";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        ClientSize      = new System.Drawing.Size(420, 340);
        MaximizeBox     = false;
        MinimizeBox     = false;
        Font            = new System.Drawing.Font("Segoe UI", 9f);

        // Layout
        TableLayoutPanel layout = new()
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 7,
            Padding     = new Padding(8),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (int i = 0; i < 5; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));      // methods list
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f)); // buttons

        // Row 0 — Class name
        layout.Controls.Add(MakeLabel("Class name:"), 0, 0);
        _classNameBox = new TextBox { Dock = DockStyle.Fill };
        _classNameBox.TextChanged += (_, _) => UpdateValidation();
        layout.Controls.Add(_classNameBox, 1, 0);

        // Row 1 — Namespace
        layout.Controls.Add(MakeLabel("Namespace:"), 0, 1);
        _namespaceBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown };
        if (knownNamespaces is not null)
        {
            foreach (string ns in knownNamespaces)
                _namespaceBox.Items.Add(ns);
        }
        if (!string.IsNullOrEmpty(defaultNamespace))
            _namespaceBox.Text = defaultNamespace;
        layout.Controls.Add(_namespaceBox, 1, 1);

        // Row 2 — Subfolder
        layout.Controls.Add(MakeLabel("Subfolder:"), 0, 2);
        Panel subfolderRow = new() { Dock = DockStyle.Fill };
        _subfolderBox = new TextBox
        {
            Dock    = DockStyle.Fill,
            Text    = "Behaviours",
        };
        _browseFolderButton = new Button
        {
            Text   = "...",
            Dock   = DockStyle.Right,
            Width  = 28,
        };
        _browseFolderButton.Click += OnBrowseFolder;
        subfolderRow.Controls.Add(_subfolderBox);
        subfolderRow.Controls.Add(_browseFolderButton);
        layout.Controls.Add(subfolderRow, 1, 2);

        // Row 3 — Validation label
        _validationLabel = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = System.Drawing.Color.OrangeRed,
            Text      = string.Empty,
        };
        layout.SetColumnSpan(_validationLabel, 2);
        layout.Controls.Add(_validationLabel, 0, 3);

        // Row 4 — Methods header
        Label methodsLabel = MakeLabel("Override:");
        layout.Controls.Add(methodsLabel, 0, 4);

        // Row 5 — Methods list
        _methodsList = new CheckedListBox
        {
            Dock         = DockStyle.Fill,
            CheckOnClick = true,
        };
        foreach (string m in new[] { "Awake", "Start", "Update", "Draw", "OnDestroy" })
            _methodsList.Items.Add(m, false);
        layout.SetColumnSpan(_methodsList, 2);
        layout.Controls.Add(_methodsList, 0, 5);

        // Row 6 — Buttons
        FlowLayoutPanel buttons = new()
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding       = new Padding(0, 4, 0, 0),
        };
        _cancelButton = new Button
        {
            Text         = "Cancel",
            DialogResult = DialogResult.Cancel,
            Width        = 80,
        };
        _createButton = new Button
        {
            Text         = "Create",
            DialogResult = DialogResult.OK,
            Width        = 80,
            Enabled      = false,
        };
        _createButton.Click += (_, _) => DialogResult = DialogResult.OK;
        buttons.Controls.Add(_cancelButton);
        buttons.Controls.Add(_createButton);
        layout.Controls.Add(buttons, 0, 6);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = _createButton;
        CancelButton = _cancelButton;
    }

    private void OnBrowseFolder(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dlg = new()
        {
            Description            = "Select the subfolder for the new behaviour",
            UseDescriptionForTitle = true,
            InitialDirectory       = Directory.Exists(_gameSourcePath) ? _gameSourcePath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        // Validate the chosen folder is inside the project root.
        string root = string.IsNullOrEmpty(_projectRootPath) ? _gameSourcePath : _projectRootPath;
        if (!string.IsNullOrEmpty(root))
        {
            string fullChosen = Path.GetFullPath(dlg.SelectedPath);
            string fullRoot   = Path.GetFullPath(root);
            if (!fullChosen.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                _validationLabel.Text = "Folder must be inside the project directory.";
                return;
            }
        }

        string rel = string.IsNullOrEmpty(_gameSourcePath)
            ? dlg.SelectedPath
            : Path.GetRelativePath(_gameSourcePath, dlg.SelectedPath);
        _subfolderBox.Text = rel;
        if (_validationLabel.Text == "Folder must be inside the project directory.")
            _validationLabel.Text = string.Empty;
    }

    private void UpdateValidation()
    {
        string name = _classNameBox.Text.Trim();
        bool   valid = _identifierRegex.IsMatch(name);
        _createButton.Enabled   = valid;
        _validationLabel.Text   = valid || name.Length == 0
            ? string.Empty
            : "Class name must start with a letter and contain only letters, digits, or underscores.";
    }

    private static Label MakeLabel(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
    };
}
